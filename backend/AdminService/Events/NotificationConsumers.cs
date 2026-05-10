using MassTransit;
using PRC.AdminService.Data;
using PRC.AdminService.Models;
using PRC.Common.Messages;

namespace PRC.AdminService.Events;

public class UpgradeRequestSubmittedConsumer : IConsumer<UpgradeRequestSubmitted>
{
    private readonly AdminDbContext _db;
    public UpgradeRequestSubmittedConsumer(AdminDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<UpgradeRequestSubmitted> ctx)
    {
        var m = ctx.Message;
        var role = m.RequestedRole.ToString().Replace("Manager", " Manager");
        _db.AdminNotifications.Add(new AdminNotification
        {
            Type      = "UpgradeRequest",
            Title     = $"New {role} request",
            Body      = $"{m.UserFullName} ({m.UserEmail}) has requested the {role} role.",
            ActionUrl = "/admin/upgrade-requests",
            SourceId  = m.RequestId.ToString()
        });
        await _db.SaveChangesAsync(ctx.CancellationToken);
    }
}

public class ExternalLinkRequestedConsumer : IConsumer<ExternalLinkRequested>
{
    private readonly AdminDbContext _db;
    public ExternalLinkRequestedConsumer(AdminDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<ExternalLinkRequested> ctx)
    {
        var m = ctx.Message;
        _db.AdminNotifications.Add(new AdminNotification
        {
            Type      = "LinkRequest",
            Title     = $"New link request — {m.ExternalPlatformName}",
            Body      = $"Loft \"{m.ExternalLoftName}\" has requested access via {m.ExternalPlatformName}.",
            ActionUrl = "/admin/link-requests",
            SourceId  = m.LinkId.ToString()
        });
        await _db.SaveChangesAsync(ctx.CancellationToken);
    }
}
