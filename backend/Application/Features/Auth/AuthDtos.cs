using MediatR;
using PigeonRacing.Application.Common;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.Application.Features.Auth;

// ── DTOs ────────────────────────────────────────────────────────────────────

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
    Guid? CountryId,
    Guid? ClubId,
    string? ProfileImageUrl,
    bool IsActive);

// ── Register ─────────────────────────────────────────────────────────────────

public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role,
    Guid? CountryId,
    string? InvitationToken) : IRequest<Result<AuthTokenDto>>;

// ── Login ────────────────────────────────────────────────────────────────────

public record LoginCommand(
    string Email,
    string Password) : IRequest<Result<AuthTokenDto>>;

// ── Refresh Token ─────────────────────────────────────────────────────────────

public record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken) : IRequest<Result<AuthTokenDto>>;

// ── Revoke Token ──────────────────────────────────────────────────────────────

public record RevokeTokenCommand(string RefreshToken) : IRequest<Result>;

// ── Change Password ───────────────────────────────────────────────────────────

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : IRequest<Result>;

// ── Get Current User ──────────────────────────────────────────────────────────

public record GetCurrentUserQuery : IRequest<Result<UserDto>>;
