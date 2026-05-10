using PRC.Common;
using PRC.IdentityService.DTOs;

namespace PRC.IdentityService.Services;

public interface IAuthService
{
    Task<Result<RegisterResultDto>> RegisterAsync(RegisterRequest req, CancellationToken ct);
    Task<Result<AuthTokenDto>> LoginAsync(LoginRequest req, CancellationToken ct);
    Task<Result<AuthTokenDto>> RefreshTokenAsync(RefreshTokenRequest req, CancellationToken ct);
    Task<Result> RevokeTokenAsync(RevokeTokenRequest req, CancellationToken ct);
    Task<Result<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct);
    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest req, CancellationToken ct);
    Task<Result> ForgotPasswordAsync(ForgotPasswordRequest req, CancellationToken ct);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest req, CancellationToken ct);
    Task<Result> VerifyEmailAsync(VerifyEmailRequest req, CancellationToken ct);
    Task<Result> ResendVerificationAsync(ResendVerificationRequest req, CancellationToken ct);
    Task<Result<UserDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequest req, CancellationToken ct);
}
