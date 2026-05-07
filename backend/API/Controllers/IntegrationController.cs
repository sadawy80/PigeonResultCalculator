using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PigeonRacing.Application.Common;
using PigeonRacing.Application.Features.Integration;
using PigeonRacing.Domain.Entities;

namespace PigeonRacing.API.Controllers;

// ─────────────────────────────────────────────────────────────────────────────
//  IntegrationController
//
//  Two audiences use these endpoints:
//
//  A) PigeonLoftManager.com (server-to-server):
//     POST /api/integrations/link/request  — initiate link (no auth, PLM uses API key)
//     POST /api/integrations/link/{id}/revoke — revoke via link token
//     GET  /api/integrations/data/results  — pull race results (Bearer = access token)
//     GET  /api/integrations/data/ace-pigeon
//     GET  /api/integrations/data/super-ace
//     GET  /api/integrations/data/best-loft
//     GET  /api/integrations/data/summary
//
//  B) PRC Club Manager (JWT):
//     GET  /api/integrations/club/{clubId}/requests — pending requests
//     POST /api/integrations/link/{id}/approve      — approve
//     POST /api/integrations/link/{id}/reject        — reject
//     GET  /api/integrations/club/{clubId}/links     — all links
//     DELETE /api/integrations/link/{id}             — revoke
//
//  C) PRC Fancier (JWT):
//     GET  /api/integrations/my-links — own links
// ─────────────────────────────────────────────────────────────────────────────

[Route("api/integrations")]
public class IntegrationController : ApiControllerBase
{
    // ── A. PigeonLoftManager server-to-server endpoints ──────────────────────

    /// <summary>
    /// Called by PLM when a fancier clicks "Link to PigeonResultCalculator".
    /// No JWT required — PLM is trusted by API key in the X-API-Key header.
    /// Returns a LinkToken that PLM stores and shows to the fancier as confirmation.
    /// </summary>
    [HttpPost("link/request")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestLink(
        [FromBody] LinkRequestBody body,
        CancellationToken ct)
    {
        // Validate API key from PLM
        if (!IsValidPlatformApiKey(Request.Headers["X-PLM-Api-Key"].ToString()))
            return Unauthorized(new { error = "Invalid or missing platform API key." });

        var result = await Mediator.Send(new RequestExternalLinkCommand(
            body.ExternalPlatformName ?? "PigeonLoftManager",
            body.ExternalUserId,
            body.ExternalLoftId,
            body.ExternalLoftName,
            body.CallbackUrl,
            body.ClubId,
            body.PrcUserId,
            body.MetadataJson), ct);

        return FromResult(result);
    }

    /// <summary>
    /// PLM calls this to revoke an active link on behalf of the fancier.
    /// Uses the LinkToken (not an access token) to identify the link.
    /// </summary>
    [HttpPost("link/revoke-by-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RevokeByToken(
        [FromBody] RevokeByTokenBody body,
        CancellationToken ct)
    {
        if (!IsValidPlatformApiKey(Request.Headers["X-PLM-Api-Key"].ToString()))
            return Unauthorized(new { error = "Invalid or missing platform API key." });

        // Look up the link by token
        var link = await GetLinkByTokenAsync(body.LinkToken, ct);
        if (link == null) return NotFound(new { error = "Link not found." });

        var result = await Mediator.Send(
            new RevokeLinkCommand(link.Id, body.Reason ?? "Revoked by external platform", true), ct);

        return FromResult(result);
    }

    // ── A. Data endpoints — authenticated by access token ────────────────────

    /// <summary>
    /// Returns the fancier's race results. Paginated.
    /// Auth: Bearer {accessToken} issued on approval, OR ?token= query param.
    /// </summary>
    [HttpGet("data/results")]
    [AllowAnonymous]
    public async Task<IActionResult> GetResults(
        [FromQuery] string? token,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var accessToken = ExtractAccessToken(token);
        if (string.IsNullOrEmpty(accessToken))
            return Unauthorized(new { error = "Access token required." });

        return FromResult(await Mediator.Send(
            new GetLinkedRaceResultsQuery(accessToken, page, pageSize), ct));
    }

    /// <summary>Returns all Ace Pigeon results across published programmes.</summary>
    [HttpGet("data/ace-pigeon")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAcePigeon(
        [FromQuery] string? token, CancellationToken ct = default)
    {
        var accessToken = ExtractAccessToken(token);
        if (string.IsNullOrEmpty(accessToken))
            return Unauthorized(new { error = "Access token required." });

        return FromResult(await Mediator.Send(new GetLinkedAcePigeonQuery(accessToken), ct));
    }

    /// <summary>Returns all Super Ace Pigeon results across published programmes.</summary>
    [HttpGet("data/super-ace")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSuperAce(
        [FromQuery] string? token, CancellationToken ct = default)
    {
        var accessToken = ExtractAccessToken(token);
        if (string.IsNullOrEmpty(accessToken))
            return Unauthorized(new { error = "Access token required." });

        return FromResult(await Mediator.Send(new GetLinkedSuperAceQuery(accessToken), ct));
    }

    /// <summary>Returns all Best Loft results across published programmes.</summary>
    [HttpGet("data/best-loft")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBestLoft(
        [FromQuery] string? token, CancellationToken ct = default)
    {
        var accessToken = ExtractAccessToken(token);
        if (string.IsNullOrEmpty(accessToken))
            return Unauthorized(new { error = "Access token required." });

        return FromResult(await Mediator.Send(new GetLinkedBestLoftQuery(accessToken), ct));
    }

    /// <summary>Returns a full summary: counts, best stats, achievements list.</summary>
    [HttpGet("data/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSummary(
        [FromQuery] string? token, CancellationToken ct = default)
    {
        var accessToken = ExtractAccessToken(token);
        if (string.IsNullOrEmpty(accessToken))
            return Unauthorized(new { error = "Access token required." });

        return FromResult(await Mediator.Send(new GetLinkedSummaryQuery(accessToken), ct));
    }

    // ── B. Club Manager endpoints (JWT) ──────────────────────────────────────

    /// <summary>
    /// All link requests for a club — optionally filtered by status.
    /// Visible to club managers.
    /// </summary>
    [HttpGet("club/{clubId:guid}/links")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> GetClubLinks(
        Guid clubId,
        [FromQuery] ExternalLinkStatus? status = null,
        CancellationToken ct = default)
        => FromResult(await Mediator.Send(new GetClubLinksQuery(clubId, status), ct));

    /// <summary>Approve a pending link request.</summary>
    [HttpPost("link/{linkId:guid}/approve")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> Approve(Guid linkId, CancellationToken ct)
        => FromResult(await Mediator.Send(
            new ReviewLinkRequestCommand(linkId, true, null), ct));

    /// <summary>Reject a pending link request.</summary>
    [HttpPost("link/{linkId:guid}/reject")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> Reject(
        Guid linkId,
        [FromBody] RejectLinkBody body,
        CancellationToken ct)
        => FromResult(await Mediator.Send(
            new ReviewLinkRequestCommand(linkId, false, body.Reason), ct));

    /// <summary>Revoke an approved link (club manager or admin).</summary>
    [HttpDelete("link/{linkId:guid}")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> RevokeLinkJwt(
        Guid linkId,
        [FromBody] RevokeBody? body,
        CancellationToken ct)
        => FromResult(await Mediator.Send(
            new RevokeLinkCommand(linkId, body?.Reason), ct));

    // ── C. Fancier endpoints (JWT) ────────────────────────────────────────────

    /// <summary>The current fancier's own links (all statuses).</summary>
    [HttpGet("my-links")]
    [Authorize]
    public async Task<IActionResult> GetMyLinks(CancellationToken ct)
        => FromResult(await Mediator.Send(new GetMyLinksQuery(), ct));

    /// <summary>Fancier revokes their own link.</summary>
    [HttpDelete("my-links/{linkId:guid}")]
    [Authorize]
    public async Task<IActionResult> RevokeMyLink(Guid linkId, CancellationToken ct)
        => FromResult(await Mediator.Send(new RevokeLinkCommand(linkId, "Revoked by fancier"), ct));

    /// <summary>
    /// JWT-authenticated: fancier views their own linked summary without needing the PLM access token.
    /// </summary>
    [HttpGet("my-links/{linkId:guid}/summary")]
    [Authorize]
    public async Task<IActionResult> GetMySummary(Guid linkId, CancellationToken ct)
    {
        var db = HttpContext.RequestServices.GetRequiredService<PigeonRacing.Application.Common.Interfaces.IAppDbContext>();
        var userId = GetCurrentUserId();
        var link = await db.ExternalLinks.FirstOrDefaultAsync(
            l => l.Id == linkId && l.UserId == userId && l.Status == ExternalLinkStatus.Approved, ct);
        if (link?.AccessToken == null) return NotFound(new { error = "Active link not found." });
        return FromResult(await Mediator.Send(new GetLinkedSummaryQuery(link.AccessToken), ct));
    }

    [HttpGet("my-links/{linkId:guid}/results")]
    [Authorize]
    public async Task<IActionResult> GetMyResults(Guid linkId, [FromQuery] int page = 1, CancellationToken ct = default)
    {
        var db = HttpContext.RequestServices.GetRequiredService<PigeonRacing.Application.Common.Interfaces.IAppDbContext>();
        var userId = GetCurrentUserId();
        var link = await db.ExternalLinks.FirstOrDefaultAsync(
            l => l.Id == linkId && l.UserId == userId && l.Status == ExternalLinkStatus.Approved, ct);
        if (link?.AccessToken == null) return NotFound();
        return FromResult(await Mediator.Send(new GetLinkedRaceResultsQuery(link.AccessToken, page), ct));
    }

    [HttpGet("my-links/{linkId:guid}/ace-pigeon")]
    [Authorize]
    public async Task<IActionResult> GetMyAce(Guid linkId, CancellationToken ct)
    {
        var db = HttpContext.RequestServices.GetRequiredService<PigeonRacing.Application.Common.Interfaces.IAppDbContext>();
        var userId = GetCurrentUserId();
        var link = await db.ExternalLinks.FirstOrDefaultAsync(
            l => l.Id == linkId && l.UserId == userId && l.Status == ExternalLinkStatus.Approved, ct);
        if (link?.AccessToken == null) return NotFound();
        return FromResult(await Mediator.Send(new GetLinkedAcePigeonQuery(link.AccessToken), ct));
    }

    [HttpGet("my-links/{linkId:guid}/super-ace")]
    [Authorize]
    public async Task<IActionResult> GetMySuperAce(Guid linkId, CancellationToken ct)
    {
        var db = HttpContext.RequestServices.GetRequiredService<PigeonRacing.Application.Common.Interfaces.IAppDbContext>();
        var userId = GetCurrentUserId();
        var link = await db.ExternalLinks.FirstOrDefaultAsync(
            l => l.Id == linkId && l.UserId == userId && l.Status == ExternalLinkStatus.Approved, ct);
        if (link?.AccessToken == null) return NotFound();
        return FromResult(await Mediator.Send(new GetLinkedSuperAceQuery(link.AccessToken), ct));
    }

    [HttpGet("my-links/{linkId:guid}/best-loft")]
    [Authorize]
    public async Task<IActionResult> GetMyBestLoft(Guid linkId, CancellationToken ct)
    {
        var db = HttpContext.RequestServices.GetRequiredService<PigeonRacing.Application.Common.Interfaces.IAppDbContext>();
        var userId = GetCurrentUserId();
        var link = await db.ExternalLinks.FirstOrDefaultAsync(
            l => l.Id == linkId && l.UserId == userId && l.Status == ExternalLinkStatus.Approved, ct);
        if (link?.AccessToken == null) return NotFound();
        return FromResult(await Mediator.Send(new GetLinkedBestLoftQuery(link.AccessToken), ct));
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                    ?? User.FindFirst("sub");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string? ExtractAccessToken(string? queryToken)
    {
        // Prefer Authorization: Bearer {token} header
        var authHeader = Request.Headers.Authorization.ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authHeader[7..].Trim();

        return queryToken;
    }

    private bool IsValidPlatformApiKey(string? key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        // In production, read from config: Integration:PigeonLoftManagerApiKey
        var configured = HttpContext.RequestServices
            .GetService<Microsoft.Extensions.Configuration.IConfiguration>()
            ?["Integration:PigeonLoftManagerApiKey"];

        // Allow any key during development
        if (string.IsNullOrEmpty(configured)) return true;
        return key == configured;
    }

    private async Task<PigeonRacing.Domain.Entities.ExternalLink?> GetLinkByTokenAsync(
        string linkToken, CancellationToken ct)
    {
        var db = HttpContext.RequestServices
            .GetRequiredService<PigeonRacing.Application.Common.Interfaces.IAppDbContext>();
        return await db.ExternalLinks
            .FirstOrDefaultAsync(l => l.LinkToken == linkToken, ct);
    }
}

// ── Request/body models ───────────────────────────────────────────────────────

public record LinkRequestBody(
    string ExternalUserId,
    string ExternalLoftId,
    string ExternalLoftName,
    string CallbackUrl,
    Guid ClubId,
    string? ExternalPlatformName,
    Guid? PrcUserId,
    string? MetadataJson);

public record RevokeByTokenBody(string LinkToken, string? Reason);
public record RejectLinkBody(string? Reason);
public record RevokeBody(string? Reason);
