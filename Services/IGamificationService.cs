using GenZCoders.Models;

namespace GenZCoders.Services;

public interface IGamificationService
{
    Task AwardXpAsync(Guid studentUserId, int amount, string reason, XpSourceType source, CancellationToken ct = default);
    Task<List<Badge>> CheckAndAwardBadgesAsync(Guid studentUserId, CancellationToken ct = default);
}
