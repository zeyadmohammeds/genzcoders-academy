using GenZCoders.DTOs;
using GenZCoders.Models;
using GenZCoders.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GenZCoders.Controllers;

[ApiController]
[Authorize]
[Route("api/cart")]
public class CartController(ICartService cart) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await cart.GetAsync(CurrentUserId(), cancellationToken));

    [HttpPost("items")]
    public async Task<IActionResult> AddCourse(AddCartItemRequest request, CancellationToken cancellationToken)
        => Ok(await cart.AddCourseAsync(CurrentUserId(), request, cancellationToken));

    [HttpDelete("items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid itemId, CancellationToken cancellationToken)
    {
        await cart.RemoveItemAsync(CurrentUserId(), itemId, cancellationToken);
        return Ok();
    }

    [HttpPost("bundle")]
    public async Task<IActionResult> AddBundle(CancellationToken cancellationToken)
        => Ok(await cart.AddBundleAsync(CurrentUserId(), cancellationToken));

    [HttpPost("promo")]
    public async Task<IActionResult> ApplyPromo(ApplyPromoRequest request, CancellationToken cancellationToken)
        => Ok(await cart.ApplyPromoAsync(CurrentUserId(), request.Code, cancellationToken));

    [HttpPost("referral")]
    public async Task<IActionResult> ApplyReferral(ApplyReferralRequest request, CancellationToken cancellationToken)
        => Ok(await cart.ApplyReferralAsync(CurrentUserId(), request.ReferralCode, cancellationToken));

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(CheckoutCartRequest request, CancellationToken cancellationToken)
        => Ok(new { orderId = await cart.CheckoutAsync(CurrentUserId(), request, cancellationToken) });

    private Guid CurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
}
