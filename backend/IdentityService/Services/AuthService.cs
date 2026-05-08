using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.IdentityService.Data;
using PRC.IdentityService.DTOs;
using PRC.IdentityService.Models;

namespace PRC.IdentityService.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly ITokenService _tokens;
    private readonly IEmailService _email;
    private readonly IdentityDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        ITokenService tokens,
        IEmailService email,
        IdentityDbContext db,
        IConfiguration config)
    {
        _users = users;
        _signIn = signIn;
        _tokens = tokens;
        _email = email;
        _db = db;
        _config = config;
    }

    public async Task<Result<RegisterResultDto>> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        if (await _users.FindByEmailAsync(req.Email) != null)
            return Result.Conflict<RegisterResultDto>("An account with this email already exists.");

        Guid? linkedClubId = null;
        if (!string.IsNullOrEmpty(req.InvitationToken))
        {
            var invite = await _db.Invitations
                .FirstOrDefaultAsync(i => i.Token == req.InvitationToken
                    && i.Status == InvitationStatus.Pending
                    && i.ExpiresAt > DateTime.UtcNow, ct);

            if (invite == null)
                return Result.Failure<RegisterResultDto>("Invalid or expired invitation token.", "INVALID_INVITATION");

            linkedClubId = invite.ClubId;
        }

        // All self-registered users start as Fancier (view-only), active immediately.
        // Role upgrades happen via separate UpgradeRequest flow.
        var user = new ApplicationUser
        {
            UserName  = req.Email,
            Email     = req.Email,
            FirstName = req.FirstName,
            LastName  = req.LastName,
            Role      = UserRole.Fancier,
            IsActive  = true
        };

        var createResult = await _users.CreateAsync(user, req.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            return Result.Failure<RegisterResultDto>(errors, "REGISTRATION_FAILED");
        }

        await _users.AddToRoleAsync(user, UserRole.Fancier.ToString());

        if (linkedClubId.HasValue)
        {
            var invite = await _db.Invitations
                .FirstAsync(i => i.Token == req.InvitationToken!, ct);

            invite.Status = InvitationStatus.Accepted;
            invite.AcceptedAt = DateTime.UtcNow;
            invite.AcceptedByUserId = user.Id;

            _db.ClubMemberships.Add(new ClubMembership
            {
                ClubId = linkedClubId.Value,
                UserId = user.Id
            });

            await _db.SaveChangesAsync(ct);
        }

        var authTokens = await _tokens.GenerateTokensAsync(user, ct);
        return Result.Success(new RegisterResultDto(IsPendingApproval: false, Tokens: authTokens));
    }

    public async Task<Result<AuthTokenDto>> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user == null)
            return Result.Failure<AuthTokenDto>("Invalid credentials.", "INVALID_CREDENTIALS");

        if (!user.IsActive)
            return Result.Failure<AuthTokenDto>("Your account has been deactivated.", "ACCOUNT_INACTIVE");

        var result = await _signIn.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
            return Result.Failure<AuthTokenDto>("Account locked. Try again in 15 minutes.", "ACCOUNT_LOCKED");
        if (!result.Succeeded)
            return Result.Failure<AuthTokenDto>("Invalid credentials.", "INVALID_CREDENTIALS");

        user.LastLoginAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        return Result.Success(await _tokens.GenerateTokensAsync(user, ct));
    }

    public async Task<Result<AuthTokenDto>> RefreshTokenAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        var stored = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == req.RefreshToken, ct);

        if (stored == null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            return Result.Failure<AuthTokenDto>("Invalid or expired refresh token.", "INVALID_REFRESH_TOKEN");

        stored.IsRevoked = true;
        stored.RevokedReason = "Rotated";

        var newTokens = await _tokens.GenerateTokensAsync(stored.User, ct);
        stored.ReplacedByToken = newTokens.RefreshToken;

        await _db.SaveChangesAsync(ct);
        return Result.Success(newTokens);
    }

    public async Task<Result> RevokeTokenAsync(RevokeTokenRequest req, CancellationToken ct)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == req.RefreshToken, ct);

        if (token == null) return Result.NotFound("Token");
        if (token.IsRevoked) return Result.Failure("Token already revoked.", "ALREADY_REVOKED");

        token.IsRevoked = true;
        token.RevokedReason = "Manual revocation";
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user == null) return Result.NotFound<UserDto>("User");

        Guid? clubId = null;
        if (user.Role == UserRole.ClubManager || user.Role == UserRole.Fancier)
        {
            clubId = await _db.ClubMemberships
                .Where(m => m.UserId == user.Id && m.IsActive && !m.IsDeleted)
                .OrderBy(m => m.JoinedAt)
                .Select(m => (Guid?)m.ClubId)
                .FirstOrDefaultAsync(ct);
        }

        return Result.Success(user.ToDto(clubId));
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest req, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user == null) return Result.NotFound("User");

        var result = await _users.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors, "CHANGE_PASSWORD_FAILED");
        }

        return Result.Success();
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest req, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user == null || !user.IsActive) return Result.Success();

        var token = await _users.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var appUrl = _config["App:FrontendUrl"] ?? "http://localhost:4300";
        var resetLink = $"{appUrl}/auth/reset-password?email={Uri.EscapeDataString(req.Email)}&token={encodedToken}";

        await _email.SendAsync(req.Email, "Reset your password",
            $"""
             <h2>Password Reset</h2>
             <p>Click the link below to reset your password. This link expires in 1 hour.</p>
             <p><a href='{resetLink}' style='background:#1a73e8;color:white;padding:12px 24px;border-radius:4px;text-decoration:none;'>Reset Password</a></p>
             <p>If you did not request this, ignore this email.</p>
             """, ct);

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user == null) return Result.Failure("Invalid reset request.", "INVALID_TOKEN");

        var result = await _users.ResetPasswordAsync(user, req.Token, req.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors, "RESET_FAILED");
        }

        return Result.Success();
    }

    public async Task<Result> VerifyEmailAsync(VerifyEmailRequest req, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(req.UserId);
        if (user == null) return Result.Failure("Invalid verification link.", "INVALID_TOKEN");

        var result = await _users.ConfirmEmailAsync(user, req.Token);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors, "VERIFICATION_FAILED");
        }

        return Result.Success();
    }

    public async Task<Result> ResendVerificationAsync(ResendVerificationRequest req, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user == null || user.EmailConfirmed) return Result.Success();

        var token = await _users.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var appUrl = _config["App:FrontendUrl"] ?? "http://localhost:4300";
        var verifyLink = $"{appUrl}/auth/verify-email?userId={user.Id}&token={encodedToken}";

        await _email.SendAsync(req.Email, "Verify your email address",
            $"""
             <h2>Email Verification</h2>
             <p>Click the link below to verify your email address.</p>
             <p><a href='{verifyLink}' style='background:#1a73e8;color:white;padding:12px 24px;border-radius:4px;text-decoration:none;'>Verify Email</a></p>
             <p>If you did not create an account, ignore this email.</p>
             """, ct);

        return Result.Success();
    }
}
