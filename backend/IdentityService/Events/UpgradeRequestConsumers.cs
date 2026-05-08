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

    public ReviewUpgradeRequestConsumer(IdentityDbContext db, UserManager<ApplicationUser> users)
    {
        _db    = db;
        _users = users;
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

        if (req.Status != UpgradeRequestStatus.Pending)
        {
            await ctx.RespondAsync(new ReviewUpgradeRequestResult(false, "Request is not pending."));
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
        await ctx.RespondAsync(new ReviewUpgradeRequestResult(true, null));
    }
}
