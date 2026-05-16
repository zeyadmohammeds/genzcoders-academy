using GenZCoders.Models.Identity;

namespace GenZCoders.Models;

public class CourseApplicationQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid? CohortId { get; set; }
    public Cohort? Cohort { get; set; }
    public ApplicationQuestionType QuestionType { get; set; } = ApplicationQuestionType.Mcq;
    public string QuestionText { get; set; } = string.Empty;
    public string? HelpText { get; set; }
    public string OptionsJson { get; set; } = "[]";
    public string? CorrectAnswer { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool AutoGrade { get; set; } = true;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class CourseApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid? CohortId { get; set; }
    public Cohort? Cohort { get; set; }
    public Guid StudentUserId { get; set; }
    public ApplicationUser? StudentUser { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
    public decimal ApplicationScore { get; set; }
    public bool QuestionsPassed { get; set; }
    public bool PaymentUnlocked { get; set; }
    public bool PaymentCompleted { get; set; }
    public Guid? EnrollmentOrderId { get; set; }
    public EnrollmentOrder? EnrollmentOrder { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public ApplicationReviewDecision ReviewDecision { get; set; } = ApplicationReviewDecision.Pending;
    public string? ReviewNotes { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public ICollection<CourseApplicationAnswer> Answers { get; set; } = [];
}

public class CourseApplicationAnswer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseApplicationId { get; set; }
    public CourseApplication? CourseApplication { get; set; }
    public Guid CourseApplicationQuestionId { get; set; }
    public CourseApplicationQuestion? Question { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public bool? IsCorrect { get; set; }
    public int ScoreAwarded { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
