using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Models;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Services;

public class ReferralService(AcademyDbContext db, INotificationService notifications) : IReferralService
{
    public async Task<string> GenerateUniqueCodeAsync(string seedName, CancellationToken cancellationToken = default)
    {
        var prefix = string.Concat((seedName ?? "GENZ").Where(char.IsLetterOrDigit)).ToUpperInvariant();
        prefix = prefix.Length >= 3 ? prefix[..Math.Min(prefix.Length, 10)] : "GENZ";

        for (var i = 0; i < 20; i++)
        {
            var code = $"{prefix}-{Random.Shared.Next(1000, 9999)}";
            if (!await db.StudentProfiles.AnyAsync(x => x.ReferralCode == code, cancellationToken))
            {
                return code;
            }
        }

        return $"GENZ-{Guid.NewGuid():N}"[..14].ToUpperInvariant();
    }

    public async Task<Guid?> ResolveReferrerAsync(string? referralCode, CancellationToken cancellationToken = default)
        => string.IsNullOrWhiteSpace(referralCode)
            ? null
            : await db.StudentProfiles
                .Where(x => x.ReferralCode == referralCode.Trim().ToUpperInvariant())
                .Select(x => (Guid?)x.UserId)
                .FirstOrDefaultAsync(cancellationToken);

    public async Task TrackRegistrationAsync(Guid referredUserId, string? referralCode, CancellationToken cancellationToken = default)
    {
        var referrerId = await ResolveReferrerAsync(referralCode, cancellationToken);
        if (referrerId is null || referrerId == referredUserId) return;

        if (await db.Referrals.AnyAsync(x => x.ReferredUserId == referredUserId, cancellationToken)) return;

        db.Referrals.Add(new Referral
        {
            ReferrerUserId = referrerId.Value,
            ReferredUserId = referredUserId,
            ReferralCode = referralCode!.Trim().ToUpperInvariant(),
            RegistrationXpAwarded = 100
        });

        await AwardXpAsync(referrerId.Value, 100, "Referral registration XP", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await notifications.QueueAsync(referrerId.Value, "Referral joined", "A friend registered with your referral code. You earned 100 XP.", [NotificationChannel.InApp, NotificationChannel.Email], cancellationToken);
    }

    public async Task TrackPaidConversionAsync(Guid referredUserId, CancellationToken cancellationToken = default)
    {
        var referral = await db.Referrals.FirstOrDefaultAsync(x => x.ReferredUserId == referredUserId && !x.ConvertedToPaidEnrollment, cancellationToken);
        if (referral is null) return;

        referral.ConvertedToPaidEnrollment = true;
        referral.ConvertedAt = DateTimeOffset.UtcNow;
        referral.EnrollmentXpAwarded = 300;
        referral.DiscountCreditEgp = 100;
        await AwardXpAsync(referral.ReferrerUserId, 300, "Referral paid conversion XP", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await notifications.QueueAsync(referral.ReferrerUserId, "Referral converted", "Your friend paid and joined. You earned 300 XP and 100 EGP credit.", [NotificationChannel.InApp, NotificationChannel.Email, NotificationChannel.WhatsApp], cancellationToken);
    }

    public async Task<ReferralSummaryDto> SummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await db.StudentProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        var referrals = await db.Referrals.AsNoTracking().Where(x => x.ReferrerUserId == userId).ToListAsync(cancellationToken);
        return new ReferralSummaryDto(
            profile?.ReferralCode ?? string.Empty,
            referrals.Count,
            referrals.Count(x => x.ConvertedToPaidEnrollment),
            referrals.Sum(x => x.RegistrationXpAwarded + x.EnrollmentXpAwarded),
            referrals.Sum(x => x.DiscountCreditEgp));
    }

    private async Task AwardXpAsync(Guid userId, int amount, string description, CancellationToken cancellationToken)
    {
        db.XpTransactions.Add(new XpTransaction { StudentUserId = userId, Amount = amount, SourceType = XpSourceType.Referral, Description = description });
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (profile is not null) profile.TotalXp += amount;
    }
}
