using GenZCoders.Data;
using GenZCoders.Models;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Services;

public class GamificationService(AcademyDbContext db) : IGamificationService
{
    public async Task AwardXpAsync(Guid studentUserId, int amount, string reason, XpSourceType source, CancellationToken ct = default)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(x => x.UserId == studentUserId, ct);
        if (profile == null) return;

        db.XpTransactions.Add(new XpTransaction
        {
            StudentUserId = studentUserId,
            Amount = amount,
            Description = reason,
            SourceType = source
        });

        profile.TotalXp += amount;
        
        // Simple level up logic: 500 XP per level
        int newLevel = (profile.TotalXp / 500) + 1;
        if (newLevel > profile.Level)
        {
            profile.Level = newLevel;
            
            // Queue congratulation message
            db.NotificationMessages.Add(new NotificationMessage
            {
                RecipientUserId = studentUserId,
                Subject = "Level Up!",
                Body = $"Congratulations! You've reached Level {newLevel}. Keep up the great work!",
                Channel = NotificationChannel.InApp,
                Status = NotificationStatus.Sent
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<List<Badge>> CheckAndAwardBadgesAsync(Guid studentUserId, CancellationToken ct = default)
    {
        var awarded = new List<Badge>();
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(x => x.UserId == studentUserId, ct);
        if (profile == null) return awarded;

        var existingBadgeIds = await db.StudentBadges
            .Where(x => x.StudentUserId == studentUserId)
            .Select(x => x.BadgeId)
            .ToListAsync(ct);

        var badges = await db.Badges.Where(x => x.IsActive).ToListAsync(ct);

        foreach (var badge in badges)
        {
            if (existingBadgeIds.Contains(badge.Id)) continue;

            // Logic for specific badges based on slug
            bool shouldAward = false;
            
            if (badge.Slug == "first-launch" && profile.OnboardingCompleted) 
                shouldAward = true;
            
            if (badge.Slug == "xp-explorer" && profile.TotalXp >= 100) 
                shouldAward = true;
                
            if (badge.Slug == "xp-warrior" && profile.TotalXp >= 1000) 
                shouldAward = true;

            if (shouldAward)
            {
                db.StudentBadges.Add(new StudentBadge
                {
                    BadgeId = badge.Id,
                    StudentUserId = studentUserId,
                    AwardedAt = DateTimeOffset.UtcNow
                });
                
                if (badge.XpReward > 0)
                {
                    // We call the inner logic but don't SaveChanges yet to keep it in one transaction
                    db.XpTransactions.Add(new XpTransaction
                    {
                        StudentUserId = studentUserId,
                        Amount = badge.XpReward,
                        Description = $"Badge Earned: {badge.Name}",
                        SourceType = XpSourceType.Badge
                    });
                    profile.TotalXp += badge.XpReward;
                }
                
                db.NotificationMessages.Add(new NotificationMessage
                {
                    RecipientUserId = studentUserId,
                    Subject = "New Badge Unlocked!",
                    Body = $"You've earned the '{badge.Name}' badge!",
                    Channel = NotificationChannel.InApp,
                    Status = NotificationStatus.Sent
                });

                awarded.Add(badge);
            }
        }

        await db.SaveChangesAsync(ct);
        return awarded;
    }
}
