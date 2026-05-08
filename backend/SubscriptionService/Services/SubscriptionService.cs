using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.SubscriptionService.Data;
using PRC.SubscriptionService.DTOs;
using PRC.SubscriptionService.Models;

namespace PRC.SubscriptionService.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly SubscriptionDbContext _db;

    public SubscriptionService(SubscriptionDbContext db) => _db = db;

    // ── Plans ─────────────────────────────────────────────────────────────────

    public async Task<Result<List<SubscriptionPlanDto>>> GetPlansAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var q = _db.SubscriptionPlans.AsQueryable();
        if (!includeInactive) q = q.Where(p => p.IsActive);

        var plans = await q.OrderBy(p => p.SortOrder).ThenBy(p => p.BillingCycle)
            .Select(p => p.ToDto()).ToListAsync(ct);

        return Result.Success(plans);
    }

    public async Task<Result<SubscriptionPlanDto>> GetPlanAsync(Guid planId, CancellationToken ct = default)
    {
        var plan = await _db.SubscriptionPlans.FindAsync([planId], ct);
        if (plan is null) return Result.NotFound<SubscriptionPlanDto>("SubscriptionPlan");
        return Result.Success(plan.ToDto());
    }

    public async Task<Result<SubscriptionPlanDto>> CreatePlanAsync(CreateSubscriptionPlanRequest req, Guid createdBy, CancellationToken ct = default)
    {
        var plan = new SubscriptionPlan
        {
            Name              = req.Name,
            Description       = req.Description,
            Type              = req.Type,
            BillingCycle      = req.BillingCycle,
            Price             = req.Price,
            Currency          = req.Currency,
            MaxClubs          = req.MaxClubs,
            MaxResultsPerClub = req.MaxResultsPerClub,
            IsActive          = true,
            IsHighlighted     = req.IsHighlighted,
            SortOrder         = req.SortOrder,
            Features          = req.Features,
            CreatedBy         = createdBy
        };

        _db.SubscriptionPlans.Add(plan);
        await _db.SaveChangesAsync(ct);
        return Result.Success(plan.ToDto());
    }

    public async Task<Result<SubscriptionPlanDto>> UpdatePlanAsync(Guid planId, UpdateSubscriptionPlanRequest req, Guid updatedBy, CancellationToken ct = default)
    {
        var plan = await _db.SubscriptionPlans.FindAsync([planId], ct);
        if (plan is null) return Result.NotFound<SubscriptionPlanDto>("SubscriptionPlan");

        plan.Name              = req.Name;
        plan.Description       = req.Description;
        plan.Price             = req.Price;
        plan.MaxClubs          = req.MaxClubs;
        plan.MaxResultsPerClub = req.MaxResultsPerClub;
        plan.IsActive          = req.IsActive;
        plan.IsHighlighted     = req.IsHighlighted;
        plan.SortOrder         = req.SortOrder;
        plan.Features          = req.Features;
        plan.UpdatedAt         = DateTime.UtcNow;
        plan.UpdatedBy         = updatedBy;

        await _db.SaveChangesAsync(ct);
        return Result.Success(plan.ToDto());
    }

    // ── Country Subscriptions ─────────────────────────────────────────────────

    public async Task<Result<FederationSubscriptionDto>> GetActiveFederationSubscriptionAsync(Guid FederationId, CancellationToken ct = default)
    {
        var sub = await _db.FederationSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.FederationId == FederationId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (sub is null) return Result.NotFound<FederationSubscriptionDto>("FederationSubscription");
        return Result.Success(sub.ToDto());
    }

    public async Task<Result<PagedResult<FederationSubscriptionDto>>> GetFederationSubscriptionsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.FederationSubscriptions.Include(s => s.Plan);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => s.ToDto()).ToListAsync(ct);

        return Result.Success(new PagedResult<FederationSubscriptionDto>
        {
            Items = items, TotalCount = total, Page = page, PageSize = pageSize
        });
    }

    public async Task<Result<FederationSubscriptionDto>> CreateFederationSubscriptionAsync(CreateFederationSubscriptionRequest req, Guid createdBy, CancellationToken ct = default)
    {
        var plan = await _db.SubscriptionPlans.FindAsync([req.PlanId], ct);
        if (plan is null) return Result.NotFound<FederationSubscriptionDto>("SubscriptionPlan");

        // Cancel any existing active subscription for this country
        var existing = await _db.FederationSubscriptions
            .Where(s => s.FederationId == req.FederationId && s.Status == SubscriptionStatus.Active)
            .ToListAsync(ct);
        foreach (var s in existing)
        {
            s.Status      = SubscriptionStatus.Cancelled;
            s.CancelledAt = DateTime.UtcNow;
            s.UpdatedAt   = DateTime.UtcNow;
        }

        var now       = DateTime.UtcNow;
        var expiresAt = req.BillingCycle switch
        {
            BillingCycle.Annual   => now.AddDays(365),
            BillingCycle.Seasonal => NextSeasonalExpiry(now),
            _                     => now.AddDays(30)
        };

        var sub = new FederationSubscription
        {
            FederationId        = req.FederationId,
            FederationName      = req.FederationName,
            PlanId           = req.PlanId,
            Status           = SubscriptionStatus.Active,
            BillingCycle     = req.BillingCycle,
            StartedAt        = now,
            ExpiresAt        = expiresAt,
            RenewsAt         = expiresAt,
            AmountPaid       = req.AmountPaid,
            PaymentReference = req.PaymentReference,
            Notes            = req.Notes,
            CreatedBy        = createdBy
        };

        _db.FederationSubscriptions.Add(sub);
        await _db.SaveChangesAsync(ct);
        return Result.Success(sub.ToDto());
    }

    // ── Club Subscriptions ────────────────────────────────────────────────────

    public async Task<Result<ClubSubscriptionDto>> GetActiveClubSubscriptionAsync(Guid clubId, CancellationToken ct = default)
    {
        var sub = await _db.ClubSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.ClubId == clubId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (sub is null) return Result.NotFound<ClubSubscriptionDto>("ClubSubscription");
        return Result.Success(sub.ToDto());
    }

    public async Task<Result<ClubSubscriptionDto>> CreateClubSubscriptionAsync(CreateClubSubscriptionRequest req, Guid createdBy, CancellationToken ct = default)
    {
        var plan = await _db.SubscriptionPlans.FindAsync([req.PlanId], ct);
        if (plan is null) return Result.NotFound<ClubSubscriptionDto>("SubscriptionPlan");

        var existing = await _db.ClubSubscriptions
            .Where(s => s.ClubId == req.ClubId && s.Status == SubscriptionStatus.Active)
            .ToListAsync(ct);
        foreach (var s in existing)
        {
            s.Status    = SubscriptionStatus.Cancelled;
            s.UpdatedAt = DateTime.UtcNow;
        }

        var now = DateTime.UtcNow;
        var sub = new ClubSubscription
        {
            ClubId           = req.ClubId,
            ClubName         = req.ClubName,
            PlanId           = req.PlanId,
            Status           = SubscriptionStatus.Active,
            StartedAt        = now,
            ExpiresAt        = now.AddDays(30),
            AmountPaid       = req.AmountPaid,
            PaymentReference = req.PaymentReference,
            Notes            = req.Notes,
            CreatedBy        = createdBy
        };

        _db.ClubSubscriptions.Add(sub);
        await _db.SaveChangesAsync(ct);
        return Result.Success(sub.ToDto());
    }

    // ── Internal Validation ───────────────────────────────────────────────────

    public async Task<SubscriptionValidationResult?> ValidateCountryAsync(Guid FederationId, CancellationToken ct = default)
    {
        var sub = await _db.FederationSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.FederationId == FederationId && s.Status == SubscriptionStatus.Active
                     && s.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(ct);

        if (sub is null) return null;
        return new SubscriptionValidationResult(
            IsActive: true,
            PlanName: sub.Plan.Name,
            MaxClubs: sub.Plan.MaxClubs,
            MaxResultsPerClub: sub.Plan.MaxResultsPerClub,
            CurrentClubCount: sub.CurrentClubCount,
            IsUnlimited: sub.Plan.MaxClubs == 0 && sub.Plan.MaxResultsPerClub == 0);
    }

    public async Task<SubscriptionValidationResult?> ValidateClubAsync(Guid clubId, CancellationToken ct = default)
    {
        var sub = await _db.ClubSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.ClubId == clubId && s.Status == SubscriptionStatus.Active
                     && s.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(ct);

        if (sub is null) return null;
        return new SubscriptionValidationResult(
            IsActive: true,
            PlanName: sub.Plan.Name,
            MaxClubs: 0,
            MaxResultsPerClub: sub.Plan.MaxResultsPerClub,
            CurrentClubCount: sub.ResultsUsedThisPeriod,
            IsUnlimited: sub.Plan.MaxResultsPerClub == 0);
    }

    private static DateTime NextSeasonalExpiry(DateTime from)
    {
        var candidate = new DateTime(from.Year, 9, 30, 23, 59, 59, DateTimeKind.Utc);
        return candidate <= from ? candidate.AddYears(1) : candidate;
    }
}

// ── Mapping extensions ────────────────────────────────────────────────────────

internal static class SubscriptionMappingExtensions
{
    internal static SubscriptionPlanDto ToDto(this SubscriptionPlan p) =>
        new(p.Id, p.Name, p.Description, p.Type, p.BillingCycle,
            p.Price, p.Currency, p.MaxClubs, p.MaxResultsPerClub,
            p.IsActive, p.IsHighlighted, p.SortOrder, p.Features, p.CreatedAt);

    internal static FederationSubscriptionDto ToDto(this FederationSubscription s) =>
        new(s.Id, s.FederationId, s.FederationName, s.PlanId, s.Plan?.Name ?? "",
            s.Status, s.BillingCycle, s.StartedAt, s.ExpiresAt,
            s.RenewsAt, s.CancelledAt, s.CurrentClubCount, s.AmountPaid,
            s.PaymentReference, s.Notes);

    internal static ClubSubscriptionDto ToDto(this ClubSubscription s) =>
        new(s.Id, s.ClubId, s.ClubName, s.PlanId, s.Plan?.Name ?? "",
            s.Status, s.StartedAt, s.ExpiresAt,
            s.ResultsUsedThisPeriod, s.AmountPaid, s.PaymentReference, s.Notes);
}
