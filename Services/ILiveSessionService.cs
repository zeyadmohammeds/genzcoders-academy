using GenZCoders.DTOs;

namespace GenZCoders.Services;

public interface ILiveSessionService
{
    Task<IReadOnlyList<LiveSessionDto>> GetUpcomingAsync(CancellationToken cancellationToken = default);
}
