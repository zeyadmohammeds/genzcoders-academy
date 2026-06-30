using GenZCoders.Data;
using GenZCoders.DTOs;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    public async Task<IActionResult> PaymobWebhook(CancellationToken cancellationToken)
    {
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken);
        }

        var hmacSecret = configuration["Paymob:HmacSecret"];
        if (string.IsNullOrWhiteSpace(hmacSecret))
        {
            logger.LogWarning("Paymob HMAC secret not configured - rejecting webhook");
            return Unauthorized();
        }

        var isValid = ValidatePaymobHmac(rawBody, hmacSecret);
        if (!isValid)
        {
            logger.LogWarning("Paymob webhook HMAC validation failed - rejecting");
            return Unauthorized(new { error = "Invalid HMAC signature" });
        }

        logger.LogInformation("Received Paymob Webhook (HMAC verified)");

        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;
        var obj = root.GetProperty("obj");
        var success = obj.GetProperty("success").GetBoolean();
        var orderId = obj.GetProperty("order").GetProperty("id").GetInt32();
        var amountCents = obj.GetProperty("amount_cents").GetInt32();
        var transactionId = obj.GetProperty("id").GetInt32().ToString();

        if (success)
        {
            var application = await db.CourseApplications
                .FirstOrDefaultAsync(x => x.PaymobOrderId == orderId, cancellationToken);

            if (application is not null)
            {
                application.ApplicationStatus = ApplicationStatus.Paid;
                db.PaymentTransactions.Add(new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    EnrollmentOrderId = null,
                    Status = PaymentStatus.Paid,
                    AmountEgp = amountCents / 100m,
                    Reference = transactionId,
                    Gateway = "paymob",
                    CreatedAt = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Paymob payment {OrderId} succeeded - application {AppId} marked as paid", orderId, application.Id);
            }
            else
            {
                logger.LogWarning("Paymob payment {OrderId} succeeded but no matching application found", orderId);
            }
        }
        else
        {
            logger.LogWarning("Paymob payment {OrderId} FAILED", orderId);
        }

        return Ok();
    }

    private static bool ValidatePaymobHmac(string rawBody, string secret)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var obj = doc.RootElement.GetProperty("obj");

            var fields = new[]
            {
                "amount_cents", "created_at", "currency", "error_occured",
                "has_parent_transaction", "id", "integration_id", "is_3d_secure",
                "is_auth", "is_capture", "is_refunded", "is_standalone_payment",
                "is_voided", "order.id", "owner", "pending",
                "source_data.pan", "source_data.sub_type", "source_data.type", "success"
            };

            var hmacString = string.Join("", fields.Select(f => GetNestedValue(obj, f)));
            var computedHmac = HMACSHA256.HashData(Encoding.UTF8.GetBytes(hmacString), Encoding.UTF8.GetBytes(secret));
            var computedHex = Convert.ToHexString(computedHmac).ToLowerInvariant();

            var receivedHmac = (doc.RootElement.TryGetProperty("hmac", out var hmacEl) ? hmacEl.GetString() : null)
                ?? obj.TryGetProperty("hmac", out var objHmac) ? objHmac.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(receivedHmac)) return false;

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHex),
                Encoding.UTF8.GetBytes(receivedHmac.ToLowerInvariant()));
        }
        catch
        {
            return false;
        }
    }

    private static string GetNestedValue(JsonElement element, string path)
    {
        var parts = path.Split('.');
        JsonElement current = element;
        foreach (var part in parts)
        {
            if (!current.TryGetProperty(part, out current)) return "";
        }
        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString() ?? "",
            JsonValueKind.Number => current.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "",
            _ => current.GetRawText()
        };
    }

    /// <summary>
    /// Fawry payment callback (webhook).
    /// Configure in Fawry dashboard: https://yourdomain.com/api/payments/fawry-callback
    /// </summary>
    [HttpPost("fawry-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> FawryCallback(CancellationToken cancellationToken)
    {
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken);
        }

        logger.LogInformation("Received Fawry callback: {Body}", rawBody);

        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;
            var statusCode = root.GetProperty("statusCode").GetInt32();
            var orderRef = root.GetProperty("merchantRefNumber").GetString() ?? "";
            var paymentRef = root.GetProperty("referenceNumber").GetString() ?? "";
            var amount = root.GetProperty("paymentAmount").GetDecimal();

            var securityKey = configuration["Fawry:SecurityKey"];
            var merchantCode = configuration["Fawry:MerchantCode"];

            if (!string.IsNullOrWhiteSpace(securityKey))
            {
                var signature = root.TryGetProperty("signature", out var sigEl) ? sigEl.GetString() : null;
                if (!string.IsNullOrWhiteSpace(signature))
                {
                    var computedSig = ComputeFawrySignature(orderRef, paymentRef, amount, statusCode, securityKey, merchantCode);
                    if (!CryptographicOperations.FixedTimeEquals(
                            Encoding.UTF8.GetBytes(signature),
                            Encoding.UTF8.GetBytes(computedSig)))
                    {
                        logger.LogWarning("Fawry callback signature mismatch - rejecting");
                        return Unauthorized(new { error = "Invalid signature" });
                    }
                }
            }

            if (statusCode == 200)
            {
                if (Guid.TryParse(orderRef, out var appId))
                {
                    var application = await db.CourseApplications.FindAsync([appId], cancellationToken);
                    if (application is not null)
                    {
                        application.ApplicationStatus = ApplicationStatus.Paid;
                        db.PaymentTransactions.Add(new PaymentTransaction
                        {
                            Id = Guid.NewGuid(),
                            EnrollmentOrderId = null,
                            Status = PaymentStatus.Paid,
                            AmountEgp = amount,
                            Reference = paymentRef,
                            Gateway = "fawry",
                            CreatedAt = DateTimeOffset.UtcNow
                        });
                        await db.SaveChangesAsync(cancellationToken);
                        logger.LogInformation("Fawry payment {Ref} succeeded - application {AppId} paid", paymentRef, appId);
                    }
                }
            }
            else
            {
                logger.LogWarning("Fawry payment failed - statusCode: {Code}, ref: {Ref}", statusCode, paymentRef);
            }
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse Fawry callback payload");
        }

        return Ok();
    }

    private static string ComputeFawrySignature(string merchantRefNumber, string referenceNumber, decimal amount, int statusCode, string securityKey, string? merchantCode)
    {
        var data = $"{merchantRefNumber}{referenceNumber}{(long)(amount * 100)}{statusCode}{securityKey}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
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
