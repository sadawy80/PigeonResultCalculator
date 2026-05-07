using MediatR;
using Microsoft.EntityFrameworkCore;
using PigeonRacing.Application.Common;
using PigeonRacing.Application.Common.Interfaces;
using PigeonRacing.Domain.Entities;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.Application.Features.CountryResults;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record CountryResultDto(
    Guid Id,
    Guid CountryId,
    string CountryName,
    string Name,
    string? Description,
    CountryResultStatus Status,
    int TotalEntriesCount,
    int TotalClubsCount,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    List<CountryResultEntryDto> TopEntries);

public record CountryResultEntryDto(
    Guid Id,
    int NationalRank,
    int? NationalCategoryRank,
    string RingNumber,
    string? FancierName,
    string ClubName,
    double VelocityMperMin,
    double DistanceKm);

// ── Commands ──────────────────────────────────────────────────────────────────

public record CreateCountryResultCommand(
    Guid CountryId,
    string Name,
    string? Description,
    List<Guid> RaceIds) : IRequest<Result<CountryResultDto>>;

public record PublishCountryResultCommand(Guid CountryResultId) : IRequest<Result<CountryResultDto>>;

// ── Queries ───────────────────────────────────────────────────────────────────

public record GetCountryResultQuery(Guid CountryResultId) : IRequest<Result<CountryResultDto>>;
public record GetCountryResultsQuery(Guid CountryId, PagedQuery Paged) : IRequest<Result<PagedResult<CountryResultDto>>>;

// ── Create Country Result Handler (Aggregation Engine) ────────────────────────

public class CreateCountryResultHandler : IRequestHandler<CreateCountryResultCommand, Result<CountryResultDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateCountryResultHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<CountryResultDto>> Handle(CreateCountryResultCommand cmd, CancellationToken ct)
    {
        var country = await _db.Countries.FirstOrDefaultAsync(c => c.Id == cmd.CountryId, ct);
        if (country == null) return Result.NotFound<CountryResultDto>("Country");

        // Validate that all races are published and belong to clubs in this country
        var races = await _db.Races
            .Include(r => r.Club)
            .Where(r => cmd.RaceIds.Contains(r.Id))
            .ToListAsync(ct);

        var invalidRaces = races.Where(r => r.Club.CountryId != cmd.CountryId || r.Status != RaceStatus.Published).ToList();
        if (invalidRaces.Any())
            return Result.Failure<CountryResultDto>(
                $"Races must be published and belong to clubs in this country. Invalid: {string.Join(", ", invalidRaces.Select(r => r.Name))}",
                "INVALID_RACES");

        // Pull all valid published results for those races
        var allResults = await _db.RaceResults
            .Include(r => r.User)
            .Where(r => cmd.RaceIds.Contains(r.RaceId)
                     && r.Status == ResultStatus.Published
                     && !r.IsDuplicate
                     && !r.HasInvalidTimestamp)
            .ToListAsync(ct);

        // Aggregate: rank by velocity descending across all clubs
        var nationalRanked = allResults
            .OrderByDescending(r => r.VelocityMperMin)
            .ThenBy(r => r.ArrivalTime)
            .ToList();

        var countryResult = new CountryResult
        {
            CountryId = cmd.CountryId,
            Name = cmd.Name,
            Description = cmd.Description,
            Status = CountryResultStatus.Draft,
            TotalEntriesCount = nationalRanked.Count,
            TotalClubsCount = races.Select(r => r.ClubId).Distinct().Count(),
            CreatedBy = _currentUser.UserId
        };

        // Map included races
        foreach (var race in races)
        {
            countryResult.IncludedRaces.Add(new CountryResultRace
            {
                RaceId = race.Id,
                ClubId = race.ClubId
            });
        }

        // Map entries with national ranks
        for (int i = 0; i < nationalRanked.Count; i++)
        {
            var r = nationalRanked[i];
            countryResult.Entries.Add(new CountryResultEntry
            {
                RaceResultId = r.Id,
                ClubId = r.Race?.ClubId ?? Guid.Empty,
                RingNumber = r.RingNumber,
                UserId = r.UserId,
                VelocityMperMin = r.VelocityMperMin,
                DistanceKm = r.DistanceKm,
                NationalRank = i + 1
            });
        }

        _db.CountryResults.Add(countryResult);
        await _db.SaveChangesAsync(ct);

        return Result.Success(await BuildDtoAsync(countryResult.Id, ct));
    }

    private async Task<CountryResultDto> BuildDtoAsync(Guid id, CancellationToken ct)
    {
        var cr = await _db.CountryResults
            .Include(x => x.Country)
            .Include(x => x.Entries).ThenInclude(e => e.Club)
            .Include(x => x.Entries).ThenInclude(e => e.User)
            .FirstAsync(x => x.Id == id, ct);
        return cr.ToDto();
    }
}

// ── Publish Country Result Handler ────────────────────────────────────────────

public class PublishCountryResultHandler : IRequestHandler<PublishCountryResultCommand, Result<CountryResultDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public PublishCountryResultHandler(IAppDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<Result<CountryResultDto>> Handle(PublishCountryResultCommand cmd, CancellationToken ct)
    {
        var cr = await _db.CountryResults
            .Include(x => x.Country)
            .Include(x => x.Entries).ThenInclude(e => e.Club)
            .Include(x => x.Entries).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(x => x.Id == cmd.CountryResultId, ct);

        if (cr == null) return Result.NotFound<CountryResultDto>("CountryResult");
        if (cr.Status == CountryResultStatus.Published)
            return Result.Conflict<CountryResultDto>("Already published.");

        cr.Status = CountryResultStatus.Published;
        cr.PublishedAt = DateTime.UtcNow;
        cr.PublishedByUserId = _currentUser.UserId;

        await _db.SaveChangesAsync(ct);

        // Notify all fanciers with entries
        var userIds = cr.Entries.Where(e => e.UserId.HasValue).Select(e => e.UserId!.Value).Distinct();
        foreach (var uid in userIds)
        {
            await _notifications.SendInAppAsync(uid,
                $"National Results: {cr.Name}",
                $"National results for {cr.Country.Name} have been published.", ct: ct);
        }

        return Result.Success(cr.ToDto());
    }
}

// ── Get Country Result Handler ────────────────────────────────────────────────

public class GetCountryResultHandler : IRequestHandler<GetCountryResultQuery, Result<CountryResultDto>>
{
    private readonly IAppDbContext _db;

    public GetCountryResultHandler(IAppDbContext db) => _db = db;

    public async Task<Result<CountryResultDto>> Handle(GetCountryResultQuery query, CancellationToken ct)
    {
        var cr = await _db.CountryResults
            .Include(x => x.Country)
            .Include(x => x.Entries).ThenInclude(e => e.Club)
            .Include(x => x.Entries).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(x => x.Id == query.CountryResultId, ct);

        return cr == null ? Result.NotFound<CountryResultDto>("CountryResult") : Result.Success(cr.ToDto());
    }
}

// ── Get Country Results Handler ───────────────────────────────────────────────

public class GetCountryResultsHandler : IRequestHandler<GetCountryResultsQuery, Result<PagedResult<CountryResultDto>>>
{
    private readonly IAppDbContext _db;

    public GetCountryResultsHandler(IAppDbContext db) => _db = db;

    public async Task<Result<PagedResult<CountryResultDto>>> Handle(GetCountryResultsQuery query, CancellationToken ct)
    {
        var q = _db.CountryResults
            .Include(x => x.Country)
            .Include(x => x.Entries).ThenInclude(e => e.Club)
            .Include(x => x.Entries).ThenInclude(e => e.User)
            .Where(x => x.CountryId == query.CountryId);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.PublishedAt ?? x.CreatedAt)
            .Skip(query.Paged.Skip)
            .Take(query.Paged.PageSize)
            .ToListAsync(ct);

        return Result.Success(new PagedResult<CountryResultDto>
        {
            Items = items.Select(x => x.ToDto()).ToList(),
            TotalCount = total,
            Page = query.Paged.Page,
            PageSize = query.Paged.PageSize
        });
    }
}

// ── Mapping ───────────────────────────────────────────────────────────────────

public static class CountryResultMappingExtensions
{
    public static CountryResultDto ToDto(this CountryResult cr) => new(
        cr.Id, cr.CountryId, cr.Country?.Name ?? string.Empty,
        cr.Name, cr.Description, cr.Status,
        cr.TotalEntriesCount, cr.TotalClubsCount,
        cr.PublishedAt, cr.CreatedAt,
        cr.Entries.OrderBy(e => e.NationalRank).Take(10).Select(e => new CountryResultEntryDto(
            e.Id, e.NationalRank, e.NationalCategoryRank,
            e.RingNumber, e.User?.FullName,
            e.Club?.Name ?? string.Empty,
            e.VelocityMperMin, e.DistanceKm)).ToList());
}
