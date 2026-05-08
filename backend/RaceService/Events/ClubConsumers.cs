using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.Common.Messages;
using PRC.RaceService.Data;

namespace PRC.RaceService.Events;

public class GetRaceSnapshotConsumer : IConsumer<GetRaceSnapshotRequest>
{
    private readonly RaceDbContext _db;
    public GetRaceSnapshotConsumer(RaceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetRaceSnapshotRequest> ctx)
    {
        var race = await _db.Races.FirstOrDefaultAsync(r => r.Id == ctx.Message.RaceId);
        if (race == null)
        {
            await ctx.RespondAsync(new RaceSnapshotResult(false, ctx.Message.RaceId, Guid.Empty, "", null, 0));
            return;
        }
        var count = await _db.RaceResults.CountAsync(r => r.RaceId == race.Id && r.Status == ResultStatus.Published);
        await ctx.RespondAsync(new RaceSnapshotResult(true, race.Id, race.ClubId, race.Name, race.ActualReleaseTime, count));
    }
}

public class GetPublishedResultsForProgrammeConsumer : IConsumer<GetPublishedResultsForProgrammeRequest>
{
    private readonly RaceDbContext _db;
    public GetPublishedResultsForProgrammeConsumer(RaceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetPublishedResultsForProgrammeRequest> ctx)
    {
        var raceIds = ctx.Message.RaceIds.ToList();
        var results = await _db.RaceResults
            .Include(r => r.Race)
            .Where(r => raceIds.Contains(r.RaceId) && r.Status == ResultStatus.Published && !r.IsDeleted)
            .ToListAsync(ctx.CancellationToken);

        var items = results.Select(r => new ProgrammeRaceResultItem(
            r.RaceId, r.Race.Name,
            r.Id, r.RingNumber,
            r.UserId, r.SpeedMperMin, r.DistanceKm,
            r.ArrivalTime, r.ClubRank ?? 0,
            r.PigeonId, r.PigeonName, r.PigeonSex, r.PigeonYearOfBirth
        )).ToList();

        await ctx.RespondAsync(new PublishedResultsForProgrammeResult(items));
    }
}

public class GetPigeonLookupConsumer : IConsumer<GetPigeonLookupRequest>
{
    private readonly RaceDbContext _db;
    public GetPigeonLookupConsumer(RaceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetPigeonLookupRequest> ctx)
    {
        var pigeon = await _db.Pigeons.FirstOrDefaultAsync(p => p.RingNumber == ctx.Message.RingNumber);
        await ctx.RespondAsync(new PigeonLookupResult(pigeon != null, pigeon?.Id, ctx.Message.RingNumber));
    }
}
