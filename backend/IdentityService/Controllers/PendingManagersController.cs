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

        if (req.RequestedRole == UserRole.ClubManager && !req.FederationId.HasValue)
            return BadRequest(ApiResponse<object?>.Fail("A federation must be selected for club manager requests."));

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
            Notes         = req.Notes,
            Status        = UpgradeRequestStatus.Pending
        };

        _db.UpgradeRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        await _bus.Publish(new UpgradeRequestSubmitted(
            request.Id, userId, user.FullName, user.Email!,
            req.RequestedRole, req.FederationId, DateTime.UtcNow), ct);

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
                r.Id, r.RequestedRole, r.FederationId, r.Status,
                r.Notes, r.RejectionReason, r.CreatedAt, r.ReviewedAt
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(items));
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
    private readonly IRequestClient<GetFederationSubscriptionLimitsRequest> _subscriptionClient;
    private readonly IRequestClient<GetActiveClubCountForFederationRequest> _clubCountClient;

    public UpgradeRequestsController(
        IdentityDbContext db,
        UserManager<ApplicationUser> users,
        IRequestClient<GetFederationSubscriptionLimitsRequest> subscriptionClient,
        IRequestClient<GetActiveClubCountForFederationRequest> clubCountClient)
    {
        _db                 = db;
        _users              = users;
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

        if (req.Status != UpgradeRequestStatus.Pending)
            return BadRequest(ApiResponse<object?>.Fail("Request is not pending."));

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

        return Ok(ApiResponse<object>.Ok(new { req.Id, req.Status, req.RejectionReason }));
    }
}
