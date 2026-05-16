using GenZCoders.Models.Identity;

namespace GenZCoders.Models;

public class XpTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentUserId { get; set; }
    public ApplicationUser? StudentUser { get; set; }
    public int Amount { get; set; }
    public XpSourceType SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class Badge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string? ColorHex { get; set; }
    public int XpReward { get; set; }
    public string CriteriaJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public ICollection<StudentBadge> StudentBadges { get; set; } = [];
}

public class StudentBadge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BadgeId { get; set; }
    public Badge? Badge { get; set; }
    public Guid StudentUserId { get; set; }
    public ApplicationUser? StudentUser { get; set; }
    public DateTimeOffset AwardedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? AwardedByUserId { get; set; }
}

public class WeeklyChallenge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int XpReward { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class StudentChallengeProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WeeklyChallengeId { get; set; }
    public WeeklyChallenge? WeeklyChallenge { get; set; }
    public Guid StudentUserId { get; set; }
    public int CurrentValue { get; set; }
    public int TargetValue { get; set; } = 1;
    public bool Completed { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
