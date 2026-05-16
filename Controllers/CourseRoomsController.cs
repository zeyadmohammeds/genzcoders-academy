using GenZCoders.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GenZCoders.Controllers;

[ApiController]
[Route("api/course-rooms")]
public class CourseRoomsController(ICourseRoomService rooms) : ControllerBase
{
    [Authorize]
    [HttpGet("{courseRoundId:guid}")]
    public async Task<IActionResult> Room(Guid courseRoundId, CancellationToken cancellationToken)
        => Ok(await rooms.GetRoomAsync(CurrentUserId(), courseRoundId, cancellationToken));

    [HttpGet("leaderboard")]
    public async Task<IActionResult> Leaderboard([FromQuery] Guid? courseRoundId, [FromQuery] Guid? courseId, CancellationToken cancellationToken)
        => Ok(await rooms.LeaderboardAsync(courseRoundId, courseId, cancellationToken));

    private Guid CurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
}
