using GenZCoders.DTOs;
using GenZCoders.Models;
using GenZCoders.Models.Identity;
using GenZCoders.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GenZCoders.Data;

namespace GenZCoders.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuthWorkflowService authWorkflow,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    IAuthenticationSchemeProvider schemeProvider,
    AcademyDbContext db) : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
        => Ok(await authWorkflow.CurrentUserAsync(CurrentUserId(), cancellationToken));

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
        => Ok(await authWorkflow.RegisterAsync(request, cancellationToken));

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
        => Ok(await authWorkflow.LoginAsync(request, cancellationToken));

    [HttpPost("email/request-verification")]
    public async Task<IActionResult> RequestVerification(RequestEmailVerificationRequest request, CancellationToken cancellationToken)
    {
        await authWorkflow.RequestEmailVerificationAsync(request.Email, cancellationToken);
        return Ok(new { queued = true });
    }

    [HttpPost("email/verify-otp")]
    public async Task<IActionResult> VerifyOtp(VerifyEmailOtpRequest request, CancellationToken cancellationToken)
        => Ok(await authWorkflow.VerifyEmailOtpAsync(request, cancellationToken));

    [HttpPost("email/verify-link")]
    public async Task<IActionResult> VerifyLink(VerifyEmailLinkRequest request, CancellationToken cancellationToken)
        => Ok(await authWorkflow.VerifyEmailLinkAsync(request, cancellationToken));

    [Authorize(Roles = "student")]
    [HttpPost("onboarding")]
    public async Task<IActionResult> Onboarding(OnboardingRequest request, CancellationToken cancellationToken)
        => Ok(await authWorkflow.CompleteOnboardingAsync(CurrentUserId(), request, cancellationToken));

    [HttpGet("debug/users")]
    public async Task<IActionResult> DebugUsers()
    {
        var users = userManager.Users.Select(u => new { u.Email, u.FirstName, u.LastName, u.RoleKey, u.EmailConfirmed, u.IsActive }).Take(10);
        return Ok(users);
    }

    [HttpPost("debug/force-create-admin")]
    public async Task<IActionResult> ForceCreateAdmin()
    {
        var email = "admin@genz.academy";
        var password = "Academy123!";
        
        // First delete user roles and related data using raw SQL
        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            // Delete user claims first
            var userId = existing.Id.ToString();
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM [AspNetUserClaims] WHERE [UserId] = '{userId}'");
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM [AspNetUserLogins] WHERE [UserId] = '{userId}'");
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM [AspNetUserRoles] WHERE [UserId] = '{userId}'");
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM [AspNetUserTokens] WHERE [UserId] = '{userId}'");
            
            // Delete the user
            await userManager.DeleteAsync(existing);
        }
        
        // Create fresh user
        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Super",
            LastName = "Admin",
            RoleKey = AcademyRole.AcademyAdmin,
            IsActive = true
        };
        
        var result = await userManager.CreateAsync(admin, password);
        
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, AcademyRole.AcademyAdmin);
            return Ok(new { success = true, message = "Admin user created successfully" });
        }
        
        return BadRequest(new { 
            success = false, 
            errors = result.Errors.Select(e => e.Description).ToList() 
        });
    }

    [HttpGet("debug/admin-status")]
    public async Task<IActionResult> GetAdminStatus()
    {
        var admin = await userManager.FindByEmailAsync("admin@genz.academy");
        if (admin == null) return NotFound("Admin not found");
        
        var hasPassword = await userManager.HasPasswordAsync(admin);
        var rolesList = await userManager.GetRolesAsync(admin);
        var pwdCheck = await userManager.CheckPasswordAsync(admin, "Academy123!");
        
        return Ok(new { 
            email = admin.Email,
            hasPassword = hasPassword,
            roles = rolesList,
            passwordValid = pwdCheck
        });
    }

    [HttpGet("google")]
    public IActionResult Google(string? returnUrl = "/")
    {
        if (string.IsNullOrWhiteSpace(returnUrl) && Request.Query.ContainsKey("ReturnUrl"))
        {
            returnUrl = Request.Query["ReturnUrl"];
        }

        if (schemeProvider.GetSchemeAsync("Google").GetAwaiter().GetResult() is null ||
            string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"]) ||
            string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]))
        {
            return BadRequest(new
            {
                message = "Google sign-in is ready, but the Google authentication handler is not configured for this environment. Add Authentication:Google:ClientId and ClientSecret, then restart the API."
            });
        }

        var properties = signInManager.ConfigureExternalAuthenticationProperties("Google", Url.Action(nameof(GoogleCallback), new { returnUrl }));
        return Challenge(properties, "Google");
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = "/")
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null) return BadRequest("Google sign-in failed.");

        var signIn = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true);
        string frontendUrl = "http://localhost:3000";
        string destination = string.Empty;

        if (signIn.Succeeded)
        {
            var existingUser = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (existingUser != null)
            {
                destination = GetDestination(existingUser);
            }
            else
            {
                destination = "/dashboard";
            }
            return Redirect($"{frontendUrl}{destination}");
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email)) return BadRequest("Google did not return an email address.");

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "GenZ",
                LastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "Coder",
                EmailConfirmed = true,
                VerifiedAt = DateTimeOffset.UtcNow,
                ProfileCompleted = false
            };

            var create = await userManager.CreateAsync(user);
            if (!create.Succeeded) return BadRequest(create.Errors);
            await userManager.AddToRoleAsync(user, "student");
        }

        await userManager.AddLoginAsync(user, info);
        await signInManager.SignInAsync(user, isPersistent: true);
        
        destination = GetDestination(user);
        return Redirect($"{frontendUrl}{destination}");
    }

    private string GetDestination(ApplicationUser user)
    {
        if (user.RoleKey == AcademyRole.AcademyAdmin) return "/admin";
        return user.ProfileCompleted ? "/dashboard" : "/onboarding";
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return Ok();
    }

    private Guid CurrentUserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
}
