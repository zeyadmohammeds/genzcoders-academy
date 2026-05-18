using GenZCoders.Models;

namespace GenZCoders.DTOs;

public record CourseRoomDto(
    Guid CourseId,
    Guid CourseRoundId,
    string CourseTitle,
    string RoundName,
    CourseAccessStatus AccessStatus,
    string? InstructorName,
    IReadOnlyList<CourseRoomWeekDto> Weeks,
    IReadOnlyList<CourseMaterialDto> Materials,
    IReadOnlyList<CourseTaskDto> Tasks,
    CourseProgressDto Progress,
    string? ZoomMeetingId,
    string? ZoomJoinUrl,
    string? ZoomMeetingPassword,
    int RoundStudentCount,
    int CourseStudentCount,
    IReadOnlyList<ClassmateDto> Classmates);

public record ClassmateDto(
    Guid UserId,
    string DisplayName,
    string? Email,
    int Level,
    int TotalXp);

public record CourseRoomWeekDto(
    Guid SessionInstanceId,
    int WeekNumber,
    string WeekTitle,
    SessionType SessionType,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    SessionStatus Status);

public record CourseMaterialDto(Guid Id, string Title, CourseMaterialType MaterialType, string Url, bool IsDownloadable);

public record CourseTaskDto(Guid Id, string Title, string Description, string TaskType, string SubmissionType, int MaxScore, int XpReward, bool IsRequired, string? Status, int? Score, string? Feedback);

public record CourseProgressDto(int TotalXp, int AttendanceCount, int SubmittedTasks, int CompletedQuizzes, decimal CompletionPercent);

public record LeaderboardEntryDto(Guid StudentUserId, string StudentName, int TotalXp, int Rank);
