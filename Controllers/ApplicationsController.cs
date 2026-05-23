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
[Route("api/applications")]
public class ApplicationsController(IApplicationService applications, AcademyDbContext db) : ControllerBase
{
    [HttpGet("questions")]
    public async Task<IActionResult> Questions([FromQuery] Guid courseId, [FromQuery] Guid? courseRoundId, CancellationToken cancellationToken)
        => Ok(await db.CourseApplicationQuestions
            .AsNoTracking()
            .Where(x => x.CourseId == courseId && x.IsActive && (x.CohortId == null || x.CohortId == courseRoundId))
            .OrderBy(x => x.SortOrder)
            .Select(x => new ApplicationQuestionDto(
                x.Id,
                x.CourseId,
                x.CohortId,
                x.QuestionType,
                x.QuestionText,
                x.HelpText,
                x.OptionsJson,
                x.IsRequired,
                x.AutoGrade,
                x.SortOrder))
            .ToListAsync(cancellationToken));

    [Authorize(Roles = AcademyRole.Student)]
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitCourseApplicationRequest request, CancellationToken cancellationToken)
        => Ok(await applications.SubmitAsync(request, cancellationToken));

    [Authorize(Policy = "AcademyStaff")]
    [HttpPost("questions")]
    public async Task<IActionResult> AddQuestion(ApplicationQuestionCreateRequest request, CancellationToken cancellationToken)
    {
        var question = new CourseApplicationQuestion
        {
            CourseId = request.CourseId,
            CohortId = request.CourseRoundId,
            QuestionType = request.QuestionType,
            QuestionText = request.QuestionText,
            HelpText = request.HelpText,
            OptionsJson = request.OptionsJson,
            CorrectAnswer = request.CorrectAnswer,
            AutoGrade = request.AutoGrade,
            SortOrder = request.SortOrder
        };
        db.CourseApplicationQuestions.Add(question);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { question.Id });
    }

    [Authorize(Roles = AcademyRole.Student)]
    [HttpPost("{applicationId:guid}/payment")]
    public async Task<IActionResult> MarkPaid(Guid applicationId, MarkApplicationPaidRequest request, CancellationToken cancellationToken)
        => Ok(await applications.MarkPaidAsync(applicationId, request, cancellationToken));

    [Authorize(Roles = AcademyRole.Student)]
    [HttpPost("{applicationId:guid}/payment-receipt")]
    public async Task<IActionResult> UploadPaymentReceipt(Guid applicationId, [FromBody] UploadPaymentReceiptRequest request, CancellationToken cancellationToken)
    {
        var app = await db.CourseApplications.FirstOrDefaultAsync(x => x.Id == applicationId && x.StudentUserId == CurrentUserId(), cancellationToken);
        if (app is null) return NotFound();
        if (app.Status != ApplicationStatus.Accepted) return BadRequest("Application must be accepted before payment.");

        app.PaymentReceiptUrl = request.ReceiptUrl;
        app.PaymentMethod = request.PaymentMethod;
        app.PaymentReceiptPendingReview = true;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { success = true });
    }

    [Authorize(Policy = "CourseManagers")]
    [HttpPost("{applicationId:guid}/approve-payment")]
    public async Task<IActionResult> ApprovePaymentReceipt(Guid applicationId, CancellationToken cancellationToken)
    {
        var app = await db.CourseApplications.Include(x => x.Course).FirstOrDefaultAsync(x => x.Id == applicationId, cancellationToken);
        if (app is null) return NotFound();
        if (!app.PaymentReceiptPendingReview) return BadRequest("No pending payment receipt.");

        app.PaymentReceiptPendingReview = false;
        app.PaymentCompleted = true;
        app.PaidAt = DateTimeOffset.UtcNow;
        app.Status = ApplicationStatus.Paid;

        var price = app.Course?.PriceEgp ?? 0;

        // 1. Create the EnrollmentOrder (adds to total payment count and revenue in dashboard)
        var order = new EnrollmentOrder
        {
            StudentUserId = app.StudentUserId,
            OrderType = OrderType.Single,
            SubtotalEgp = price,
            TotalAmountEgp = price,
            PaymentMethod = app.PaymentMethod ?? "Manual",
            PaymentReference = "Receipt Approved",
            PaymentStatus = PaymentStatus.Paid,
            PaidAt = DateTimeOffset.UtcNow
        };
        db.EnrollmentOrders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        // 2. Create the PaymentTransaction
        var transaction = new PaymentTransaction
        {
            EnrollmentOrderId = order.Id,
            Provider = "manual",
            ProviderTransactionId = app.PaymentMethod ?? "ReceiptApproved",
            AmountEgp = price,
            Status = PaymentStatus.Paid,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.PaymentTransactions.Add(transaction);

        // 3. Create or activate the main Enrollment record
        var existingEnrollment = await db.Enrollments.FirstOrDefaultAsync(x => x.CourseId == app.CourseId && x.StudentUserId == app.StudentUserId, cancellationToken);
        if (existingEnrollment is null)
        {
            existingEnrollment = new Enrollment
            {
                CourseId = app.CourseId,
                CohortId = app.CohortId,
                StudentUserId = app.StudentUserId,
                EnrollmentOrderId = order.Id,
                EnrollmentStatus = EnrollmentStatus.Active,
                Status = "active",
                UnitPriceEgp = price,
                FinalPriceEgp = price,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Enrollments.Add(existingEnrollment);
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            existingEnrollment.EnrollmentOrderId = order.Id;
            existingEnrollment.EnrollmentStatus = EnrollmentStatus.Active;
            existingEnrollment.Status = "active";
            existingEnrollment.CohortId = app.CohortId;
        }

        // 4. Enroll the student in the cohort and increment student count
        if (app.CohortId.HasValue)
        {
            var existingCohortEnrollment = await db.CohortEnrollments
                .FirstOrDefaultAsync(e => e.CohortId == app.CohortId.Value && e.StudentUserId == app.StudentUserId, cancellationToken);
            if (existingCohortEnrollment is null)
            {
                var cohortEnrollment = new CohortEnrollment
                {
                    CohortId = app.CohortId.Value,
                    StudentUserId = app.StudentUserId,
                    EnrollmentId = existingEnrollment.Id,
                    EnrolledAt = DateTimeOffset.UtcNow,
                };
                db.CohortEnrollments.Add(cohortEnrollment);

                var cohort = await db.Cohorts.FindAsync(new object[] { app.CohortId.Value }, cancellationToken);
                if (cohort != null)
                {
                    cohort.CurrentStudents += 1;
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { success = true });
    }

    [Authorize(Policy = "CourseManagers")]
    [HttpPost("{applicationId:guid}/reject-payment")]
    public async Task<IActionResult> RejectPaymentReceipt(Guid applicationId, CancellationToken cancellationToken)
    {
        var app = await db.CourseApplications.Include(x => x.Course).FirstOrDefaultAsync(x => x.Id == applicationId, cancellationToken);
        if (app is null) return NotFound();
        if (!app.PaymentReceiptPendingReview) return BadRequest("No pending payment receipt.");

        app.PaymentReceiptPendingReview = false;
        app.PaymentCompleted = false;
        app.Status = ApplicationStatus.PaymentPending; // back to payment pending so student can try again

        var price = app.Course?.PriceEgp ?? 0;
        var order = new EnrollmentOrder
        {
            StudentUserId = app.StudentUserId,
            OrderType = OrderType.Single,
            SubtotalEgp = price,
            TotalAmountEgp = price,
            PaymentMethod = app.PaymentMethod ?? "Manual",
            PaymentReference = "Receipt Rejected",
            PaymentStatus = PaymentStatus.Failed,
            PaidAt = null
        };
        db.EnrollmentOrders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        var transaction = new PaymentTransaction
        {
            EnrollmentOrderId = order.Id,
            Provider = "manual",
            ProviderTransactionId = "Rejected",
            AmountEgp = price,
            Status = PaymentStatus.Failed,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.PaymentTransactions.Add(transaction);

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { success = true });
    }

    [Authorize(Policy = "CourseManagers")]
    [HttpPost("{applicationId:guid}/review")]
    public async Task<IActionResult> Review(Guid applicationId, ApplicationReviewRequest request, CancellationToken cancellationToken)
        => Ok(await applications.ReviewAsync(applicationId, CurrentUserId(), request, cancellationToken));

    [Authorize(Policy = "CourseManagers")]
    [HttpGet("{applicationId:guid}")]
    public async Task<IActionResult> GetApplicationDetails(Guid applicationId, CancellationToken cancellationToken)
    {
        var app = await db.CourseApplications
            .AsNoTracking()
            .Include(x => x.StudentUser)
            .Include(x => x.Course)
            .Include(x => x.Cohort)
            .Include(x => x.EnrollmentOrder)
                .ThenInclude(x => x.PromoCode)
            .Include(x => x.Answers)
                .ThenInclude(ans => ans.Question)
            .FirstOrDefaultAsync(x => x.Id == applicationId, cancellationToken);

        if (app is null) return NotFound();

        var price = app.Course?.PriceEgp ?? 0;
        var orderAmount = app.EnrollmentOrder?.TotalAmountEgp;
        var subtotal = app.EnrollmentOrder?.SubtotalEgp;
        var discount = app.EnrollmentOrder?.DiscountAmountEgp;

        return Ok(new
        {
            app.Id,
            app.CourseId,
            CourseTitle = app.Course?.Title ?? "",
            CoursePriceEgp = price,
            AmountPaid = orderAmount,
            SubtotalEgp = subtotal,
            DiscountEgp = discount > 0 ? discount : (decimal?)null,
            DiscountReason = app.EnrollmentOrder?.DiscountReason,
            PromoCode = app.EnrollmentOrder?.PromoCode?.Code,
            PaymentReference = app.EnrollmentOrder?.PaymentReference,
            RoundName = app.Cohort?.Name ?? "",
            StudentName = app.StudentUser == null ? "" : app.StudentUser.FirstName + " " + app.StudentUser.LastName,
            StudentEmail = app.StudentUser?.Email ?? "",
            app.QuestionsPassed,
            app.PaymentUnlocked,
            app.PaymentCompleted,
            app.PaymentReceiptUrl,
            app.PaymentMethod,
            app.PaymentReceiptPendingReview,
            app.ApplicationScore,
            Status = app.Status.ToString(),
            SubmittedAt = app.SubmittedAt.ToString("yyyy-MM-dd HH:mm"),
            Answers = app.Answers.OrderBy(ans => ans.Question != null ? ans.Question.SortOrder : 0).Select(ans => new
            {
                ans.Id,
                QuestionText = ans.Question != null ? ans.Question.QuestionText : "",
                QuestionType = ans.Question != null ? ans.Question.QuestionType.ToString() : "",
                OptionsJson = ans.Question != null ? ans.Question.OptionsJson : "[]",
                ans.AnswerText,
                ans.IsCorrect
            }).ToList()
        });
    }

    [Authorize(Policy = "CourseManagers")]
    [HttpGet("pending")]
    public async Task<IActionResult> Pending(CancellationToken cancellationToken)
        => Ok(await applications.PendingAsync(cancellationToken));

    [Authorize(Policy = "CourseManagers")]
    [HttpGet("pending-payments")]
    public async Task<IActionResult> PendingPayments(CancellationToken cancellationToken)
    {
        var apps = await db.CourseApplications
            .AsNoTracking()
            .Where(x => x.PaymentReceiptPendingReview)
            .Include(x => x.StudentUser)
            .Include(x => x.Course)
            .Select(x => new
            {
                x.Id,
                x.CourseId,
                CourseTitle = x.Course!.Title,
                StudentName = x.StudentUser!.FirstName + " " + x.StudentUser.LastName,
                StudentEmail = x.StudentUser.Email,
                x.PaymentMethod,
                x.PaymentReceiptUrl,
                SubmittedAt = x.SubmittedAt.ToString("yyyy-MM-dd HH:mm")
            })
            .ToListAsync(cancellationToken);
        return Ok(apps);
    }

    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> My(CancellationToken cancellationToken)
        => Ok(await applications.GetByUserAsync(CurrentUserId(), cancellationToken));

    private Guid CurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
}
