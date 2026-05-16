using GenZCoders.Data;
using GenZCoders.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Services;

public class LiveSessionService(AcademyDbContext db) : ILiveSessionService
{
    public async Task<IReadOnlyList<LiveSessionDto>> GetUpcomingAsync(CancellationToken cancellationToken = default)
        => await db.LiveSessions
            .AsNoTracking()
            .Include(x => x.Course)
            .Where(x => x.StartsAt >= DateTimeOffset.UtcNow.AddDays(-1))
            .OrderBy(x => x.StartsAt)
            .Take(20)
            .Select(x => new LiveSessionDto(
                x.Id,
                x.CourseId,
                x.Course == null ? string.Empty : x.Course.Title,
                x.Title,
                x.StartsAt,
                x.DurationMinutes,
                x.HostName,
                x.ZoomMeetingId,
                x.ZoomJoinUrl,
                x.EmbedEnabled))
            .ToListAsync(cancellationToken);
}
