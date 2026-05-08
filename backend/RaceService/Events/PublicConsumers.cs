using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.Common.Messages;
using PRC.RaceService.Data;

namespace PRC.RaceService.Events;

public class GetPublishedRacesForPublicConsumer : IConsumer<GetPublishedRacesForPublicRequest>
{
    private readonly RaceDbContext _db;
    public GetPublishedRacesForPublicConsumer(RaceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetPublishedRacesForPublicRequest> ctx)
    {
        var m = ctx.Message;

        var published = await _db.Races
            .Include(r => r.Categories)
            .Where(r => r.ClubId == m.ClubId && r.Status == RaceStatus.Published && !r.IsDeleted)
            .OrderByDescending(r => r.PublishedAt)
            .Take(m.Take)
            .ToListAsync(ctx.CancellationToken);

        var raceIds = published.Select(r => r.Id).ToList();
        var results = await _db.RaceResults
            .Include(r => r.Category)
            .Where(r => raceIds.Contains(r.RaceId) && r.Status == ResultStatus.Published && !r.IsDeleted)
            .OrderBy(r => r.ClubRank)
            .ToListAsync(ctx.CancellationToken);

        var resultsByRace = results
            .GroupBy(r => r.RaceId)
            .ToDictionary(g => g.Key, g => g.Take(20).ToList());

        var publishedItems = published.Select(r =>
        {
            var top = resultsByRace.TryGetValue(r.Id, out var rs) ? rs : new();
            return new PublicRaceItem(
                r.Id, r.Name, r.Description, (int)r.Status,
                r.ReleaseLocation, r.ActualReleaseTime, r.ScheduledReleaseTime,
                r.PublishedAt, r.TotalPigeonsEntered ?? 0,
                r.WindSpeedKmh, r.WindDirection?.ToString(), r.TemperatureCelsius,
                r.Categories.OrderBy(c => c.SortOrder)
                    .Select(c => new PublicRaceCategoryItem(c.Id, c.Name, c.SortOrder))
                    .ToList(),
                top.Select(res => new PublicRaceResultItem(
                    res.Id, res.RingNumber, res.PigeonName,
                    res.UserId, res.SpeedMperMin, res.DistanceKm,
                    res.ClubRank ?? 0, res.CategoryRank,
                    res.Category?.Name, res.ArrivalTime))
                    .ToList());
        }).ToList();

        var liveRaces = await _db.Races
            .Where(r => r.ClubId == m.ClubId && r.Status == RaceStatus.InProgress && !r.IsDeleted)
            .Select(r => new PublicLiveRaceItem(r.Id, r.Name, r.TotalPigeonsEntered ?? 0))
            .ToListAsync(ctx.CancellationToken);

        await ctx.RespondAsync(new PublishedRacesForPublicResult(publishedItems, liveRaces));
    }
}
