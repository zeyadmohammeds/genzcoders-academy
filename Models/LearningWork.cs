using GenZCoders.Models.Identity;

namespace GenZCoders.Models;

public class LearningTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CourseSessionId { get; set; }
    public CourseSession? CourseSession { get; set; }
    public Guid? CohortId { get; set; }
    public Cohort? Cohort { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public TaskType TaskType { get; set; } = TaskType.Project;
    public SubmissionType SubmissionType { get; set; } = SubmissionType.Link;
    public int MaxScore { get; set; } = 100;
    public int XpReward { get; set; } = 50;
    public int DueHoursAfterSession { get; set; } = 48;
    public bool IsRequired { get; set; } = true;
    public string RubricJson { get; set; } = "[]";
    public string? SampleSolutionUrl { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<TaskSubmission> Submissions { get; set; } = [];
}

public class TaskSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LearningTaskId { get; set; }
    public LearningTask? LearningTask { get; set; }
    public Guid StudentUserId { get; set; }
    public ApplicationUser? StudentUser { get; set; }
    public string? SubmissionUrl { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? SubmissionText { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsLate { get; set; }
    public int? Score { get; set; }
    public string? Feedback { get; set; }
    public string RubricScoresJson { get; set; } = "{}";
    public Guid? GradedByUserId { get; set; }
    public ApplicationUser? GradedByUser { get; set; }
    public DateTimeOffset? GradedAt { get; set; }
    public int XpAwarded { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
}

public class StudentQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentUserId { get; set; }
    public ApplicationUser? StudentUser { get; set; }
    public Guid? CohortId { get; set; }
    public Cohort? Cohort { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "open";
    public Guid? AssignedToUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }
}
