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
    RoleManager<ApplicationRole> roleManager,
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
        
        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "Super",
                LastName = "Admin",
                RoleKey = AcademyRole.AcademyAdmin,
                IsActive = true
            };
            var createResult = await userManager.CreateAsync(admin, password);
            if (!createResult.Succeeded)
            {
                return BadRequest(new { success = false, errors = createResult.Errors.Select(e => e.Description).ToList() });
            }
        }
        else
        {
            admin.IsActive = true;
            admin.RoleKey = AcademyRole.AcademyAdmin;
            await userManager.UpdateAsync(admin);
            
            var token = await userManager.GeneratePasswordResetTokenAsync(admin);
            await userManager.ResetPasswordAsync(admin, token, password);
        }
        
        // Ensure role exists and user is in role
        if (!await roleManager.RoleExistsAsync(AcademyRole.AcademyAdmin))
        {
            await roleManager.CreateAsync(new ApplicationRole { Name = AcademyRole.AcademyAdmin });
        }
        if (!await userManager.IsInRoleAsync(admin, AcademyRole.AcademyAdmin))
        {
            await userManager.AddToRoleAsync(admin, AcademyRole.AcademyAdmin);
        }
        
        return Ok(new { success = true, message = "Admin user ready" });
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
        try
        {
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info is null) return BadRequest("Google sign-in failed (missing cookie).");

            var signIn = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true);
            string frontendUrl = configuration["Frontend:Url"]?.TrimEnd('/') ?? "http://localhost:3000";
            string destination = string.Empty;

            if (signIn.Succeeded)
            {
                var existingUser = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (existingUser != null)
                {
                    destination = GetDestination(existingUser);
                    return Redirect($"{frontendUrl}{destination}");
                }
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email)) return BadRequest("Google did not return an email address.");

            var user = await userManager.FindByEmailAsync(email);
            bool isNewUser = false;
            
            if (user is null)
            {
                isNewUser = true;
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "GenZ",
                    LastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "Coder",
                    EmailConfirmed = true,
                    VerifiedAt = DateTimeOffset.UtcNow,
                    ProfileCompleted = false,
                    RoleKey = AcademyRole.Student
                };

                var create = await userManager.CreateAsync(user);
                if (!create.Succeeded) 
                {
                    SentrySdk.CaptureMessage($"User creation failed: {string.Join(", ", create.Errors.Select(e => e.Description))}");
                    return BadRequest(create.Errors);
                }
                
                // Ensure role exists to prevent 500
                if (!await roleManager.RoleExistsAsync(AcademyRole.Student))
                {
                    await roleManager.CreateAsync(new ApplicationRole { Name = AcademyRole.Student });
                }
                await userManager.AddToRoleAsync(user, AcademyRole.Student);
            }

            await userManager.AddLoginAsync(user, info);
            await signInManager.SignInAsync(user, isPersistent: true);
            
            destination = isNewUser ? "/onboarding" : GetDestination(user);
            return Redirect($"{frontendUrl}{destination}");
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { message = "Critical failure in Google callback", error = ex.Message, stack = ex.StackTrace });
        }
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
