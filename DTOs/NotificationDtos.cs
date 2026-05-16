using GenZCoders.Models;

namespace GenZCoders.DTOs;

public record NotificationSettingsRequest(bool InAppEnabled, bool EmailEnabled, bool WhatsAppEnabled, bool SmsEnabled, string? WhatsAppNumber, string? EmailOverride);

public record NotificationSettingsDto(bool InAppEnabled, bool EmailEnabled, bool WhatsAppEnabled, bool SmsEnabled, string? WhatsAppNumber, string? EmailOverride);

public record SendNotificationRequest(Guid RecipientUserId, string TemplateKey, string Subject, string Body, IReadOnlyList<NotificationChannel> Channels);
