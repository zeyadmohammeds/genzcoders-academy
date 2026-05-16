using Microsoft.AspNetCore.Identity;

namespace GenZCoders.Models.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public string Description { get; set; } = string.Empty;
}
