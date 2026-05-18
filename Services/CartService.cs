using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Models;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Services;

public class CartService(AcademyDbContext db, IReferralService referrals) : ICartService
{
    public async Task<CartDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
        => ToDto(await GetOrCreateCartAsync(userId, cancellationToken));

    public async Task<CartDto> AddCourseAsync(Guid userId, AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);
        var course = await db.Courses.FirstOrDefaultAsync(x => x.Id == request.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        if (cart.Items.All(x => x.CourseId != course.Id))
        {
            cart.Items.Add(new ShoppingCartItem
            {
                CourseId = course.Id,
                CohortId = request.CourseRoundId,
                UnitPriceEgp = course.PriceEgp,
                FinalPriceEgp = course.PriceEgp
            });
        }

        await RecalculateAsync(cart, cancellationToken);
        return ToDto(cart);
    }

    public async Task<CartDto> AddBundleAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);
        var courses = await db.Courses.Where(x => x.IsActive && !x.IsDeleted).ToListAsync(cancellationToken);
        foreach (var course in courses)
        {
            if (cart.Items.All(x => x.CourseId != course.Id))
            {
                cart.Items.Add(new ShoppingCartItem
                {
                    CourseId = course.Id,
                    UnitPriceEgp = course.PriceEgp,
                    FinalPriceEgp = course.PriceEgp,
                    IsBundleItem = true
                });
            }
        }

        await RecalculateAsync(cart, cancellationToken);
        return ToDto(cart);
    }

    public async Task<CartDto> ApplyPromoAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);
        cart.PromoCode = code.Trim().ToUpperInvariant();
        await RecalculateAsync(cart, cancellationToken);
        return ToDto(cart);
    }

    public async Task<CartDto> ApplyReferralAsync(Guid userId, string referralCode, CancellationToken cancellationToken = default)
    {
        var referrer = await referrals.ResolveReferrerAsync(referralCode, cancellationToken);
        if (referrer is null || referrer == userId)
        {
            throw new InvalidOperationException("Invalid referral code.");
        }

        var cart = await GetOrCreateCartAsync(userId, cancellationToken);
        cart.ReferralCode = referralCode.Trim().ToUpperInvariant();
        await RecalculateAsync(cart, cancellationToken);
        return ToDto(cart);
    }

    public async Task RemoveItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);
        var item = cart.Items.FirstOrDefault(x => x.Id == itemId);
        if (item != null)
        {
            cart.Items.Remove(item);
            await RecalculateAsync(cart, cancellationToken);
        }
    }

    public async Task<Guid> CheckoutAsync(Guid userId, CheckoutCartRequest request, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(userId, cancellationToken);
        if (cart.Items.Count == 0) throw new InvalidOperationException("Cart is empty.");
        await RecalculateAsync(cart, cancellationToken);

        var order = new EnrollmentOrder
        {
            StudentUserId = userId,
            OrderType = cart.Items.Count > 1 ? OrderType.Bundle : OrderType.Single,
            SubtotalEgp = cart.SubtotalEgp,
            DiscountAmountEgp = cart.DiscountAmountEgp,
            DiscountReason = cart.DiscountSummary,
            ReferralCode = cart.ReferralCode,
            TotalAmountEgp = cart.TotalEgp,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = PaymentStatus.Paid, // Auto-paid for demo
            PaidAt = DateTimeOffset.UtcNow
        };
        db.EnrollmentOrders.Add(order);

        foreach (var item in cart.Items)
        {
            db.Enrollments.Add(new Enrollment
            {
                CourseId = item.CourseId,
                CohortId = item.CohortId,
                StudentUserId = userId,
                EnrollmentOrder = order,
                EnrollmentStatus = EnrollmentStatus.Active,
                Status = "active",
                UnitPriceEgp = item.UnitPriceEgp,
                DiscountAmountEgp = item.DiscountAmountEgp,
                FinalPriceEgp = item.FinalPriceEgp,
                PromoCode = cart.PromoCode,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        cart.Status = CartStatus.Converted;
        await db.SaveChangesAsync(cancellationToken);
        return order.Id;
    }

    private async Task<ShoppingCart> GetOrCreateCartAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cart = await db.ShoppingCarts.Include(x => x.Items).ThenInclude(x => x.Course).FirstOrDefaultAsync(x => x.StudentUserId == userId && x.Status == CartStatus.Active, cancellationToken);
        if (cart is not null) return cart;
        cart = new ShoppingCart { StudentUserId = userId };
        db.ShoppingCarts.Add(cart);
        await db.SaveChangesAsync(cancellationToken);
        return cart;
    }

    private async Task RecalculateAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        foreach (var item in cart.Items)
        {
            if (item.Course is null)
            {
                item.Course = await db.Courses.FirstAsync(x => x.Id == item.CourseId, cancellationToken);
            }
            item.UnitPriceEgp = item.Course.PriceEgp;
            item.DiscountAmountEgp = 0;
            item.FinalPriceEgp = item.UnitPriceEgp;
        }

        cart.SubtotalEgp = cart.Items.Sum(x => x.UnitPriceEgp);
        cart.DiscountAmountEgp = 0;
        var discounts = new List<string>();

        if (cart.Items.Count >= 5)
        {
            var bundleDiscount = decimal.Round(cart.SubtotalEgp * 0.25m, 2);
            cart.DiscountAmountEgp += bundleDiscount;
            discounts.Add("Bundle 25%");
        }

        if (!string.IsNullOrWhiteSpace(cart.ReferralCode))
        {
            var referralDiscount = decimal.Round(cart.SubtotalEgp * 0.10m, 2);
            cart.DiscountAmountEgp += referralDiscount;
            discounts.Add("Referral 10%");
        }

        if (!string.IsNullOrWhiteSpace(cart.PromoCode))
        {
            var promo = await db.PromoCodes.FirstOrDefaultAsync(x => x.Code == cart.PromoCode && x.IsActive, cancellationToken);
            if (promo is not null && (promo.MaxUses == null || promo.UsedCount < promo.MaxUses))
            {
                var promoDiscount = promo.DiscountType == DiscountType.Percentage
                    ? decimal.Round(cart.SubtotalEgp * (promo.Value / 100m), 2)
                    : promo.Value;
                cart.DiscountAmountEgp += promoDiscount;
                discounts.Add($"Promo {promo.Code}");
            }
        }

        cart.DiscountAmountEgp = Math.Min(cart.DiscountAmountEgp, cart.SubtotalEgp);
        cart.TotalEgp = cart.SubtotalEgp - cart.DiscountAmountEgp;
        cart.DiscountSummary = string.Join(", ", discounts);
        cart.UpdatedAt = DateTimeOffset.UtcNow;

        var perItemDiscount = cart.Items.Count == 0 ? 0 : decimal.Round(cart.DiscountAmountEgp / cart.Items.Count, 2);
        foreach (var item in cart.Items)
        {
            item.DiscountAmountEgp = Math.Min(perItemDiscount, item.UnitPriceEgp);
            item.FinalPriceEgp = item.UnitPriceEgp - item.DiscountAmountEgp;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static CartDto ToDto(ShoppingCart cart)
        => new(cart.Id, cart.SubtotalEgp, cart.DiscountAmountEgp, cart.TotalEgp, cart.DiscountSummary,
            cart.Items.Select(x => new CartItemDto(x.Id, x.CourseId, x.Course?.Title ?? string.Empty, x.CohortId, x.UnitPriceEgp, x.DiscountAmountEgp, x.FinalPriceEgp, x.IsBundleItem, x.Course?.CoverImageUrl ?? x.Course?.ImageUrl)).ToList());
}
