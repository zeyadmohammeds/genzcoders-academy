using GenZCoders.Models;

namespace GenZCoders.DTOs;

public record LessonCreateRequest(
    Guid CourseId,
    Guid? CourseRoundId,
    Guid? CourseSessionId,
    int WeekNumber,
    SessionType SessionType,
    string Title,
    string Summary,
    string ContentMarkdown,
    int SortOrder,
    bool IsPublished);

public record MaterialCreateRequest(
    Guid CourseId,
    Guid? CourseRoundId,
    Guid? CourseLessonId,
    CourseMaterialType MaterialType,
    string Title,
    string Url,
    string? Description,
    bool IsDownloadable,
    bool IsPublished);

public record TaskCreateRequest(
    Guid? CourseSessionId,
    Guid? CourseRoundId,
    string Title,
    string Description,
    string Instructions,
    TaskType TaskType,
    SubmissionType SubmissionType,
    int MaxScore,
    int XpReward,
    int DueHoursAfterSession,
    string RubricJson);

public record TaskSubmitRequest(Guid LearningTaskId, Guid StudentUserId, string? SubmissionUrl, string? RepositoryUrl, string? SubmissionText);

public record GradeSubmissionRequest(int Score, string Feedback, string RubricScoresJson);

public record AttendanceMarkRequest(Guid SessionInstanceId, Guid StudentUserId, AttendanceStatus Status, int XpEarned);

public record StudentProgressDto(
    int XpTotal,
    int XpThisWeek,
    int RankGlobal,
    int StreakCurrent,
    int StreakLongest,
    int AttendanceCount,
    int SubmittedTasks,
    int CompletedQuizzes,
    IReadOnlyList<BadgeDto> Badges);

public record BadgeDto(string Id, string Name, string Description, string IconName, DateTimeOffset EarnedAt);

public record StudentEnrollmentDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string CourseSlug,
    Guid CohortId,
    string CohortName,
    string Status,
    DateTimeOffset EnrolledAt,
    decimal ProgressPercent,
    string? CurrentSessionTitle,
    DateTimeOffset? NextSessionAt);

public record CourseMaterialDto2(
    Guid Id,
    string Title,
    string MaterialType,
    string Url,
    bool IsDownloadable,
    string? FolderPath,
    bool IsFolder);

public record StudentCertificateDto(
    Guid Id,
    string CourseTitle,
    string CertificateNumber,
    DateTimeOffset IssuedAt,
    string VerificationUrl);

public record StudentSessionDto(
    Guid Id,
    string Title,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    string SessionType,
    string Status,
    string? InstructorName,
    string? RecordingUrl,
    string? ZoomJoinUrl,
    bool IsUpcoming);
