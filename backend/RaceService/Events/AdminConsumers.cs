using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.Common.Messages;
using PRC.RaceService.Data;

namespace PRC.RaceService.Events;

public class GetRaceStatsConsumer : IConsumer<GetRaceStatsRequest>
{
    private readonly RaceDbContext _db;
    public GetRaceStatsConsumer(RaceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetRaceStatsRequest> ctx)
    {
        var now        = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var total     = await _db.Races.CountAsync(r => !r.IsDeleted);
        var published = await _db.Races.CountAsync(r => r.Status == RaceStatus.Published);
        var thisMonth = await _db.Races.CountAsync(r => !r.IsDeleted && r.CreatedAt >= monthStart);
        var results   = await _db.RaceResults.CountAsync(r => !r.IsDeleted);

        await ctx.RespondAsync(new RaceStatsResult(total, published, thisMonth, results));
    }
}
