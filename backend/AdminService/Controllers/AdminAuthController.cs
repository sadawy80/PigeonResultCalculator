using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.AdminService.DTOs;
using PRC.AdminService.Services;
using PRC.Common;
using System.Security.Claims;

namespace PRC.AdminService.Controllers;

[Route("api/admin/auth")]
[ApiController]
public class AdminAuthController : AdminControllerBase
{
    private readonly IBusAdminClient  _bus;
    private readonly IAdminTokenService _tokens;
    private readonly IAuditService     _audit;

    public AdminAuthController(IBusAdminClient bus, IAdminTokenService tokens, IAuditService audit)
    {
        _bus    = bus;
        _tokens = tokens;
        _audit  = audit;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest req, CancellationToken ct)
    {
        var user = await _bus.ValidateCredentialsAsync(req.Email, req.Password, ct);

        if (user is null || !user.IsValid || !user.IsActive || user.Role != UserRole.SuperAdmin)
        {
            await _audit.LogAsync("LOGIN_FAILED", "User", null, AuditSeverity.Warning,
                $"Failed admin login attempt for {req.Email}",
                null, null, CorrelationId, ClientIp, ct);
            return Problem(detail: "Invalid credentials or insufficient permissions.", statusCode: 401);
        }

        var token = _tokens.GenerateAdminToken(user.UserId, user.FullName);

        await _audit.LogAsync("LOGIN", "User", user.UserId, AuditSeverity.Info,
            $"Admin login: {user.FullName} ({req.Email}) from {ClientIp}",
            user.UserId, user.FullName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            Token     = token,
            UserId    = user.UserId,
            FullName  = user.FullName,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        }));
    }

    [HttpPost("impersonate")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Impersonate([FromBody] ImpersonateRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Reason))
            return Problem(detail: "Impersonation reason is required.", statusCode: 400);

        var adminId   = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var adminName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

        var token = _tokens.GenerateImpersonationToken(adminId, req.TargetUserId, "User", req.Reason);

        await _audit.LogAsync("IMPERSONATION", "User", req.TargetUserId, AuditSeverity.Critical,
            $"Admin '{adminName}' impersonating user {req.TargetUserId}. Reason: {req.Reason}",
            adminId, adminName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            Token        = token,
            TargetUserId = req.TargetUserId,
            ExpiresAt    = DateTime.UtcNow.AddMinutes(30),
            Warning      = "This impersonation session is fully audited."
        }));
    }
}
