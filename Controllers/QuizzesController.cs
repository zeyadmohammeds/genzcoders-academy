using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Models;
using GenZCoders.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GenZCoders.Controllers;

[ApiController]
[Route("api/quizzes")]
public class QuizzesController(AcademyDbContext db, INotificationService notifications) : ControllerBase
{
    [Authorize(Policy = "AcademyStaff")]
    [HttpPost]
    public async Task<IActionResult> Create(QuizCreateRequest request, CancellationToken cancellationToken)
    {
        var quiz = new Quiz
        {
            CourseSessionId = request.CourseSessionId,
            CohortId = request.CourseRoundId,
            Title = request.Title,
            QuizType = request.QuizType,
            TimeLimitMinutes = request.TimeLimitMinutes,
            MaxAttempts = request.MaxAttempts,
            PassScore = request.PassScore,
            XpReward = request.XpReward,
            IsPublished = request.IsPublished,
            CreatedByUserId = CurrentUserIdOrNull()
        };
        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { quiz.Id });
    }

    [Authorize(Policy = "AcademyStaff")]
    [HttpPost("questions")]
    public async Task<IActionResult> AddQuestion(QuestionCreateRequest request, CancellationToken cancellationToken)
    {
        var question = new Question
        {
            QuizId = request.QuizId,
            QuestionText = request.QuestionText,
            QuestionType = request.QuestionType,
            ImageUrl = request.ImageUrl,
            CodeSnippet = request.CodeSnippet,
            Points = request.Points,
            Explanation = request.Explanation,
            SortOrder = request.SortOrder,
            Options = request.Options.Select(x => new QuestionOption
            {
                OptionText = x.OptionText,
                IsCorrect = x.IsCorrect,
                SortOrder = x.SortOrder
            }).ToList()
        };
        db.Questions.Add(question);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { question.Id });
    }

    [Authorize(Roles = AcademyRole.Student)]
    [HttpPost("attempts")]
    public async Task<IActionResult> SubmitAttempt(SubmitQuizAttemptRequest request, CancellationToken cancellationToken)
    {
        var quiz = await db.Quizzes.Include(x => x.Questions).ThenInclude(x => x.Options).FirstOrDefaultAsync(x => x.Id == request.QuizId, cancellationToken)
            ?? throw new InvalidOperationException("Quiz not found.");

        var attemptNumber = await db.QuizAttempts.CountAsync(x => x.QuizId == request.QuizId && x.StudentUserId == request.StudentUserId, cancellationToken) + 1;
        if (attemptNumber > quiz.MaxAttempts)
        {
            return BadRequest("Maximum attempts reached.");
        }

        var answerMap = request.Answers.ToDictionary(x => x.QuestionId);
        var total = quiz.Questions.Sum(x => x.Points);
        var earned = 0;
        var attempt = new QuizAttempt
        {
            QuizId = request.QuizId,
            StudentUserId = request.StudentUserId,
            AttemptNumber = attemptNumber,
            SubmittedAt = DateTimeOffset.UtcNow
        };

        foreach (var question in quiz.Questions)
        {
            answerMap.TryGetValue(question.Id, out var answer);
            var correctOption = question.Options.FirstOrDefault(x => x.IsCorrect);
            var isCorrect = answer?.SelectedOptionId != null && correctOption?.Id == answer.SelectedOptionId;
            var points = isCorrect ? question.Points : 0;
            earned += points;

            attempt.Answers.Add(new QuestionAnswer
            {
                QuestionId = question.Id,
                SelectedOptionId = answer?.SelectedOptionId,
                TextAnswer = answer?.TextAnswer,
                IsCorrect = isCorrect,
                PointsEarned = points
            });
        }

        attempt.Score = earned;
        attempt.Percentage = total == 0 ? 0 : decimal.Round(earned * 100m / total, 2);
        attempt.Passed = attempt.Percentage >= quiz.PassScore;
        attempt.XpAwarded = attempt.Passed == true ? quiz.XpReward : 0;
        db.QuizAttempts.Add(attempt);

        if (attempt.XpAwarded > 0)
        {
            db.XpTransactions.Add(new XpTransaction
            {
                StudentUserId = request.StudentUserId,
                Amount = attempt.XpAwarded,
                SourceType = XpSourceType.Quiz,
                SourceId = attempt.Id,
                Description = $"Quiz passed: {quiz.Title}"
            });

            var profile = await db.StudentProfiles.FirstOrDefaultAsync(x => x.UserId == request.StudentUserId, cancellationToken);
            if (profile is not null)
            {
                profile.TotalXp += attempt.XpAwarded;
                profile.LastActiveAt = DateTimeOffset.UtcNow;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await notifications.QueueAsync(request.StudentUserId, "Quiz submitted", $"Quiz submitted. Score: {attempt.Percentage}%. XP: {attempt.XpAwarded}.", [NotificationChannel.InApp, NotificationChannel.Email], cancellationToken);
        return Ok(new { attempt.Id, attempt.Score, attempt.Percentage, attempt.Passed, attempt.XpAwarded });
    }

    private Guid? CurrentUserIdOrNull()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
