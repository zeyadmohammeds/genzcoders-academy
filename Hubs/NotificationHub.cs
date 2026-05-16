using GenZCoders.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Hubs;

[Authorize]
public class NotificationHub(AcademyDbContext db) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            var unreadCount = await db.NotificationMessages
                .CountAsync(x => x.RecipientUserId == Guid.Parse(userId) && x.Status != Models.NotificationStatus.Read);
            await Clients.Caller.SendAsync("UnreadCount", unreadCount);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (userId is not null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        await base.OnDisconnectedAsync(exception);
    }
}
