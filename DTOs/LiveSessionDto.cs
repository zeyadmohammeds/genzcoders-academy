namespace GenZCoders.DTOs;

public record LiveSessionDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string Title,
    DateTimeOffset StartsAt,
    int DurationMinutes,
    string HostName,
    string ZoomMeetingId,
    string ZoomJoinUrl,
    bool EmbedEnabled);
