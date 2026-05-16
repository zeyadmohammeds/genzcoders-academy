using GenZCoders.Services;
using Microsoft.AspNetCore.Mvc;

namespace GenZCoders.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController(ICourseService courseService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await courseService.GetFeaturedAsync(cancellationToken));

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var course = await courseService.GetBySlugAsync(slug, cancellationToken);
        return course is null ? NotFound() : Ok(course);
    }

    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations([FromServices] ICourseRecommendationService recommendations, CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdString, out var userId))
        {
            return Ok(await recommendations.RecommendForUserAsync(userId, cancellationToken));
        }
        return Ok(await courseService.GetFeaturedAsync(cancellationToken));
    }
}
