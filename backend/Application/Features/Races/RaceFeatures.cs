using MediatR;
using Microsoft.EntityFrameworkCore;
using PigeonRacing.Application.Common;
using PigeonRacing.Application.Common.Interfaces;
using PigeonRacing.Domain.Entities;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.Application.Features.Races;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record RaceDto(
    Guid Id,
    Guid ClubId,
    string ClubName,
    string Name,
    string? Description,
    RaceStatus Status,
    string ReleaseLocation,
    double ReleaseLongitude,
    double ReleaseLatitude,
    DateTime? ScheduledReleaseTime,
    DateTime? ActualReleaseTime,
    double? WindSpeedKmh,
    string? WindDirection,
    double? TemperatureCelsius,
    int? TotalPigeonsEntered,
    bool IsLiveTracking,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    List<RaceCategoryDto> Categories);

public record RaceCategoryDto(Guid Id, string Name, string? Description, int SortOrder);

public record RaceSummaryDto(
    Guid Id, string Name, RaceStatus Status,
    DateTime? ScheduledReleaseTime, DateTime? ActualReleaseTime,
    int? TotalPigeonsEntered, string ClubName, Guid ClubId);

// ── Commands ──────────────────────────────────────────────────────────────────

public record CreateRaceCommand(
    Guid ClubId,
    string Name,
    string? Description,
    string ReleaseLocation,
    double ReleaseLongitude,
    double ReleaseLatitude,
    DateTime? ScheduledReleaseTime,
    double? WindSpeedKmh,
    WindDirection? WindDirection,
    double? TemperatureCelsius,
    List<CreateRaceCategoryDto> Categories) : IRequest<Result<RaceDto>>;

public record CreateRaceCategoryDto(string Name, string? Description, int SortOrder);

public record UpdateRaceCommand(
    Guid RaceId,
    string Name,
    string? Description,
    string ReleaseLocation,
    double ReleaseLongitude,
    double ReleaseLatitude,
    DateTime? ScheduledReleaseTime,
    double? WindSpeedKmh,
    WindDirection? WindDirection,
    double? TemperatureCelsius) : IRequest<Result<RaceDto>>;

public record StartRaceCommand(Guid RaceId, DateTime ActualReleaseTime) : IRequest<Result<RaceDto>>;
public record CompleteRaceCommand(Guid RaceId) : IRequest<Result<RaceDto>>;
public record PublishRaceCommand(Guid RaceId) : IRequest<Result<RaceDto>>;
public record DeleteRaceCommand(Guid RaceId) : IRequest<Result>;

// ── Queries ───────────────────────────────────────────────────────────────────

public record GetRaceQuery(Guid RaceId) : IRequest<Result<RaceDto>>;
public record GetClubRacesQuery(Guid ClubId, PagedQuery Paged) : IRequest<Result<PagedResult<RaceSummaryDto>>>;
public record GetLiveRacesQuery(Guid ClubId) : IRequest<Result<List<RaceSummaryDto>>>;

// ── Handlers ──────────────────────────────────────────────────────────────────

public class CreateRaceHandler : IRequestHandler<CreateRaceCommand, Result<RaceDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateRaceHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<RaceDto>> Handle(CreateRaceCommand cmd, CancellationToken ct)
    {
        var club = await _db.Clubs.FirstOrDefaultAsync(c => c.Id == cmd.ClubId && !c.IsDeleted, ct);
        if (club == null) return Result.NotFound<RaceDto>("Club");

        var race = new Race
        {
            ClubId = cmd.ClubId,
            Name = cmd.Name,
            Description = cmd.Description,
            ReleaseLocation = cmd.ReleaseLocation,
            ReleaseLongitude = cmd.ReleaseLongitude,
            ReleaseLatitude = cmd.ReleaseLatitude,
            ScheduledReleaseTime = cmd.ScheduledReleaseTime,
            WindSpeedKmh = cmd.WindSpeedKmh,
            WindDirection = cmd.WindDirection,
            TemperatureCelsius = cmd.TemperatureCelsius,
            Status = RaceStatus.Draft,
            CreatedBy = _currentUser.UserId
        };

        foreach (var cat in cmd.Categories)
        {
            race.Categories.Add(new RaceCategory
            {
                Name = cat.Name,
                Description = cat.Description,
                SortOrder = cat.SortOrder
            });
        }

        _db.Races.Add(race);
        await _db.SaveChangesAsync(ct);

        return Result.Success(await GetRaceDtoAsync(race.Id, ct));
    }

    private async Task<RaceDto> GetRaceDtoAsync(Guid raceId, CancellationToken ct)
    {
        var race = await _db.Races
            .Include(r => r.Club)
            .Include(r => r.Categories)
            .FirstAsync(r => r.Id == raceId, ct);
        return race.ToDto();
    }
}

public class UpdateRaceHandler : IRequestHandler<UpdateRaceCommand, Result<RaceDto>>
{
    private readonly IAppDbContext _db;

    public UpdateRaceHandler(IAppDbContext db) => _db = db;

    public async Task<Result<RaceDto>> Handle(UpdateRaceCommand cmd, CancellationToken ct)
    {
        var race = await _db.Races.Include(r => r.Club).Include(r => r.Categories)
            .FirstOrDefaultAsync(r => r.Id == cmd.RaceId, ct);

        if (race == null) return Result.NotFound<RaceDto>("Race");
        if (race.Status == RaceStatus.Published)
            return Result.Failure<RaceDto>("Cannot edit a published race.", "RACE_PUBLISHED");

        race.Name = cmd.Name;
        race.Description = cmd.Description;
        race.ReleaseLocation = cmd.ReleaseLocation;
        race.ReleaseLongitude = cmd.ReleaseLongitude;
        race.ReleaseLatitude = cmd.ReleaseLatitude;
        race.ScheduledReleaseTime = cmd.ScheduledReleaseTime;
        race.WindSpeedKmh = cmd.WindSpeedKmh;
        race.WindDirection = cmd.WindDirection;
        race.TemperatureCelsius = cmd.TemperatureCelsius;
        race.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result.Success(race.ToDto());
    }
}

public class StartRaceHandler : IRequestHandler<StartRaceCommand, Result<RaceDto>>
{
    private readonly IAppDbContext _db;

    public StartRaceHandler(IAppDbContext db) => _db = db;

    public async Task<Result<RaceDto>> Handle(StartRaceCommand cmd, CancellationToken ct)
    {
        var race = await _db.Races.Include(r => r.Club).Include(r => r.Categories)
            .FirstOrDefaultAsync(r => r.Id == cmd.RaceId, ct);

        if (race == null) return Result.NotFound<RaceDto>("Race");
        if (race.Status != RaceStatus.Scheduled && race.Status != RaceStatus.Draft)
            return Result.Failure<RaceDto>($"Cannot start a race in status {race.Status}.", "INVALID_STATUS");

        race.Status = RaceStatus.InProgress;
        race.ActualReleaseTime = cmd.ActualReleaseTime;
        race.IsLiveTracking = true;
        race.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result.Success(race.ToDto());
    }
}

public class CompleteRaceHandler : IRequestHandler<CompleteRaceCommand, Result<RaceDto>>
{
    private readonly IAppDbContext _db;

    public CompleteRaceHandler(IAppDbContext db) => _db = db;

    public async Task<Result<RaceDto>> Handle(CompleteRaceCommand cmd, CancellationToken ct)
    {
        var race = await _db.Races.Include(r => r.Club).Include(r => r.Categories)
            .FirstOrDefaultAsync(r => r.Id == cmd.RaceId, ct);

        if (race == null) return Result.NotFound<RaceDto>("Race");
        if (race.Status != RaceStatus.InProgress)
            return Result.Failure<RaceDto>("Race is not in progress.", "INVALID_STATUS");

        race.Status = RaceStatus.Completed;
        race.IsLiveTracking = false;
        race.CompletedAt = DateTime.UtcNow;
        race.UpdatedAt = DateTime.UtcNow;

        var resultCount = await _db.RaceResults.CountAsync(r => r.RaceId == cmd.RaceId, ct);
        race.TotalPigeonsEntered = resultCount;

        await _db.SaveChangesAsync(ct);
        return Result.Success(race.ToDto());
    }
}

public class PublishRaceHandler : IRequestHandler<PublishRaceCommand, Result<RaceDto>>
{
    private readonly IAppDbContext _db;
    private readonly INotificationService _notifications;

    public PublishRaceHandler(IAppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<Result<RaceDto>> Handle(PublishRaceCommand cmd, CancellationToken ct)
    {
        var race = await _db.Races.Include(r => r.Club).Include(r => r.Categories)
            .FirstOrDefaultAsync(r => r.Id == cmd.RaceId, ct);

        if (race == null) return Result.NotFound<RaceDto>("Race");
        if (race.Status != RaceStatus.Completed)
            return Result.Failure<RaceDto>("Only completed races can be published.", "INVALID_STATUS");

        race.Status = RaceStatus.Published;
        race.PublishedAt = DateTime.UtcNow;
        race.UpdatedAt = DateTime.UtcNow;

        // Mark all validated results as published
        var results = _db.RaceResults.Where(r => r.RaceId == cmd.RaceId && r.Status == ResultStatus.Validated);
        await results.ForEachAsync(r => r.Status = ResultStatus.Published, ct);

        await _db.SaveChangesAsync(ct);

        // Notify club members
        var memberUserIds = await _db.ClubMemberships
            .Where(m => m.ClubId == race.ClubId && m.IsActive)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        foreach (var userId in memberUserIds)
        {
            await _notifications.SendInAppAsync(userId,
                $"Results Published: {race.Name}",
                $"Final results for {race.Name} are now available.", ct: ct);
        }

        return Result.Success(race.ToDto());
    }
}

public class DeleteRaceHandler : IRequestHandler<DeleteRaceCommand, Result>
{
    private readonly IAppDbContext _db;

    public DeleteRaceHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(DeleteRaceCommand cmd, CancellationToken ct)
    {
        var race = await _db.Races.FirstOrDefaultAsync(r => r.Id == cmd.RaceId, ct);
        if (race == null) return Result.NotFound("Race");
        if (race.Status == RaceStatus.Published)
            return Result.Failure("Cannot delete a published race.", "RACE_PUBLISHED");

        race.IsDeleted = true;
        race.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class GetRaceHandler : IRequestHandler<GetRaceQuery, Result<RaceDto>>
{
    private readonly IAppDbContext _db;

    public GetRaceHandler(IAppDbContext db) => _db = db;

    public async Task<Result<RaceDto>> Handle(GetRaceQuery query, CancellationToken ct)
    {
        var race = await _db.Races
            .Include(r => r.Club)
            .Include(r => r.Categories)
            .FirstOrDefaultAsync(r => r.Id == query.RaceId, ct);

        return race == null ? Result.NotFound<RaceDto>("Race") : Result.Success(race.ToDto());
    }
}

public class GetClubRacesHandler : IRequestHandler<GetClubRacesQuery, Result<PagedResult<RaceSummaryDto>>>
{
    private readonly IAppDbContext _db;

    public GetClubRacesHandler(IAppDbContext db) => _db = db;

    public async Task<Result<PagedResult<RaceSummaryDto>>> Handle(GetClubRacesQuery query, CancellationToken ct)
    {
        var q = _db.Races.Include(r => r.Club)
            .Where(r => r.ClubId == query.ClubId);

        if (!string.IsNullOrEmpty(query.Paged.Search))
            q = q.Where(r => r.Name.Contains(query.Paged.Search));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(r => r.ScheduledReleaseTime)
            .Skip(query.Paged.Skip)
            .Take(query.Paged.PageSize)
            .Select(r => r.ToSummaryDto())
            .ToListAsync(ct);

        return Result.Success(new PagedResult<RaceSummaryDto>
        {
            Items = items,
            TotalCount = total,
            Page = query.Paged.Page,
            PageSize = query.Paged.PageSize
        });
    }
}

public class GetLiveRacesHandler : IRequestHandler<GetLiveRacesQuery, Result<List<RaceSummaryDto>>>
{
    private readonly IAppDbContext _db;

    public GetLiveRacesHandler(IAppDbContext db) => _db = db;

    public async Task<Result<List<RaceSummaryDto>>> Handle(GetLiveRacesQuery query, CancellationToken ct)
    {
        var races = await _db.Races
            .Include(r => r.Club)
            .Where(r => r.ClubId == query.ClubId && r.Status == RaceStatus.InProgress)
            .Select(r => r.ToSummaryDto())
            .ToListAsync(ct);

        return Result.Success(races);
    }
}

// ── Mapping extensions ────────────────────────────────────────────────────────

public static class RaceMappingExtensions
{
    public static RaceDto ToDto(this Race r) => new(
        r.Id, r.ClubId, r.Club?.Name ?? string.Empty, r.Name, r.Description,
        r.Status, r.ReleaseLocation, r.ReleaseLongitude, r.ReleaseLatitude,
        r.ScheduledReleaseTime, r.ActualReleaseTime, r.WindSpeedKmh,
        r.WindDirection?.ToString(), r.TemperatureCelsius,
        r.TotalPigeonsEntered, r.IsLiveTracking, r.PublishedAt, r.CreatedAt,
        r.Categories.Select(c => new RaceCategoryDto(c.Id, c.Name, c.Description, c.SortOrder)).ToList());

    public static RaceSummaryDto ToSummaryDto(this Race r) => new(
        r.Id, r.Name, r.Status, r.ScheduledReleaseTime, r.ActualReleaseTime,
        r.TotalPigeonsEntered, r.Club?.Name ?? string.Empty, r.ClubId);
}
