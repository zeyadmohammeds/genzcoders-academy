namespace GenZCoders.Models;

public class PartnershipLead
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SchoolName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public LeadStatus LeadStatus { get; set; } = LeadStatus.New;
    public string? Source { get; set; }
    public string? AssignedTo { get; set; }
    public DateTimeOffset? FollowUpAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
