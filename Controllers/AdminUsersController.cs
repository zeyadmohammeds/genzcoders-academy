using GenZCoders.Data;
using GenZCoders.Models;
using GenZCoders.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Controllers;

[Authorize(Roles = AcademyRole.AcademyAdmin)]
[ApiController]
[Route("api/admin/users")]
public class AdminUsersController(UserManager<ApplicationUser> userManager, AcademyDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? role, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleUserIds = await db.UserRoles
                .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Where(x => x.Name == role)
                .Select(x => x.UserId)
                .ToListAsync(ct);
            query = query.Where(u => roleUserIds.Contains(u.Id));
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new {
                u.Id,
                u.Email,
                DisplayName = u.FirstName + " " + u.LastName,
                Role = u.RoleKey,
                u.TotalXp,
                u.CreatedAt,
                u.EmailConfirmed,
                u.IsActive
            })
            .Take(200)
            .ToListAsync(ct);
        return Ok(users);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.Name.Split(' ')[0],
            LastName = request.Name.Contains(' ') ? request.Name.Split(' ')[1] : "",
            RoleKey = request.Role
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        await userManager.AddToRoleAsync(user, request.Role);
        return Ok(new { user.Id });
    }

    [HttpPut("{id:guid}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        user.IsActive = !user.IsActive;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { user.Id, user.IsActive });
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateUserRoleRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRoleAsync(user, request.Role);

        user.RoleKey = request.Role;
        await userManager.UpdateAsync(user);

        return Ok(new { user.Id, Role = request.Role });
    }
}

public record CreateUserRequest(string Email, string Name, string Role, string Password);
public record UpdateUserRoleRequest(string Role);
