using GenZCoders.Models;

namespace GenZCoders.DTOs;

public record RegisterRequest(string Email, string Password, string FirstName, string LastName, string? PhoneNumber, string? ReferralCode);

public record LoginRequest(string Email, string Password, bool RememberMe);

public record AuthUserDto(Guid Id, string Email, string DisplayName, string Role, bool EmailConfirmed, bool ProfileCompleted, string? ReferralCode, int TotalXp = 0, int Level = 1);

public record RequestEmailVerificationRequest(string Email);

public record VerifyEmailOtpRequest(string Email, string Code);

public record VerifyEmailLinkRequest(Guid UserId, string Token);

public record OnboardingRequest(
    int Age,
    string? NationalId,
    Guid? SchoolId,
    string? SchoolName,
    string GradeLevel,
    string InterestsJson,
    ExperienceLevel ExperienceLevel,
    string? Goals,
    string? PreferredTrack,
    bool Skip);

public record UpdateProfileRequest(string? FirstName, string? LastName, string? Bio, string? PhoneNumber, string? InterestsJson);
