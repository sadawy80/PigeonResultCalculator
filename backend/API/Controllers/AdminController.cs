using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PigeonRacing.Application.Common;
using PigeonRacing.Application.Common.Interfaces;
using PigeonRacing.Domain.Entities;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.API.Controllers;

[Route("api/admin")]
[Authorize(Roles = "SuperAdmin")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AdminController(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ── Platform Stats ────────────────────────────────────────────────────────

    [HttpGet("stats")]
    public async Task<IActionResult> GetPlatformStats(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var stats = new
        {
            TotalCountries    = await _db.Countries.CountAsync(c => !c.IsDeleted, ct),
            TotalClubs        = await _db.Clubs.CountAsync(c => !c.IsDeleted, ct),
            TotalUsers        = await _db.Users.CountAsync(u => !u.IsDeleted, ct),
            TotalRaces        = await _db.Races.CountAsync(r => !r.IsDeleted, ct),
            PublishedRaces    = await _db.Races.CountAsync(r => r.Status == RaceStatus.Published, ct),
            RacesThisMonth    = await _db.Races.CountAsync(r => r.CreatedAt >= monthStart, ct),
            ActiveSubscriptions = await _db.CountrySubscriptions
                .CountAsync(s => s.Status == SubscriptionStatus.Active, ct),
            TotalResults      = await _db.RaceResults.CountAsync(r => !r.IsDeleted, ct),
        };

        return Ok(ApiResponse<object>.Ok(stats));
    }

    // ── Countries ─────────────────────────────────────────────────────────────

    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var q = _db.Countries.Include(c => c.Clubs).Where(c => !c.IsDeleted);
        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id, c.Name, c.Code, c.Slug, c.IsActive,
                ClubCount = c.Clubs.Count(cl => cl.IsActive),
                c.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { items, total, page, pageSize }));
    }

    [HttpPost("countries")]
    public async Task<IActionResult> CreateCountry([FromBody] CreateCountryRequest req, CancellationToken ct)
    {
        var exists = await _db.Countries.AnyAsync(c => c.Code == req.Code.ToUpperInvariant(), ct);
        if (exists) return Conflict(ApiResponse<object>.Fail("Country code already exists."));

        var country = new Country
        {
            Name = req.Name,
            Code = req.Code.ToUpperInvariant(),
            Slug = req.Slug.ToLowerInvariant(),
            IsActive = true,
            CreatedBy = _currentUser.UserId
        };

        _db.Countries.Add(country);
        await _db.SaveChangesAsync(ct);

        // Auto-create a country page
        _db.CountryPages.Add(new CountryPage
        {
            CountryId = country.Id,
            Slug = country.Slug,
            Theme = SiteTheme.Skyline
        });
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { country.Id, country.Name, country.Code }));
    }

    [HttpPut("countries/{countryId:guid}/toggle-active")]
    public async Task<IActionResult> ToggleCountryActive(Guid countryId, CancellationToken ct)
    {
        var country = await _db.Countries.FindAsync(new object[] { countryId }, ct);
        if (country == null) return NotFound(ApiResponse<object>.Fail("Country not found."));
        country.IsActive = !country.IsActive;
        country.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { country.Id, country.IsActive }));
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var q = _db.Users.Where(u => !u.IsDeleted);

        if (!string.IsNullOrEmpty(search))
            q = q.Where(u => u.FirstName.Contains(search) || u.LastName.Contains(search)
                           || (u.Email != null && u.Email.Contains(search)));

        if (role.HasValue)
            q = q.Where(u => u.Role == role.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(u => u.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id, u.FirstName, u.LastName, u.Email, u.Role,
                u.IsActive, u.CountryId, u.LastLoginAt, u.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { items, total, page, pageSize }));
    }

    [HttpPut("users/{userId:guid}/toggle-active")]
    public async Task<IActionResult> ToggleUserActive(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        if (user == null) return NotFound(ApiResponse<object>.Fail("User not found."));

        // Prevent self-deactivation
        if (user.Id == _currentUser.UserId)
            return BadRequest(ApiResponse<object>.Fail("Cannot deactivate your own account."));

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { user.Id, user.IsActive }));
    }

    // ── Clubs ─────────────────────────────────────────────────────────────────

    [HttpGet("clubs")]
    public async Task<IActionResult> GetAllClubs(
        [FromQuery] string? search = null,
        [FromQuery] Guid? countryId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var q = _db.Clubs.Include(c => c.Country).Where(c => !c.IsDeleted);

        if (!string.IsNullOrEmpty(search))
            q = q.Where(c => c.Name.Contains(search) || c.Code.Contains(search));

        if (countryId.HasValue)
            q = q.Where(c => c.CountryId == countryId.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id, c.Name, c.Code, c.City, c.IsActive,
                c.CountryId, CountryName = c.Country.Name, CountryCode = c.Country.Code,
                c.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { items, total, page, pageSize }));
    }

    [HttpPut("clubs/{clubId:guid}/suspend")]
    public async Task<IActionResult> SuspendClub(Guid clubId, CancellationToken ct)
    {
        var club = await _db.Clubs.FindAsync(new object[] { clubId }, ct);
        if (club == null) return NotFound(ApiResponse<object>.Fail("Club not found."));
        club.IsActive = !club.IsActive;
        club.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { club.Id, club.IsActive }));
    }

    // ── Subscription Plans ────────────────────────────────────────────────────

    [HttpGet("subscription-plans")]
    public async Task<IActionResult> GetSubscriptionPlans(CancellationToken ct)
    {
        var plans = await _db.SubscriptionPlans
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(plans));
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var q = _db.CountrySubscriptions
            .Include(s => s.Country)
            .Include(s => s.Plan)
            .Where(s => !s.IsDeleted);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                CountryName = s.Country.Name,
                PlanName = s.Plan.Name,
                s.Status, s.BillingCycle,
                s.StartedAt, s.ExpiresAt, s.RenewsAt
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { items, total, page, pageSize }));
    }

    [HttpPost("subscriptions")]
    public async Task<IActionResult> CreateSubscription(
        [FromBody] CreateSubscriptionRequest req, CancellationToken ct)
    {
        var plan = await _db.SubscriptionPlans.FindAsync(new object[] { req.PlanId }, ct);
        if (plan == null) return NotFound(ApiResponse<object>.Fail("Plan not found."));

        // Cancel any existing active subscription for this country
        var existing = await _db.CountrySubscriptions
            .Where(s => s.CountryId == req.CountryId && s.Status == SubscriptionStatus.Active)
            .ToListAsync(ct);
        foreach (var s in existing)
        {
            s.Status = SubscriptionStatus.Cancelled;
            s.CancelledAt = DateTime.UtcNow;
        }

        var duration = req.BillingCycle == BillingCycle.Annual ? TimeSpan.FromDays(365) : TimeSpan.FromDays(30);
        var sub = new CountrySubscription
        {
            CountryId    = req.CountryId,
            PlanId       = req.PlanId,
            Status       = SubscriptionStatus.Active,
            BillingCycle = req.BillingCycle,
            StartedAt    = DateTime.UtcNow,
            ExpiresAt    = DateTime.UtcNow.Add(duration),
            RenewsAt     = DateTime.UtcNow.Add(duration)
        };

        _db.CountrySubscriptions.Add(sub);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { sub.Id, sub.Status, sub.ExpiresAt }));
    }

    // ── Event Log ─────────────────────────────────────────────────────────────

    [HttpGet("events")]
    public async Task<IActionResult> GetEventLog(
        [FromQuery] string? eventType = null,
        [FromQuery] string? aggregateType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var q = _db.DomainEvents.AsQueryable();

        if (!string.IsNullOrEmpty(eventType))
            q = q.Where(e => e.EventType == eventType);

        if (!string.IsNullOrEmpty(aggregateType))
            q = q.Where(e => e.AggregateType == aggregateType);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id, e.EventType, e.AggregateId, e.AggregateType,
                e.TriggeredByUserId, e.IsProcessed, e.ProcessedAt,
                e.RetryCount, e.CorrelationId, e.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { items, total, page, pageSize }));
    }
}

// ── Request models ────────────────────────────────────────────────────────────

public record CreateCountryRequest(string Name, string Code, string Slug);

public record CreateSubscriptionRequest(
    Guid CountryId, Guid PlanId, BillingCycle BillingCycle);
