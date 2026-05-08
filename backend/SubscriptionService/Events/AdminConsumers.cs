using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.Common.Messages;
using Messages = PRC.Common.Messages;
using PRC.SubscriptionService.Data;
using PRC.SubscriptionService.DTOs;
using PRC.SubscriptionService.Services;
using SubDtos = PRC.SubscriptionService.DTOs;

namespace PRC.SubscriptionService.Events;

public class GetSubscriptionStatsConsumer : IConsumer<GetSubscriptionStatsRequest>
{
    private readonly SubscriptionDbContext _db;
    public GetSubscriptionStatsConsumer(SubscriptionDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetSubscriptionStatsRequest> ctx)
    {
        var countrySubs = await _db.FederationSubscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Active && !s.IsDeleted);
        var clubSubs    = await _db.ClubSubscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Active && !s.IsDeleted);

        await ctx.RespondAsync(new SubscriptionStatsResult(countrySubs, clubSubs));
    }
}

public class GetSubscriptionPlansConsumer : IConsumer<GetSubscriptionPlansRequest>
{
    private readonly ISubscriptionService _svc;
    public GetSubscriptionPlansConsumer(ISubscriptionService svc) => _svc = svc;

    public async Task Consume(ConsumeContext<GetSubscriptionPlansRequest> ctx)
    {
        var result = await _svc.GetPlansAsync(includeInactive: true);
        var plans  = result.IsSuccess
            ? result.Value!.Select(p => new SubscriptionPlanItem(
                p.Id, p.Name, p.Type.ToString(), p.BillingCycle.ToString(),
                p.Price, p.Currency, p.MaxClubs, p.MaxResultsPerClub,
                p.IsActive, p.IsHighlighted, p.SortOrder)).ToList()
            : new List<SubscriptionPlanItem>();

        await ctx.RespondAsync(new SubscriptionPlansResult(plans));
    }
}

public class GetFederationSubscriptionsConsumer : IConsumer<GetFederationSubscriptionsRequest>
{
    private readonly ISubscriptionService _svc;
    public GetFederationSubscriptionsConsumer(ISubscriptionService svc) => _svc = svc;

    public async Task Consume(ConsumeContext<GetFederationSubscriptionsRequest> ctx)
    {
        var result = await _svc.GetFederationSubscriptionsAsync(ctx.Message.Page, ctx.Message.PageSize);
        if (!result.IsSuccess)
        {
            await ctx.RespondAsync(new FederationSubscriptionsResult(new List<FederationSubscriptionItem>(), 0));
            return;
        }

        var items = result.Value!.Items.Select(s => new FederationSubscriptionItem(
            s.Id, s.FederationId, s.FederationName,
            s.PlanName, s.BillingCycle.ToString(),
            s.Status, s.ExpiresAt, s.StartedAt)).ToList();

        await ctx.RespondAsync(new FederationSubscriptionsResult(items, result.Value.TotalCount));
    }
}

public class CreateFederationSubscriptionConsumer : IConsumer<Messages.CreateFederationSubscriptionRequest>
{
    private readonly ISubscriptionService _svc;
    public CreateFederationSubscriptionConsumer(ISubscriptionService svc) => _svc = svc;

    public async Task Consume(ConsumeContext<Messages.CreateFederationSubscriptionRequest> ctx)
    {
        var m = ctx.Message;

        if (!Enum.TryParse<BillingCycle>(m.BillingCycle, out var billingCycle))
        {
            await ctx.RespondAsync(new CreateFederationSubscriptionResult(false, null, $"Invalid billing cycle: {m.BillingCycle}"));
            return;
        }

        var req    = new SubDtos.CreateFederationSubscriptionRequest(
            m.FederationId, m.FederationName, m.PlanId, billingCycle,
            m.AmountPaid, m.PaymentReference, m.Notes);

        var result = await _svc.CreateFederationSubscriptionAsync(req, m.CreatedBy);
        if (result.IsFailure)
        {
            await ctx.RespondAsync(new CreateFederationSubscriptionResult(false, null, result.Error));
            return;
        }

        await ctx.RespondAsync(new CreateFederationSubscriptionResult(true, result.Value!.Id, null));
    }
}

public class GetActiveSubscriptionCountConsumer : IConsumer<GetActiveSubscriptionCountRequest>
{
    private readonly SubscriptionDbContext _db;
    public GetActiveSubscriptionCountConsumer(SubscriptionDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetActiveSubscriptionCountRequest> ctx)
    {
        var country = await _db.FederationSubscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Active && !s.IsDeleted);
        var club    = await _db.ClubSubscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Active && !s.IsDeleted);

        await ctx.RespondAsync(new ActiveSubscriptionCountResult(country, club));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Result limit enforcement (RaceService → SubscriptionService)
// ─────────────────────────────────────────────────────────────────────────────

public class CheckResultLimitConsumer : IConsumer<CheckResultLimitRequest>
{
    private readonly SubscriptionDbContext _db;
    public CheckResultLimitConsumer(SubscriptionDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<CheckResultLimitRequest> ctx)
    {
        var sub = await _db.ClubSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.ClubId == ctx.Message.ClubId
                     && s.Status == SubscriptionStatus.Active
                     && s.ExpiresAt > DateTime.UtcNow
                     && !s.IsDeleted)
            .FirstOrDefaultAsync(ctx.CancellationToken);

        if (sub == null)
        {
            // No active club subscription — unlimited
            await ctx.RespondAsync(new CheckResultLimitResult(true, null, 0, 0));
            return;
        }

        var max     = sub.Plan.MaxResultsPerClub; // 0 = unlimited
        var current = sub.ResultsUsedThisPeriod;

        if (max == 0)
        {
            await ctx.RespondAsync(new CheckResultLimitResult(true, null, current, 0));
            return;
        }

        var allowed = current + ctx.Message.NewResultsCount <= max;
        await ctx.RespondAsync(new CheckResultLimitResult(
            allowed,
            allowed ? null : $"Result limit reached: {current}/{max} used this period.",
            current, max));
    }
}

public class IncrementResultUsageConsumer : IConsumer<IncrementResultUsageRequest>
{
    private readonly SubscriptionDbContext _db;
    public IncrementResultUsageConsumer(SubscriptionDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<IncrementResultUsageRequest> ctx)
    {
        var sub = await _db.ClubSubscriptions
            .Where(s => s.ClubId == ctx.Message.ClubId
                     && s.Status == SubscriptionStatus.Active
                     && s.ExpiresAt > DateTime.UtcNow
                     && !s.IsDeleted)
            .FirstOrDefaultAsync(ctx.CancellationToken);

        if (sub == null)
        {
            await ctx.RespondAsync(new IncrementResultUsageResult(true, null));
            return;
        }

        sub.ResultsUsedThisPeriod += ctx.Message.Count;
        sub.UpdatedAt              = DateTime.UtcNow;
        await _db.SaveChangesAsync(ctx.CancellationToken);

        await ctx.RespondAsync(new IncrementResultUsageResult(true, null));
    }
}

public class GetFederationSubscriptionLimitsConsumer : IConsumer<GetFederationSubscriptionLimitsRequest>
{
    private readonly SubscriptionDbContext _db;
    public GetFederationSubscriptionLimitsConsumer(SubscriptionDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetFederationSubscriptionLimitsRequest> ctx)
    {
        var sub = await _db.FederationSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.FederationId == ctx.Message.FederationId
                     && s.Status == SubscriptionStatus.Active
                     && s.ExpiresAt > DateTime.UtcNow
                     && !s.IsDeleted)
            .FirstOrDefaultAsync(ctx.CancellationToken);

        if (sub == null)
        {
            await ctx.RespondAsync(new GetFederationSubscriptionLimitsResult(false, 0, 0, true));
            return;
        }

        var max = sub.Plan.MaxClubs;
        await ctx.RespondAsync(new GetFederationSubscriptionLimitsResult(
            true, max, sub.CurrentClubCount, max == 0));
    }
}

// ─────────────────────────────────────────────────────────────────────────────

public class GetPublicSubscriptionPlansConsumer : IConsumer<GetPublicSubscriptionPlansRequest>
{
    private readonly ISubscriptionService _svc;
    public GetPublicSubscriptionPlansConsumer(ISubscriptionService svc) => _svc = svc;

    public async Task Consume(ConsumeContext<GetPublicSubscriptionPlansRequest> ctx)
    {
        var result = await _svc.GetPlansAsync(includeInactive: false, ctx.CancellationToken);
        if (!result.IsSuccess)
        {
            await ctx.RespondAsync(new PublicSubscriptionPlansResult(new List<PublicPlanGroup>()));
            return;
        }

        var grouped = result.Value!
            .Where(p => p.Type == SubscriptionType.Federation)
            .GroupBy(p => p.Name)
            .Select(g =>
            {
                var monthly  = g.FirstOrDefault(p => p.BillingCycle == BillingCycle.Monthly);
                var seasonal = g.FirstOrDefault(p => p.BillingCycle == BillingCycle.Seasonal);
                var annual   = g.FirstOrDefault(p => p.BillingCycle == BillingCycle.Annual);
                var any      = monthly ?? seasonal ?? annual!;

                static PublicPlanCycle? Cycle(SubDtos.SubscriptionPlanDto? p) =>
                    p == null ? null : new PublicPlanCycle(p.Price, p.MaxClubs, p.MaxResultsPerClub, p.Features);

                return new PublicPlanGroup(
                    any.Name, any.Description, any.IsHighlighted, any.Currency, any.SortOrder,
                    Cycle(monthly), Cycle(seasonal), Cycle(annual));
            })
            .OrderBy(g => g.SortOrder)
            .ToList();

        await ctx.RespondAsync(new PublicSubscriptionPlansResult(grouped));
    }
}
