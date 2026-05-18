namespace GenZCoders.DTOs;

public record AddCartItemRequest(Guid CourseId, Guid? CourseRoundId);

public record ApplyPromoRequest(string Code);

public record ApplyReferralRequest(string ReferralCode);

public record CartDto(Guid Id, decimal SubtotalEgp, decimal DiscountAmountEgp, decimal TotalEgp, string? DiscountSummary, IReadOnlyList<CartItemDto> Items);

public record CartItemDto(Guid Id, Guid CourseId, string CourseTitle, Guid? CourseRoundId, decimal UnitPriceEgp, decimal DiscountAmountEgp, decimal FinalPriceEgp, bool IsBundleItem, string? CourseImageUrl);

public record CheckoutCartRequest(string PaymentMethod);

public record ReferralSummaryDto(string ReferralCode, int TotalReferrals, int PaidConversions, int XpEarned, decimal DiscountCreditsEgp);
