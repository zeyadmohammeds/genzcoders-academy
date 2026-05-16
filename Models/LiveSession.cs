namespace GenZCoders.Models;

public class LiveSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid? CohortId { get; set; }
    public Cohort? Cohort { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public int DurationMinutes { get; set; } = 90;
    public string HostName { get; set; } = string.Empty;
    public Guid? HostUserId { get; set; }
    public string ZoomMeetingId { get; set; } = string.Empty;
    public string? ZoomMeetingPassword { get; set; }
    public string ZoomJoinUrl { get; set; } = string.Empty;
    public string? RecordingUrl { get; set; }
    public string? ZoomSdkSignatureEndpoint { get; set; }
    public bool EmbedEnabled { get; set; } = true;
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
