using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Models;
using GenZCoders.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GenZCoders.Controllers;

[ApiController]
[Route("api/profiles")]
public class ProfilesController(
    AcademyDbContext db,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        var studentProfile = await db.StudentProfiles.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        var staffProfile = await db.StaffProfiles.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        return Ok(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.DisplayName,
            user.AvatarUrl,
            user.Bio,
            user.PhoneNumber,
            student = studentProfile != null ? new {
                studentProfile.TotalXp,
                studentProfile.Level,
                studentProfile.ExperienceLevel,
                studentProfile.OnboardingCompleted,
                studentProfile.InterestsJson
            } : null,
            staff = staffProfile != null ? new {
                staffProfile.Position,
                staffProfile.Department
            } : null
        });
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        user.FirstName = request.FirstName ?? user.FirstName;
        user.LastName = request.LastName ?? user.LastName;
        user.Bio = request.Bio ?? user.Bio;
        user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(result.Errors);

        if (await db.StudentProfiles.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken) is StudentProfile sp)
        {
            sp.InterestsJson = request.InterestsJson ?? sp.InterestsJson;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(new { success = true });
    }

    [Authorize(Policy = "AcademyStaff")]
    [HttpGet("engineer/dashboard")]
    public async Task<IActionResult> GetEngineerDashboard(CancellationToken cancellationToken)
    {
        var engineerUserId = CurrentUserId();

        var cohorts = await db.Cohorts
            .Where(x => x.EngineerUserId == engineerUserId)
            .ToListAsync(cancellationToken);

        var cohortIds = cohorts.Select(x => x.Id).ToList();

        var activeStudents = await db.CohortEnrollments
            .Where(x => cohortIds.Contains(x.CohortId))
            .Select(x => x.StudentUserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var pendingEvaluations = await db.TaskSubmissions
            .Include(x => x.LearningTask)
            .Where(x => x.LearningTask != null && x.LearningTask.CohortId != null && cohortIds.Contains(x.LearningTask.CohortId.Value) && x.Score == null)
            .CountAsync(cancellationToken);

        var upcomingSessions = await db.SessionInstances
            .Include(x => x.Cohort)
            .ThenInclude(c => c!.Course)
            .Where(x => cohortIds.Contains(x.CohortId) && x.ScheduledAt >= DateTimeOffset.UtcNow)
            .OrderBy(x => x.ScheduledAt)
            .Take(5)
            .Select(x => new
            {
                id = x.Id.ToString(),
                title = x.Cohort != null && x.Cohort.Course != null ? $"{x.Cohort.Course.Title}: {x.WeekTitle}" : x.WeekTitle,
                group = x.Cohort != null ? x.Cohort.Name : "Default Group",
                time = x.ScheduledAt.ToString("yyyy-MM-dd HH:mm EET")
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            activeStudents,
            pendingEvaluations,
            upcomingSessions
        });
    }

    [Authorize(Policy = "AcademyStaff")]
    [HttpGet("cta/dashboard")]
    public async Task<IActionResult> GetCTADashboard(CancellationToken cancellationToken)
    {
        var upcomingSupport = await db.SessionInstances
            .Include(x => x.Cohort)
            .ThenInclude(c => c!.Course)
            .Include(x => x.Cohort)
            .ThenInclude(c => c!.EngineerUser)
            .Where(x => x.ScheduledAt >= DateTimeOffset.UtcNow)
            .OrderBy(x => x.ScheduledAt)
            .Take(5)
            .Select(x => new
            {
                title = x.Cohort != null && x.Cohort.Course != null ? $"{x.Cohort.Course.Title}: {x.Cohort.Name}" : x.WeekTitle,
                time = x.ScheduledAt.ToString("yyyy-MM-dd HH:mm EET"),
                lead = x.Cohort != null && x.Cohort.EngineerUser != null ? x.Cohort.EngineerUser.FirstName + " " + x.Cohort.EngineerUser.LastName : "TBD"
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            sessionsToSupport = await db.SessionInstances.CountAsync(x => x.ScheduledAt >= DateTimeOffset.UtcNow, cancellationToken),
            studentsMentored = await db.CohortEnrollments.Select(x => x.StudentUserId).Distinct().CountAsync(cancellationToken),
            pendingNotes = 0,
            upcomingSupport
        });
    }

    private Guid CurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
}

public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Bio { get; set; }
    public string? PhoneNumber { get; set; }
    public string? InterestsJson { get; set; }
}
