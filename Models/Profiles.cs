using GenZCoders.Models.Identity;

namespace GenZCoders.Models;

public class ParentProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string? WhatsAppNumber { get; set; }
    public string NotificationPreferencesJson { get; set; } = "{\"whatsapp\":true,\"email\":true,\"sms\":false}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class StaffProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string Bio { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string? LinkedInUrl { get; set; }
    public bool IsCta { get; set; }
    public Guid? CtaSchoolId { get; set; }
    public School? CtaSchool { get; set; }
    public int MentorXp { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class SchoolCoordinator
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public Guid SchoolId { get; set; }
    public School? School { get; set; }
    public bool IsPrimary { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
