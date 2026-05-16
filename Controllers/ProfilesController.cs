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
