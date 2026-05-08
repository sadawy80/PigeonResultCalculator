using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PRC.Common;
using PRC.IdentityService.Data;
using PRC.IdentityService.DTOs;
using PRC.IdentityService.Models;

namespace PRC.IdentityService.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly IdentityDbContext _db;

    public TokenService(IConfiguration config, IdentityDbContext db)
    {
        _config = config;
        _db = db;
    }

    public async Task<AuthTokenDto> GenerateTokensAsync(ApplicationUser user, CancellationToken ct = default)
    {
        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured.");
        var issuer = _config["Jwt:Issuer"] ?? "PRC";
        var audience = _config["Jwt:Audience"] ?? "PRC";
        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

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

        if (user.FederationId.HasValue)
            claims.Add(new Claim("federationId", user.FederationId.Value.ToString()));

        if (clubId.HasValue)
            claims.Add(new Claim("clubId", clubId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(issuer, audience, claims,
            expires: expires, signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

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

public static class UserMappingExtensions
{
    public static UserDto ToDto(this ApplicationUser user, Guid? clubId = null) => new(
        user.Id,
        user.Email!,
        user.FirstName,
        user.LastName,
        user.FullName,
        user.Role,
        user.FederationId,
        clubId,
        user.ProfileImageUrl,
        user.IsActive);
}
