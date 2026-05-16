using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Models;
using GenZCoders.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace GenZCoders.Services;

public class AuthWorkflowService(
    AcademyDbContext db,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    INotificationService notifications,
    IReferralService referrals) : IAuthWorkflowService
{
    public async Task<AuthUserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber,
            RoleKey = AcademyRole.Student
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        await userManager.AddToRoleAsync(user, AcademyRole.Student);

        db.StudentProfiles.Add(new StudentProfile
        {
            UserId = user.Id,
            ReferralCode = await referrals.GenerateUniqueCodeAsync(user.FirstName, cancellationToken),
            ReferredByUserId = await referrals.ResolveReferrerAsync(request.ReferralCode, cancellationToken)
        });
        db.UserNotificationSettings.Add(new UserNotificationSetting { UserId = user.Id, EmailEnabled = true, WhatsAppEnabled = true });
        await db.SaveChangesAsync(cancellationToken);

        await referrals.TrackRegistrationAsync(user.Id, request.ReferralCode, cancellationToken);
        await notifications.QueueAsync(
            user.Id,
            "Welcome to ElSewedy GenZ Coders",
            $"Hi {user.FirstName}, your studio account is ready. Browse tracks, add courses to your cart, and join a live cohort when enrollment opens. Verify your email to unlock the full experience.",
            [NotificationChannel.InApp, NotificationChannel.Email],
            cancellationToken);
        await RequestEmailVerificationAsync(email, cancellationToken);
        await signInManager.SignInAsync(user, isPersistent: false);
        return await ToDtoAsync(user, cancellationToken);
    }

    public async Task<AuthUserDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant())
            ?? throw new InvalidOperationException("Invalid email or password.");

        var result = await signInManager.PasswordSignInAsync(user, request.Password, request.RememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.IsLockedOut ? "Account is temporarily locked." : "Invalid email or password.");
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);
        return await ToDtoAsync(user, cancellationToken);
    }

    public async Task<AuthUserDto> CurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        return await ToDtoAsync(user, cancellationToken);
    }

    public async Task RequestEmailVerificationAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email.Trim().ToLowerInvariant());
        if (user is null)
        {
            return;
        }

        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        db.EmailVerificationCodes.Add(new EmailVerificationCode
        {
            UserId = user.Id,
            Email = user.Email ?? email,
            CodeHash = Hash(code),
            VerificationTokenHash = Hash(token),
            Purpose = VerificationPurpose.EmailVerification,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
        });
        await db.SaveChangesAsync(cancellationToken);

        await notifications.QueueAsync(user.Id, "Verify your email", $"Your OTP is {code}. Verification token: {token}", [NotificationChannel.Email, NotificationChannel.InApp], cancellationToken);
    }

    public async Task<AuthUserDto> VerifyEmailOtpAsync(VerifyEmailOtpRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant())
            ?? throw new InvalidOperationException("User not found.");
        var record = await LatestVerificationAsync(user.Id, VerificationPurpose.EmailVerification, cancellationToken);
        if (record.CodeHash != Hash(request.Code) || record.ExpiresAt < DateTimeOffset.UtcNow)
        {
            record.AttemptCount++;
            if (record.ExpiresAt < DateTimeOffset.UtcNow) record.Status = VerificationStatus.Expired;
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Invalid or expired OTP.");
        }

        await ConfirmAsync(user, record, cancellationToken);
        return await ToDtoAsync(user, cancellationToken);
    }

    public async Task<AuthUserDto> VerifyEmailLinkAsync(VerifyEmailLinkRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new InvalidOperationException("User not found.");
        var record = await LatestVerificationAsync(user.Id, VerificationPurpose.EmailVerification, cancellationToken);
        if (record.VerificationTokenHash != Hash(request.Token) || record.ExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("Invalid or expired verification link.");
        }

        await ConfirmAsync(user, record, cancellationToken);
        return await ToDtoAsync(user, cancellationToken);
    }

    public async Task<AuthUserDto> CompleteOnboardingAsync(Guid userId, OnboardingRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (profile is null)
        {
            profile = new StudentProfile { UserId = userId, ReferralCode = await referrals.GenerateUniqueCodeAsync(user.FirstName, cancellationToken) };
            db.StudentProfiles.Add(profile);
        }

        if (request.Skip)
        {
            profile.OnboardingSkippedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            await notifications.QueueAsync(userId, "Complete your profile to get more XP", "Finish onboarding and earn profile completion XP.", [NotificationChannel.InApp, NotificationChannel.Email], cancellationToken);
            return await ToDtoAsync(user, cancellationToken);
        }

        profile.Age = request.Age;
        profile.NationalId = request.NationalId;
        profile.SchoolId = request.SchoolId;
        profile.SchoolName = request.SchoolName;
        profile.GradeLevel = request.GradeLevel;
        profile.InterestsJson = request.InterestsJson;
        profile.ExperienceLevel = request.ExperienceLevel;
        profile.Goals = request.Goals;
        profile.PreferredTrack = request.PreferredTrack;
        profile.IsOnboardingCompleted = true;
        profile.OnboardingCompletedAt = DateTimeOffset.UtcNow;
        user.ProfileCompleted = true;
        await userManager.UpdateAsync(user);

        if (!profile.ProfileCompletionXpAwarded)
        {
            profile.ProfileCompletionXpAwarded = true;
            profile.TotalXp += 100;
            db.XpTransactions.Add(new XpTransaction
            {
                StudentUserId = userId,
                Amount = 100,
                SourceType = XpSourceType.Bonus,
                Description = "Profile onboarding completed"
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await notifications.QueueAsync(userId, "Profile completed", "You earned 100 XP for completing your profile.", [NotificationChannel.InApp, NotificationChannel.Email], cancellationToken);
        return await ToDtoAsync(user, cancellationToken);
    }

    private async Task<EmailVerificationCode> LatestVerificationAsync(Guid userId, VerificationPurpose purpose, CancellationToken cancellationToken)
        => await db.EmailVerificationCodes
            .Where(x => x.UserId == userId && x.Purpose == purpose && x.Status == VerificationStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No pending verification request was found.");

    private async Task ConfirmAsync(ApplicationUser user, EmailVerificationCode record, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = true;
        user.VerifiedAt = DateTimeOffset.UtcNow;
        record.Status = VerificationStatus.Used;
        record.UsedAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthUserDto> ToDtoAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var profile = await db.StudentProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);
        return new AuthUserDto(
            user.Id, 
            user.Email ?? string.Empty, 
            user.DisplayName, 
            user.RoleKey, 
            user.EmailConfirmed, 
            profile?.IsOnboardingCompleted ?? false, 
            profile?.ReferralCode,
            profile?.TotalXp ?? 0,
            profile?.Level ?? 1);
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
