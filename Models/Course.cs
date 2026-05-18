namespace GenZCoders.Models;

public class Course
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public int MinimumAge { get; set; }
    public int? MaximumAge { get; set; }
    public decimal PriceEgp { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? IconName { get; set; }
    public string? ColorHex { get; set; }
    public string SkillsTaughtJson { get; set; } = "[]";
    public int Phase { get; set; } = 1;
    public int SortOrder { get; set; }
    public int CoreSessions { get; set; } = 8;
    public int SupportSessions { get; set; } = 4;
    public string Level { get; set; } = "Phase 1";
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public string? ImageUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<CourseModule> Modules { get; set; } = [];
    public ICollection<CourseSession> CourseSessions { get; set; } = [];
    public ICollection<LiveSession> LiveSessions { get; set; } = [];
}
