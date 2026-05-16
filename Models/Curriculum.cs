using GenZCoders.Models.Identity;

namespace GenZCoders.Models;

public class CourseSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public int SessionNumber { get; set; }
    public SessionType SessionType { get; set; } = SessionType.Core;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string? Principle { get; set; }
    public int DurationMinutes { get; set; } = 90;
    public string MaterialsJson { get; set; } = "[]";
    public int SortOrder { get; set; }
}

public class Cohort
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? EngineerUserId { get; set; }
    public ApplicationUser? EngineerUser { get; set; }
    public Guid? CtaUserId { get; set; }
    public ApplicationUser? CtaUser { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int MaxStudents { get; set; } = 20;
    public int CurrentStudents { get; set; }
    public CourseRoundMode Mode { get; set; } = CourseRoundMode.Online;
    public bool AutoAcceptPaidApplications { get; set; }
    public bool RequireEngineerApproval { get; set; } = true;
    public bool IsEnrollmentOpen { get; set; } = true;
    public string? SessionLink { get; set; }
    public string? ZoomMeetingId { get; set; }
    public string? ZoomStartUrl { get; set; }
    public string? ZoomJoinUrl { get; set; }
    public CohortStatus Status { get; set; } = CohortStatus.Upcoming;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<CohortEnrollment> CohortEnrollments { get; set; } = [];
    public ICollection<SessionInstance> SessionInstances { get; set; } = [];
    public ICollection<CourseLesson> Lessons { get; set; } = [];
    public ICollection<CourseMaterial> Materials { get; set; } = [];
}

public class CohortEnrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CohortId { get; set; }
    public Cohort? Cohort { get; set; }
    public Guid StudentUserId { get; set; }
    public ApplicationUser? StudentUser { get; set; }
    public Guid? EnrollmentId { get; set; }
    public Enrollment? Enrollment { get; set; }
    public DateTimeOffset EnrolledAt { get; set; } = DateTimeOffset.UtcNow;
}

public class SessionInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CohortId { get; set; }
    public Cohort? Cohort { get; set; }
    public Guid CourseSessionId { get; set; }
    public CourseSession? CourseSession { get; set; }
    public int WeekNumber { get; set; } = 1;
    public string WeekTitle { get; set; } = string.Empty;
    public SessionType SessionType { get; set; } = SessionType.Core;
    public DateTimeOffset ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 90;
    public string? SessionLink { get; set; }
    public string? RecordingUrl { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = [];
}

public class AttendanceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionInstanceId { get; set; }
    public SessionInstance? SessionInstance { get; set; }
    public Guid StudentUserId { get; set; }
    public ApplicationUser? StudentUser { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public DateTimeOffset? JoinedAt { get; set; }
    public DateTimeOffset? LeftAt { get; set; }
    public int XpEarned { get; set; }
    public Guid? MarkedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class CourseLesson
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid? CohortId { get; set; }
    public Cohort? Cohort { get; set; }
    public Guid? CourseSessionId { get; set; }
    public CourseSession? CourseSession { get; set; }
    public int WeekNumber { get; set; } = 1;
    public SessionType SessionType { get; set; } = SessionType.Core;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<CourseMaterial> Materials { get; set; } = [];
}

public class CourseMaterial
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid? CohortId { get; set; }
    public Cohort? Cohort { get; set; }
    public Guid? CourseLessonId { get; set; }
    public CourseLesson? CourseLesson { get; set; }
    public CourseMaterialType MaterialType { get; set; } = CourseMaterialType.Pdf;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDownloadable { get; set; } = true;
    public bool IsPublished { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
