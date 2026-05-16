using GenZCoders.Models.Identity;

namespace GenZCoders.Models;

public class Enrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public Course? Course { get; set; }
    public Guid? EnrollmentOrderId { get; set; }
    public EnrollmentOrder? EnrollmentOrder { get; set; }
    public Guid? CohortId { get; set; }
    public Cohort? Cohort { get; set; }
    public Guid StudentUserId { get; set; }
    public ApplicationUser? StudentUser { get; set; }
    public EnrollmentStatus EnrollmentStatus { get; set; } = EnrollmentStatus.Pending;
    public string Status { get; set; } = "pending";
    public string? PromoCode { get; set; }
    public decimal UnitPriceEgp { get; set; }
    public decimal DiscountAmountEgp { get; set; }
    public decimal FinalPriceEgp { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
