using GenZCoders.DTOs;
using GenZCoders.Services;
using Microsoft.AspNetCore.Mvc;

namespace GenZCoders.Controllers;

[ApiController]
[Route("api/enrollments")]
public class EnrollmentsController(IEnrollmentService enrollmentService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(EnrollmentRequest request, CancellationToken cancellationToken)
        => Ok(await enrollmentService.CreateLeadAsync(request, cancellationToken));
}
