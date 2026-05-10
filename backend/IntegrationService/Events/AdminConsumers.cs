using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.Common.Messages;
using PRC.IntegrationService.Data;
using PRC.IntegrationService.Models;
using PRC.IntegrationService.Services;

namespace PRC.IntegrationService.Events;

public class GetAdminExternalLinksConsumer : IConsumer<GetAdminExternalLinksRequest>
{
    private readonly IntegrationDbContext _db;
    public GetAdminExternalLinksConsumer(IntegrationDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetAdminExternalLinksRequest> ctx)
    {
        var m = ctx.Message;
        var q = _db.ExternalLinks.AsQueryable();
        if (m.Status.HasValue)
            q = q.Where(l => (int)l.Status == m.Status.Value);

        var total = await q.CountAsync(ctx.CancellationToken);
        var items = await q
            .OrderByDescending(l => l.RequestedAt)
            .Skip((m.Page - 1) * m.PageSize)
            .Take(m.PageSize)
            .Select(l => new AdminExternalLinkItem(
                l.Id, l.ExternalLoftName, l.ExternalLoftId,
                l.ExternalPlatformName, l.UserId, l.ClubId,
                (int)l.Status, l.Status.ToString(),
                l.RejectionReason, l.RevokedReason,
                l.RequestedAt, l.LastDataAccessAt))
            .ToListAsync(ctx.CancellationToken);

        await ctx.RespondAsync(new GetAdminExternalLinksResult(items, total));
    }
}

public class AdminApproveLinkConsumer : IConsumer<AdminApproveLinkBusRequest>
{
    private readonly IIntegrationService _svc;
    public AdminApproveLinkConsumer(IIntegrationService svc) => _svc = svc;

    public async Task Consume(ConsumeContext<AdminApproveLinkBusRequest> ctx)
    {
        var result = await _svc.ReviewLinkAsync(ctx.Message.LinkId, true, null, ctx.Message.AdminUserId, ctx.CancellationToken);
        await ctx.RespondAsync(result.IsSuccess
            ? new AdminApproveLinkBusResult(true, null)
            : new AdminApproveLinkBusResult(false, result.Error));
    }
}

public class AdminRejectLinkConsumer : IConsumer<AdminRejectLinkBusRequest>
{
    private readonly IIntegrationService _svc;
    public AdminRejectLinkConsumer(IIntegrationService svc) => _svc = svc;

    public async Task Consume(ConsumeContext<AdminRejectLinkBusRequest> ctx)
    {
        var m = ctx.Message;
        var result = await _svc.ReviewLinkAsync(m.LinkId, false, m.Reason, m.AdminUserId, ctx.CancellationToken);
        await ctx.RespondAsync(result.IsSuccess
            ? new AdminRejectLinkBusResult(true, null)
            : new AdminRejectLinkBusResult(false, result.Error));
    }
}

public class AdminRevokeLinkConsumer : IConsumer<AdminRevokeLinkBusRequest>
{
    private readonly IIntegrationService _svc;
    public AdminRevokeLinkConsumer(IIntegrationService svc) => _svc = svc;

    public async Task Consume(ConsumeContext<AdminRevokeLinkBusRequest> ctx)
    {
        var result = await _svc.RevokeLinkAsync(ctx.Message.LinkId, "Revoked by admin", ctx.CancellationToken);
        await ctx.RespondAsync(result.IsSuccess
            ? new AdminRevokeLinkBusResult(true, null)
            : new AdminRevokeLinkBusResult(false, result.Error));
    }
}
