using System.Security.Claims;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.Common.Messages;
using PRC.IdentityService.Data;
using PRC.IdentityService.DTOs;
using PRC.IdentityService.Models;

namespace PRC.IdentityService.Controllers;

// ── User: submit a role upgrade request ──────────────────────────────────────

[ApiController]
[Route("api/auth")]
[Authorize]
public class UpgradeRequestSubmitController : ControllerBase
{
    private readonly IdentityDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IPublishEndpoint _bus;

    public UpgradeRequestSubmitController(
        IdentityDbContext db,
        UserManager<ApplicationUser> users,
        IPublishEndpoint bus)
    {
        _db    = db;
        _users = users;
        _bus   = bus;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("upgrade-request")]
    public async Task<IActionResult> Submit([FromBody] SubmitUpgradeRequestBody req, CancellationToken ct)
    {
        if (req.RequestedRole != UserRole.ClubManager && req.RequestedRole != UserRole.FederationManager)
            return BadRequest(ApiResponse<object?>.Fail("Only ClubManager or FederationManager roles can be requested."));

        // FederationId is optional for ClubManager — routes to admin if no federation exists for the country

        var userId = CurrentUserId;

        var existing = await _db.UpgradeRequests
            .AnyAsync(r => r.UserId == userId && r.Status == UpgradeRequestStatus.Pending, ct);
        if (existing)
            return Conflict(ApiResponse<object?>.Fail("You already have a pending upgrade request."));

        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null) return NotFound();

        var request = new RoleUpgradeRequest
        {
            UserId        = userId,
            RequestedRole = req.RequestedRole,
            FederationId  = req.FederationId,
            ClubName      = req.ClubName?.Trim(),
            Notes         = req.Notes,
            Status        = UpgradeRequestStatus.Pending
        };

        _db.UpgradeRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        await _bus.Publish(new UpgradeRequestSubmitted(
            request.Id, userId, user.FullName, user.Email!,
            req.RequestedRole, req.FederationId, DateTime.UtcNow), ct);

        await NotifyReviewers(req.RequestedRole, req.FederationId, user.FullName, req.ClubName, ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            request.Id,
            request.RequestedRole,
            request.FederationId,
            request.Status,
            request.CreatedAt
        }));
    }

    [HttpGet("upgrade-requests")]
    public async Task<IActionResult> GetMyRequests(CancellationToken ct)
    {
        var userId = CurrentUserId;
        var items = await _db.UpgradeRequests
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id, r.RequestedRole, r.FederationId, r.ClubName, r.Status,
                r.Notes, r.RejectionReason, r.CreatedAt, r.ReviewedAt
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpPost("upgrade-request/{requestId:guid}/remind")]
    public async Task<IActionResult> SendReminder(Guid requestId, CancellationToken ct)
    {
        var userId  = CurrentUserId;
        var request = await _db.UpgradeRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.UserId == userId, ct);

        if (request is null)
            return NotFound(ApiResponse<object?>.Fail("Request not found."));

        if (request.Status != UpgradeRequestStatus.Pending)
            return BadRequest(ApiResponse<object?>.Fail("Only pending requests can send a reminder."));

        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null) return NotFound();

        await _bus.Publish(new UpgradeRequestSubmitted(
            request.Id, userId, user.FullName, user.Email!,
            request.RequestedRole, request.FederationId, DateTime.UtcNow), ct);

        await NotifyReviewers(request.RequestedRole, request.FederationId, user.FullName, request.ClubName, ct, isReminder: true);

        return Ok(ApiResponse<object>.Ok(new { requestId, remindedAt = DateTime.UtcNow }));
    }

    private async Task NotifyReviewers(UserRole requestedRole, Guid? federationId,
        string applicantName, string? clubName, CancellationToken ct, bool isReminder = false)
    {
        var prefix = isReminder ? "Reminder: " : "";
        var body   = $"{applicantName} has requested the {requestedRole} role" +
                     (clubName != null ? $" for club \"{clubName}\"" : "") + ".";

        var superAdmins = await _users.GetUsersInRoleAsync(UserRole.SuperAdmin.ToString());
        foreach (var admin in superAdmins)
        {
            await _bus.Publish(new CreateInAppNotification(
                admin.Id,
                NotificationType.RoleRequest,
                $"{prefix}New {requestedRole} request",
                body,
                "/admin/upgrade-requests"), ct);
        }

        if (requestedRole == UserRole.ClubManager && federationId.HasValue)
        {
            var fedManagers = await _users.Users
                .Where(u => u.FederationId == federationId.Value &&
                            u.Role == UserRole.FederationManager && u.IsActive)
                .ToListAsync(ct);

            foreach (var fm in fedManagers)
            {
                await _bus.Publish(new CreateInAppNotification(
                    fm.Id,
                    NotificationType.RoleRequest,
                    $"{prefix}New Club Manager request",
                    body,
                    "/federation/upgrade-requests"), ct);
            }
        }
    }
}

// ── FederationManager / SuperAdmin: review upgrade requests ──────────────────

[ApiController]
[Route("api/federation")]
[Authorize(Roles = "FederationManager,SuperAdmin")]
public class UpgradeRequestsController : ControllerBase
{
    private readonly IdentityDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IPublishEndpoint _bus;
    private readonly IRequestClient<GetFederationSubscriptionLimitsRequest> _subscriptionClient;
    private readonly IRequestClient<GetActiveClubCountForFederationRequest> _clubCountClient;

    public UpgradeRequestsController(
        IdentityDbContext db,
        UserManager<ApplicationUser> users,
        IPublishEndpoint bus,
        IRequestClient<GetFederationSubscriptionLimitsRequest> subscriptionClient,
        IRequestClient<GetActiveClubCountForFederationRequest> clubCountClient)
    {
        _db                 = db;
        _users              = users;
        _bus                = bus;
        _subscriptionClient = subscriptionClient;
        _clubCountClient    = clubCountClient;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private Guid? CurrentFederationId
    {
        get
        {
            var val = User.FindFirstValue("federationId");
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }

    [HttpGet("upgrade-requests")]
    public async Task<IActionResult> GetUpgradeRequests(
        [FromQuery] UpgradeRequestStatus? status = UpgradeRequestStatus.Pending,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var federationId = CurrentFederationId;

        var q = _db.UpgradeRequests
            .Include(r => r.User)
            .AsQueryable();

        if (status.HasValue)
            q = q.Where(r => r.Status == status.Value);

        if (federationId.HasValue && !User.IsInRole("SuperAdmin"))
            q = q.Where(r => r.FederationId == federationId);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.UserId,
                UserFullName = r.User.FirstName + " " + r.User.LastName,
                r.User.Email,
                r.RequestedRole,
                r.FederationId,
                r.ClubName,
                r.Status,
                r.Notes,
                r.RejectionReason,
                r.CreatedAt,
                r.ReviewedAt
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { items, totalCount = total, page, pageSize }));
    }

    [HttpPost("upgrade-requests/{requestId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid requestId, CancellationToken ct)
    {
        var req = await _db.UpgradeRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (req is null)
            return NotFound(ApiResponse<object?>.Fail("Request not found."));

        var isRevoked = req.Status == UpgradeRequestStatus.Revoked ||
                        req.Status == UpgradeRequestStatus.AdminRevoked;

        if (!isRevoked && req.Status != UpgradeRequestStatus.Pending)
            return BadRequest(ApiResponse<object?>.Fail("Request cannot be approved in its current state."));

        if (req.Status == UpgradeRequestStatus.AdminRevoked && !User.IsInRole("SuperAdmin"))
            return BadRequest(ApiResponse<object?>.Fail("This request was revoked by an admin — only an admin can re-approve it."));

        var federationId = CurrentFederationId;
        if (federationId.HasValue && !User.IsInRole("SuperAdmin") && req.FederationId != federationId)
            return Forbid();

        // Subscription limit check for ClubManager approvals
        if (req.RequestedRole == UserRole.ClubManager && req.FederationId.HasValue)
        {
            try
            {
                var limitsResp = await _subscriptionClient.GetResponse<GetFederationSubscriptionLimitsResult>(
                    new GetFederationSubscriptionLimitsRequest(req.FederationId.Value), ct);
                var limits = limitsResp.Message;

                if (limits.HasActiveSubscription && !limits.IsUnlimited)
                {
                    var clubCountResp = await _clubCountClient.GetResponse<GetActiveClubCountForFederationResult>(
                        new GetActiveClubCountForFederationRequest(req.FederationId.Value), ct);

                    if (clubCountResp.Message.ActiveClubCount >= limits.MaxClubs)
                        return BadRequest(ApiResponse<object?>.Fail(
                            $"Subscription limit reached: {limits.MaxClubs} clubs allowed, " +
                            $"{clubCountResp.Message.ActiveClubCount} currently active."));
                }
            }
            catch (RequestTimeoutException) { }
        }

        var user = req.User;
        await _users.RemoveFromRoleAsync(user, user.Role.ToString());

        user.Role         = req.RequestedRole;
        user.FederationId = req.FederationId ?? user.FederationId;
        user.UpdatedAt    = DateTime.UtcNow;

        await _users.AddToRoleAsync(user, req.RequestedRole.ToString());
        await _users.UpdateAsync(user);

        req.Status           = UpgradeRequestStatus.Approved;
        req.ReviewedByUserId = CurrentUserId;
        req.ReviewedAt       = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _bus.Publish(new CreateInAppNotification(
            user.Id,
            NotificationType.RoleRequest,
            "Role upgrade approved",
            $"Your request to become a {req.RequestedRole} has been approved. Your new role is now active.",
            "/settings"), ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            RequestId = req.Id, req.Status,
            UserId = user.Id, user.Email, user.FullName, user.Role
        }));
    }

    [HttpPost("upgrade-requests/{requestId:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid requestId, [FromBody] RejectUpgradeRequestBody body, CancellationToken ct)
    {
        var req = await _db.UpgradeRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (req is null)
            return NotFound(ApiResponse<object?>.Fail("Request not found."));

        if (req.Status != UpgradeRequestStatus.Pending)
            return BadRequest(ApiResponse<object?>.Fail("Request is not pending."));

        var federationId = CurrentFederationId;
        if (federationId.HasValue && !User.IsInRole("SuperAdmin") && req.FederationId != federationId)
            return Forbid();

        req.Status           = UpgradeRequestStatus.Rejected;
        req.RejectionReason  = body.Reason;
        req.ReviewedByUserId = CurrentUserId;
        req.ReviewedAt       = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var rejectBody = "Your role upgrade request was declined." +
                         (body.Reason != null ? $" Reason: {body.Reason}" : "");
        await _bus.Publish(new CreateInAppNotification(
            req.UserId,
            NotificationType.RoleRequest,
            "Role upgrade request declined",
            rejectBody,
            "/auth/upgrade-request"), ct);

        return Ok(ApiResponse<object>.Ok(new { req.Id, req.Status, req.RejectionReason }));
    }

    [HttpPost("upgrade-requests/{requestId:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid requestId, CancellationToken ct)
    {
        var req = await _db.UpgradeRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (req is null)
            return NotFound(ApiResponse<object?>.Fail("Request not found."));

        if (req.Status != UpgradeRequestStatus.Approved)
            return BadRequest(ApiResponse<object?>.Fail("Only approved requests can be revoked."));

        var federationId = CurrentFederationId;
        if (federationId.HasValue && !User.IsInRole("SuperAdmin") && req.FederationId != federationId)
            return Forbid();

        var isAdmin = User.IsInRole("SuperAdmin");
        var user    = req.User;

        await _users.RemoveFromRoleAsync(user, user.Role.ToString());
        user.Role      = UserRole.Fancier;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.AddToRoleAsync(user, UserRole.Fancier.ToString());
        await _users.UpdateAsync(user);

        req.Status           = isAdmin ? UpgradeRequestStatus.AdminRevoked : UpgradeRequestStatus.Revoked;
        req.ReviewedByUserId = CurrentUserId;
        req.ReviewedAt       = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _bus.Publish(new CreateInAppNotification(
            user.Id, NotificationType.RoleRequest,
            "Role revoked",
            $"Your {req.RequestedRole} role has been revoked. Please contact your administrator.",
            "/auth/upgrade-request"), ct);

        return Ok(ApiResponse<object>.Ok(new { req.Id, req.Status }));
    }
}
