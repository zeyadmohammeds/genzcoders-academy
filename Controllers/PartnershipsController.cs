using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Models;
using Microsoft.AspNetCore.Mvc;

namespace GenZCoders.Controllers;

[ApiController]
[Route("api/partnerships")]
public class PartnershipsController(AcademyDbContext db) : ControllerBase
{
    [HttpPost("leads")]
    public async Task<IActionResult> CreateLead(PartnershipLeadRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var lead = new PartnershipLead
        {
            SchoolName = request.SchoolName.Trim(),
            ContactName = request.ContactName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            Message = request.Message.Trim()
        };

        db.PartnershipLeads.Add(lead);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { lead.Id, lead.CreatedAt });
    }
}
