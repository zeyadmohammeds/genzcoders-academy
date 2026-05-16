using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Hubs;
using GenZCoders.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Services;

public class NotificationService(AcademyDbContext db, IHubContext<NotificationHub> hubContext) : INotificationService
{
    public async Task QueueAsync(Guid userId, string subject, string body, IEnumerable<NotificationChannel> channels, CancellationToken cancellationToken = default)
    {
        var settings = await db.UserNotificationSettings.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        var created = new List<NotificationMessage>();

        foreach (var channel in channels.Distinct())
        {
            if (!IsEnabled(channel, settings)) continue;

            var msg = new NotificationMessage
            {
                RecipientUserId = userId,
                Channel = channel,
                Subject = subject,
                Body = body,
                Destination = channel switch
                {
                    NotificationChannel.Email => settings?.EmailOverride ?? user?.Email,
                    NotificationChannel.WhatsApp => settings?.WhatsAppNumber ?? user?.PhoneNumber,
                    NotificationChannel.Sms => user?.PhoneNumber,
                    _ => null
                }
            };
            db.NotificationMessages.Add(msg);
            created.Add(msg);
        }

        await db.SaveChangesAsync(cancellationToken);

        // Broadcast in-app notifications in real-time
        var inApp = created.Where(n => n.Channel == NotificationChannel.InApp).ToList();
        foreach (var notification in inApp)
        {
            await hubContext.Clients.Group($"user_{userId}").SendAsync("NewNotification", new
            {
                id = notification.Id.ToString(),
                subject = notification.Subject,
                body = notification.Body,
                channel = notification.Channel.ToString(),
                status = notification.Status.ToString(),
                createdAt = notification.CreatedAt.ToString("o")
            }, cancellationToken);

            var unreadCount = await db.NotificationMessages
                .CountAsync(x => x.RecipientUserId == userId && x.Status != NotificationStatus.Read, cancellationToken);
            await hubContext.Clients.Group($"user_{userId}").SendAsync("UnreadCount", unreadCount, cancellationToken);
        }
    }

    public async Task UpdateSettingsAsync(Guid userId, NotificationSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var settings = await db.UserNotificationSettings.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (settings is null)
        {
            settings = new UserNotificationSetting { UserId = userId };
            db.UserNotificationSettings.Add(settings);
        }

        settings.InAppEnabled = request.InAppEnabled;
        settings.EmailEnabled = request.EmailEnabled;
        settings.WhatsAppEnabled = request.WhatsAppEnabled;
        settings.SmsEnabled = request.SmsEnabled;
        settings.WhatsAppNumber = request.WhatsAppNumber;
        settings.EmailOverride = request.EmailOverride;
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<NotificationMessage>> ListByUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await db.NotificationMessages
            .Where(x => x.RecipientUserId == userId && x.Channel == NotificationChannel.InApp)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToListAsync(ct);
    }

    public async Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var msg = await db.NotificationMessages.FirstOrDefaultAsync(x => x.Id == notificationId && x.RecipientUserId == userId, ct);
        if (msg is not null)
        {
            msg.Status = NotificationStatus.Read;
            msg.ReadAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            var unreadCount = await db.NotificationMessages
                .CountAsync(x => x.RecipientUserId == userId && x.Status != NotificationStatus.Read, ct);
            await hubContext.Clients.Group($"user_{userId}").SendAsync("UnreadCount", unreadCount, ct);
        }
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        var unread = await db.NotificationMessages
            .Where(x => x.RecipientUserId == userId && x.Status != NotificationStatus.Read)
            .ToListAsync(ct);

        foreach (var msg in unread)
        {
            msg.Status = NotificationStatus.Read;
            msg.ReadAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        await hubContext.Clients.Group($"user_{userId}").SendAsync("UnreadCount", 0, ct);
    }

    public async Task<NotificationSettingsDto> GetSettingsAsync(Guid userId, CancellationToken ct = default)
    {
        var settings = await db.UserNotificationSettings.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
        return new NotificationSettingsDto(
            InAppEnabled: settings?.InAppEnabled ?? true,
            EmailEnabled: settings?.EmailEnabled ?? false,
            WhatsAppEnabled: settings?.WhatsAppEnabled ?? false,
            SmsEnabled: settings?.SmsEnabled ?? false,
            WhatsAppNumber: settings?.WhatsAppNumber,
            EmailOverride: settings?.EmailOverride
        );
    }

    private static bool IsEnabled(NotificationChannel channel, UserNotificationSetting? settings) => channel switch
    {
        NotificationChannel.InApp => true,
        NotificationChannel.Email => settings?.EmailEnabled ?? false,
        NotificationChannel.WhatsApp => settings?.WhatsAppEnabled ?? false,
        NotificationChannel.Sms => settings?.SmsEnabled ?? false,
        _ => false
    };
}
