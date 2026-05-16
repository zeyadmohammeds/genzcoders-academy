using GenZCoders.Models.Identity;

namespace GenZCoders.Models;

public class EnrollmentOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentUserId { get; set; }
    public ApplicationUser? StudentUser { get; set; }
    public OrderType OrderType { get; set; } = OrderType.Single;
    public decimal SubtotalEgp { get; set; }
    public decimal DiscountAmountEgp { get; set; }
    public string? DiscountReason { get; set; }
    public Guid? PromoCodeId { get; set; }
    public PromoCode? PromoCode { get; set; }
    public string? ReferralCode { get; set; }
    public decimal TotalAmountEgp { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Enrollment> Enrollments { get; set; } = [];
}

public class PromoCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
    public decimal Value { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public Guid? CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid? SchoolId { get; set; }
    public School? School { get; set; }
    public bool AppliesToBundle { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class PaymentTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EnrollmentOrderId { get; set; }
    public EnrollmentOrder? EnrollmentOrder { get; set; }
    public string Provider { get; set; } = "manual";
    public string? ProviderTransactionId { get; set; }
    public decimal AmountEgp { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string RawPayloadJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class Referral
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReferrerUserId { get; set; }
    public ApplicationUser? ReferrerUser { get; set; }
    public Guid? ReferredUserId { get; set; }
    public ApplicationUser? ReferredUser { get; set; }
    public string ReferralCode { get; set; } = string.Empty;
    public int RegistrationXpAwarded { get; set; }
    public int EnrollmentXpAwarded { get; set; }
    public decimal DiscountCreditEgp { get; set; }
    public bool ConvertedToPaidEnrollment { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ConvertedAt { get; set; }
}

public class ShoppingCart
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentUserId { get; set; }
    public ApplicationUser? StudentUser { get; set; }
    public CartStatus Status { get; set; } = CartStatus.Active;
    public string? PromoCode { get; set; }
    public string? ReferralCode { get; set; }
    public decimal SubtotalEgp { get; set; }
    public decimal DiscountAmountEgp { get; set; }
    public string? DiscountSummary { get; set; }
    public decimal TotalEgp { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<ShoppingCartItem> Items { get; set; } = [];
}

public class ShoppingCartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShoppingCartId { get; set; }
    public ShoppingCart? ShoppingCart { get; set; }
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid? CohortId { get; set; }
    public Cohort? Cohort { get; set; }
    public decimal UnitPriceEgp { get; set; }
    public decimal DiscountAmountEgp { get; set; }
    public decimal FinalPriceEgp { get; set; }
    public bool IsBundleItem { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
