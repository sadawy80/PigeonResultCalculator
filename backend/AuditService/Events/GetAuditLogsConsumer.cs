using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.AuditService.Data;
using PRC.Common.Messages;

namespace PRC.AuditService.Events;

public class GetAuditLogsConsumer : IConsumer<GetAuditLogsRequest>
{
    private readonly AuditDbContext _db;

    public GetAuditLogsConsumer(AuditDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetAuditLogsRequest> ctx)
    {
        var req = ctx.Message;
        var q   = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(req.Action))     q = q.Where(e => e.Action     == req.Action);
        if (!string.IsNullOrEmpty(req.EntityType)) q = q.Where(e => e.EntityType == req.EntityType);
        if (req.Severity.HasValue)                 q = q.Where(e => e.Severity   == req.Severity.Value);

        var total = await q.CountAsync(ctx.CancellationToken);
        var items = await q
            .OrderByDescending(e => e.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .Select(e => new AuditLogItem(
                e.Id, e.Action, e.EntityType, e.EntityId,
                e.Severity.ToString(), e.Details,
                e.TriggeredByUserId, e.TriggeredByName,
                e.CorrelationId, e.ServiceName,
                e.IpAddress, e.Country, e.CreatedAt))
            .ToListAsync(ctx.CancellationToken);

        await ctx.RespondAsync(new GetAuditLogsResponse(items, total, req.Page, req.PageSize));
    }
}
