using GenZCoders.DTOs;
using GenZCoders.Models;

namespace GenZCoders.Services;

public interface INotificationService
{
    Task QueueAsync(Guid userId, string subject, string body, IEnumerable<NotificationChannel> channels, CancellationToken cancellationToken = default);
    Task UpdateSettingsAsync(Guid userId, NotificationSettingsRequest request, CancellationToken cancellationToken = default);
    Task<List<NotificationMessage>> ListByUserAsync(Guid userId, CancellationToken ct = default);
    Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default);
    Task<NotificationSettingsDto> GetSettingsAsync(Guid userId, CancellationToken ct = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken ct = default);
}
