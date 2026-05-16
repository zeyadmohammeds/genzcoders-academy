using System.ComponentModel.DataAnnotations;

namespace GenZCoders.DTOs;

public record PartnershipLeadRequest(
    [Required] string SchoolName,
    [Required] string ContactName,
    [EmailAddress] string Email,
    string Phone,
    string Message);
