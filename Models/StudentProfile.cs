namespace GenZCoders.Models;

public class StudentProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public GenZCoders.Models.Identity.ApplicationUser? User { get; set; }
    public Guid? SchoolId { get; set; }
    public School? School { get; set; }
    public Guid? ParentUserId { get; set; }
    public string? NationalId { get; set; }
    public string? SchoolName { get; set; }
    public int Age { get; set; }
    public string GradeLevel { get; set; } = string.Empty;
    public string InterestsJson { get; set; } = "[]";
    public ExperienceLevel ExperienceLevel { get; set; } = ExperienceLevel.New;
    public string? Goals { get; set; }
    public string? PreferredTrack { get; set; }
    public bool IsOnboardingCompleted { get; set; }
    public bool OnboardingCompleted => IsOnboardingCompleted; // Shim for compatibility
    public DateTimeOffset? OnboardingCompletedAt { get; set; }
    public DateTimeOffset? OnboardingSkippedAt { get; set; }
    public bool ProfileCompletionXpAwarded { get; set; }
    public int TotalXp { get; set; }
    public int Level { get; set; } = 1;
    public int StreakDays { get; set; }
    public int StreakFreezes { get; set; }
    public DateTimeOffset? LastActiveAt { get; set; }
    public string ReferralCode { get; set; } = string.Empty;
    public Guid? ReferredByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
