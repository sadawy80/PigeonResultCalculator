using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRC.AdminService.Data;
using PRC.AdminService.DTOs;
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
        var idStatsTask   = _bus.GetIdentityStatsAsync(ct);
        var clubStatsTask = _bus.GetClubStatsAsync(ct);
        var raceStatsTask = _bus.GetRaceStatsAsync(ct);
        var fedStatsTask  = _bus.GetFederationStatsAsync(ct);
        var subStatsTask  = _bus.GetActiveSubscriptionCountAsync(ct);
        await Task.WhenAll(idStatsTask, clubStatsTask, raceStatsTask, fedStatsTask, subStatsTask);
        var idStats   = idStatsTask.Result;
        var clubStats = clubStatsTask.Result;
        var raceStats = raceStatsTask.Result;
        var fedStats  = fedStatsTask.Result;
        var subStats  = subStatsTask.Result;

        var stats = new
        {
            TotalFederations          = fedStats?.TotalFederations ?? 0,
            TotalClubs                = clubStats?.TotalClubs ?? 0,
            TotalUsers                = idStats?.TotalUsers ?? 0,
            TotalFanciers             = idStats?.TotalFanciers ?? 0,
            TotalRaces                = raceStats?.TotalRaces ?? 0,
            PublishedRaces            = raceStats?.PublishedRaces ?? 0,
            RacesThisMonth            = raceStats?.RacesThisMonth ?? 0,
            RacesThisYear             = raceStats?.RacesThisYear ?? 0,
            FederationSubscriptions   = subStats?.FederationSubscriptions ?? 0,
            ClubSubscriptions         = subStats?.ClubSubscriptions ?? 0,
            TotalResults              = raceStats?.TotalResults ?? 0,
            ResultsThisYear           = raceStats?.ResultsThisYear ?? 0,
            TotalPigeons              = raceStats?.TotalPigeons ?? 0,
            TotalProgrammes           = clubStats?.TotalProgrammes ?? 0,
            ProgrammesThisYear        = clubStats?.ProgrammesThisYear ?? 0,
            TotalAceResults           = clubStats?.TotalAceResults ?? 0,
            TotalSuperAceResults      = clubStats?.TotalSuperAceResults ?? 0,
            TotalBestLoftResults      = clubStats?.TotalBestLoftResults ?? 0,
            // This Year — People & Platform
            FederationsThisYear       = fedStats?.FederationsThisYear ?? 0,
            ClubsThisYear             = clubStats?.ClubsThisYear ?? 0,
            UsersThisYear             = idStats?.UsersThisYear ?? 0,
            FanciersThisYear          = idStats?.FanciersThisYear ?? 0,
            PigeonsThisYear           = raceStats?.PigeonsThisYear ?? 0,
            FederationSubsThisYear    = subStats?.FederationSubsThisYear ?? 0,
            ClubSubsThisYear          = subStats?.ClubSubsThisYear ?? 0,
            // This Year — Activity
            AceResultsThisYear        = clubStats?.AceResultsThisYear ?? 0,
            SuperAceResultsThisYear   = clubStats?.SuperAceResultsThisYear ?? 0,
            BestLoftResultsThisYear   = clubStats?.BestLoftResultsThisYear ?? 0
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

    [HttpPut("federations/{federationId:guid}/assign-manager")]
    public async Task<IActionResult> AssignFederationManager(Guid federationId, [FromBody] AssignManagerBody req, CancellationToken ct)
    {
        var users = await _bus.GetUsersAsync(req.Email, null, 1, 5, ct);
        if (users is null) return StatusCode(503, ApiResponse<object?>.Fail("IdentityService unavailable."));

        var user = users.Users.FirstOrDefault(u => string.Equals(u.Email, req.Email, StringComparison.OrdinalIgnoreCase));
        if (user is null) return NotFound(ApiResponse<object?>.Fail($"No user found with email '{req.Email}'."));

        var result = await _bus.AssignRoleAsync(user.Id, UserRole.FederationManager, federationId, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("IdentityService unavailable."));
        if (result.Error is not null) return BadRequest(ApiResponse<object?>.Fail(result.Error));

        await _audit.LogAsync("FEDERATION_MANAGER_ASSIGNED", "Federation", federationId, AuditSeverity.Warning,
            $"User {user.Email} assigned as FederationManager for {federationId} by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new { federationId, userId = user.Id, email = user.Email, role = "FederationManager" }));
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

    [HttpDelete("federations/{federationId:guid}")]
    public async Task<IActionResult> DeleteFederation(Guid federationId, CancellationToken ct)
    {
        var result = await _bus.DeleteFederationAsync(federationId, CurrentUserId, CurrentUserName, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("FederationService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed to delete federation."));

        await _audit.LogAsync("FEDERATION_DELETED", "Federation", federationId, AuditSeverity.Critical,
            $"Federation '{result.FederationName}' ({federationId}) permanently deleted by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new { deleted = true, federationId, name = result.FederationName }));
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

    [HttpDelete("users/{userId:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken ct)
    {
        if (userId == CurrentUserId)
            return BadRequest(ApiResponse<object?>.Fail("Cannot delete your own account."));

        var result = await _bus.DeleteUserAsync(userId, CurrentUserId, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("IdentityService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed to delete user."));

        await _audit.LogAsync("USER_DELETED", "User", userId, AuditSeverity.Warning,
            $"User {userId} permanently deleted by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
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

    [HttpPost("clubs")]
    public async Task<IActionResult> CreateClub([FromBody] CreateClubAdminBody req, CancellationToken ct)
    {
        var result = await _bus.AdminCreateClubAsync(req.FederationId, req.Name, req.Code, req.City, CurrentUserId, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("ClubService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed to create club."));

        await _audit.LogAsync("CLUB_CREATED", "Club", result.ClubId, AuditSeverity.Info,
            $"Club '{req.Name}' ({req.Code}) created by admin {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPut("clubs/{clubId:guid}/assign-manager")]
    public async Task<IActionResult> AssignClubManager(Guid clubId, [FromBody] AssignClubManagerBody req, CancellationToken ct)
    {
        var users = await _bus.GetUsersAsync(req.Email, null, 1, 5, ct);
        if (users is null) return StatusCode(503, ApiResponse<object?>.Fail("IdentityService unavailable."));

        var user = users.Users.FirstOrDefault(u => string.Equals(u.Email, req.Email, StringComparison.OrdinalIgnoreCase));
        if (user is null) return NotFound(ApiResponse<object?>.Fail($"No user found with email '{req.Email}'."));

        var result = await _bus.AdminAssignClubManagerAsync(
            clubId, user.Id, $"{user.FirstName} {user.LastName}".Trim(), user.Email, req.Force ?? false, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("ClubService unavailable."));
        if (!result.Success && result.HasConflict)
            return Conflict(ApiResponse<object>.Ok(new
            {
                conflict = true, conflictClubId = result.ConflictClubId, conflictClubName = result.ConflictClubName,
                userId = user.Id, email = user.Email, fullName = $"{user.FirstName} {user.LastName}".Trim()
            }));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed to assign manager."));

        await _bus.AssignRoleAsync(user.Id, UserRole.ClubManager, result.FederationId, ct);

        await _audit.LogAsync("CLUB_MANAGER_ASSIGNED", "Club", clubId, AuditSeverity.Warning,
            $"User {user.Email} assigned as ClubManager for club {clubId} by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new { clubId, userId = user.Id, email = user.Email, role = "ClubManager" }));
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

    [HttpPut("clubs/{clubId:guid}/subscription-expiry")]
    public async Task<IActionResult> SetClubSubscriptionExpiry(Guid clubId, [FromBody] SetClubExpiryBody req, CancellationToken ct)
    {
        var result = await _bus.SetClubSubscriptionExpiryAsync(clubId, req.ExpiresAt, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("ClubService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed."));

        await _audit.LogAsync("CLUB_EXPIRY_SET", "Club", clubId, AuditSeverity.Info,
            $"Club {clubId} subscription expiry set to {req.ExpiresAt:yyyy-MM-dd} by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new { clubId, expiresAt = req.ExpiresAt }));
    }

    [HttpDelete("clubs/{clubId:guid}")]
    public async Task<IActionResult> DeleteClub(Guid clubId, CancellationToken ct)
    {
        var result = await _bus.DeleteClubAsync(clubId, CurrentUserId, CurrentUserName, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("ClubService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed to delete club."));

        await _audit.LogAsync("CLUB_DELETED", "Club", clubId, AuditSeverity.Critical,
            $"Club '{result.ClubName}' ({clubId}) permanently deleted by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new { deleted = true, clubId, name = result.ClubName }));
    }

    // ── Subscription Plans (via SubscriptionService bus consumers) ───────────

    [HttpGet("subscription-plans")]
    public async Task<IActionResult> GetSubscriptionPlans(CancellationToken ct)
    {
        var result = await _bus.GetSubscriptionPlansAsync(ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("SubscriptionService unavailable."));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPut("subscription-plans/{planId:guid}")]
    public async Task<IActionResult> UpdateSubscriptionPlan(Guid planId, [FromBody] UpdatePlanBody req, CancellationToken ct)
    {
        var busReq = new UpdateSubscriptionPlanBusRequest(
            planId, req.Name, req.Description, req.Price,
            req.MaxClubs, req.MaxResultsPerClub,
            req.IsActive, req.IsHighlighted, req.SortOrder, req.Features,
            CurrentUserId);

        var result = await _bus.UpdateSubscriptionPlanAsync(busReq, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("SubscriptionService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed to update plan."));

        await _audit.LogAsync("SUBSCRIPTION_PLAN_UPDATED", "SubscriptionPlan", planId, AuditSeverity.Info,
            $"Plan '{req.Name}' updated by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(result.Plan));
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? billingCycle = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var result = await _bus.GetFederationSubscriptionsAsync(page, pageSize, search, billingCycle, dateFrom, dateTo, ct);
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
        [FromQuery] UpgradeRequestStatus? status = null,
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
        var result = await _bus.ReviewUpgradeRequestAsync(requestId, true, null, CurrentUserId, isAdmin: true, ct);
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
        var result = await _bus.ReviewUpgradeRequestAsync(requestId, false, body.Reason, CurrentUserId, isAdmin: true, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("IdentityService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed."));

        await _audit.LogAsync("UPGRADE_REQUEST_REJECTED", "UpgradeRequest", requestId, AuditSeverity.Info,
            $"Role upgrade request {requestId} rejected by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new { requestId, rejected = true }));
    }

    [HttpPost("upgrade-requests/{requestId:guid}/revoke")]
    public async Task<IActionResult> RevokeUpgradeRequest(Guid requestId, CancellationToken ct)
    {
        var result = await _bus.RevokeUpgradeRequestAsync(requestId, CurrentUserId, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("IdentityService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed."));

        await _audit.LogAsync("UPGRADE_REQUEST_REVOKED", "UpgradeRequest", requestId, AuditSeverity.Warning,
            $"Role upgrade request {requestId} revoked by admin {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new { requestId, revoked = true }));
    }

    // ── Races ─────────────────────────────────────────────────────────────────

    [HttpGet("races")]
    public async Task<IActionResult> GetAdminRaces(
        [FromQuery] string? search = null,
        [FromQuery] Guid? clubId = null,
        [FromQuery] int? status = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _bus.GetAdminRacesAsync(search, clubId, status, dateFrom, dateTo, page, pageSize, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("RaceService unavailable."));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpDelete("races/{raceId:guid}")]
    public async Task<IActionResult> DeleteRace(Guid raceId, CancellationToken ct)
    {
        var result = await _bus.DeleteRaceAsync(raceId, CurrentUserId, CurrentUserName, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("RaceService unavailable."));
        if (!result.Success) return NotFound(ApiResponse<object?>.Fail(result.Error ?? "Race not found."));

        await _audit.LogAsync("RACE_DELETED", "Race", raceId, AuditSeverity.Warning,
            $"Race '{result.RaceName}' deleted by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        if (result.ClubId.HasValue)
        {
            await _bus.NotifyClubManagersAsync(
                result.ClubId.Value,
                "Race removed by admin",
                $"The race '{result.RaceName}' was removed by an administrator.",
                "race", raceId.ToString(), ct);
        }

        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }

    // ── Pigeons ───────────────────────────────────────────────────────────────

    [HttpGet("pigeons")]
    public async Task<IActionResult> GetAdminPigeons(
        [FromQuery] string? search = null,
        [FromQuery] Guid? federationId = null,
        [FromQuery] Guid? clubId = null,
        [FromQuery] string? fancierSearch = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _bus.GetAdminPigeonsAsync(search, federationId, clubId, page, pageSize, fancierSearch, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("RaceService unavailable."));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPut("pigeons/{pigeonId:guid}")]
    public async Task<IActionResult> UpdatePigeon(Guid pigeonId, [FromBody] UpdatePigeonBody req, CancellationToken ct)
    {
        var result = await _bus.UpdatePigeonAsync(pigeonId, req.Name, req.Sex, req.YearOfBirth, req.Color, CurrentUserId, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("RaceService unavailable."));
        if (!result.Success) return NotFound(ApiResponse<object?>.Fail(result.Error ?? "Pigeon not found."));

        await _audit.LogAsync("PIGEON_UPDATED", "Pigeon", pigeonId, AuditSeverity.Info,
            $"Pigeon {pigeonId} updated by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new { updated = true }));
    }

    [HttpDelete("pigeons/{pigeonId:guid}")]
    public async Task<IActionResult> DeletePigeon(Guid pigeonId, CancellationToken ct)
    {
        var result = await _bus.DeletePigeonAsync(pigeonId, CurrentUserId, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("RaceService unavailable."));
        if (!result.Success) return NotFound(ApiResponse<object?>.Fail(result.Error ?? "Pigeon not found."));

        await _audit.LogAsync("PIGEON_DELETED", "Pigeon", pigeonId, AuditSeverity.Warning,
            $"Pigeon '{result.RingNumber}' deleted by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }

    // ── Fanciers ─────────────────────────────────────────────────────────────

    [HttpGet("fanciers")]
    public async Task<IActionResult> GetFanciers(
        [FromQuery] string? search = null,
        [FromQuery] Guid? clubId = null,
        [FromQuery] Guid? federationId = null,
        [FromQuery] bool? isLinked = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _bus.GetFanciersAsync(search, clubId, federationId, isLinked, page, pageSize, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("RaceService unavailable."));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("fanciers/{fancierId:guid}/link")]
    public async Task<IActionResult> LinkFancier(Guid fancierId, [FromBody] LinkFancierBody req, CancellationToken ct)
    {
        var result = await _bus.LinkFancierAsync(fancierId, req.UserId, req.UserName, req.UserEmail, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("RaceService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed to link fancier."));

        await _audit.LogAsync("FANCIER_LINKED", "Fancier", fancierId, AuditSeverity.Info,
            $"Fancier {fancierId} linked to user {req.UserId} by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new { linked = true }));
    }

    [HttpDelete("fanciers/{fancierId:guid}/link")]
    public async Task<IActionResult> UnlinkFancier(Guid fancierId, CancellationToken ct)
    {
        var result = await _bus.UnlinkFancierAsync(fancierId, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("RaceService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed to unlink fancier."));
        return Ok(ApiResponse<object>.Ok(new { unlinked = true }));
    }

    // ── Programmes ────────────────────────────────────────────────────────────

    [HttpGet("programmes")]
    public async Task<IActionResult> GetAdminProgrammes(
        [FromQuery] string? search = null,
        [FromQuery] Guid? clubId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _bus.GetAdminProgrammesAsync(search, clubId, page, pageSize, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("ClubService unavailable."));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpDelete("programmes/{programmeId:guid}")]
    public async Task<IActionResult> DeleteProgramme(Guid programmeId, CancellationToken ct)
    {
        var result = await _bus.DeleteProgrammeAsync(programmeId, CurrentUserId, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("ClubService unavailable."));
        if (!result.Success) return NotFound(ApiResponse<object?>.Fail(result.Error ?? "Programme not found."));

        await _audit.LogAsync("PROGRAMME_DELETED", "Programme", programmeId, AuditSeverity.Warning,
            $"Programme '{result.ProgrammeName}' deleted by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }

    // ── Results ───────────────────────────────────────────────────────────────

    [HttpGet("results/ace")]
    public async Task<IActionResult> GetAcePigeonResults(
        [FromQuery] string? search = null,
        [FromQuery] Guid? clubId = null,
        [FromQuery] Guid? programmeId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _bus.GetAdminAcePigeonResultsAsync(search, clubId, programmeId, page, pageSize, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("ClubService unavailable."));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("results/super-ace")]
    public async Task<IActionResult> GetSuperAceResults(
        [FromQuery] string? search = null,
        [FromQuery] Guid? clubId = null,
        [FromQuery] Guid? programmeId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _bus.GetAdminSuperAceResultsAsync(search, clubId, programmeId, page, pageSize, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("ClubService unavailable."));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("results/best-loft")]
    public async Task<IActionResult> GetBestLoftResults(
        [FromQuery] string? search = null,
        [FromQuery] Guid? clubId = null,
        [FromQuery] Guid? programmeId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _bus.GetAdminBestLoftResultsAsync(search, clubId, programmeId, page, pageSize, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("ClubService unavailable."));
        return Ok(ApiResponse<object>.Ok(result));
    }

    // ── Subscription Plans — Create / Delete ──────────────────────────────────

    [HttpPost("subscription-plans")]
    public async Task<IActionResult> CreateSubscriptionPlan([FromBody] CreateSubscriptionPlanBody req, CancellationToken ct)
    {
        var busReq = new AdminCreateSubscriptionPlanBusRequest(
            req.Name, req.Description, req.Type, req.BillingCycle,
            req.Price, req.Currency ?? "GBP", req.MaxClubs, req.MaxResultsPerClub,
            req.IsHighlighted, req.SortOrder, req.Features, CurrentUserId);

        var result = await _bus.AdminCreateSubscriptionPlanAsync(busReq, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("SubscriptionService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed to create plan."));

        await _audit.LogAsync("SUBSCRIPTION_PLAN_CREATED", "SubscriptionPlan", result.Plan?.Id, AuditSeverity.Info,
            $"Plan '{req.Name}' created by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(result.Plan));
    }

    [HttpDelete("subscription-plans/{planId:guid}")]
    public async Task<IActionResult> DeleteSubscriptionPlan(Guid planId, CancellationToken ct)
    {
        var result = await _bus.AdminDeleteSubscriptionPlanAsync(planId, CurrentUserId, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("SubscriptionService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed to delete plan."));

        await _audit.LogAsync("SUBSCRIPTION_PLAN_DELETED", "SubscriptionPlan", planId, AuditSeverity.Warning,
            $"Subscription plan {planId} deleted by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new { deleted = true }));
    }

    // ── Admin Notifications (stored in AdminService DB) ───────────────────────

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool? unreadOnly = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var q = _db.AdminNotifications.AsQueryable();
        if (unreadOnly == true) q = q.Where(n => !n.IsRead);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(n => new {
                n.Id, n.Type, n.Title, n.Body, n.ActionUrl,
                n.SourceId, n.IsRead, n.ReadAt, n.CreatedAt
            })
            .ToListAsync(ct);

        var unreadCount = await _db.AdminNotifications.CountAsync(n => !n.IsRead, ct);
        return Ok(ApiResponse<object>.Ok(new { items, totalCount = total, unreadCount, page, pageSize }));
    }

    [HttpPut("notifications/{notificationId:guid}/read")]
    public async Task<IActionResult> MarkNotificationRead(Guid notificationId, CancellationToken ct)
    {
        var n = await _db.AdminNotifications.FindAsync([notificationId], ct);
        if (n is null) return NotFound(ApiResponse<object?>.Fail("Notification not found."));
        if (!n.IsRead) { n.IsRead = true; n.ReadAt = DateTime.UtcNow; await _db.SaveChangesAsync(ct); }
        return Ok(ApiResponse<object>.Ok(new { read = true }));
    }

    [HttpPut("notifications/read-all")]
    public async Task<IActionResult> MarkAllNotificationsRead(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await _db.AdminNotifications
            .Where(n => !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now), ct);
        return Ok(ApiResponse<object>.Ok(new { readAll = true }));
    }

    [HttpDelete("notifications/{notificationId:guid}")]
    public async Task<IActionResult> DismissNotification(Guid notificationId, CancellationToken ct)
    {
        var n = await _db.AdminNotifications.FindAsync([notificationId], ct);
        if (n is null) return NotFound(ApiResponse<object?>.Fail("Notification not found."));
        n.IsDeleted = true;
        n.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { dismissed = true }));
    }

    [HttpDelete("notifications")]
    public async Task<IActionResult> DismissAllNotifications(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await _db.AdminNotifications
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsDeleted, true)
                .SetProperty(n => n.DeletedAt, now), ct);
        return Ok(ApiResponse<object>.Ok(new { dismissedAll = true }));
    }

    // ── External Link Requests ────────────────────────────────────────────────

    [HttpGet("link-requests")]
    public async Task<IActionResult> GetLinkRequests(
        [FromQuery] int? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _bus.GetAdminExternalLinksAsync(status, page, pageSize, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("IntegrationService unavailable."));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("link-requests/{linkId:guid}/approve")]
    public async Task<IActionResult> ApproveLinkRequest(Guid linkId, CancellationToken ct)
    {
        var result = await _bus.AdminApproveLinkAsync(linkId, CurrentUserId, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("IntegrationService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed to approve link."));

        await _audit.LogAsync("LINK_APPROVED", "ExternalLink", linkId, AuditSeverity.Info,
            $"External link {linkId} approved by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new { approved = true }));
    }

    [HttpPost("link-requests/{linkId:guid}/reject")]
    public async Task<IActionResult> RejectLinkRequest(Guid linkId, [FromBody] RejectLinkBody req, CancellationToken ct)
    {
        var result = await _bus.AdminRejectLinkAsync(linkId, req.Reason, CurrentUserId, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("IntegrationService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed to reject link."));

        await _audit.LogAsync("LINK_REJECTED", "ExternalLink", linkId, AuditSeverity.Info,
            $"External link {linkId} rejected by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new { rejected = true }));
    }

    [HttpDelete("link-requests/{linkId:guid}")]
    public async Task<IActionResult> RevokeLinkRequest(Guid linkId, CancellationToken ct)
    {
        var result = await _bus.AdminRevokeLinkAsync(linkId, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("IntegrationService unavailable."));
        if (!result.Success) return BadRequest(ApiResponse<object?>.Fail(result.Error ?? "Failed to revoke link."));

        await _audit.LogAsync("LINK_REVOKED", "ExternalLink", linkId, AuditSeverity.Warning,
            $"External link {linkId} revoked by {CurrentUserName}",
            CurrentUserId, CurrentUserName, CorrelationId, ClientIp, ct);

        return Ok(ApiResponse<object>.Ok(new { revoked = true }));
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
        var result = await _bus.GetAuditLogsAsync(action, entityType, severity, page, pageSize, ct);
        if (result is null) return StatusCode(503, ApiResponse<object?>.Fail("AuditService unavailable."));

        return Ok(ApiResponse<object>.Ok(new
        {
            items      = result.Items,
            totalCount = result.TotalCount,
            page,
            pageSize
        }));
    }
}
