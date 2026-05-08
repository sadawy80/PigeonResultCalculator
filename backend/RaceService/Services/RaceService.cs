using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.Common.Messages;
using PRC.RaceService.Data;
using PRC.RaceService.DTOs;
using PRC.RaceService.Models;

namespace PRC.RaceService.Services;

public interface IRaceService
{
    Task<Result<RaceDto>> CreateAsync(CreateRaceRequest req, Guid createdBy, CancellationToken ct);
    Task<Result<RaceDto>> UpdateAsync(Guid raceId, UpdateRaceRequest req, CancellationToken ct);
    Task<Result<RaceDto>> StartAsync(Guid raceId, DateTime actualReleaseTime, CancellationToken ct);
    Task<Result<RaceDto>> CompleteAsync(Guid raceId, CancellationToken ct);
    Task<Result<RaceDto>> PublishAsync(Guid raceId, CancellationToken ct);
    Task<Result> DeleteAsync(Guid raceId, CancellationToken ct);
    Task<Result<RaceDto>> GetAsync(Guid raceId, CancellationToken ct);
    Task<Result<PagedResult<RaceSummaryDto>>> GetByClubAsync(Guid clubId, PagedQuery paged, CancellationToken ct);
    Task<Result<List<RaceSummaryDto>>> GetLiveAsync(Guid clubId, CancellationToken ct);
    // Cross-service helpers
    Task<bool> ExistsAsync(Guid raceId, Guid clubId, CancellationToken ct);
    Task<RaceSnapshotDto?> GetSnapshotAsync(Guid raceId, CancellationToken ct);
    Task<int> GetResultCountAsync(Guid raceId, CancellationToken ct);
}

public class RaceService : IRaceService
{
    private readonly RaceDbContext _db;
    private readonly IPublishEndpoint _bus;

    public RaceService(RaceDbContext db, IPublishEndpoint bus)
    {
        _db  = db;
        _bus = bus;
    }

    public async Task<Result<RaceDto>> CreateAsync(CreateRaceRequest req, Guid createdBy, CancellationToken ct)
    {
        var race = new Race
        {
            ClubId = req.ClubId, FederationId = req.FederationId,
            Name = req.Name, Description = req.Description,
            ReleaseLocation = req.ReleaseLocation,
            ReleaseLongitude = req.ReleaseLongitude, ReleaseLatitude = req.ReleaseLatitude,
            ScheduledReleaseTime = req.ScheduledReleaseTime,
            WindSpeedKmh = req.WindSpeedKmh, WindDirection = req.WindDirection,
            TemperatureCelsius = req.TemperatureCelsius,
            Status = RaceStatus.Draft, CreatedBy = createdBy
        };

        foreach (var cat in req.Categories)
        {
            race.Categories.Add(new RaceCategory
            {
                Name = cat.Name, Description = cat.Description, SortOrder = cat.SortOrder
            });
        }

        _db.Races.Add(race);
        await _db.SaveChangesAsync(ct);
        return Result.Success(await BuildDtoAsync(race.Id, ct));
    }

    public async Task<Result<RaceDto>> UpdateAsync(Guid raceId, UpdateRaceRequest req, CancellationToken ct)
    {
        var race = await _db.Races.Include(r => r.Categories)
            .FirstOrDefaultAsync(r => r.Id == raceId, ct);
        if (race == null) return Result.NotFound<RaceDto>("Race");
        if (race.Status == RaceStatus.Published)
            return Result.Failure<RaceDto>("Cannot edit a published race.", "RACE_PUBLISHED");

        race.Name = req.Name; race.Description = req.Description;
        race.ReleaseLocation = req.ReleaseLocation;
        race.ReleaseLongitude = req.ReleaseLongitude; race.ReleaseLatitude = req.ReleaseLatitude;
        race.ScheduledReleaseTime = req.ScheduledReleaseTime;
        race.WindSpeedKmh = req.WindSpeedKmh; race.WindDirection = req.WindDirection;
        race.TemperatureCelsius = req.TemperatureCelsius;
        race.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result.Success(race.ToDto());
    }

    public async Task<Result<RaceDto>> StartAsync(Guid raceId, DateTime actualReleaseTime, CancellationToken ct)
    {
        var race = await _db.Races.Include(r => r.Categories)
            .FirstOrDefaultAsync(r => r.Id == raceId, ct);
        if (race == null) return Result.NotFound<RaceDto>("Race");
        if (race.Status != RaceStatus.Draft && race.Status != RaceStatus.Scheduled)
            return Result.Failure<RaceDto>($"Cannot start a race in status {race.Status}.", "INVALID_STATUS");

        race.Status = RaceStatus.InProgress;
        race.ActualReleaseTime = actualReleaseTime;
        race.IsLiveTracking = true;
        race.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result.Success(race.ToDto());
    }

    public async Task<Result<RaceDto>> CompleteAsync(Guid raceId, CancellationToken ct)
    {
        var race = await _db.Races.Include(r => r.Categories)
            .FirstOrDefaultAsync(r => r.Id == raceId, ct);
        if (race == null) return Result.NotFound<RaceDto>("Race");
        if (race.Status != RaceStatus.InProgress)
            return Result.Failure<RaceDto>("Race is not in progress.", "INVALID_STATUS");

        race.Status = RaceStatus.Completed;
        race.IsLiveTracking = false;
        race.CompletedAt = DateTime.UtcNow;
        race.UpdatedAt = DateTime.UtcNow;
        race.TotalPigeonsEntered = await _db.RaceResults.CountAsync(r => r.RaceId == raceId, ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success(race.ToDto());
    }

    public async Task<Result<RaceDto>> PublishAsync(Guid raceId, CancellationToken ct)
    {
        var race = await _db.Races.Include(r => r.Categories)
            .FirstOrDefaultAsync(r => r.Id == raceId, ct);
        if (race == null) return Result.NotFound<RaceDto>("Race");
        if (race.Status != RaceStatus.Completed)
            return Result.Failure<RaceDto>("Only completed races can be published.", "INVALID_STATUS");

        race.Status     = RaceStatus.Published;
        race.PublishedAt = DateTime.UtcNow;
        race.UpdatedAt  = DateTime.UtcNow;

        var resultEntities = await _db.RaceResults
            .Where(r => r.RaceId == raceId && r.Status == ResultStatus.Validated && !r.IsDeleted)
            .ToListAsync(ct);

        foreach (var r in resultEntities) r.Status = ResultStatus.Published;

        await _db.SaveChangesAsync(ct);

        // Notify all subscribers (FederationService caches these for country result aggregation)
        var resultItems = resultEntities.Select(r => new RaceResultItem(
            r.Id, r.RingNumber, r.UserId, null,
            r.SpeedMperMin, r.DistanceKm, r.ArrivalTime)).ToList();

        await _bus.Publish(new RaceResultsPublished(
            race.Id, race.ClubId, race.ClubName ?? string.Empty,
            race.Name, RaceStatus.Published, resultItems, DateTime.UtcNow,
            race.FederationId), ct);

        return Result.Success(race.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid raceId, CancellationToken ct)
    {
        var race = await _db.Races.FirstOrDefaultAsync(r => r.Id == raceId, ct);
        if (race == null) return Result.NotFound("Race");
        if (race.Status == RaceStatus.Published)
            return Result.Failure("Cannot delete a published race.", "RACE_PUBLISHED");

        race.IsDeleted = true;
        race.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<RaceDto>> GetAsync(Guid raceId, CancellationToken ct)
    {
        var race = await _db.Races.Include(r => r.Categories)
            .FirstOrDefaultAsync(r => r.Id == raceId, ct);
        return race == null ? Result.NotFound<RaceDto>("Race") : Result.Success(race.ToDto());
    }

    public async Task<Result<PagedResult<RaceSummaryDto>>> GetByClubAsync(Guid clubId, PagedQuery paged, CancellationToken ct)
    {
        var q = _db.Races.Where(r => r.ClubId == clubId);
        if (!string.IsNullOrEmpty(paged.Search))
            q = q.Where(r => r.Name.Contains(paged.Search));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(r => r.ScheduledReleaseTime)
            .Skip(paged.Skip).Take(paged.PageSize)
            .Select(r => r.ToSummaryDto())
            .ToListAsync(ct);

        return Result.Success(new PagedResult<RaceSummaryDto>
        {
            Items = items, TotalCount = total, Page = paged.Page, PageSize = paged.PageSize
        });
    }

    public async Task<Result<List<RaceSummaryDto>>> GetLiveAsync(Guid clubId, CancellationToken ct)
    {
        var races = await _db.Races
            .Where(r => r.ClubId == clubId && r.Status == RaceStatus.InProgress)
            .Select(r => r.ToSummaryDto())
            .ToListAsync(ct);
        return Result.Success(races);
    }

    public async Task<bool> ExistsAsync(Guid raceId, Guid clubId, CancellationToken ct)
        => await _db.Races.AnyAsync(r => r.Id == raceId && r.ClubId == clubId, ct);

    public async Task<RaceSnapshotDto?> GetSnapshotAsync(Guid raceId, CancellationToken ct)
    {
        var race = await _db.Races.FirstOrDefaultAsync(r => r.Id == raceId, ct);
        return race == null ? null : new RaceSnapshotDto(race.Id, race.Name, race.ActualReleaseTime);
    }

    public async Task<int> GetResultCountAsync(Guid raceId, CancellationToken ct)
        => await _db.RaceResults.CountAsync(r => r.RaceId == raceId, ct);

    private async Task<RaceDto> BuildDtoAsync(Guid id, CancellationToken ct)
    {
        var race = await _db.Races.Include(r => r.Categories).FirstAsync(r => r.Id == id, ct);
        return race.ToDto();
    }
}

public static class RaceMappingExtensions
{
    public static RaceDto ToDto(this Race r) => new(
        r.Id, r.ClubId, r.ClubName, r.Name, r.Description,
        r.Status, r.ReleaseLocation, r.ReleaseLongitude, r.ReleaseLatitude,
        r.ScheduledReleaseTime, r.ActualReleaseTime, r.WindSpeedKmh,
        r.WindDirection?.ToString(), r.TemperatureCelsius,
        r.TotalPigeonsEntered, r.IsLiveTracking, r.PublishedAt, r.CreatedAt,
        r.Categories.Select(c => new RaceCategoryDto(c.Id, c.Name, c.Description, c.SortOrder)).ToList());

    public static RaceSummaryDto ToSummaryDto(this Race r) => new(
        r.Id, r.Name, r.Status, r.ScheduledReleaseTime, r.ActualReleaseTime,
        r.TotalPigeonsEntered, r.ClubName, r.ClubId);
}
