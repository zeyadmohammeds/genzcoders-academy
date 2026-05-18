using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Models;
using GenZCoders.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Services;

public class ApplicationService(
    AcademyDbContext db,
    UserManager<ApplicationUser> userManager,
    INotificationService notifications) : IApplicationService
{
    public async Task<CourseApplicationDto> SubmitAsync(SubmitCourseApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var course = await db.Courses.FirstOrDefaultAsync(x => x.Id == request.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        var student = await EnsureStudentAsync(request.StudentEmail, request.StudentName);
        var questions = await db.CourseApplicationQuestions
            .Where(x => x.CourseId == request.CourseId && x.IsActive && (x.CohortId == null || x.CohortId == request.CourseRoundId))
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var answers = request.Answers.ToDictionary(x => x.QuestionId, x => x.AnswerText);
        var correct = 0;
        var graded = 0;

        var application = new CourseApplication
        {
            CourseId = request.CourseId,
            CohortId = request.CourseRoundId,
            StudentUserId = student.Id,
            Status = ApplicationStatus.Submitted
        };

        foreach (var question in questions)
        {
            answers.TryGetValue(question.Id, out var answerText);
            var isCorrect = Grade(question, answerText ?? string.Empty);
            if (isCorrect.HasValue)
            {
                graded++;
                if (isCorrect.Value) correct++;
            }

            application.Answers.Add(new CourseApplicationAnswer
            {
                CourseApplicationQuestionId = question.Id,
                AnswerText = answerText ?? string.Empty,
                IsCorrect = isCorrect,
                ScoreAwarded = isCorrect == true ? 1 : 0
            });
        }

        application.ApplicationScore = graded == 0 ? 100 : decimal.Round(correct * 100m / graded, 2);
        application.QuestionsPassed = questions.Count == 0 || application.ApplicationScore >= 50;
        application.PaymentUnlocked = application.QuestionsPassed;
        application.Status = application.QuestionsPassed ? ApplicationStatus.PaymentPending : ApplicationStatus.UnderReview;

        db.CourseApplications.Add(application);
        await db.SaveChangesAsync(cancellationToken);

        await notifications.QueueAsync(student.Id, "Application received", application.PaymentUnlocked
            ? $"Your {course.Title} application passed. Payment is now open."
            : $"Your {course.Title} application was received and is waiting for review.",
            [NotificationChannel.InApp, NotificationChannel.Email, NotificationChannel.WhatsApp], cancellationToken);

        return ToDto(application, student.Email ?? string.Empty);
    }

    public async Task<CourseApplicationDto> MarkPaidAsync(Guid applicationId, MarkApplicationPaidRequest request, CancellationToken cancellationToken = default)
    {
        var application = await db.CourseApplications.Include(x => x.Course).Include(x => x.StudentUser).FirstOrDefaultAsync(x => x.Id == applicationId, cancellationToken)
            ?? throw new InvalidOperationException("Application not found.");

        if (!application.PaymentUnlocked)
        {
            throw new InvalidOperationException("Payment is locked until application questions pass or staff approve it.");
        }

        var order = new EnrollmentOrder
        {
            StudentUserId = application.StudentUserId,
            OrderType = OrderType.Single,
            SubtotalEgp = application.Course?.PriceEgp ?? request.AmountEgp,
            TotalAmountEgp = request.AmountEgp,
            PaymentMethod = request.PaymentMethod,
            PaymentReference = request.PaymentReference,
            PaymentStatus = PaymentStatus.Paid,
            PaidAt = DateTimeOffset.UtcNow
        };
        db.EnrollmentOrders.Add(order);

        application.EnrollmentOrder = order;
        application.PaymentCompleted = true;
        application.PaidAt = DateTimeOffset.UtcNow;
        application.Status = ApplicationStatus.UnderReview;

        var round = application.CohortId.HasValue
            ? await db.Cohorts.FirstOrDefaultAsync(x => x.Id == application.CohortId, cancellationToken)
            : null;

        if (round is { AutoAcceptPaidApplications: true, RequireEngineerApproval: false })
        {
            await AcceptApplicationAsync(application, null, "Auto accepted after payment.", cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        await notifications.QueueAsync(application.StudentUserId, "Payment received", "Your payment was received. Your application is now waiting for final academy approval.", [NotificationChannel.InApp, NotificationChannel.Email, NotificationChannel.WhatsApp], cancellationToken);
        return ToDto(application, application.StudentUser?.Email ?? string.Empty);
    }

    public async Task<CourseApplicationDto> ReviewAsync(Guid applicationId, Guid reviewerUserId, ApplicationReviewRequest request, CancellationToken cancellationToken = default)
    {
        var application = await db.CourseApplications.Include(x => x.Course).Include(x => x.StudentUser).FirstOrDefaultAsync(x => x.Id == applicationId, cancellationToken)
            ?? throw new InvalidOperationException("Application not found.");

        if (request.Accepted)
        {
            await AcceptApplicationAsync(application, reviewerUserId, request.Notes, cancellationToken);
        }
        else
        {
            application.Status = ApplicationStatus.Rejected;
            application.ReviewDecision = ApplicationReviewDecision.Rejected;
            application.ReviewedByUserId = reviewerUserId;
            application.ReviewNotes = request.Notes;
            application.ReviewedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        await notifications.QueueAsync(application.StudentUserId, request.Accepted ? "Application accepted" : "Application update", request.Accepted
            ? "You have been accepted. Your course room is now open."
            : "Your application was reviewed. Please check your dashboard for details.",
            [NotificationChannel.InApp, NotificationChannel.Email, NotificationChannel.WhatsApp], cancellationToken);

        return ToDto(application, application.StudentUser?.Email ?? string.Empty);
    }

    public async Task<IReadOnlyList<CourseApplicationDto>> PendingAsync(CancellationToken cancellationToken = default)
        => await db.CourseApplications
            .AsNoTracking()
            .Include(x => x.StudentUser)
            .Where(x => x.Status == ApplicationStatus.UnderReview || x.Status == ApplicationStatus.PaymentPending)
            .OrderBy(x => x.SubmittedAt)
            .Select(x => ToDto(x, x.StudentUser == null ? string.Empty : x.StudentUser.Email ?? string.Empty))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CourseApplicationDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await db.CourseApplications
            .AsNoTracking()
            .Include(x => x.StudentUser)
            .Where(x => x.StudentUserId == userId)
            .OrderByDescending(x => x.SubmittedAt)
            .Select(x => ToDto(x, x.StudentUser == null ? string.Empty : x.StudentUser.Email ?? string.Empty))
            .ToListAsync(cancellationToken);

    private async Task AcceptApplicationAsync(CourseApplication application, Guid? reviewerUserId, string? notes, CancellationToken cancellationToken)
    {
        application.Status = ApplicationStatus.Accepted;
        application.ReviewDecision = ApplicationReviewDecision.Accepted;
        application.ReviewedByUserId = reviewerUserId;
        application.ReviewNotes = notes;
        application.ReviewedAt = DateTimeOffset.UtcNow;
        application.AcceptedAt = DateTimeOffset.UtcNow;

        var enrollment = await db.Enrollments.FirstOrDefaultAsync(x => x.CourseId == application.CourseId && x.StudentUserId == application.StudentUserId, cancellationToken);
        if (enrollment is null)
        {
            enrollment = new Enrollment
            {
                CourseId = application.CourseId,
                CohortId = application.CohortId,
                StudentUserId = application.StudentUserId,
                EnrollmentOrderId = application.EnrollmentOrderId,
                EnrollmentStatus = EnrollmentStatus.Active,
                Status = "active",
                UnitPriceEgp = application.Course?.PriceEgp ?? 0,
                FinalPriceEgp = application.Course?.PriceEgp ?? 0
            };
            db.Enrollments.Add(enrollment);
        }

        if (application.CohortId.HasValue)
        {
            var cohort = await db.Cohorts
                .Include(c => c.CohortEnrollments)
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.Id == application.CohortId.Value, cancellationToken);

            if (cohort != null)
            {
                // Check if current round is full
                if (cohort.MaxStudents > 0 && cohort.CohortEnrollments.Count >= cohort.MaxStudents)
                {
                    // Create a new round
                    var newCohort = new Cohort
                    {
                        CourseId = cohort.CourseId,
                        Name = $"{cohort.Course!.Title} - Round {(await db.Cohorts.CountAsync(c => c.CourseId == cohort.CourseId, cancellationToken)) + 1}",
                        Slug = $"{cohort.Slug}-overflow-{Guid.NewGuid().ToString("N")[..6]}",
                        StartDate = cohort.StartDate,
                        MaxStudents = cohort.MaxStudents,
                        Status = CohortStatus.Upcoming,
                        IsEnrollmentOpen = true,
                        AutoAcceptPaidApplications = cohort.AutoAcceptPaidApplications,
                        RequireEngineerApproval = cohort.RequireEngineerApproval
                    };
                    db.Cohorts.Add(newCohort);
                    await db.SaveChangesAsync(cancellationToken);
                    
                    // Assign application and enrollment to the new round
                    application.CohortId = newCohort.Id;
                    enrollment.CohortId = newCohort.Id;
                    cohort = newCohort;
                }

                if (!await db.CohortEnrollments.AnyAsync(x => x.CohortId == cohort.Id && x.StudentUserId == application.StudentUserId, cancellationToken))
                {
                    db.CohortEnrollments.Add(new CohortEnrollment
                    {
                        CohortId = cohort.Id,
                        StudentUserId = application.StudentUserId,
                        Enrollment = enrollment
                    });
                }
            }
        }
    }

    private async Task<ApplicationUser> EnsureStudentAsync(string email, string name)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(normalized);
        if (user is not null) return user;

        var names = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        user = new ApplicationUser
        {
            UserName = normalized,
            Email = normalized,
            FirstName = names.FirstOrDefault() ?? "Student",
            LastName = names.Length > 1 ? names[1] : string.Empty,
            RoleKey = AcademyRole.Student
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        await userManager.AddToRoleAsync(user, AcademyRole.Student);
        return user;
    }

    private static bool? Grade(CourseApplicationQuestion question, string answer)
    {
        if (!question.AutoGrade || string.IsNullOrWhiteSpace(question.CorrectAnswer)) return null;
        return string.Equals(question.CorrectAnswer.Trim(), answer.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static CourseApplicationDto ToDto(CourseApplication application, string email)
        => new(application.Id, application.CourseId, application.CohortId, email, application.Status, application.QuestionsPassed, application.PaymentUnlocked, application.PaymentCompleted, application.PaymentReceiptUrl, application.PaymentMethod, application.PaymentReceiptPendingReview, application.ReviewDecision, application.ApplicationScore);
}
