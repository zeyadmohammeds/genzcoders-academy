using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(AcademyDbContext db) : ControllerBase
{
    [Authorize(Roles = AcademyRole.Student)]
    [HttpGet("application/{applicationId:guid}/checkout")]
    public async Task<IActionResult> Checkout(Guid applicationId, CancellationToken cancellationToken)
    {
        var application = await db.CourseApplications.Include(x => x.Course).FirstOrDefaultAsync(x => x.Id == applicationId, cancellationToken);
        if (application is null)
        {
            return NotFound();
        }

        if (!application.PaymentUnlocked)
        {
            return BadRequest("Payment is locked until application questions pass or staff approve it.");
        }

        return Ok(new
        {
            application.Id,
            application.CourseId,
            courseTitle = application.Course?.Title,
            amountEgp = application.Course?.PriceEgp ?? 0,
            supportedProviders = new[] { "manual", "paymob", "fawry" },
            nextApi = $"/api/applications/{application.Id}/payment"
        });
    }

    [Authorize(Policy = "CourseManagers")]
    [HttpPost("transactions/{transactionId:guid}/status")]
    public async Task<IActionResult> UpdateTransaction(Guid transactionId, PaymentStatus status, CancellationToken cancellationToken)
    {
        var transaction = await db.PaymentTransactions.FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken);
        if (transaction is null) return NotFound();
        transaction.Status = status;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { transaction.Id, transaction.Status });
    }
}
