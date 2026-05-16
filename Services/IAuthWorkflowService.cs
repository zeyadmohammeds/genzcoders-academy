using GenZCoders.DTOs;

namespace GenZCoders.Services;

public interface IAuthWorkflowService
{
    Task<AuthUserDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthUserDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthUserDto> CurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task RequestEmailVerificationAsync(string email, CancellationToken cancellationToken = default);
    Task<AuthUserDto> VerifyEmailOtpAsync(VerifyEmailOtpRequest request, CancellationToken cancellationToken = default);
    Task<AuthUserDto> VerifyEmailLinkAsync(VerifyEmailLinkRequest request, CancellationToken cancellationToken = default);
    Task<AuthUserDto> CompleteOnboardingAsync(Guid userId, OnboardingRequest request, CancellationToken cancellationToken = default);
}
