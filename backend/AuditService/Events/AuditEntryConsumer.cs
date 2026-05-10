using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.AuditService.Data;
using PRC.AuditService.Models;
using PRC.Common.Messages;

namespace PRC.AuditService.Events;

public class AuditEntryConsumer : IConsumer<AuditEntryEvent>
{
    private readonly AuditDbContext _db;

    public AuditEntryConsumer(AuditDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<AuditEntryEvent> ctx)
    {
        var e = ctx.Message;
        _db.AuditLogs.Add(new AuditLog
        {
            Action            = e.Action,
            EntityType        = e.EntityType,
            EntityId          = e.EntityId,
            Severity          = e.Severity,
            Details           = e.Details,
            TriggeredByUserId = e.TriggeredByUserId,
            TriggeredByName   = e.TriggeredByName,
            CorrelationId     = e.CorrelationId,
            ServiceName       = e.ServiceName,
            IpAddress         = e.IpAddress,
            Country           = e.Country,
            CreatedAt         = e.OccurredAt
        });
        await _db.SaveChangesAsync(ctx.CancellationToken);
    }
}
