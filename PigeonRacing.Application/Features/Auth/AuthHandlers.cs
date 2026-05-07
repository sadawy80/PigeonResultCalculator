using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PigeonRacing.Application.Common;
using PigeonRacing.Application.Common.Interfaces;
using PigeonRacing.Domain.Entities;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.Application.Features.Auth;

// ── Register Handler ──────────────────────────────────────────────────────────

public class RegisterHandler : IRequestHandler<RegisterCommand, Result<AuthTokenDto>>
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IAppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ITokenService _tokenService;

    public RegisterHandler(UserManager<ApplicationUser> users, IAppDbContext db,
        IConfiguration config, ITokenService tokenService)
    {
        _users = users;
        _db = db;
        _config = config;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthTokenDto>> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        if (await _users.FindByEmailAsync(cmd.Email) != null)
            return Result.Conflict<AuthTokenDto>("An account with this email already exists.");

        // Validate invitation token if provided
        Guid? linkedClubId = null;
        if (!string.IsNullOrEmpty(cmd.InvitationToken))
        {
            var invite = await _db.Invitations
                .FirstOrDefaultAsync(i => i.Token == cmd.InvitationToken
                    && i.Status == InvitationStatus.Pending
                    && i.ExpiresAt > DateTime.UtcNow, ct);

            if (invite == null)
                return Result.Failure<AuthTokenDto>("Invalid or expired invitation token.", "INVALID_INVITATION");

            linkedClubId = invite.ClubId;
        }

        var user = new ApplicationUser
        {
            UserName = cmd.Email,
            Email = cmd.Email,
            FirstName = cmd.FirstName,
            LastName = cmd.LastName,
            Role = cmd.Role,
            CountryId = cmd.CountryId,
            IsActive = true
        };

        var createResult = await _users.CreateAsync(user, cmd.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            return Result.Failure<AuthTokenDto>(errors, "REGISTRATION_FAILED");
        }

        await _users.AddToRoleAsync(user, cmd.Role.ToString());

        // Accept invitation & create membership
        if (linkedClubId.HasValue)
        {
            var invite = await _db.Invitations
                .FirstAsync(i => i.Token == cmd.InvitationToken!, ct);

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

        return Result.Success(await _tokenService.GenerateTokensAsync(user, ct));
    }
}

// ── Login Handler ─────────────────────────────────────────────────────────────

public class LoginHandler : IRequestHandler<LoginCommand, Result<AuthTokenDto>>
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly ITokenService _tokenService;

    public LoginHandler(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn,
        ITokenService tokenService)
    {
        _users = users;
        _signIn = signIn;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthTokenDto>> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(cmd.Email);
        if (user == null || !user.IsActive)
            return Result.Failure<AuthTokenDto>("Invalid credentials.", "INVALID_CREDENTIALS");

        var result = await _signIn.CheckPasswordSignInAsync(user, cmd.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
            return Result.Failure<AuthTokenDto>("Account locked. Try again in 15 minutes.", "ACCOUNT_LOCKED");
        if (!result.Succeeded)
            return Result.Failure<AuthTokenDto>("Invalid credentials.", "INVALID_CREDENTIALS");

        user.LastLoginAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        return Result.Success(await _tokenService.GenerateTokensAsync(user, ct));
    }
}

// ── Refresh Token Handler ─────────────────────────────────────────────────────

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, Result<AuthTokenDto>>
{
    private readonly IAppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ITokenService _tokenService;

    public RefreshTokenHandler(IAppDbContext db, UserManager<ApplicationUser> users, ITokenService tokenService)
    {
        _db = db;
        _users = users;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthTokenDto>> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var stored = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == cmd.RefreshToken, ct);

        if (stored == null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            return Result.Failure<AuthTokenDto>("Invalid or expired refresh token.", "INVALID_REFRESH_TOKEN");

        // Rotate token
        stored.IsRevoked = true;
        stored.RevokedReason = "Rotated";

        var newTokens = await _tokenService.GenerateTokensAsync(stored.User, ct);
        stored.ReplacedByToken = newTokens.RefreshToken;

        await _db.SaveChangesAsync(ct);
        return Result.Success(newTokens);
    }
}

// ── Revoke Token Handler ──────────────────────────────────────────────────────

public class RevokeTokenHandler : IRequestHandler<RevokeTokenCommand, Result>
{
    private readonly IAppDbContext _db;

    public RevokeTokenHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(RevokeTokenCommand cmd, CancellationToken ct)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == cmd.RefreshToken, ct);

        if (token == null) return Result.NotFound("Token");
        if (token.IsRevoked) return Result.Failure("Token already revoked.", "ALREADY_REVOKED");

        token.IsRevoked = true;
        token.RevokedReason = "Manual revocation";
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── Get Current User Handler ──────────────────────────────────────────────────

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IAppDbContext _db;

    public GetCurrentUserHandler(
        ICurrentUserService currentUser,
        UserManager<ApplicationUser> users,
        IAppDbContext db)
    {
        _currentUser = currentUser;
        _users = users;
        _db = db;
    }

    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery _, CancellationToken ct)
    {
        if (!_currentUser.UserId.HasValue)
            return Result.Failure<UserDto>("Not authenticated.", "UNAUTHENTICATED");

        var user = await _users.FindByIdAsync(_currentUser.UserId.Value.ToString());
        if (user == null) return Result.NotFound<UserDto>("User");

        // Resolve primary club for ClubManagers
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
}

// ── Token Service ─────────────────────────────────────────────────────────────

public interface ITokenService
{
    Task<AuthTokenDto> GenerateTokensAsync(ApplicationUser user, CancellationToken ct = default);
}

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly IAppDbContext _db;

    public JwtTokenService(IConfiguration config, IAppDbContext db)
    {
        _config = config;
        _db = db;
    }

    public async Task<AuthTokenDto> GenerateTokensAsync(ApplicationUser user, CancellationToken ct = default)
    {
        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured.");
        var issuer = _config["Jwt:Issuer"] ?? "PigeonRacing";
        var audience = _config["Jwt:Audience"] ?? "PigeonRacing";
        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

        // Resolve primary club for ClubManagers and Fanciers
        Guid? clubId = null;
        if (user.Role == UserRole.ClubManager || user.Role == UserRole.Fancier)
        {
            clubId = await _db.ClubMemberships
                .Where(m => m.UserId == user.Id && m.IsActive && !m.IsDeleted)
                .OrderBy(m => m.JoinedAt)
                .Select(m => (Guid?)m.ClubId)
                .FirstOrDefaultAsync(ct);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.Email!),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (user.CountryId.HasValue)
            claims.Add(new Claim("countryId", user.CountryId.Value.ToString()));

        if (clubId.HasValue)
            claims.Add(new Claim("clubId", clubId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(issuer, audience, claims,
            expires: expires, signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // Generate and persist refresh token
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });
        await _db.SaveChangesAsync(ct);

        return new AuthTokenDto(accessToken, refreshToken, expires, user.ToDto(clubId));
    }
}

// ── Mapper extension ──────────────────────────────────────────────────────────

public static class UserMappingExtensions
{
    public static UserDto ToDto(this ApplicationUser user, Guid? clubId = null) => new(
        user.Id,
        user.Email!,
        user.FirstName,
        user.LastName,
        user.FullName,
        user.Role,
        user.CountryId,
        clubId,
        user.ProfileImageUrl,
        user.IsActive);
}
