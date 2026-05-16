using GenZCoders.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenZCoders.Controllers;

public record ZoomSignatureRequest(string MeetingNumber, int Role);

[ApiController]
[Route("api/live-sessions")]
public class LiveSessionsController(ILiveSessionService liveSessionService, IConfiguration configuration) : ControllerBase
{
    [HttpGet("upcoming")]
    public async Task<IActionResult> Upcoming(CancellationToken cancellationToken)
        => Ok(await liveSessionService.GetUpcomingAsync(cancellationToken));

    [Authorize]
    [HttpGet("{id:guid}/embed-config")]
    public IActionResult EmbedConfig(Guid id)
        => Ok(new
        {
            sessionId = id,
            sdkKey = configuration["Zoom:SdkKey"],
            signatureEndpoint = "/api/live-sessions/zoom-signature",
            leaveUrl = Url.Content("~/live.html")
        });

    [Authorize]
    [HttpPost("zoom-signature")]
    public IActionResult ZoomSignature([FromBody] ZoomSignatureRequest request)
    {
        var sdkKey = configuration["Zoom:SdkKey"];
        var sdkSecret = configuration["Zoom:SdkSecret"];
        if (string.IsNullOrWhiteSpace(sdkKey) || string.IsNullOrWhiteSpace(sdkSecret))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Zoom Meeting SDK is not configured. Add Zoom:SdkKey and Zoom:SdkSecret to appsettings (from your Zoom Marketplace SDK app)."
            });
        }

        if (string.IsNullOrWhiteSpace(request.MeetingNumber))
            return BadRequest(new { message = "meetingNumber is required." });

        var signature = ZoomMeetingSdkSignature.Generate(sdkKey, sdkSecret, request.MeetingNumber.Trim(), request.Role);
        return Ok(new { signature, sdkKey, expiresIn = 7200 });
    }
}
