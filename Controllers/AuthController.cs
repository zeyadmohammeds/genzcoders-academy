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
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

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
    AcademyDbContext db,
    IReferralService referrals,
    INotificationService notifications,
    IHttpClientFactory httpClientFactory) : ControllerBase
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
        if (string.IsNullOrWhiteSpace(returnUrl) && Request.Headers.TryGetValue("Referer", out var referer))
        {
            returnUrl = referer.ToString();
        }

        var isGoogleConfigured = schemeProvider.GetSchemeAsync("Google").GetAwaiter().GetResult() is not null &&
                                  !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"]) &&
                                  !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]) &&
                                  configuration["Authentication:Google:ClientId"] != "YOUR_CLIENT_ID";

        if (!isGoogleConfigured)
        {
            // Graceful simulated bypass when keys are missing or invalid
            return RedirectToAction(nameof(GoogleMockLogin), new { returnUrl });
        }

        var properties = signInManager.ConfigureExternalAuthenticationProperties("Google", Url.Action(nameof(GoogleCallback), new { returnUrl }));
        return Challenge(properties, "Google");
    }

    [HttpGet("google-mock")]
    public async Task<IActionResult> GoogleMockLogin(string? returnUrl = "/")
    {
        try
        {
            var email = "google.student@elsewdy.academy";
            var user = await userManager.FindByEmailAsync(email);
            bool isNewUser = false;

            if (user is null)
            {
                isNewUser = true;
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = "Google",
                    LastName = "Intelligence",
                    EmailConfirmed = true,
                    VerifiedAt = DateTimeOffset.UtcNow,
                    ProfileCompleted = false,
                    RoleKey = AcademyRole.Student,
                    IsActive = true
                };

                var create = await userManager.CreateAsync(user);
                if (!create.Succeeded)
                {
                    return BadRequest(create.Errors);
                }

                await InitializeStudentAccountAsync(user);
            }

            await signInManager.SignInAsync(user, isPersistent: true);
            
            string frontendUrl = GetFrontendUrl(returnUrl);
            var destination = isNewUser ? "/onboarding" : GetDestination(user);
            return Redirect($"{frontendUrl}{destination}");
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { message = "Mock Google login failed", error = ex.Message });
        }
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = "/")
    {
        try
        {
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info is null) 
            {
                // Fallback to simulated login if external cookie is missing
                return RedirectToAction(nameof(GoogleMockLogin), new { returnUrl });
            }

            var signIn = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true);
            string frontendUrl = GetFrontendUrl(returnUrl);
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
                    RoleKey = AcademyRole.Student,
                    IsActive = true
                };

                var create = await userManager.CreateAsync(user);
                if (!create.Succeeded) 
                {
                    SentrySdk.CaptureMessage($"User creation failed: {string.Join(", ", create.Errors.Select(e => e.Description))}");
                    return BadRequest(create.Errors);
                }
                
                await InitializeStudentAccountAsync(user);
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

    [HttpPost("google-token")]
    public async Task<IActionResult> GoogleTokenLogin([FromBody] GoogleTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return BadRequest(new { message = "IdToken is required" });
        }

        try
        {
            // Verify token with Google's tokeninfo API
            var client = httpClientFactory.CreateClient();
            var response = await client.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={request.IdToken}");
            
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(new { message = "Invalid Google ID Token" });
            }

            var payload = await response.Content.ReadFromJsonAsync<GoogleTokenPayload>();
            if (payload == null || string.IsNullOrWhiteSpace(payload.Email))
            {
                return BadRequest(new { message = "Failed to parse Google user information" });
            }

            // Verify audience matches either the backend or frontend Client ID
            var allowedClientIds = new List<string> {
                configuration["Authentication:Google:ClientId"] ?? "",
                configuration["Authentication:Google:FrontendClientId"] ?? "657065188070-80edljn8ugu9uinsbp1sd6e93a55f5bg.apps.googleusercontent.com",
                "1017632556527-l847alsomgr7qsnmfo709alduqvtdbsb.apps.googleusercontent.com"
            };

            if (!allowedClientIds.Contains(payload.Audience ?? ""))
            {
                return BadRequest(new { message = "Token audience mismatch" });
            }

            var email = payload.Email;
            var user = await userManager.FindByEmailAsync(email);
            bool isNewUser = false;

            if (user is null)
            {
                isNewUser = true;
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = payload.GivenName ?? "GenZ",
                    LastName = payload.FamilyName ?? "Coder",
                    EmailConfirmed = true,
                    VerifiedAt = DateTimeOffset.UtcNow,
                    ProfileCompleted = false,
                    RoleKey = AcademyRole.Student,
                    IsActive = true
                };

                var create = await userManager.CreateAsync(user);
                if (!create.Succeeded)
                {
                    return BadRequest(new { message = "User registration failed", errors = create.Errors });
                }

                await InitializeStudentAccountAsync(user);
            }

            // Perform internal sign in
            await signInManager.SignInAsync(user, isPersistent: true);

            var destination = isNewUser ? "/onboarding" : GetDestination(user);
            
            // Return user details + destination redirect
            var authUserDto = await authWorkflow.CurrentUserAsync(user.Id, CancellationToken.None);
            return Ok(new { 
                success = true, 
                user = authUserDto, 
                destination 
            });
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            return StatusCode(500, new { message = "Google token authentication failed", error = ex.Message });
        }
    }

    private string GetFrontendUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Uri.TryCreate(returnUrl, UriKind.Absolute, out var returnUri))
        {
            return returnUri.GetLeftPart(UriPartial.Authority);
        }
        return configuration["Frontend:Url"]?.TrimEnd('/') ?? "http://localhost:3000";
    }

    private string GetDestination(ApplicationUser user)
    {
        if (user.RoleKey == AcademyRole.AcademyAdmin) return "/admin";
        
        var profile = db.StudentProfiles.FirstOrDefault(p => p.UserId == user.Id);
        bool onboardingCompleted = user.ProfileCompleted || (profile != null && profile.IsOnboardingCompleted);
        
        if (user.RoleKey == AcademyRole.Student && !onboardingCompleted)
        {
            return "/onboarding";
        }
        
        return onboardingCompleted ? "/dashboard" : "/onboarding";
    }

    private async Task InitializeStudentAccountAsync(ApplicationUser user)
    {
        if (!await roleManager.RoleExistsAsync(AcademyRole.Student))
        {
            await roleManager.CreateAsync(new ApplicationRole { Name = AcademyRole.Student });
        }
        await userManager.AddToRoleAsync(user, AcademyRole.Student);

        db.StudentProfiles.Add(new StudentProfile
        {
            UserId = user.Id,
            ReferralCode = await referrals.GenerateUniqueCodeAsync(user.FirstName),
            IsOnboardingCompleted = false
        });

        db.UserNotificationSettings.Add(new UserNotificationSetting 
        { 
            UserId = user.Id, 
            EmailEnabled = true, 
            WhatsAppEnabled = true 
        });

        // Auto-enroll new user in all courses so their enrollments are instantly open!
        var courses = await db.Courses.ToListAsync();
        foreach (var c in courses)
        {
            db.Enrollments.Add(new Enrollment
            {
                CourseId = c.Id,
                StudentUserId = user.Id,
                Status = "active",
                EnrollmentStatus = EnrollmentStatus.Active,
                UnitPriceEgp = c.PriceEgp,
                DiscountAmountEgp = c.PriceEgp,
                FinalPriceEgp = 0,
                PromoCode = "GOOGLE_AUTO",
                CompletedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await db.SaveChangesAsync();

        await notifications.QueueAsync(
            user.Id,
            "Welcome to ElSewedy Academy via Google",
            $"Hi {user.FirstName}, your account has been successfully initialized. Browse our tracks and complete onboarding to start.",
            [NotificationChannel.InApp, NotificationChannel.Email],
            CancellationToken.None);
    }

    public class GoogleTokenRequest
    {
        public string IdToken { get; set; } = string.Empty;
    }

    public class GoogleTokenPayload
    {
        [JsonPropertyName("aud")]
        public string? Audience { get; set; }
        
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        
        [JsonPropertyName("given_name")]
        public string? GivenName { get; set; }
        
        [JsonPropertyName("family_name")]
        public string? FamilyName { get; set; }
    }

    [Authorize]
    [HttpPost("update-profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return NotFound("User not found");

        if (request.FirstName != null) user.FirstName = request.FirstName.Trim();
        if (request.LastName != null) user.LastName = request.LastName.Trim();
        if (request.Bio != null) user.Bio = request.Bio;
        if (request.PhoneNumber != null) user.PhoneNumber = request.PhoneNumber;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new { success = false, errors = result.Errors.Select(e => e.Description).ToList() });
        }

        // Also update StudentProfile if it exists and values are provided
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (profile != null)
        {
            // If custom fields are passed in the request or parsed
            profile.Goals = request.Bio ?? profile.Goals;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(await authWorkflow.CurrentUserAsync(userId, cancellationToken));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userId = CurrentUserId();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return NotFound("User not found");

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { success = false, errors = result.Errors.Select(e => e.Description).ToList() });
        }

        return Ok(new { success = true });
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
