using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.FederationService.Data;
using PRC.FederationService.DTOs;
using PRC.FederationService.Models;

namespace PRC.FederationService.Services;

public class FederationService : IFederationService
{
    private readonly FederationDbContext _db;

    public FederationService(FederationDbContext db) => _db = db;

    public async Task<Result<FederationResultDto>> CreateFederationResultAsync(
        CreateFederationResultRequest req, Guid createdBy, CancellationToken ct)
    {
        var federation = await _db.Federations.FirstOrDefaultAsync(c => c.Id == req.FederationId, ct);
        if (federation == null) return Result.NotFound<FederationResultDto>("Federation");

        var races = await _db.RaceSnapshotCaches
            .Where(r => req.RaceIds.Contains(r.RaceId))
            .ToListAsync(ct);

        var missingRaceIds = req.RaceIds.Except(races.Select(r => r.RaceId)).ToList();
        if (missingRaceIds.Any())
            return Result.Failure<FederationResultDto>(
                $"Races not yet cached locally (not published via bus): {string.Join(", ", missingRaceIds)}",
                "RACES_NOT_CACHED");

        var invalidRaces = races.Where(r => r.Status != RaceStatus.Published).ToList();
        if (invalidRaces.Any())
            return Result.Failure<FederationResultDto>(
                $"Races must be published. Invalid: {string.Join(", ", invalidRaces.Select(r => r.RaceName))}",
                "INVALID_RACES");

        var allResults = await _db.RaceResultSnapshotCaches
            .Where(r => races.Select(s => s.Id).Contains(r.RaceSnapshotCacheId))
            .OrderByDescending(r => r.SpeedMperMin)
            .ThenBy(r => r.ArrivalTime)
            .ToListAsync(ct);

        var federationResult = new FederationResult
        {
            FederationId      = req.FederationId,
            Name              = req.Name,
            Description       = req.Description,
            Status            = FederationResultStatus.Draft,
            TotalEntriesCount = allResults.Count,
            TotalClubsCount   = races.Select(r => r.ClubId).Distinct().Count(),
            CreatedBy         = createdBy
        };

        foreach (var race in races)
        {
            federationResult.IncludedRaces.Add(new FederationResultRace
            {
                RaceId = race.RaceId,
                ClubId = race.ClubId
            });
        }

        for (int i = 0; i < allResults.Count; i++)
        {
            var r = allResults[i];
            federationResult.Entries.Add(new FederationResultEntry
            {
                RaceResultId    = r.ResultId,
                ClubId          = r.ClubId,
                ClubName        = r.ClubName,
                RingNumber      = r.RingNumber,
                UserId          = r.UserId,
                UserFullName    = r.UserFullName,
                SpeedMperMin    = r.SpeedMperMin,
                DistanceKm      = r.DistanceKm,
                NationalRank    = i + 1
            });
        }

        _db.FederationResults.Add(federationResult);
        await _db.SaveChangesAsync(ct);

        return Result.Success(await BuildDtoAsync(federationResult.Id, ct));
    }

    public async Task<Result<FederationResultDto>> PublishFederationResultAsync(
        Guid federationResultId, Guid publishedBy, CancellationToken ct)
    {
        var fr = await _db.FederationResults
            .Include(x => x.Federation)
            .Include(x => x.Entries)
            .FirstOrDefaultAsync(x => x.Id == federationResultId, ct);

        if (fr == null) return Result.NotFound<FederationResultDto>("FederationResult");
        if (fr.Status == FederationResultStatus.Published)
            return Result.Conflict<FederationResultDto>("Already published.");

        fr.Status = FederationResultStatus.Published;
        fr.PublishedAt = DateTime.UtcNow;
        fr.PublishedByUserId = publishedBy;

        await _db.SaveChangesAsync(ct);
        return Result.Success(fr.ToDto());
    }

    public async Task<Result<FederationResultDto>> GetFederationResultAsync(Guid federationResultId, CancellationToken ct)
    {
        var fr = await _db.FederationResults
            .Include(x => x.Federation)
            .Include(x => x.Entries)
            .FirstOrDefaultAsync(x => x.Id == federationResultId, ct);

        return fr == null ? Result.NotFound<FederationResultDto>("FederationResult") : Result.Success(fr.ToDto());
    }

    public async Task<Result<PagedResult<FederationResultDto>>> GetFederationResultsAsync(
        Guid federationId, PagedQuery paged, CancellationToken ct)
    {
        var q = _db.FederationResults
            .Include(x => x.Federation)
            .Include(x => x.Entries)
            .Where(x => x.FederationId == federationId);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.PublishedAt ?? x.CreatedAt)
            .Skip(paged.Skip)
            .Take(paged.PageSize)
            .ToListAsync(ct);

        return Result.Success(new PagedResult<FederationResultDto>
        {
            Items = items.Select(x => x.ToDto()).ToList(),
            TotalCount = total,
            Page = paged.Page,
            PageSize = paged.PageSize
        });
    }

    public async Task<Result<object>> GetFederationPageAsync(Guid federationId, CancellationToken ct)
    {
        var page = await _db.FederationPages
            .FirstOrDefaultAsync(p => p.FederationId == federationId && !p.IsDeleted, ct);

        if (page == null) return Result.NotFound<object>("FederationPage");

        return Result.Success<object>(new
        {
            page.Id, page.Slug, page.IsPublished, page.Theme,
            page.AnnouncementsJson, page.HeaderHtml, page.FooterHtml
        });
    }

    public async Task<Result> UpdateFederationPageAsync(Guid federationId, UpdateFederationPageRequest req, CancellationToken ct)
    {
        var page = await _db.FederationPages
            .FirstOrDefaultAsync(p => p.FederationId == federationId && !p.IsDeleted, ct);

        if (page == null) return Result.NotFound("FederationPage");

        if (req.Theme.HasValue) page.Theme = req.Theme.Value;
        if (req.IsPublished.HasValue) page.IsPublished = req.IsPublished.Value;
        if (req.AnnouncementsJson != null) page.AnnouncementsJson = req.AnnouncementsJson;
        if (req.HeaderHtml != null) page.HeaderHtml = req.HeaderHtml;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IEnumerable<FederationDto>>> GetAllFederationsAsync(CancellationToken ct)
    {
        var federations = await _db.Federations
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        return Result.Success(federations.Select(c => c.ToDto()));
    }

    private async Task<FederationResultDto> BuildDtoAsync(Guid id, CancellationToken ct)
    {
        var fr = await _db.FederationResults
            .Include(x => x.Federation)
            .Include(x => x.Entries)
            .FirstAsync(x => x.Id == id, ct);
        return fr.ToDto();
    }
}

public static class MappingExtensions
{
    public static FederationResultDto ToDto(this FederationResult fr) => new(
        fr.Id, fr.FederationId, fr.Federation?.Name ?? string.Empty,
        fr.Name, fr.Description, fr.Status,
        fr.TotalEntriesCount, fr.TotalClubsCount,
        fr.PublishedAt, fr.CreatedAt,
        fr.Entries.OrderBy(e => e.NationalRank).Take(10).Select(e => new FederationResultEntryDto(
            e.Id, e.NationalRank, e.NationalCategoryRank,
            e.RingNumber, e.UserFullName,
            e.ClubName ?? string.Empty,
            e.SpeedMperMin, e.DistanceKm)).ToList());

    public static FederationDto ToDto(this Federation f) => new(
        f.Id, f.Name, f.Code, f.Slug, f.FlagUrl,
        f.DefaultLanguage, f.DefaultTimezone, f.DefaultDistanceUnit, f.IsActive);
}
