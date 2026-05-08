using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.Common;
using PRC.IdentityService.DTOs;
using PRC.IdentityService.Services;

namespace PRC.IdentityService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
        => FromResult(await _auth.RegisterAsync(req, ct));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
        => FromResult(await _auth.LoginAsync(req, ct));

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest req, CancellationToken ct)
        => FromResult(await _auth.RefreshTokenAsync(req, ct));

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest req, CancellationToken ct)
        => FromResult(await _auth.RevokeTokenAsync(req, ct));

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var id))
            return Unauthorized();
        return FromResult(await _auth.GetCurrentUserAsync(id, ct));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var id))
            return Unauthorized();
        return FromResult(await _auth.ChangePasswordAsync(id, req, ct));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req, CancellationToken ct)
        => FromResult(await _auth.ForgotPasswordAsync(req, ct));

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req, CancellationToken ct)
        => FromResult(await _auth.ResetPasswordAsync(req, ct));

    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(
        [FromQuery] string userId, [FromQuery] string token, CancellationToken ct)
        => FromResult(await _auth.VerifyEmailAsync(new VerifyEmailRequest(userId, token), ct));

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest req, CancellationToken ct)
        => FromResult(await _auth.ResendVerificationAsync(req, ct));

    private IActionResult FromResult<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(ApiResponse<T>.Ok(result.Value!));
        return result.ErrorCode switch
        {
            "NOT_FOUND"        => NotFound(ApiResponse<T>.Fail(result.Error, result.ErrorCode)),
            "FORBIDDEN"        => Forbid(),
            "CONFLICT"         => Conflict(ApiResponse<T>.Fail(result.Error, result.ErrorCode)),
            "VALIDATION_ERROR" => BadRequest(ApiResponse<T>.Fail(result.Error, result.ErrorCode)),
            _                  => BadRequest(ApiResponse<T>.Fail(result.Error, result.ErrorCode))
        };
    }

    private IActionResult FromResult(Result result)
    {
        if (result.IsSuccess) return Ok(ApiResponse<object?>.Ok(null, "Success"));
        return result.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(ApiResponse<object>.Fail(result.Error, result.ErrorCode)),
            "FORBIDDEN" => Forbid(),
            "CONFLICT"  => Conflict(ApiResponse<object>.Fail(result.Error, result.ErrorCode)),
            _           => BadRequest(ApiResponse<object>.Fail(result.Error, result.ErrorCode))
        };
    }
}
