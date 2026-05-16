using GenZCoders.Models.Identity;

namespace GenZCoders.Models;

public class Quiz
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CourseSessionId { get; set; }
    public CourseSession? CourseSession { get; set; }
    public Guid? CohortId { get; set; }
    public Cohort? Cohort { get; set; }
    public string Title { get; set; } = string.Empty;
    public QuizType QuizType { get; set; } = QuizType.Formative;
    public int? TimeLimitMinutes { get; set; }
    public int MaxAttempts { get; set; } = 1;
    public int PassScore { get; set; } = 60;
    public int XpReward { get; set; } = 100;
    public bool ShuffleQuestions { get; set; } = true;
    public AnswerRevealPolicy ShowAnswersAfter { get; set; } = AnswerRevealPolicy.AfterDeadline;
    public DateTimeOffset? AvailableFrom { get; set; }
    public DateTimeOffset? AvailableUntil { get; set; }
    public bool IsPublished { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Question> Questions { get; set; } = [];
}

public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuizId { get; set; }
    public Quiz? Quiz { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; } = QuestionType.Mcq;
    public string? ImageUrl { get; set; }
    public string? CodeSnippet { get; set; }
    public int Points { get; set; } = 10;
    public string? Explanation { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<QuestionOption> Options { get; set; } = [];
}

public class QuestionOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public Question? Question { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }
}

public class QuizAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuizId { get; set; }
    public Quiz? Quiz { get; set; }
    public Guid StudentUserId { get; set; }
    public ApplicationUser? StudentUser { get; set; }
    public int AttemptNumber { get; set; } = 1;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAt { get; set; }
    public int? Score { get; set; }
    public decimal? Percentage { get; set; }
    public bool? Passed { get; set; }
    public int XpAwarded { get; set; }
    public int? TimeTakenSeconds { get; set; }
    public ICollection<QuestionAnswer> Answers { get; set; } = [];
}

public class QuestionAnswer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuizAttemptId { get; set; }
    public QuizAttempt? QuizAttempt { get; set; }
    public Guid QuestionId { get; set; }
    public Question? Question { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public QuestionOption? SelectedOption { get; set; }
    public string? TextAnswer { get; set; }
    public bool? IsCorrect { get; set; }
    public int PointsEarned { get; set; }
    public DateTimeOffset AnsweredAt { get; set; } = DateTimeOffset.UtcNow;
}
