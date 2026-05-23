using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Models;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Services;

public class LearningService(AcademyDbContext db, INotificationService notifications) : ILearningService
{
    public async Task<Guid> AddLessonAsync(LessonCreateRequest request, Guid? createdByUserId, CancellationToken cancellationToken = default)
    {
        var lesson = new CourseLesson
        {
            CourseId = request.CourseId,
            CohortId = request.CourseRoundId,
            CourseSessionId = request.CourseSessionId,
            WeekNumber = request.WeekNumber,
            SessionType = request.SessionType,
            Title = request.Title,
            Summary = request.Summary,
            ContentMarkdown = request.ContentMarkdown,
            SortOrder = request.SortOrder,
            IsPublished = request.IsPublished,
            CreatedByUserId = createdByUserId
        };
        db.CourseLessons.Add(lesson);
        await db.SaveChangesAsync(cancellationToken);
        return lesson.Id;
    }

    public async Task<Guid> AddMaterialAsync(MaterialCreateRequest request, Guid? createdByUserId, CancellationToken cancellationToken = default)
    {
        var material = new CourseMaterial
        {
            CourseId = request.CourseId,
            CohortId = request.CourseRoundId,
            CourseLessonId = request.CourseLessonId,
            MaterialType = request.MaterialType,
            Title = request.Title,
            Url = request.Url,
            Description = request.Description,
            IsDownloadable = request.IsDownloadable,
            IsPublished = request.IsPublished,
            CreatedByUserId = createdByUserId
        };
        db.CourseMaterials.Add(material);
        await db.SaveChangesAsync(cancellationToken);
        return material.Id;
    }

    public async Task<Guid> CreateTaskAsync(TaskCreateRequest request, Guid? createdByUserId, CancellationToken cancellationToken = default)
    {
        var task = new LearningTask
        {
            CourseSessionId = request.CourseSessionId,
            CohortId = request.CourseRoundId,
            Title = request.Title,
            Description = request.Description,
            Instructions = request.Instructions,
            TaskType = request.TaskType,
            SubmissionType = request.SubmissionType,
            MaxScore = request.MaxScore,
            XpReward = request.XpReward,
            DueHoursAfterSession = request.DueHoursAfterSession,
            RubricJson = request.RubricJson,
            CreatedByUserId = createdByUserId
        };
        db.LearningTasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);
        return task.Id;
    }

    public async Task<Guid> SubmitTaskAsync(TaskSubmitRequest request, CancellationToken cancellationToken = default)
    {
        var task = await db.LearningTasks.FirstOrDefaultAsync(x => x.Id == request.LearningTaskId, cancellationToken)
            ?? throw new InvalidOperationException("Task not found.");

        var submission = new TaskSubmission
        {
            LearningTaskId = request.LearningTaskId,
            StudentUserId = request.StudentUserId,
            SubmissionUrl = request.SubmissionUrl,
            RepositoryUrl = request.RepositoryUrl,
            SubmissionText = request.SubmissionText,
            IsLate = DateTimeOffset.UtcNow > task.CreatedAt.AddHours(task.DueHoursAfterSession)
        };

        db.TaskSubmissions.Add(submission);
        await db.SaveChangesAsync(cancellationToken);
        await notifications.QueueAsync(request.StudentUserId, "Task submitted", "Your task was submitted successfully.", [NotificationChannel.InApp, NotificationChannel.Email], cancellationToken);
        return submission.Id;
    }

    public async Task GradeSubmissionAsync(Guid submissionId, Guid graderUserId, GradeSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var submission = await db.TaskSubmissions.Include(x => x.LearningTask).FirstOrDefaultAsync(x => x.Id == submissionId, cancellationToken)
            ?? throw new InvalidOperationException("Submission not found.");

        submission.Score = request.Score;
        submission.Feedback = request.Feedback;
        submission.RubricScoresJson = request.RubricScoresJson;
        submission.GradedByUserId = graderUserId;
        submission.GradedAt = DateTimeOffset.UtcNow;
        submission.Status = SubmissionStatus.Graded;
        submission.XpAwarded = submission.LearningTask?.XpReward ?? 0;

        await AwardXpAsync(submission.StudentUserId, submission.XpAwarded, XpSourceType.Task, submission.Id, $"Task graded: {submission.LearningTask?.Title}", cancellationToken);
        await notifications.QueueAsync(submission.StudentUserId, "Task graded", $"Your task was graded. Score: {request.Score}.", [NotificationChannel.InApp, NotificationChannel.Email, NotificationChannel.WhatsApp], cancellationToken);
    }

    public async Task MarkAttendanceAsync(AttendanceMarkRequest request, Guid? markedByUserId, CancellationToken cancellationToken = default)
    {
        var attendance = await db.AttendanceRecords.FirstOrDefaultAsync(x => x.SessionInstanceId == request.SessionInstanceId && x.StudentUserId == request.StudentUserId, cancellationToken);
        if (attendance is null)
        {
            attendance = new AttendanceRecord
            {
                SessionInstanceId = request.SessionInstanceId,
                StudentUserId = request.StudentUserId
            };
            db.AttendanceRecords.Add(attendance);
        }

        attendance.Status = request.Status;
        attendance.MarkedByUserId = markedByUserId;
        attendance.XpEarned = request.Status == AttendanceStatus.Present ? request.XpEarned : 0;
        attendance.JoinedAt ??= request.Status == AttendanceStatus.Present ? DateTimeOffset.UtcNow : null;

        if (attendance.XpEarned > 0)
        {
            await AwardXpAsync(request.StudentUserId, attendance.XpEarned, XpSourceType.Attendance, attendance.Id, "Session attendance XP", cancellationToken);
        }

        await notifications.QueueAsync(request.StudentUserId, "Attendance updated", $"Attendance status: {request.Status}. XP earned: {attendance.XpEarned}.", [NotificationChannel.InApp], cancellationToken);
    }

    private async Task AwardXpAsync(Guid studentUserId, int amount, XpSourceType sourceType, Guid sourceId, string description, CancellationToken cancellationToken)
    {
        db.XpTransactions.Add(new XpTransaction
        {
            StudentUserId = studentUserId,
            Amount = amount,
            SourceType = sourceType,
            SourceId = sourceId,
            Description = description
        });

        var profile = await db.StudentProfiles.FirstOrDefaultAsync(x => x.UserId == studentUserId, cancellationToken);
        if (profile is not null)
        {
            profile.TotalXp += amount;
            profile.LastActiveAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CourseRoomDto> GetRoomAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default)
    {
        var course = await db.Courses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == courseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        var enrollment = await db.Enrollments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CourseId == courseId && x.StudentUserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("You are not enrolled in this course.");

        var lessons = await db.CourseLessons
            .AsNoTracking()
            .Where(x => x.CourseId == courseId && x.IsPublished)
            .OrderBy(x => x.WeekNumber)
            .ThenBy(x => x.SortOrder)
            .Select(x => new CourseRoomWeekDto(x.Id, x.WeekNumber, x.Title, x.SessionType, DateTimeOffset.UtcNow, 90, SessionStatus.Scheduled)) // Simple mapping for now
            .ToListAsync(cancellationToken);

        var materials = await db.CourseMaterials
            .AsNoTracking()
            .Where(x => x.CourseId == courseId && x.IsPublished)
            .Select(x => new CourseMaterialDto(x.Id, x.Title, x.MaterialType, x.Url, x.IsDownloadable))
            .ToListAsync(cancellationToken);

        var profile = await db.StudentProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        
        var attendanceCount = await db.AttendanceRecords.CountAsync(x => x.StudentUserId == userId && x.Status == AttendanceStatus.Present, cancellationToken);
        var tasksCount = await db.TaskSubmissions.CountAsync(x => x.StudentUserId == userId, cancellationToken);

        // Fetch round classmate statistics and list
        var roundStudentCount = enrollment.CohortId.HasValue 
            ? await db.CohortEnrollments.CountAsync(x => x.CohortId == enrollment.CohortId.Value, cancellationToken) 
            : 0;

        var courseStudentCount = await db.Enrollments.CountAsync(x => x.CourseId == courseId, cancellationToken);

        var classmates = enrollment.CohortId.HasValue 
            ? await db.CohortEnrollments
                .AsNoTracking()
                .Where(x => x.CohortId == enrollment.CohortId.Value)
                .Join(db.Users, ce => ce.StudentUserId, u => u.Id, (ce, u) => new { ce, u })
                .Join(db.StudentProfiles, combined => combined.u.Id, sp => sp.UserId, (combined, sp) => new ClassmateDto(
                    combined.u.Id,
                    combined.u.DisplayName ?? combined.u.UserName ?? "Student",
                    combined.u.Email,
                    sp.Level,
                    sp.TotalXp
                ))
                .ToListAsync(cancellationToken)
            : new List<ClassmateDto>();

        var dbTasks = await db.LearningTasks.AsNoTracking()
            .Where(x => x.CohortId == enrollment.CohortId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                TaskType = x.TaskType.ToString(),
                SubmissionType = x.SubmissionType.ToString(),
                x.MaxScore,
                x.XpReward,
                x.IsRequired,
                x.DueHoursAfterSession,
                Status = x.Submissions.Where(s => s.StudentUserId == userId).Select(s => s.Status.ToString()).FirstOrDefault(),
                Score = x.Submissions.Where(s => s.StudentUserId == userId).Select(s => (int?)s.Score).FirstOrDefault(),
                Feedback = x.Submissions.Where(s => s.StudentUserId == userId).Select(s => s.Feedback).FirstOrDefault(),
                SessionScheduledAt = db.SessionInstances
                    .Where(s => s.CohortId == enrollment.CohortId && s.CourseSessionId == x.CourseSessionId)
                    .Select(s => (DateTimeOffset?)s.ScheduledAt)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var tasks = dbTasks.Select(x => new CourseTaskDto(
            x.Id, x.Title, x.Description,
            x.TaskType, x.SubmissionType,
            x.MaxScore, x.XpReward, x.IsRequired,
            x.Status, x.Score, x.Feedback,
            x.SessionScheduledAt?.AddHours(x.DueHoursAfterSession) ?? x.SessionScheduledAt
        )).ToList();

        var quizList = enrollment.CohortId.HasValue
            ? await db.Quizzes.AsNoTracking()
                .Where(x => x.CohortId == enrollment.CohortId.Value && x.IsPublished)
                .Select(x => new QuizItemDto(
                    x.Id,
                    x.CourseSessionId,
                    x.CohortId,
                    x.Title,
                    x.QuizType.ToString(),
                    x.TimeLimitMinutes,
                    x.MaxAttempts,
                    x.PassScore,
                    x.XpReward,
                    x.IsPublished,
                    x.Questions.Count
                ))
                .ToListAsync(cancellationToken)
            : new List<QuizItemDto>();

        return new CourseRoomDto(
            course.Id,
            enrollment.CohortId ?? Guid.Empty,
            course.Title,
            "Active Cohort",
            CourseAccessStatus.Open,
            "Academy Instructor",
            "Senior Instructor with extensive industry and teaching experience.",
            "https://api.dicebear.com/7.x/notionists/svg?seed=Instructor&backgroundColor=f0f0f0",
            lessons,
            materials,
            tasks,
            quizList,
            new CourseProgressDto(profile?.TotalXp ?? 0, attendanceCount, tasksCount, 0, 0),
            null, null, null,
            roundStudentCount,
            courseStudentCount,
            classmates);
    }

    public async Task<StudentProgressDto> GetProgressAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await db.StudentProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        var xpTotal = profile?.TotalXp ?? 0;
        
        var weekStart = DateTimeOffset.UtcNow.AddDays(-(int)DateTimeOffset.UtcNow.DayOfWeek);
        var xpThisWeek = await db.XpTransactions
            .Where(x => x.StudentUserId == userId && x.CreatedAt >= weekStart)
            .SumAsync(x => (int?)x.Amount, cancellationToken) ?? 0;

        var allStudents = await db.StudentProfiles.OrderByDescending(x => x.TotalXp).ToListAsync(cancellationToken);
        var rank = allStudents.FindIndex(x => x.UserId == userId) + 1;
        if (rank == 0) rank = allStudents.Count + 1;

        var totalSessions = await db.SessionInstances
            .Include(x => x.Cohort)
            .Where(x => x.Cohort!.CohortEnrollments.Any(e => e.StudentUserId == userId))
            .CountAsync(cancellationToken);
        var attendanceCount = await db.AttendanceRecords.CountAsync(x => x.StudentUserId == userId && x.Status == AttendanceStatus.Present, cancellationToken);
        var submittedTasks = await db.TaskSubmissions.CountAsync(x => x.StudentUserId == userId && x.Status == SubmissionStatus.Graded, cancellationToken);
        var completedQuizzes = await db.QuizAttempts.CountAsync(x => x.StudentUserId == userId && x.SubmittedAt != null, cancellationToken);

        var progressPercent = totalSessions > 0 ? decimal.Round(attendanceCount * 100m / totalSessions, 0) : 0;

        var badges = await db.XpTransactions
            .Where(x => x.StudentUserId == userId && x.SourceType == XpSourceType.Badge)
            .Select(x => new BadgeDto(x.SourceId.ToString(), x.Description, x.Description, "Trophy", x.CreatedAt))
            .Take(10)
            .ToListAsync(cancellationToken);

        return new StudentProgressDto(xpTotal, xpThisWeek, rank, 0, 0, attendanceCount, submittedTasks, completedQuizzes, badges);
    }

    public async Task<IReadOnlyList<StudentEnrollmentDto>> GetMyEnrollmentsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var enrollments = await db.Enrollments
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.Cohort).ThenInclude(x => x!.SessionInstances.OrderBy(s => s.ScheduledAt).Take(1))
            .Where(x => x.StudentUserId == userId)
            .ToListAsync(cancellationToken);

        return enrollments.Select(e =>
        {
            var nextSession = e.Cohort?.SessionInstances.FirstOrDefault();
            var progressPercent = 0m;
            if (e.CohortId.HasValue)
            {
                var total = db.SessionInstances.Count(x => x.CohortId == e.CohortId);
                var attended = db.AttendanceRecords.Count(x => x.StudentUserId == userId && x.SessionInstance!.CohortId == e.CohortId && x.Status == AttendanceStatus.Present);
                progressPercent = total > 0 ? (attended * 100m / total) : 0;
            }
            return new StudentEnrollmentDto(
                e.Id,
                e.CourseId,
                e.Course?.Title ?? "",
                e.Course?.Slug ?? "",
                e.CohortId ?? Guid.Empty,
                e.Cohort?.Name ?? "",
                e.EnrollmentStatus.ToString(),
                e.CreatedAt,
                progressPercent,
                nextSession?.WeekTitle,
                nextSession?.ScheduledAt);
        }).ToList();
    }

    public async Task<IReadOnlyList<CourseMaterialDto2>> GetMyMaterialsAsync(Guid userId, Guid? courseId, CancellationToken cancellationToken = default)
    {
        var cohortIds = await db.Enrollments.Where(x => x.StudentUserId == userId).Select(x => x.CohortId).ToListAsync(cancellationToken);
        
        var materials = await db.CourseMaterials
            .AsNoTracking()
            .Where(x => x.IsPublished && (x.CohortId == null || cohortIds.Contains(x.CohortId.Value)))
            .Where(x => courseId == null || x.CourseId == courseId)
            .OrderBy(x => x.Title)
            .Select(x => new CourseMaterialDto2(x.Id, x.Title, x.MaterialType.ToString(), x.Url ?? "", x.IsDownloadable, null, false))
            .ToListAsync(cancellationToken);

        return materials;
    }

    public async Task<IReadOnlyList<StudentCertificateDto>> GetMyCertificatesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var certs = await db.Enrollments
            .AsNoTracking()
            .Include(x => x.Course)
            .Where(x => x.StudentUserId == userId && x.EnrollmentStatus == EnrollmentStatus.Completed)
            .ToListAsync(cancellationToken);

        return certs.Select(e => new StudentCertificateDto(
            e.Id,
            e.Course?.Title ?? "Course",
            $"CERT-{e.Id:N}",
            e.CreatedAt,
            $"/verify/{e.Id}"
        )).ToList();
    }

    public async Task<IReadOnlyList<StudentSessionDto>> GetMySessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        
        var sessionInstances = await db.SessionInstances
            .AsNoTracking()
            .Include(x => x.Cohort).ThenInclude(x => x!.Course)
            .Include(x => x.Cohort).ThenInclude(x => x!.EngineerUser)
            .Where(x => x.Cohort!.CohortEnrollments.Any(e => e.StudentUserId == userId))
            .OrderBy(x => x.ScheduledAt)
            .ToListAsync(cancellationToken);

        return sessionInstances.Select(s => new StudentSessionDto(
            s.Id,
            s.WeekTitle,
            s.ScheduledAt,
            s.DurationMinutes,
            s.SessionType.ToString(),
            s.Status.ToString(),
            s.Cohort?.EngineerUser?.DisplayName,
            s.RecordingUrl,
            s.Cohort?.ZoomJoinUrl,
            s.ScheduledAt > now,
            s.Cohort?.CourseId,
            s.Cohort?.Course?.Title,
            s.Cohort?.Course?.Slug,
            s.CohortId
        )).ToList();
    }

    public async Task<IReadOnlyList<CourseTaskDto>> GetMyTasksAsync(IReadOnlyList<Guid> cohortIds, Guid userId, CancellationToken cancellationToken = default)
    {
        if (cohortIds.Count == 0) return [];

        var dbTasks = await db.LearningTasks.AsNoTracking()
            .Where(x => x.CohortId != null && cohortIds.Contains(x.CohortId.Value))
            .OrderBy(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                TaskType = x.TaskType.ToString(),
                SubmissionType = x.SubmissionType.ToString(),
                x.MaxScore,
                x.XpReward,
                x.IsRequired,
                x.DueHoursAfterSession,
                x.CohortId,
                x.CourseSessionId,
                Status = x.Submissions.Where(s => s.StudentUserId == userId).Select(s => s.Status.ToString()).FirstOrDefault(),
                Score = x.Submissions.Where(s => s.StudentUserId == userId).Select(s => (int?)s.Score).FirstOrDefault(),
                Feedback = x.Submissions.Where(s => s.StudentUserId == userId).Select(s => s.Feedback).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var tasks = new List<CourseTaskDto>();
        foreach (var x in dbTasks)
        {
            var scheduledAt = await db.SessionInstances.AsNoTracking()
                .Where(s => s.CohortId == x.CohortId && s.CourseSessionId == x.CourseSessionId)
                .Select(s => (DateTimeOffset?)s.ScheduledAt)
                .FirstOrDefaultAsync(cancellationToken);
                
            tasks.Add(new CourseTaskDto(
                x.Id, x.Title, x.Description,
                x.TaskType, x.SubmissionType,
                x.MaxScore, x.XpReward, x.IsRequired,
                x.Status, x.Score, x.Feedback,
                scheduledAt?.AddHours(x.DueHoursAfterSession) ?? scheduledAt
            ));
        }
        return tasks;
    }
}
