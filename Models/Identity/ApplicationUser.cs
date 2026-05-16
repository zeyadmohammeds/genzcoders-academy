using Microsoft.AspNetCore.Identity;

namespace GenZCoders.Models.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        Id = Guid.NewGuid();
    }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string DisplayName => $"{FirstName} {LastName}".Trim();
    public string RoleKey { get; set; } = AcademyRole.Student;
    public string? AvatarUrl { get; set; }
    public string? PreferredLanguage { get; set; } = "en";
    public string? City { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int TotalXp { get; set; } = 0;
    public int Level { get; set; } = 1;
    public bool ProfileCompleted { get; set; } = false;
    public int? Age { get; set; }
    public string? GradeLevel { get; set; }
    public string? NationalId { get; set; }
    public string? SchoolName { get; set; }
    public string? ExperienceLevel { get; set; }
    public string? PreferredTrack { get; set; }
    public string? Goals { get; set; }
    public string? InterestsJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
