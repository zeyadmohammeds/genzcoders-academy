using GenZCoders.Data;
using GenZCoders.DTOs;
using System.Security.Cryptography;
using System.Text;
using GenZCoders.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(AcademyDbContext db, IConfiguration configuration, ILogger<PaymentsController> logger) : ControllerBase
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

    /// <summary>
    /// Paymob Processed Callback URL (Webhook).
    /// Put this in Paymob: https://yourdomain.com/api/payments/paymob-webhook
    /// </summary>
    [HttpPost("paymob-webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymobWebhook([FromQuery] string hmac, [FromBody] dynamic payload, CancellationToken cancellationToken)
    {
        // 1. Get HMAC secret from configuration
        var hmacSecret = configuration["Paymob:HmacSecret"];
        
        // 2. Validate HMAC (In production, build the string exactly as Paymob specifies: amount_cents, created_at, currency, error_occured, has_parent_transaction, id, integration_id, is_3d_secure, is_auth, is_capture, is_refunded, is_standalone_payment, is_voided, order.id, owner, pending, source_data.pan, source_data.sub_type, source_data.type, success)
        // Note: For full HMAC validation logic, you need to extract the exact fields from the payload object.
        // For now, we accept the webhook to prevent Paymob from retrying, but you MUST implement HMAC before going live.
        logger.LogInformation("Received Paymob Webhook. Payload: {Payload}", (string)payload.ToString());

        var obj = System.Text.Json.JsonDocument.Parse(payload.ToString()).RootElement;
        var success = obj.GetProperty("obj").GetProperty("success").GetBoolean();
        var orderId = obj.GetProperty("obj").GetProperty("order").GetProperty("id").GetInt32();

        if (success)
        {
            // Payment succeeded! You should find the CourseApplication linked to this Paymob Order ID.
            // Mark it as paid:
            // var application = await db.CourseApplications.FirstOrDefaultAsync(x => x.PaymobOrderId == orderId);
            // application.PaymentCompleted = true;
            // await db.SaveChangesAsync();
            logger.LogInformation("Paymob payment {OrderId} was SUCCESSFUL.", (object)orderId);
        }
        else
        {
            logger.LogWarning("Paymob payment {OrderId} FAILED.", (object)orderId);
        }

        return Ok();
    }

    /// <summary>
    /// Paymob Response Callback URL (User Redirect).
    /// Put this in Paymob: https://yourdomain.com/api/payments/paymob-response
    /// </summary>
    [HttpGet("paymob-response")]
    [AllowAnonymous]
    public IActionResult PaymobResponse([FromQuery] bool success, [FromQuery] int order)
    {
        // This is where Paymob redirects the user's browser after payment.
        var frontendUrl = configuration["Frontend:Url"]?.TrimEnd('/');
        
        if (success)
        {
            // Redirect the user to their dashboard or a success page
            return Redirect($"{frontendUrl}/my-courses?payment=success&order={order}");
        }
        else
        {
            // Redirect to a failure page
            return Redirect($"{frontendUrl}/applications?payment=failed&order={order}");
        }
    }
}
