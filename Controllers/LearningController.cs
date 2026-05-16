using GenZCoders.DTOs;
using GenZCoders.Models;
using GenZCoders.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GenZCoders.Controllers;

[ApiController]
[Route("api/learning")]
public class LearningController(ILearningService learning) : ControllerBase
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
    [HttpGet("room/{courseId:guid}")]
    public async Task<IActionResult> Room(Guid courseId, CancellationToken cancellationToken)
        => Ok(await learning.GetRoomAsync(courseId, CurrentUserId(), cancellationToken));

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

    private Guid CurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    private Guid? CurrentUserIdOrNull()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
