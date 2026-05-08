using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRC.AdminService.Data;
using PRC.AdminService.DTOs;
using PRC.AdminService.Models;
using PRC.AdminService.Services;
using PRC.Common;
using PRC.Common.Messages;
using System.Security.Claims;
using Messages = PRC.Common.Messages;

namespace PRC.AdminService.Controllers;

[Route("api/admin")]
[Authorize(Roles = "SuperAdmin")]
public class AdminController : AdminControllerBase
{
    private readonly AdminDbContext   _db;
    private readonly IBusAdminClient  _bus;
    private readonly IAuditService    _audit;

    public AdminController(AdminDbContext db, IBusAdminClient bus, IAuditService audit)
    {
        _db    = db;
        _bus   = bus;
        _audit = audit;
    }

    private Guid   CurrentUserId   => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
    private string CurrentUserName => User.FindFirstValue(ClaimTypes.Name) ?? "Admin";

    // ── Platform Stats ────────────────────────────────────────────────────────

    [HttpGet("stats")]
    public async Task<IActionResult> GetPlatformStats(CancellationToken ct)
    {
        var idStats  = await _bus.GetIdentityStatsAsync(ct);
        var clubStats= await _bus.GetClubStatsAsync(ct);
        var raceStats= await _bus.GetRaceStatsAsync(ct);
        var fedStats = await _bus.GetFederationStatsAsync(ct);
        var subStats = await _bus.GetActiveSubscriptionCountAsync(ct);

        var stats = new
        {
            TotalFederations    = fedStats?.TotalFederations ?? 0,
            TotalClubs          = clubStats?.TotalClubs ?? 0,
            TotalUsers          = idStats?.TotalUsers ?? 0,
            TotalRaces          = raceStats?.TotalRaces ?? 0,
            PublishedRaces      = raceStats?.PublishedRaces ?? 0,
            RacesThisMonth      = raceStats?.RacesThisMonth ?? 0,
            ActiveSubscriptions = (subStats?.FederationSubscriptions ?? 0) + (subStats?.ClubSubscriptions ?? 0),
            TotalResults        = raceStats?.TotalResults ?? 0
        };

        return Ok(ApiResponse<object>.Ok(stats));
    }

    // ── Federations ───────────────────────────────────────────────────────────

    [HttpGet("federations")]
    public async Task<IActionResult> GetFederations(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _bus.GetFederationsAsync(page, pageSize, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("FederationService unavailable."));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("federations")]
    public async Task<IActionResult> CreateFederation([FromBody] CreateFederationBody req, CancellationToken ct)
    {
        var msg    = new Messages.CreateFederationRequest(req.Name, req.Code, req.Slug, req.FlagUrl, req.DefaultLanguage, req.DefaultTimezone, req.DefaultDistanceUnit, CurrentUserId);
        var result = await _bus.CreateFederationAsync(msg, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("FederationService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed to create federation."));

        await _audit.LogAsync("FEDERATION_CREATED", "Federation", result.Id, AuditSeverity.Info,
            $"Federation '{req.Name}' ({req.Code}) created",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPut("federations/{federationId:guid}/toggle-active")]
    public async Task<IActionResult> ToggleFederationActive(Guid federationId, CancellationToken ct)
    {
        var result = await _bus.ToggleFederationActiveAsync(federationId, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("FederationService unavailable."));
        if (result.Error is not null) return BadRequest(ApiResponse<object?>.Fail(result.Error));

        await _audit.LogAsync("FEDERATION_TOGGLED", "Federation", federationId,
            result.IsActive ? AuditSeverity.Info : AuditSeverity.Warning,
            $"Federation {federationId} active state → {result.IsActive}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(result));
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _bus.GetUsersAsync(search, role, page, pageSize, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("IdentityService unavailable."));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPut("users/{userId:guid}/toggle-active")]
    public async Task<IActionResult> ToggleUserActive(Guid userId, CancellationToken ct)
    {
        if (userId == CurrentUserId)
            return BadRequest(ApiResponse<object?>.Fail("Cannot deactivate your own account."));

        var result = await _bus.ToggleUserActiveAsync(userId, CurrentUserId, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("IdentityService unavailable."));
        if (result.Error is not null) return BadRequest(ApiResponse<object?>.Fail(result.Error));

        await _audit.LogAsync("USER_TOGGLED", "User", userId, AuditSeverity.Warning,
            $"User {userId} active state toggled by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPut("users/{userId:guid}/assign-role")]
    public async Task<IActionResult> AssignRole(Guid userId, [FromBody] AssignRoleBody req, CancellationToken ct)
    {
        if (req.Role == UserRole.Pending)
            return BadRequest(ApiResponse<object?>.Fail("Cannot assign Pending as a role."));

        var result = await _bus.AssignRoleAsync(userId, req.Role, req.FederationId, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("IdentityService unavailable."));
        if (result.Error is not null) return BadRequest(ApiResponse<object?>.Fail(result.Error));

        await _audit.LogAsync("ROLE_ASSIGNED", "User", userId, AuditSeverity.Warning,
            $"Role '{req.Role}' assigned to user {userId} by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPut("users/{userId:guid}/limits")]
    public async Task<IActionResult> SetUserLimits(Guid userId, [FromBody] SetUserLimitsBody req, CancellationToken ct)
    {
        var result = await _bus.SetUserLimitsAsync(userId, req.MaxResults, req.MaxClubs, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("IdentityService unavailable."));
        if (result.Error is not null) return BadRequest(ApiResponse<object?>.Fail(result.Error));

        await _audit.LogAsync("LIMITS_CHANGED", "User", userId, AuditSeverity.Info,
            $"Limits set for user {userId}: MaxResults={req.MaxResults}, MaxClubs={req.MaxClubs}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(result));
    }

    // ── Clubs ─────────────────────────────────────────────────────────────────

    [HttpGet("clubs")]
    public async Task<IActionResult> GetAllClubs(
        [FromQuery] string? search = null,
        [FromQuery] Guid? federationId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _bus.GetAllClubsAsync(search, federationId, page, pageSize, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("ClubService unavailable."));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPut("clubs/{clubId:guid}/suspend")]
    public async Task<IActionResult> SuspendClub(Guid clubId, CancellationToken ct)
    {
        var result = await _bus.ToggleClubActiveAsync(clubId, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("ClubService unavailable."));
        if (result.Error is not null) return NotFound(ApiResponse<object?>.Fail(result.Error));

        await _audit.LogAsync("CLUB_SUSPENDED", "Club", clubId, AuditSeverity.Critical,
            $"Club {clubId} active state → {result.IsActive} by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(result));
    }

    // ── Subscription Plans (via SubscriptionService bus consumers) ───────────

    [HttpGet("subscription-plans")]
    public async Task<IActionResult> GetSubscriptionPlans(CancellationToken ct)
    {
        var result = await _bus.GetSubscriptionPlansAsync(ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("SubscriptionService unavailable."));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _bus.GetFederationSubscriptionsAsync(page, pageSize, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("SubscriptionService unavailable."));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("subscriptions")]
    public async Task<IActionResult> CreateSubscription(
        [FromBody] CreateFederationSubscriptionRequest req, CancellationToken ct)
    {
        var result = await _bus.CreateFederationSubscriptionAsync(req with { CreatedBy = CurrentUserId }, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("SubscriptionService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed."));

        await _audit.LogAsync("SUBSCRIPTION_CREATED", "Subscription", result.Id, AuditSeverity.Info,
            $"Federation subscription created for {req.FederationName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(result));
    }

    // ── Role Upgrade Requests ─────────────────────────────────────────────────

    [HttpGet("upgrade-requests")]
    public async Task<IActionResult> GetUpgradeRequests(
        [FromQuery] Guid? federationId = null,
        [FromQuery] UpgradeRequestStatus? status = UpgradeRequestStatus.Pending,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _bus.GetUpgradeRequestsAsync(federationId, status, page, pageSize, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("IdentityService unavailable."));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("upgrade-requests/{requestId:guid}/approve")]
    public async Task<IActionResult> ApproveUpgradeRequest(Guid requestId, CancellationToken ct)
    {
        var result = await _bus.ReviewUpgradeRequestAsync(requestId, true, null, CurrentUserId, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("IdentityService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed."));

        await _audit.LogAsync("UPGRADE_REQUEST_APPROVED", "UpgradeRequest", requestId, AuditSeverity.Info,
            $"Role upgrade request {requestId} approved by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new { requestId, approved = true }));
    }

    [HttpPost("upgrade-requests/{requestId:guid}/reject")]
    public async Task<IActionResult> RejectUpgradeRequest(
        Guid requestId, [FromBody] RejectUpgradeBody body, CancellationToken ct)
    {
        var result = await _bus.ReviewUpgradeRequestAsync(requestId, false, body.Reason, CurrentUserId, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("IdentityService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed."));

        await _audit.LogAsync("UPGRADE_REQUEST_REJECTED", "UpgradeRequest", requestId, AuditSeverity.Info,
            $"Role upgrade request {requestId} rejected by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new { requestId, rejected = true }));
    }

    // ── Event Log ─────────────────────────────────────────────────────────────

    [HttpGet("events")]
    public async Task<IActionResult> GetEventLog(
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] AuditSeverity? severity = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var q = _db.AuditEvents.AsQueryable();

        if (!string.IsNullOrEmpty(action))     q = q.Where(e => e.Action == action);
        if (!string.IsNullOrEmpty(entityType)) q = q.Where(e => e.EntityType == entityType);
        if (severity.HasValue)                 q = q.Where(e => e.Severity == severity.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new AuditEventDto(
                e.Id, e.Action, e.EntityType, e.EntityId,
                e.Severity.ToString(), e.Details,
                e.TriggeredByUserId, e.TriggeredByName,
                e.CorrelationId, e.ServiceName,
                e.IpAddress, e.CreatedAt))
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { items, totalCount = total, page, pageSize }));
    }
}
