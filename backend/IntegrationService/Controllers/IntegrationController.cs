using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.Common;
using PRC.IntegrationService.DTOs;
using PRC.IntegrationService.Models;
using PRC.IntegrationService.Services;

namespace PRC.IntegrationService.Controllers;

[Route("api/integrations")]
[ApiController]
public class IntegrationController : ControllerBase
{
    private readonly IIntegrationService _svc;
    private readonly IIntegrationDataService _data;
    private readonly IConfiguration _config;

    public IntegrationController(
        IIntegrationService svc,
        IIntegrationDataService data,
        IConfiguration config)
    {
        _svc = svc;
        _data = data;
        _config = config;
    }

    private Guid CurrentUserId => Guid.TryParse(
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    // ── A. PLM server-to-server endpoints ────────────────────────────────────

    [HttpPost("link/request")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestLink([FromBody] LinkRequestBody body, CancellationToken ct)
    {
        if (!IsValidApiKey(Request.Headers["X-PLM-Api-Key"].ToString()))
            return Unauthorized(new { error = "Invalid or missing platform API key." });

        var result = await _svc.RequestLinkAsync(
            body.ExternalPlatformName ?? "PigeonLoftManager",
            body.ExternalUserId, body.ExternalLoftId, body.ExternalLoftName,
            body.CallbackUrl, body.ClubId, body.PrcUserId, body.MetadataJson, ct);

        return result.IsSuccess ? Ok(result.Value) : Conflict(result.Error);
    }

    [HttpPost("link/revoke-by-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RevokeByToken([FromBody] RevokeByTokenBody body, CancellationToken ct)
    {
        if (!IsValidApiKey(Request.Headers["X-PLM-Api-Key"].ToString()))
            return Unauthorized(new { error = "Invalid or missing platform API key." });

        var result = await _svc.RevokeByTokenAsync(body.LinkToken, body.Reason, ct);
        return result.IsSuccess ? Ok() : NotFound(result.Error);
    }

    // ── A. Data endpoints (access-token authenticated) ────────────────────────

    [HttpGet("data/results")]
    [AllowAnonymous]
    public async Task<IActionResult> GetResults(
        [FromQuery] string? token,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (link, err) = await ResolveTokenAsync(token, ct);
        if (link == null) return Unauthorized(new { error = err });

        var result = await _data.GetRaceResultsAsync(link.UserId, link.ClubId, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(503, result.Error);
    }

    [HttpGet("data/ace-pigeon")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAcePigeon([FromQuery] string? token, CancellationToken ct = default)
    {
        var (link, err) = await ResolveTokenAsync(token, ct);
        if (link == null) return Unauthorized(new { error = err });

        var result = await _data.GetAcePigeonAsync(link.UserId, link.ClubId, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(503, result.Error);
    }

    [HttpGet("data/super-ace")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSuperAce([FromQuery] string? token, CancellationToken ct = default)
    {
        var (link, err) = await ResolveTokenAsync(token, ct);
        if (link == null) return Unauthorized(new { error = err });

        var result = await _data.GetSuperAceAsync(link.UserId, link.ClubId, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(503, result.Error);
    }

    [HttpGet("data/best-loft")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBestLoft([FromQuery] string? token, CancellationToken ct = default)
    {
        var (link, err) = await ResolveTokenAsync(token, ct);
        if (link == null) return Unauthorized(new { error = err });

        var result = await _data.GetBestLoftAsync(link.UserId, link.ClubId, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(503, result.Error);
    }

    [HttpGet("data/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSummary([FromQuery] string? token, CancellationToken ct = default)
    {
        var (link, err) = await ResolveTokenAsync(token, ct);
        if (link == null) return Unauthorized(new { error = err });

        var result = await _data.GetSummaryAsync(link.UserId, link.ClubId, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(503, result.Error);
    }

    // ── B. Club manager endpoints (JWT) ───────────────────────────────────────

    [HttpGet("club/{clubId:guid}/links")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> GetClubLinks(
        Guid clubId,
        [FromQuery] ExternalLinkStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _svc.GetClubLinksAsync(clubId, status, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("link/{linkId:guid}/approve")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> Approve(Guid linkId, CancellationToken ct)
    {
        var result = await _svc.ReviewLinkAsync(linkId, true, null, CurrentUserId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("link/{linkId:guid}/reject")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> Reject(Guid linkId, [FromBody] RejectLinkBody body, CancellationToken ct)
    {
        var result = await _svc.ReviewLinkAsync(linkId, false, body.Reason, CurrentUserId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("link/{linkId:guid}")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> RevokeLink(Guid linkId, [FromBody] RevokeBody? body, CancellationToken ct)
    {
        var result = await _svc.RevokeLinkAsync(linkId, body?.Reason, ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    // ── C. Fancier endpoints (JWT) ─────────────────────────────────────────────

    [HttpGet("my-links")]
    [Authorize]
    public async Task<IActionResult> GetMyLinks(CancellationToken ct)
    {
        var result = await _svc.GetMyLinksAsync(CurrentUserId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("my-links/{linkId:guid}")]
    [Authorize]
    public async Task<IActionResult> RevokeMyLink(Guid linkId, CancellationToken ct)
    {
        var result = await _svc.RevokeLinkAsync(linkId, "Revoked by fancier", ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpGet("my-links/{linkId:guid}/summary")]
    [Authorize]
    public async Task<IActionResult> GetMySummary(Guid linkId, CancellationToken ct)
    {
        var linksResult = await _svc.GetMyLinksAsync(CurrentUserId, ct);
        if (!linksResult.IsSuccess) return Unauthorized();
        var link = linksResult.Value!.Find(l => l.Id == linkId && l.Status == ExternalLinkStatus.Approved);
        if (link == null) return NotFound();
        var result = await _data.GetSummaryAsync(link.UserId, link.ClubId, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(503, result.Error);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(Models.ExternalLink? Link, string? Error)> ResolveTokenAsync(
        string? queryToken, CancellationToken ct)
    {
        var authHeader = Request.Headers.Authorization.ToString();
        var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader[7..].Trim()
            : queryToken;

        if (string.IsNullOrEmpty(token)) return (null, "Access token required.");

        var result = await _svc.ValidateTokenAsync(token, ct);
        if (!result.IsSuccess) return (null, result.Error);
        return (result.Value.Link, null);
    }

    private bool IsValidApiKey(string? key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        var configured = _config["Integration:PigeonLoftManagerApiKey"];
        if (string.IsNullOrEmpty(configured)) return true; // allow any key in dev
        return key == configured;
    }
}
