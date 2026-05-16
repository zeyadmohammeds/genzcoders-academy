using GenZCoders.Models.Identity;

namespace GenZCoders.Models;

public class StudentProject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentUserId { get; set; }
    public ApplicationUser? StudentUser { get; set; }
    public Guid? CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid? TaskSubmissionId { get; set; }
    public TaskSubmission? TaskSubmission { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ProjectUrl { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public ProjectVisibility Visibility { get; set; } = ProjectVisibility.Private;
    public bool IsFeatured { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class Certificate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentUserId { get; set; }
    public ApplicationUser? StudentUser { get; set; }
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid? CohortId { get; set; }
    public Cohort? Cohort { get; set; }
    public string CertificateNumber { get; set; } = string.Empty;
    public string? PdfUrl { get; set; }
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
}
