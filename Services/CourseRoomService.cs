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

        var accepted = await db.CourseApplications.AnyAsync(x => x.CohortId == courseRoundId && x.StudentUserId == studentUserId && x.Status == ApplicationStatus.Accepted, cancellationToken);
        var enrolled = await db.CohortEnrollments.AnyAsync(x => x.CohortId == courseRoundId && x.StudentUserId == studentUserId, cancellationToken);
        var access = accepted || enrolled ? CourseAccessStatus.Open : CourseAccessStatus.PendingApproval;

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
        var quizzes = await db.QuizAttempts.CountAsync(x => x.StudentUserId == studentUserId && x.SubmittedAt != null, cancellationToken);
        var totalWeeks = await db.SessionInstances.CountAsync(x => x.CohortId == courseRoundId, cancellationToken);
        var completion = totalWeeks == 0 ? 0 : decimal.Round(attendance * 100m / totalWeeks, 2);

        var tasks = access == CourseAccessStatus.Open
            ? await db.LearningTasks.AsNoTracking()
                .Where(x => x.CohortId == courseRoundId)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new CourseTaskDto(
                    x.Id, x.Title, x.Description,
                    x.TaskType.ToString(), x.SubmissionType.ToString(),
                    x.MaxScore, x.XpReward, x.IsRequired,
                    x.Submissions.Where(s => s.StudentUserId == studentUserId).Select(s => s.Status.ToString()).FirstOrDefault(),
                    x.Submissions.Where(s => s.StudentUserId == studentUserId).Select(s => (int?)s.Score).FirstOrDefault(),
                    x.Submissions.Where(s => s.StudentUserId == studentUserId).Select(s => s.Feedback).FirstOrDefault()
                ))
                .ToListAsync(cancellationToken)
            : [];

        return new CourseRoomDto(
            round.CourseId,
            round.Id,
            round.Course?.Title ?? string.Empty,
            round.Name,
            access,
            round.EngineerUser?.DisplayName,
            weeks,
            materials,
            tasks,
            new CourseProgressDto(xp, attendance, taskCount, quizzes, completion),
            round.ZoomMeetingId,
            round.ZoomJoinUrl,
            null);
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
