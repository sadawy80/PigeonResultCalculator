using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.Common.Messages;
using PRC.RaceService.Data;

namespace PRC.RaceService.Events;

public class GetRaceForRenderConsumer : IConsumer<GetRaceForRenderRequest>
{
    private readonly RaceDbContext _db;
    public GetRaceForRenderConsumer(RaceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetRaceForRenderRequest> ctx)
    {
        var race = await _db.Races
            .Include(r => r.Results).ThenInclude(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == ctx.Message.RaceId);

        if (race == null)
        {
            await ctx.RespondAsync(new RaceForRenderResult(
                false, Guid.Empty, "", Guid.Empty, null, null, null, 0, null, null,
                Array.Empty<RaceResultRenderItem>()));
            return;
        }

        var results = race.Results
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.ClubRank)
            .Select(r => new RaceResultRenderItem(
                r.Id, r.ClubRank, r.CategoryRank,
                r.RingNumber, r.PigeonName, r.PigeonSex, r.PigeonYearOfBirth,
                r.UserId, r.ArrivalTime, r.DistanceKm, r.SpeedMperMin,
                r.Category?.Name ?? "Open"))
            .ToList();

        var wind = race.WindSpeedKmh.HasValue && race.WindDirection.HasValue
            ? $"{race.WindDirection} {race.WindSpeedKmh:F0} km/h"
            : null;
        var temp = race.TemperatureCelsius.HasValue
            ? $"{race.TemperatureCelsius:F0}°C"
            : null;

        await ctx.RespondAsync(new RaceForRenderResult(
            true, race.Id, race.Name, race.ClubId,
            race.ReleaseLocation, race.ActualReleaseTime,
            race.NominatedDistanceKm, race.TotalPigeonsEntered ?? results.Count,
            wind, temp, results));
    }
}

public class GetRaceResultForRenderConsumer : IConsumer<GetRaceResultForRenderRequest>
{
    private readonly RaceDbContext _db;
    public GetRaceResultForRenderConsumer(RaceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetRaceResultForRenderRequest> ctx)
    {
        var r = await _db.RaceResults
            .Include(x => x.Race)
            .FirstOrDefaultAsync(x => x.Id == ctx.Message.RaceResultId);

        if (r == null)
        {
            await ctx.RespondAsync(new RaceResultForRenderResult(
                false, Guid.Empty, null, "", null, null, 0, 0,
                default, null, Guid.Empty, "", null, null, Guid.Empty));
            return;
        }

        await ctx.RespondAsync(new RaceResultForRenderResult(
            true, r.Id, r.ClubRank, r.RingNumber, r.PigeonName, r.PigeonSex,
            r.SpeedMperMin, r.DistanceKm, r.ArrivalTime, r.UserId,
            r.RaceId, r.Race.Name, r.Race.ActualReleaseTime,
            r.Race.ReleaseLocation, r.Race.ClubId));
    }
}
