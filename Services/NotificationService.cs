using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Hubs;
using GenZCoders.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Sentry;

namespace GenZCoders.Services;

public class NotificationService(
    AcademyDbContext db, 
    IHubContext<NotificationHub> hubContext,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : INotificationService
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

        // Dispatch external messages (Email, SMS, WhatsApp) in the background so it doesn't block the request
        foreach (var msg in created.Where(n => n.Channel != NotificationChannel.InApp))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = db.Database.BeginTransaction();
                    // Reload inside background thread to modify status
                    var activeMsg = await db.NotificationMessages.FindAsync(msg.Id);
                    if (activeMsg != null)
                    {
                        await DispatchMessageAsync(activeMsg);
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[Notification Background Dispatch Failed] msg ID: {msg.Id}, Error: {ex.Message}");
                    SentrySdk.CaptureException(ex);
                }
            }, cancellationToken);
        }

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

    private async Task DispatchMessageAsync(NotificationMessage msg)
    {
        try
        {
            if (msg.Channel == NotificationChannel.Email)
            {
                await SendEmailViaSmtpAsync(msg.Destination ?? string.Empty, msg.Subject, msg.Body);
                msg.Status = NotificationStatus.Sent;
                msg.SentAt = DateTimeOffset.UtcNow;
            }
            else if (msg.Channel == NotificationChannel.Sms)
            {
                await SendSmsViaTwilioAsync(msg.Destination ?? string.Empty, msg.Body);
                msg.Status = NotificationStatus.Sent;
                msg.SentAt = DateTimeOffset.UtcNow;
            }
            else if (msg.Channel == NotificationChannel.WhatsApp)
            {
                await SendWhatsAppViaTwilioAsync(msg.Destination ?? string.Empty, msg.Body);
                msg.Status = NotificationStatus.Sent;
                msg.SentAt = DateTimeOffset.UtcNow;
            }
        }
        catch (Exception ex)
        {
            msg.Status = NotificationStatus.Failed;
            System.Console.WriteLine($"[Notification Error] Failed to send {msg.Channel} to {msg.Destination}: {ex.Message}");
            SentrySdk.CaptureException(ex);
        }
    }

    private async Task SendEmailViaSmtpAsync(string to, string subject, string body)
    {
        var host = configuration["Email:Host"];
        var portStr = configuration["Email:Port"];
        var username = configuration["Email:Username"];
        var password = configuration["Email:Password"];
        var fromAddress = configuration["Email:FromAddress"];
        var fromName = configuration["Email:FromName"] ?? "ElSewedy Academy";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(to))
        {
            return;
        }

        // Support Resend custom API key if configured
        var resendKey = configuration["Resend:ApiKey"] ?? configuration["Authentication:Resend:ApiKey"];
        if (!string.IsNullOrWhiteSpace(resendKey))
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", resendKey);
            
            var payload = new
            {
                from = $"{fromName} <onboarding@resend.dev>",
                to = new[] { to },
                subject = subject,
                html = body
            };

            var response = await httpClient.PostAsJsonAsync("https://api.resend.com/emails", payload);
            response.EnsureSuccessStatusCode();
            return;
        }

        int port = int.TryParse(portStr, out var p) ? p : 587;

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        mailMessage.To.Add(to);

        await client.SendMailAsync(mailMessage);
    }

    private async Task SendSmsViaTwilioAsync(string to, string body)
    {
        var sid = configuration["Twilio:AccountSid"];
        var token = configuration["Twilio:AuthToken"];
        var from = configuration["Twilio:FromNumber"];

        if (string.IsNullOrWhiteSpace(sid) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(to))
        {
            return;
        }

        using var httpClient = httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{sid}:{token}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("To", to),
            new KeyValuePair<string, string>("From", from),
            new KeyValuePair<string, string>("Body", body)
        });

        var response = await httpClient.PostAsync($"https://api.twilio.com/2010-04-01/Accounts/{sid}/Messages.json", content);
        response.EnsureSuccessStatusCode();
    }

    private async Task SendWhatsAppViaTwilioAsync(string to, string body)
    {
        var sid = configuration["Twilio:AccountSid"];
        var token = configuration["Twilio:AuthToken"];
        var from = configuration["Twilio:FromNumber"];

        if (string.IsNullOrWhiteSpace(sid) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(to))
        {
            return;
        }

        using var httpClient = httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{sid}:{token}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        var whatsappTo = to.StartsWith("whatsapp:") ? to : $"whatsapp:{to}";
        var whatsappFrom = from.StartsWith("whatsapp:") ? from : $"whatsapp:{from}";

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("To", whatsappTo),
            new KeyValuePair<string, string>("From", whatsappFrom),
            new KeyValuePair<string, string>("Body", body)
        });

        var response = await httpClient.PostAsync($"https://api.twilio.com/2010-04-01/Accounts/{sid}/Messages.json", content);
        response.EnsureSuccessStatusCode();
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
