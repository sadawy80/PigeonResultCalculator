using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.Common.Messages;
using PRC.FederationService.Data;
using PRC.FederationService.Models;

namespace PRC.FederationService.Events;

public class GetFederationStatsConsumer : IConsumer<GetFederationStatsRequest>
{
    private readonly FederationDbContext _db;
    public GetFederationStatsConsumer(FederationDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetFederationStatsRequest> ctx)
    {
        var total  = await _db.Federations.CountAsync();
        var active = await _db.Federations.CountAsync(c => c.IsActive);
        await ctx.RespondAsync(new FederationStatsResult(total, active));
    }
}

public class GetFederationsConsumer : IConsumer<GetFederationsRequest>
{
    private readonly FederationDbContext _db;
    public GetFederationsConsumer(FederationDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetFederationsRequest> ctx)
    {
        var q     = _db.Federations.AsQueryable();
        var total = await q.CountAsync();
        var items = await q
            .OrderBy(c => c.Name)
            .Skip((ctx.Message.Page - 1) * ctx.Message.PageSize)
            .Take(ctx.Message.PageSize)
            .Select(c => new AdminFederationItem(c.Id, c.Name, c.Code, c.Slug, c.FlagUrl, c.IsActive))
            .ToListAsync();

        await ctx.RespondAsync(new FederationsResult(items, total));
    }
}

public class CreateFederationConsumer : IConsumer<CreateFederationRequest>
{
    private readonly FederationDbContext _db;
    public CreateFederationConsumer(FederationDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<CreateFederationRequest> ctx)
    {
        var m = ctx.Message;

        if (await _db.Federations.AnyAsync(c => c.Code == m.Code))
        {
            await ctx.RespondAsync(new CreateFederationResult(false, null, $"Federation code '{m.Code}' already exists."));
            return;
        }

        var federation = new Federation
        {
            Name                = m.Name,
            Code                = m.Code,
            Slug                = m.Slug ?? m.Code.ToLowerInvariant(),
            FlagUrl             = m.FlagUrl ?? string.Empty,
            DefaultLanguage     = m.DefaultLanguage ?? "en",
            DefaultTimezone     = m.DefaultTimezone ?? "UTC",
            DefaultDistanceUnit = m.DefaultDistanceUnit ?? "km",
            IsActive            = true
        };
        _db.Federations.Add(federation);

        var page = new FederationPage { FederationId = federation.Id };
        _db.FederationPages.Add(page);

        await _db.SaveChangesAsync();
        await ctx.RespondAsync(new CreateFederationResult(true, federation.Id, null));
    }
}

public class ToggleFederationActiveConsumer : IConsumer<ToggleFederationActiveRequest>
{
    private readonly FederationDbContext _db;
    public ToggleFederationActiveConsumer(FederationDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<ToggleFederationActiveRequest> ctx)
    {
        var federation = await _db.Federations.FindAsync(ctx.Message.FederationId);
        if (federation is null)
        {
            await ctx.RespondAsync(new ToggleFederationActiveResult(ctx.Message.FederationId, false, "Federation not found."));
            return;
        }

        federation.IsActive   = !federation.IsActive;
        federation.UpdatedAt  = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await ctx.RespondAsync(new ToggleFederationActiveResult(federation.Id, federation.IsActive, null));
    }
}

public class FederationManagerAssignedConsumer : IConsumer<FederationManagerAssigned>
{
    private readonly FederationDbContext _db;
    private readonly ILogger<FederationManagerAssignedConsumer> _log;

    public FederationManagerAssignedConsumer(FederationDbContext db, ILogger<FederationManagerAssignedConsumer> log)
    {
        _db  = db;
        _log = log;
    }

    public async Task Consume(ConsumeContext<FederationManagerAssigned> ctx)
    {
        var msg       = ctx.Message;
        var federation = await _db.Federations.FirstOrDefaultAsync(c => c.Id == msg.FederationId);
        if (federation is null) return;

        federation.ManagerEmail = msg.ManagerEmail;
        federation.ManagerName  = msg.ManagerName;
        federation.UpdatedAt    = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _log.LogInformation("Federation {FederationId} manager updated to {Email}", msg.FederationId, msg.ManagerEmail);
    }
}
