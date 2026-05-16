using GenZCoders.Data;
using GenZCoders.Models;
using GenZCoders.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GenZCoders.Controllers;

[ApiController]
[Route("api/gamification")]
public class GamificationController(IGamificationService gamification, AcademyDbContext db) : ControllerBase
{
    [Authorize]
    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard(CancellationToken ct)
    {
        var top = await db.StudentProfiles
            .Include(x => x.User)
            .OrderByDescending(x => x.TotalXp)
            .Take(10)
            .Select(x => new {
                x.UserId,
                DisplayName = x.User != null ? x.User.DisplayName : "Anonymous Explorer",
                x.TotalXp,
                x.Level,
                ExperienceLevel = x.ExperienceLevel.ToString()
            })
            .ToListAsync(ct);
        return Ok(top);
    }

    [Authorize]
    [HttpGet("badges")]
    public async Task<IActionResult> GetMyBadges(CancellationToken ct)
    {
        var userId = CurrentUserId();
        var badges = await db.StudentBadges
            .Include(x => x.Badge)
            .Where(x => x.StudentUserId == userId)
            .OrderByDescending(x => x.AwardedAt)
            .Select(x => new {
                x.BadgeId,
                Name = x.Badge != null ? x.Badge.Name : "Unknown Badge",
                Description = x.Badge != null ? x.Badge.Description : "",
                IconUrl = x.Badge != null ? x.Badge.IconUrl : "",
                AwardedAt = x.AwardedAt
            })
            .ToListAsync(ct);
        return Ok(badges);
    }

    [Authorize]
    [HttpPost("check-achievements")]
    public async Task<IActionResult> CheckAchievements(CancellationToken ct)
    {
        var awarded = await gamification.CheckAndAwardBadgesAsync(CurrentUserId(), ct);
        return Ok(new { awardedCount = awarded.Count, badges = awarded });
    }

    private Guid CurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
}
