using GenZCoders.DTOs;

namespace GenZCoders.Services;

public interface IReferralService
{
    Task<string> GenerateUniqueCodeAsync(string seedName, CancellationToken cancellationToken = default);
    Task<Guid?> ResolveReferrerAsync(string? referralCode, CancellationToken cancellationToken = default);
    Task TrackRegistrationAsync(Guid referredUserId, string? referralCode, CancellationToken cancellationToken = default);
    Task TrackPaidConversionAsync(Guid referredUserId, CancellationToken cancellationToken = default);
    Task<ReferralSummaryDto> SummaryAsync(Guid userId, CancellationToken cancellationToken = default);
}
