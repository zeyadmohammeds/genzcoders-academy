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
[Route("api/applications")]
public class ApplicationsController(IApplicationService applications, AcademyDbContext db) : ControllerBase
{
    [HttpGet("questions")]
    public async Task<IActionResult> Questions([FromQuery] Guid courseId, [FromQuery] Guid? courseRoundId, CancellationToken cancellationToken)
        => Ok(await db.CourseApplicationQuestions
            .AsNoTracking()
            .Where(x => x.CourseId == courseId && x.IsActive && (x.CohortId == null || x.CohortId == courseRoundId))
            .OrderBy(x => x.SortOrder)
            .Select(x => new ApplicationQuestionDto(
                x.Id,
                x.CourseId,
                x.CohortId,
                x.QuestionType,
                x.QuestionText,
                x.HelpText,
                x.OptionsJson,
                x.IsRequired,
                x.AutoGrade,
                x.SortOrder))
            .ToListAsync(cancellationToken));

    [Authorize(Roles = AcademyRole.Student)]
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitCourseApplicationRequest request, CancellationToken cancellationToken)
        => Ok(await applications.SubmitAsync(request, cancellationToken));

    [Authorize(Policy = "AcademyStaff")]
    [HttpPost("questions")]
    public async Task<IActionResult> AddQuestion(ApplicationQuestionCreateRequest request, CancellationToken cancellationToken)
    {
        var question = new CourseApplicationQuestion
        {
            CourseId = request.CourseId,
            CohortId = request.CourseRoundId,
            QuestionType = request.QuestionType,
            QuestionText = request.QuestionText,
            HelpText = request.HelpText,
            OptionsJson = request.OptionsJson,
            CorrectAnswer = request.CorrectAnswer,
            AutoGrade = request.AutoGrade,
            SortOrder = request.SortOrder
        };
        db.CourseApplicationQuestions.Add(question);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { question.Id });
    }

    [Authorize(Roles = AcademyRole.Student)]
    [HttpPost("{applicationId:guid}/payment")]
    public async Task<IActionResult> MarkPaid(Guid applicationId, MarkApplicationPaidRequest request, CancellationToken cancellationToken)
        => Ok(await applications.MarkPaidAsync(applicationId, request, cancellationToken));

    [Authorize(Policy = "CourseManagers")]
    [HttpPost("{applicationId:guid}/review")]
    public async Task<IActionResult> Review(Guid applicationId, ApplicationReviewRequest request, CancellationToken cancellationToken)
        => Ok(await applications.ReviewAsync(applicationId, CurrentUserId(), request, cancellationToken));

    [Authorize(Policy = "CourseManagers")]
    [HttpGet("pending")]
    public async Task<IActionResult> Pending(CancellationToken cancellationToken)
        => Ok(await applications.PendingAsync(cancellationToken));

    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> My(CancellationToken cancellationToken)
        => Ok(await applications.GetByUserAsync(CurrentUserId(), cancellationToken));

    private Guid CurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
}
