using GenZCoders.Data;
using GenZCoders.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GenZCoders.Controllers;

[ApiController]
[Route("api/course-rooms")]
public class CourseRoomsController(ICourseRoomService rooms, AcademyDbContext db) : ControllerBase
{
    [Authorize]
    [HttpGet("{courseRoundIdOrSlug}")]
    public async Task<IActionResult> Room(string courseRoundIdOrSlug, CancellationToken cancellationToken)
    {
        Guid roundId;
        if (Guid.TryParse(courseRoundIdOrSlug, out var id))
        {
            roundId = id;
        }
        else
        {
            var round = await db.Cohorts.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == courseRoundIdOrSlug, cancellationToken);
            if (round == null) return NotFound(new { message = "Course round not found." });
            roundId = round.Id;
        }
        return Ok(await rooms.GetRoomAsync(CurrentUserId(), roundId, cancellationToken));
    }

    [HttpGet("leaderboard")]
    public async Task<IActionResult> Leaderboard([FromQuery] Guid? courseRoundId, [FromQuery] Guid? courseId, CancellationToken cancellationToken)
        => Ok(await rooms.LeaderboardAsync(courseRoundId, courseId, cancellationToken));

    private Guid CurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
}
