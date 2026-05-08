using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PRC.Common;

namespace PRC.AdminService.Services;

public interface IAdminTokenService
{
    string GenerateAdminToken(Guid userId, string fullName, string role = "SuperAdmin");
    string GenerateImpersonationToken(Guid adminId, Guid targetUserId, string targetName, string reason);
}

public class AdminTokenService : IAdminTokenService
{
    private readonly IConfiguration _config;

    public AdminTokenService(IConfiguration config) => _config = config;

    public string GenerateAdminToken(Guid userId, string fullName, string role = "SuperAdmin")
    {
        var key     = _config["Jwt:AdminKey"] ?? throw new InvalidOperationException("Jwt:AdminKey is required.");
        var issuer  = _config["Jwt:AdminIssuer"] ?? "PRC.AdminService";
        var audience= _config["Jwt:AdminAudience"] ?? "PRC.Admin";
        var expiry  = int.Parse(_config["Jwt:AdminExpiryMinutes"] ?? "60");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, fullName),
            new Claim(ClaimTypes.Role, role),
            new Claim("token_type", "admin")
        };

        return BuildToken(key, issuer, audience, claims, expiry);
    }

    public string GenerateImpersonationToken(Guid adminId, Guid targetUserId, string targetName, string reason)
    {
        var key      = _config["Jwt:AdminKey"] ?? throw new InvalidOperationException("Jwt:AdminKey is required.");
        var issuer   = _config["Jwt:AdminIssuer"] ?? "PRC.AdminService";
        var audience = _config["Jwt:Audience"] ?? "PRC.Services";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, targetUserId.ToString()),
            new Claim(ClaimTypes.Name, targetName),
            new Claim("impersonated_by", adminId.ToString()),
            new Claim("impersonation_reason", reason),
            new Claim("token_type", "impersonation")
        };

        // 30-minute impersonation window
        return BuildToken(key, issuer, audience, claims, 30);
    }

    private static string BuildToken(string key, string issuer, string audience,
        Claim[] claims, int expiryMinutes)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds      = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token      = new JwtSecurityToken(
            issuer:              issuer,
            audience:            audience,
            claims:              claims,
            notBefore:           DateTime.UtcNow,
            expires:             DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials:  creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
