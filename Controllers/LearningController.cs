using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Models;
using GenZCoders.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GenZCoders.Controllers;

[ApiController]
[Route("api/learning")]
public class LearningController(ILearningService learning, AcademyDbContext db) : ControllerBase
{
    [Authorize(Policy = "AcademyStaff")]
    [HttpPost("lessons")]
    public async Task<IActionResult> AddLesson(LessonCreateRequest request, CancellationToken cancellationToken)
        => Ok(new { id = await learning.AddLessonAsync(request, CurrentUserIdOrNull(), cancellationToken) });

    [Authorize(Policy = "AcademyStaff")]
    [HttpPost("materials")]
    public async Task<IActionResult> AddMaterial(MaterialCreateRequest request, CancellationToken cancellationToken)
        => Ok(new { id = await learning.AddMaterialAsync(request, CurrentUserIdOrNull(), cancellationToken) });

    [Authorize(Policy = "AcademyStaff")]
    [HttpPost("tasks")]
    public async Task<IActionResult> CreateTask(TaskCreateRequest request, CancellationToken cancellationToken)
        => Ok(new { id = await learning.CreateTaskAsync(request, CurrentUserIdOrNull(), cancellationToken) });

    [Authorize(Roles = AcademyRole.Student)]
    [HttpPost("tasks/submissions")]
    public async Task<IActionResult> SubmitTask(TaskSubmitRequest request, CancellationToken cancellationToken)
        => Ok(new { id = await learning.SubmitTaskAsync(request, cancellationToken) });

    [Authorize(Policy = "AcademyStaff")]
    [HttpPost("tasks/submissions/{submissionId:guid}/grade")]
    public async Task<IActionResult> Grade(Guid submissionId, GradeSubmissionRequest request, CancellationToken cancellationToken)
    {
        await learning.GradeSubmissionAsync(submissionId, CurrentUserId(), request, cancellationToken);
        return Ok(new { graded = true });
    }

    [Authorize(Policy = "AcademyStaff")]
    [HttpPost("attendance")]
    public async Task<IActionResult> MarkAttendance(AttendanceMarkRequest request, CancellationToken cancellationToken)
    {
        await learning.MarkAttendanceAsync(request, CurrentUserIdOrNull(), cancellationToken);
        return Ok(new { marked = true });
    }

    [Authorize]
    [HttpGet("room/{courseIdOrSlug}")]
    public async Task<IActionResult> Room(string courseIdOrSlug, CancellationToken cancellationToken)
    {
        Guid courseId;
        if (Guid.TryParse(courseIdOrSlug, out var id))
        {
            courseId = id;
        }
        else
        {
            var course = await db.Courses.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == courseIdOrSlug, cancellationToken);
            if (course == null) return NotFound(new { message = "Course not found." });
            courseId = course.Id;
        }
        return Ok(await learning.GetRoomAsync(courseId, CurrentUserId(), cancellationToken));
    }

    [Authorize(Roles = AcademyRole.Student)]
    [HttpGet("progress")]
    public async Task<IActionResult> Progress(CancellationToken cancellationToken)
        => Ok(await learning.GetProgressAsync(CurrentUserId(), cancellationToken));

    [Authorize(Roles = AcademyRole.Student)]
    [HttpGet("enrollments")]
    public async Task<IActionResult> MyEnrollments(CancellationToken cancellationToken)
        => Ok(await learning.GetMyEnrollmentsAsync(CurrentUserId(), cancellationToken));

    [Authorize(Roles = AcademyRole.Student)]
    [HttpGet("materials")]
    public async Task<IActionResult> MyMaterials([FromQuery] Guid? courseId, CancellationToken cancellationToken)
        => Ok(await learning.GetMyMaterialsAsync(CurrentUserId(), courseId, cancellationToken));

    [Authorize(Roles = AcademyRole.Student)]
    [HttpGet("certificates")]
    public async Task<IActionResult> MyCertificates(CancellationToken cancellationToken)
        => Ok(await learning.GetMyCertificatesAsync(CurrentUserId(), cancellationToken));

    [Authorize(Roles = AcademyRole.Student)]
    [HttpGet("sessions")]
    public async Task<IActionResult> MySessions(CancellationToken cancellationToken)
        => Ok(await learning.GetMySessionsAsync(CurrentUserId(), cancellationToken));

    [Authorize(Roles = AcademyRole.Student)]
    [HttpGet("tasks")]
    public async Task<IActionResult> MyTasks(CancellationToken cancellationToken)
    {
        var enrollments = await learning.GetMyEnrollmentsAsync(CurrentUserId(), cancellationToken);
        var cohortIds = enrollments.Select(e => e.CohortId).Distinct().ToList();
        return Ok(await learning.GetMyTasksAsync(cohortIds, CurrentUserId(), cancellationToken));
    }

    [Authorize(Policy = "AcademyStaff")]
    [HttpGet("engineer/sessions")]
    public async Task<IActionResult> EngineerSessions(CancellationToken cancellationToken)
    {
        var engineerUserId = CurrentUserId();

        var cohorts = await db.Cohorts
            .Where(x => x.EngineerUserId == engineerUserId)
            .ToListAsync(cancellationToken);

        var cohortIds = cohorts.Select(x => x.Id).ToList();

        var sessions = await db.SessionInstances
            .Include(x => x.Cohort)
            .ThenInclude(c => c!.Course)
            .Where(x => cohortIds.Contains(x.CohortId))
            .OrderBy(x => x.ScheduledAt)
            .Select(x => new
            {
                id = x.Id.ToString(),
                title = x.Cohort != null && x.Cohort.Course != null ? $"{x.Cohort.Course.Title}: {x.WeekTitle}" : x.WeekTitle,
                group = x.Cohort != null ? x.Cohort.Name : "Default Group",
                date = x.ScheduledAt.ToString("MMMM dd, yyyy"),
                time = x.ScheduledAt.ToString("HH:mm") + " - " + x.ScheduledAt.AddMinutes(x.DurationMinutes).ToString("HH:mm") + " EET",
                students = x.Cohort != null ? x.Cohort.CurrentStudents : 0,
                status = x.Status.ToString().ToLowerInvariant(),
                zoomStartUrl = x.Cohort != null ? x.Cohort.ZoomStartUrl : null
            })
            .ToListAsync(cancellationToken);

        return Ok(sessions);
    }

    [Authorize(Policy = "AcademyStaff")]
    [HttpGet("engineer/students")]
    public async Task<IActionResult> EngineerStudents(CancellationToken cancellationToken)
    {
        var engineerUserId = CurrentUserId();

        var cohorts = await db.Cohorts
            .Where(x => x.EngineerUserId == engineerUserId)
            .ToListAsync(cancellationToken);

        var cohortIds = cohorts.Select(x => x.Id).ToList();

        var enrollments = await db.CohortEnrollments
            .Include(x => x.StudentUser)
            .Include(x => x.Cohort)
                .ThenInclude(c => c!.Course)
            .Where(x => cohortIds.Contains(x.CohortId))
            .ToListAsync(cancellationToken);

        var result = new List<object>();

        foreach (var enrollment in enrollments)
        {
            var studentUserId = enrollment.StudentUserId;
            var courseId = enrollment.Cohort?.CourseId ?? Guid.Empty;

            var app = await db.CourseApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.StudentUserId == studentUserId && x.CourseId == courseId, cancellationToken);

            result.Add(new
            {
                id = $"STU-{enrollment.Id.ToString()[..6].ToUpper()}",
                name = enrollment.StudentUser != null ? $"{enrollment.StudentUser.FirstName} {enrollment.StudentUser.LastName}".Trim() : "Student Candidate",
                course = enrollment.Cohort?.Course?.Title ?? "Enrolled Course",
                group = enrollment.Cohort?.Name ?? "Group",
                attendance = "95%",
                grade = "A",
                applicationId = app?.Id.ToString()
            });
        }

        return Ok(result);
    }

    [Authorize(Policy = "AcademyStaff")]
    [HttpGet("engineer/pending-tasks")]
    public async Task<IActionResult> EngineerPendingTasks(CancellationToken cancellationToken)
    {
        var engineerUserId = CurrentUserId();
        var cohorts = await db.Cohorts.Where(x => x.EngineerUserId == engineerUserId).ToListAsync(cancellationToken);
        var cohortIds = cohorts.Select(x => x.Id).ToList();

        var tasks = await db.TaskSubmissions
            .Include(x => x.LearningTask)
            .ThenInclude(t => t!.Cohort)
            .ThenInclude(c => c!.Course)
            .Where(x => x.LearningTask != null && x.LearningTask.CohortId != null && cohortIds.Contains(x.LearningTask.CohortId.Value) && x.Score == null)
            .GroupBy(x => new { x.LearningTask!.Id, x.LearningTask.Title, CourseTitle = x.LearningTask.Cohort!.Course!.Title, GroupName = x.LearningTask.Cohort.Name })
            .Select(g => new
            {
                id = g.Key.Id,
                title = g.Key.Title,
                course = g.Key.CourseTitle,
                group = g.Key.GroupName,
                submissions = g.Count()
            })
            .ToListAsync(cancellationToken);

        return Ok(tasks);
    }

    [Authorize(Policy = "AcademyStaff")]
    [HttpGet("cta/sessions")]
    public async Task<IActionResult> CTASessions(CancellationToken cancellationToken)
    {
        var ctaUserId = CurrentUserId();
        // Since there is no explicit CTA field yet, we'll return upcoming sessions for the cohorts they might assist
        // Alternatively, just return all sessions for the academy
        var sessions = await db.SessionInstances
            .Include(x => x.Cohort)
            .ThenInclude(c => c!.Course)
            .OrderBy(x => x.ScheduledAt)
            .Select(x => new
            {
                id = x.Id.ToString(),
                title = x.Cohort != null && x.Cohort.Course != null ? $"{x.Cohort.Course.Title}: {x.WeekTitle}" : x.WeekTitle,
                group = x.Cohort != null ? x.Cohort.Name : "Default Group",
                date = x.ScheduledAt.ToString("MMMM dd, yyyy"),
                time = x.ScheduledAt.ToString("HH:mm") + " - " + x.ScheduledAt.AddMinutes(x.DurationMinutes).ToString("HH:mm") + " EET",
                lead = x.Cohort != null && x.Cohort.EngineerUser != null ? x.Cohort.EngineerUser.FirstName + " " + x.Cohort.EngineerUser.LastName : "TBD",
                zoomStartUrl = x.Cohort != null ? x.Cohort.ZoomStartUrl : null
            })
            .ToListAsync(cancellationToken);

        return Ok(sessions);
    }

    [Authorize(Policy = "AcademyStaff")]
    [HttpGet("cta/students")]
    public async Task<IActionResult> CTAStudents(CancellationToken cancellationToken)
    {
        var enrollments = await db.CohortEnrollments
            .Include(x => x.StudentUser)
            .Include(x => x.Cohort)
                .ThenInclude(c => c!.Course)
            .Take(50) // limit for demo
            .ToListAsync(cancellationToken);

        var result = enrollments.Select(enrollment => new
        {
            id = $"STU-{enrollment.Id.ToString()[..6].ToUpper()}",
            name = enrollment.StudentUser != null ? $"{enrollment.StudentUser.FirstName} {enrollment.StudentUser.LastName}".Trim() : "Student Candidate",
            course = enrollment.Cohort?.Course?.Title ?? "Enrolled Course",
            group = enrollment.Cohort?.Name ?? "Group",
            notesCount = 0,
            status = "On Track"
        });

        return Ok(result);
    }

    private Guid CurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    private Guid? CurrentUserIdOrNull()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
