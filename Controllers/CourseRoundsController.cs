using GenZCoders.DTOs;
using GenZCoders.Models;
using GenZCoders.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenZCoders.Controllers;

[ApiController]
[Route("api/course-rounds")]
public class CourseRoundsController(ICourseRoundService courseRounds) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? courseId, CancellationToken cancellationToken)
        => Ok(await courseRounds.ListAsync(courseId, cancellationToken));

    [Authorize(Policy = "CourseManagers")]
    [HttpPost]
    public async Task<IActionResult> Create(CourseRoundCreateRequest request, CancellationToken cancellationToken)
        => Ok(await courseRounds.CreateAsync(request, cancellationToken));

    [Authorize(Policy = "CourseManagers")]
    [HttpPost("move-student")]
    public async Task<IActionResult> MoveStudent(MoveStudentRoundRequest request, CancellationToken cancellationToken)
    {
        await courseRounds.MoveStudentAsync(request, cancellationToken);
        return Ok(new { moved = true });
    }
}
