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
        var yearStart = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var total     = await _db.Federations.CountAsync();
        var active    = await _db.Federations.CountAsync(c => c.IsActive);
        var thisYear  = await _db.Federations.CountAsync(f => f.CreatedAt >= yearStart);
        await ctx.RespondAsync(new FederationStatsResult(total, active, thisYear));
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
            .Select(c => new AdminFederationItem(c.Id, c.Name, c.Code, c.Slug, c.FlagUrl, c.IsActive, c.ManagerEmail))
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

public class AdminDeleteFederationConsumer : IConsumer<AdminDeleteFederationRequest>
{
    private readonly FederationDbContext _db;
    public AdminDeleteFederationConsumer(FederationDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<AdminDeleteFederationRequest> ctx)
    {
        var m = ctx.Message;
        var fed = await _db.Federations
            .Include(f => f.FederationPage)
            .Include(f => f.FederationResults)
                .ThenInclude(r => r.IncludedRaces)
            .Include(f => f.FederationResults)
                .ThenInclude(r => r.Entries)
            .FirstOrDefaultAsync(f => f.Id == m.FederationId, ctx.CancellationToken);

        if (fed is null)
        {
            await ctx.RespondAsync(new AdminDeleteFederationResult(false, "Federation not found.", null));
            return;
        }
        if (fed.IsActive)
        {
            await ctx.RespondAsync(new AdminDeleteFederationResult(false, "Federation must be deactivated before deletion.", fed.Name));
            return;
        }

        var now = DateTime.UtcNow;

        if (fed.FederationPage != null)
        {
            fed.FederationPage.IsDeleted = true;
            fed.FederationPage.UpdatedAt = now;
        }

        foreach (var result in fed.FederationResults)
        {
            foreach (var race  in result.IncludedRaces) { race.IsDeleted  = true; race.UpdatedAt  = now; }
            foreach (var entry in result.Entries)       { entry.IsDeleted = true; entry.UpdatedAt = now; }
            result.IsDeleted = true;
            result.UpdatedAt = now;
        }

        fed.IsDeleted = true;
        fed.UpdatedAt = now;

        await _db.SaveChangesAsync(ctx.CancellationToken);
        await ctx.RespondAsync(new AdminDeleteFederationResult(true, null, fed.Name));
    }
}

