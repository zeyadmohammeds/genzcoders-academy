using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Models;
using GenZCoders.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GenZCoders.Controllers;

[Authorize]
[ApiController]
[Route("api/notifications")]
public class NotificationsController(INotificationService notifications, AcademyDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var userId = CurrentUserId();
        var messages = await notifications.ListByUserAsync(userId, ct);

        var totalCount = messages.Count;
        var skip = (page - 1) * pageSize;
        var paged = messages.Skip(skip).Take(pageSize);

        return Ok(new
        {
            items = paged.Select(m => new {
                m.Id,
                Title = m.Subject,
                Message = m.Body,
                Type = "info",
                IsRead = m.Status == NotificationStatus.Read,
                m.CreatedAt
            }),
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        await notifications.MarkReadAsync(CurrentUserId(), id, ct);
        return Ok();
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
        => Ok(await notifications.GetSettingsAsync(CurrentUserId(), ct));

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await notifications.MarkAllReadAsync(CurrentUserId(), ct);
        return Ok();
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings(NotificationSettingsRequest request, CancellationToken ct)
    {
        await notifications.UpdateSettingsAsync(CurrentUserId(), request, ct);
        return Ok();
    }

    [Authorize(Roles = AcademyRole.AcademyAdmin)]
    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast(BroadcastRequest request, CancellationToken ct)
    {
        var users = await db.Users.Select(u => u.Id).ToListAsync(ct);
        foreach (var userId in users)
        {
            await notifications.QueueAsync(userId, request.Title, request.Message, [NotificationChannel.InApp], ct);
        }
        return Ok(new { Queued = users.Count });
    }

    private Guid CurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
}

public record BroadcastRequest(string Title, string Message, string Type);
