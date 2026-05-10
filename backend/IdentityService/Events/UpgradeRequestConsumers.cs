using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.Common.Messages;
using PRC.IdentityService.Data;
using PRC.IdentityService.Models;

namespace PRC.IdentityService.Events;

public class GetUpgradeRequestsConsumer : IConsumer<GetUpgradeRequestsRequest>
{
    private readonly IdentityDbContext _db;
    public GetUpgradeRequestsConsumer(IdentityDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetUpgradeRequestsRequest> ctx)
    {
        var m = ctx.Message;
        var q = _db.UpgradeRequests.Include(r => r.User).AsQueryable();

        if (m.Status.HasValue)
            q = q.Where(r => r.Status == m.Status.Value);
        if (m.FederationId.HasValue)
            q = q.Where(r => r.FederationId == m.FederationId.Value);

        var total = await q.CountAsync(ctx.CancellationToken);
        var items = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((m.Page - 1) * m.PageSize).Take(m.PageSize)
            .Select(r => new UpgradeRequestItem(
                r.Id, r.UserId,
                r.User.FirstName + " " + r.User.LastName,
                r.User.Email!,
                r.RequestedRole, r.FederationId, null,
                r.Status, r.Notes, r.RejectionReason,
                r.CreatedAt, r.ReviewedAt))
            .ToListAsync(ctx.CancellationToken);

        await ctx.RespondAsync(new GetUpgradeRequestsResult(items, total));
    }
}

public class ReviewUpgradeRequestConsumer : IConsumer<ReviewUpgradeRequestRequest>
{
    private readonly IdentityDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IPublishEndpoint _bus;

    public ReviewUpgradeRequestConsumer(IdentityDbContext db, UserManager<ApplicationUser> users, IPublishEndpoint bus)
    {
        _db    = db;
        _users = users;
        _bus   = bus;
    }

    public async Task Consume(ConsumeContext<ReviewUpgradeRequestRequest> ctx)
    {
        var m   = ctx.Message;
        var req = await _db.UpgradeRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == m.RequestId, ctx.CancellationToken);

        if (req is null)
        {
            await ctx.RespondAsync(new ReviewUpgradeRequestResult(false, "Request not found."));
            return;
        }

        var isRevoked = req.Status == UpgradeRequestStatus.Revoked || req.Status == UpgradeRequestStatus.AdminRevoked;
        var approvable = req.Status == UpgradeRequestStatus.Pending ||
                         (isRevoked && m.Approved && (m.IsAdmin || req.Status == UpgradeRequestStatus.Revoked));

        if (!approvable && req.Status != UpgradeRequestStatus.Pending)
        {
            var msg = req.Status == UpgradeRequestStatus.AdminRevoked
                ? "This request was revoked by an admin — only an admin can re-approve it."
                : "Request cannot be reviewed in its current state.";
            await ctx.RespondAsync(new ReviewUpgradeRequestResult(false, msg));
            return;
        }

        if (m.Approved)
        {
            var user = req.User;
            await _users.RemoveFromRoleAsync(user, user.Role.ToString());

            user.Role         = req.RequestedRole;
            user.FederationId = req.FederationId ?? user.FederationId;
            user.UpdatedAt    = DateTime.UtcNow;

            await _users.AddToRoleAsync(user, req.RequestedRole.ToString());
            await _users.UpdateAsync(user);

            req.Status = UpgradeRequestStatus.Approved;
        }
        else
        {
            req.Status          = UpgradeRequestStatus.Rejected;
            req.RejectionReason = m.RejectionReason;
        }

        req.ReviewedByUserId = m.ReviewedByUserId;
        req.ReviewedAt       = DateTime.UtcNow;

        await _db.SaveChangesAsync(ctx.CancellationToken);

        if (m.Approved)
        {
            await _bus.Publish(new CreateInAppNotification(
                req.UserId, NotificationType.RoleRequest,
                "Role upgrade approved",
                $"Your request to become a {req.RequestedRole} has been approved. Your new role is now active.",
                "/settings"), ctx.CancellationToken);
        }
        else
        {
            var body = "Your role upgrade request was declined." +
                       (m.RejectionReason != null ? $" Reason: {m.RejectionReason}" : "");
            await _bus.Publish(new CreateInAppNotification(
                req.UserId, NotificationType.RoleRequest,
                "Role upgrade request declined",
                body,
                "/auth/upgrade-request"), ctx.CancellationToken);
        }

        await ctx.RespondAsync(new ReviewUpgradeRequestResult(true, null));
    }
}

public class RevokeUpgradeRequestConsumer : IConsumer<RevokeUpgradeRequestRequest>
{
    private readonly IdentityDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IPublishEndpoint _bus;

    public RevokeUpgradeRequestConsumer(IdentityDbContext db, UserManager<ApplicationUser> users, IPublishEndpoint bus)
    {
        _db    = db;
        _users = users;
        _bus   = bus;
    }

    public async Task Consume(ConsumeContext<RevokeUpgradeRequestRequest> ctx)
    {
        var m   = ctx.Message;
        var req = await _db.UpgradeRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == m.RequestId, ctx.CancellationToken);

        if (req is null)
        {
            await ctx.RespondAsync(new RevokeUpgradeRequestResult(false, "Request not found."));
            return;
        }

        if (req.Status != UpgradeRequestStatus.Approved)
        {
            await ctx.RespondAsync(new RevokeUpgradeRequestResult(false, "Only approved requests can be revoked."));
            return;
        }

        var user = req.User;
        await _users.RemoveFromRoleAsync(user, user.Role.ToString());
        user.Role      = UserRole.Fancier;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.AddToRoleAsync(user, UserRole.Fancier.ToString());
        await _users.UpdateAsync(user);

        req.Status           = m.IsAdmin ? UpgradeRequestStatus.AdminRevoked : UpgradeRequestStatus.Revoked;
        req.ReviewedByUserId = m.RevokedByUserId;
        req.ReviewedAt       = DateTime.UtcNow;

        await _db.SaveChangesAsync(ctx.CancellationToken);

        await _bus.Publish(new CreateInAppNotification(
            user.Id, NotificationType.RoleRequest,
            "Role revoked",
            $"Your {req.RequestedRole} role has been revoked. Please contact your administrator.",
            "/auth/upgrade-request"), ctx.CancellationToken);

        await ctx.RespondAsync(new RevokeUpgradeRequestResult(true, null));
    }
}
