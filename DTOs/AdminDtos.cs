namespace GenZCoders.DTOs;

public record CourseSessionCreateDto(
    string Title,
    string? Description,
    int SessionNumber,
    int DurationMinutes,
    string SessionType,
    int SortOrder);

public record CourseLessonCreateDto(
    string Title,
    string Summary,
    string ContentMarkdown,
    int WeekNumber,
    string SessionType,
    int SortOrder);

public record CourseMaterialCreateDto(
    string Title,
    string MaterialType,
    string Url,
    string? Description,
    bool IsDownloadable);

public record CourseQuestionCreateDto(
    string QuestionText,
    string QuestionType,
    string OptionsJson,
    string? CorrectAnswer,
    bool IsRequired,
    int SortOrder);

// Admin course management DTOs
public record AdminCourseDto(
    Guid Id,
    string Slug,
    string Title,
    string? Subtitle,
    string Description,
    string ShortDescription,
    string Outcome,
    int MinimumAge,
    int? MaximumAge,
    decimal PriceEgp,
    string? CoverImageUrl,
    string? ImageUrl,
    string? IconName,
    string? ColorHex,
    string SkillsTaughtJson,
    int Phase,
    int SortOrder,
    int CoreSessions,
    int SupportSessions,
    string Level,
    bool IsActive,
    bool IsFeatured,
    bool IsDeleted,
    IReadOnlyList<CourseModuleDto> Modules);

public record AdminCourseCreateRequest(
    string Slug,
    string Title,
    string? Subtitle,
    string Description,
    string ShortDescription,
    string Outcome,
    int MinimumAge,
    int? MaximumAge,
    decimal PriceEgp,
    string? CoverImageUrl,
    string? ImageUrl,
    string? IconName,
    string? ColorHex,
    string? SkillsTaughtJson,
    int Phase,
    int SortOrder,
    int CoreSessions,
    int SupportSessions,
    string Level,
    bool IsActive,
    bool IsFeatured,
    IReadOnlyList<CourseSessionCreateDto>? CourseSessions,
    IReadOnlyList<CourseLessonCreateDto>? Lessons,
    IReadOnlyList<CourseMaterialCreateDto>? Materials,
    IReadOnlyList<CourseQuestionCreateDto>? Questions,
    bool? IsDeleted = null);

public record AdminRoundCreateRequest(
    Guid CourseId,
    string Name,
    string Slug,
    DateOnly StartDate,
    int MaxStudents,
    bool IsEnrollmentOpen,
    bool AutoAcceptPaidApplications,
    bool RequireEngineerApproval);

public record UpdateRoundWeekRequest(
    string? WeekTitle,
    string? SessionType,
    string? Status,
    string? SessionLink,
    string? RecordingUrl,
    int WeekNumber);

public record AdminBroadcastRequest(
    string Title,
    string Body,
    string Audience = "all");

public record AdminDashboardDto(
    int TotalCourses,
    int ActiveCourses,
    int TotalSchools,
    int PartnerSchools,
    int TotalEnrollments,
    int PaidOrders,
    decimal RevenueEgp,
    int UpcomingSessions,
    int PendingSubmissions,
    int OpenStudentQuestions,
    IReadOnlyList<CourseDemandDto> CourseDemand,
    IReadOnlyList<AtRiskStudentDto> AtRiskStudents);

public record CourseDemandDto(Guid CourseId, string CourseTitle, int EnrollmentCount, decimal RevenueEgp);
public record AtRiskStudentDto(Guid StudentUserId, string Name, string Email, int MissedSessions, int MissingTasks);
