using PRC.Common;

namespace PRC.IdentityService.DTOs;

public record AuthTokenDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User);

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    UserRole Role,
    Guid? FederationId,
    Guid? ClubId,
    string? ProfileImageUrl,
    bool IsActive);

public record RegisterResultDto(
    bool IsPendingApproval,
    AuthTokenDto? Tokens);

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role,
    Guid? FederationId,
    string? InvitationToken);

public record LoginRequest(
    string Email,
    string Password);

public record RefreshTokenRequest(
    string AccessToken,
    string RefreshToken);

public record RevokeTokenRequest(string RefreshToken);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword);

public record VerifyEmailRequest(
    string UserId,
    string Token);

public record ResendVerificationRequest(string Email);

public record SubmitUpgradeRequestBody(
    UserRole RequestedRole,
    Guid? FederationId,
    string? ClubName,
    string? Notes);

public record UpgradeRequestDto(
    Guid Id,
    Guid UserId,
    string UserFullName,
    string UserEmail,
    UserRole RequestedRole,
    Guid? FederationId,
    UpgradeRequestStatus Status,
    string? Notes,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? ReviewedAt);

public record RejectUpgradeRequestBody(string? Reason);

public record UpdateProfileRequest(
    string FirstName,
    string LastName);
