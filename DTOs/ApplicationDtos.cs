using GenZCoders.Models;

namespace GenZCoders.DTOs;

public record ApplicationAnswerInput(Guid QuestionId, string AnswerText);

public record SubmitCourseApplicationRequest(
    Guid CourseId,
    Guid? CourseRoundId,
    string StudentEmail,
    string StudentName,
    IReadOnlyList<ApplicationAnswerInput> Answers);

public record ApplicationReviewRequest(bool Accepted, string? Notes);

public record MarkApplicationPaidRequest(string PaymentMethod, string PaymentReference, decimal AmountEgp);
public record UploadPaymentReceiptRequest(string ReceiptUrl, string PaymentMethod);

public record ApplicationQuestionCreateRequest(
    Guid CourseId,
    Guid? CourseRoundId,
    ApplicationQuestionType QuestionType,
    string QuestionText,
    string? HelpText,
    string OptionsJson,
    string? CorrectAnswer,
    bool AutoGrade,
    int SortOrder);

public record ApplicationQuestionDto(
    Guid Id,
    Guid CourseId,
    Guid? CourseRoundId,
    ApplicationQuestionType QuestionType,
    string QuestionText,
    string? HelpText,
    string OptionsJson,
    bool IsRequired,
    bool AutoGrade,
    int SortOrder);

public record CourseApplicationDto(
    Guid Id,
    Guid CourseId,
    Guid? CourseRoundId,
    string StudentEmail,
    ApplicationStatus Status,
    bool QuestionsPassed,
    bool PaymentUnlocked,
    bool PaymentCompleted,
    string? PaymentReceiptUrl,
    string? PaymentMethod,
    bool PaymentReceiptPendingReview,
    ApplicationReviewDecision ReviewDecision,
    decimal ApplicationScore);
