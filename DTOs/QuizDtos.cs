using GenZCoders.Models;

namespace GenZCoders.DTOs;

public record QuizCreateRequest(
    Guid? CourseSessionId,
    Guid? CourseRoundId,
    string Title,
    QuizType QuizType,
    int? TimeLimitMinutes,
    int MaxAttempts,
    int PassScore,
    int XpReward,
    bool IsPublished);

public record QuestionCreateRequest(
    Guid QuizId,
    string QuestionText,
    QuestionType QuestionType,
    string? ImageUrl,
    string? CodeSnippet,
    int Points,
    string? Explanation,
    int SortOrder,
    IReadOnlyList<QuestionOptionCreateRequest> Options);

public record QuestionOptionCreateRequest(string OptionText, bool IsCorrect, int SortOrder);

public record QuizAnswerInput(Guid QuestionId, Guid? SelectedOptionId, string? TextAnswer);

public record SubmitQuizAttemptRequest(Guid QuizId, Guid StudentUserId, IReadOnlyList<QuizAnswerInput> Answers);
