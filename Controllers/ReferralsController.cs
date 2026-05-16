using GenZCoders.Models;
using GenZCoders.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GenZCoders.Controllers;

[ApiController]
[Authorize(Roles = AcademyRole.Student)]
[Route("api/referrals")]
public class ReferralsController(IReferralService referrals) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
        => Ok(await referrals.SummaryAsync(CurrentUserId(), cancellationToken));

    private Guid CurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
}
