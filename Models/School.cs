namespace GenZCoders.Models;

public class School
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public SchoolType Type { get; set; } = SchoolType.Other;
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
    public string CoordinatorName { get; set; } = string.Empty;
    public string CoordinatorEmail { get; set; } = string.Empty;
    public PartnershipStatus PartnershipStatus { get; set; } = PartnershipStatus.Prospect;
    public string Status { get; set; } = "prospect";
    public decimal PartnerDiscountPercent { get; set; } = 15;
    public decimal BundleDiscountPercent { get; set; } = 25;
    public DateOnly? PartnerSince { get; set; }
    public bool MouSigned { get; set; }
    public DateTimeOffset? MouSignedAt { get; set; }
    public string? MouDocumentUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<StudentProfile> Students { get; set; } = [];
    public ICollection<SchoolCoordinator> Coordinators { get; set; } = [];
}
