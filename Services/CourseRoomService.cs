using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Models;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Services;

public class CourseRoomService(AcademyDbContext db) : ICourseRoomService
{
    public async Task<CourseRoomDto> GetRoomAsync(Guid studentUserId, Guid courseRoundId, CancellationToken cancellationToken = default)
    {
        var round = await db.Cohorts
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.EngineerUser)
            .FirstOrDefaultAsync(x => x.Id == courseRoundId, cancellationToken)
            ?? throw new InvalidOperationException("Course round not found.");

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == studentUserId, cancellationToken);
        var isAdminStaff = user != null && (user.RoleKey == AcademyRole.AcademyAdmin || user.RoleKey == AcademyRole.Engineer || user.RoleKey == AcademyRole.Cta);

        var accepted = isAdminStaff || await db.CourseApplications.AnyAsync(x => x.CohortId == courseRoundId && x.StudentUserId == studentUserId && x.Status == ApplicationStatus.Accepted, cancellationToken);
        var enrolled = isAdminStaff || await db.CohortEnrollments.AnyAsync(x => x.CohortId == courseRoundId && x.StudentUserId == studentUserId, cancellationToken);
        var access = isAdminStaff || accepted || enrolled ? CourseAccessStatus.Open : CourseAccessStatus.PendingApproval;

        var weeks = access == CourseAccessStatus.Open
            ? await db.SessionInstances.AsNoTracking()
                .Where(x => x.CohortId == courseRoundId)
                .OrderBy(x => x.WeekNumber)
                .ThenBy(x => x.ScheduledAt)
                .Select(x => new CourseRoomWeekDto(x.Id, x.WeekNumber, x.WeekTitle, x.SessionType, x.ScheduledAt, x.DurationMinutes, x.Status))
                .ToListAsync(cancellationToken)
            : [];

        var materials = access == CourseAccessStatus.Open
            ? await db.CourseMaterials.AsNoTracking()
                .Where(x => x.CourseId == round.CourseId && (x.CohortId == null || x.CohortId == courseRoundId) && x.IsPublished)
                .Select(x => new CourseMaterialDto(x.Id, x.Title, x.MaterialType, x.Url, x.IsDownloadable))
                .ToListAsync(cancellationToken)
            : [];

        var xp = await db.XpTransactions.Where(x => x.StudentUserId == studentUserId).SumAsync(x => (int?)x.Amount, cancellationToken) ?? 0;
        var attendance = await db.AttendanceRecords.CountAsync(x => x.StudentUserId == studentUserId && x.Status == AttendanceStatus.Present && x.SessionInstance!.CohortId == courseRoundId, cancellationToken);
        var taskCount = await db.TaskSubmissions.CountAsync(x => x.StudentUserId == studentUserId && x.Status == SubmissionStatus.Graded, cancellationToken);
        var quizzesCount = await db.QuizAttempts.CountAsync(x => x.StudentUserId == studentUserId && x.SubmittedAt != null && x.Quiz!.CohortId == courseRoundId, cancellationToken);
        var totalWeeks = await db.SessionInstances.CountAsync(x => x.CohortId == courseRoundId, cancellationToken);
        var completion = totalWeeks == 0 ? 0 : decimal.Round(attendance * 100m / totalWeeks, 2);

        var dbTasks = access == CourseAccessStatus.Open
            ? await db.LearningTasks.AsNoTracking()
                .Where(x => x.CohortId == courseRoundId)
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
                    Status = x.Submissions.Where(s => s.StudentUserId == studentUserId).Select(s => s.Status.ToString()).FirstOrDefault(),
                    Score = x.Submissions.Where(s => s.StudentUserId == studentUserId).Select(s => (int?)s.Score).FirstOrDefault(),
                    Feedback = x.Submissions.Where(s => s.StudentUserId == studentUserId).Select(s => s.Feedback).FirstOrDefault(),
                    SessionScheduledAt = db.SessionInstances
                        .Where(s => s.CohortId == courseRoundId && s.CourseSessionId == x.CourseSessionId)
                        .Select(s => (DateTimeOffset?)s.ScheduledAt)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken)
            : new List<System.Dynamic.ExpandoObject>().Select(x => new { Id = Guid.Empty, Title = "", Description = "", TaskType = "", SubmissionType = "", MaxScore = 0, XpReward = 0, IsRequired = false, DueHoursAfterSession = 0, Status = (string?)null, Score = (int?)null, Feedback = (string?)null, SessionScheduledAt = (DateTimeOffset?)null }).ToList();

        var tasks = dbTasks.Select(x => new CourseTaskDto(
            x.Id, x.Title, x.Description,
            x.TaskType, x.SubmissionType,
            x.MaxScore, x.XpReward, x.IsRequired,
            x.Status, x.Score, x.Feedback,
            x.SessionScheduledAt?.AddHours(x.DueHoursAfterSession) ?? x.SessionScheduledAt
        )).ToList();

        var roundStudentCount = await db.CohortEnrollments.CountAsync(x => x.CohortId == courseRoundId, cancellationToken);
        var courseStudentCount = await db.CohortEnrollments.CountAsync(x => x.Cohort!.CourseId == round.CourseId, cancellationToken);

        var classmatesList = await db.CohortEnrollments
            .AsNoTracking()
            .Where(x => x.CohortId == courseRoundId)
            .Join(db.Users, ce => ce.StudentUserId, u => u.Id, (ce, u) => new { ce, u })
            .Join(db.StudentProfiles, combined => combined.u.Id, sp => sp.UserId, (combined, sp) => new ClassmateDto(
                combined.u.Id,
                (combined.u.FirstName + " " + combined.u.LastName).Trim(),
                combined.u.Email,
                sp.Level,
                sp.TotalXp
            ))
            .ToListAsync(cancellationToken);

        var instructorBio = round.EngineerUser?.Bio ?? "Senior Instructor with extensive industry and teaching experience.";
        var instructorAvatar = round.EngineerUser?.AvatarUrl ?? $"https://api.dicebear.com/7.x/notionists/svg?seed={round.EngineerUser?.FirstName ?? "Ahmed"}&backgroundColor=f0f0f0";

        var quizList = access == CourseAccessStatus.Open
            ? await db.Quizzes.AsNoTracking()
                .Where(x => x.CohortId == courseRoundId && x.IsPublished)
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
            round.CourseId,
            round.Id,
            round.Course?.Title ?? string.Empty,
            round.Name,
            access,
            round.EngineerUser?.DisplayName,
            instructorBio,
            instructorAvatar,
            weeks,
            materials,
            tasks,
            quizList,
            new CourseProgressDto(xp, attendance, taskCount, quizzesCount, completion),
            round.ZoomMeetingId,
            round.ZoomJoinUrl,
            null,
            roundStudentCount,
            courseStudentCount,
            classmatesList);
    }

    public async Task<IReadOnlyList<QuizItemDto>> GetQuizzesAsync(Guid studentUserId, Guid courseRoundId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == studentUserId, cancellationToken);
        var isAdminStaff = user != null && (user.RoleKey == AcademyRole.AcademyAdmin || user.RoleKey == AcademyRole.Engineer || user.RoleKey == AcademyRole.Cta);

        var accepted = isAdminStaff || await db.CourseApplications.AnyAsync(x => x.CohortId == courseRoundId && x.StudentUserId == studentUserId && x.Status == ApplicationStatus.Accepted, cancellationToken);
        var enrolled = isAdminStaff || await db.CohortEnrollments.AnyAsync(x => x.CohortId == courseRoundId && x.StudentUserId == studentUserId, cancellationToken);

        if (!isAdminStaff && !accepted && !enrolled)
        {
            return new List<QuizItemDto>();
        }

        return await db.Quizzes.AsNoTracking()
            .Where(x => x.CohortId == courseRoundId && x.IsPublished)
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
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> LeaderboardAsync(Guid? courseRoundId = null, Guid? courseId = null, CancellationToken cancellationToken = default)
    {
        var studentIds = db.CohortEnrollments.AsNoTracking()
            .Where(x => courseRoundId == null || x.CohortId == courseRoundId)
            .Where(x => courseId == null || x.Cohort!.CourseId == courseId)
            .Select(x => x.StudentUserId);

        var rows = await db.StudentProfiles
            .AsNoTracking()
            .Where(x => studentIds.Contains(x.UserId))
            .Join(db.Users, profile => profile.UserId, user => user.Id, (profile, user) => new { profile.UserId, user.FirstName, user.LastName, profile.TotalXp })
            .OrderByDescending(x => x.TotalXp)
            .Take(10)
            .ToListAsync(cancellationToken);

        return rows.Select((x, index) => new LeaderboardEntryDto(x.UserId, $"{x.FirstName} {x.LastName}".Trim(), x.TotalXp, index + 1)).ToList();
    }
}
