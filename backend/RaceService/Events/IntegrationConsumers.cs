using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.Common.Messages;
using PRC.RaceService.Data;

namespace PRC.RaceService.Events;

public class GetFancierRaceResultsConsumer : IConsumer<GetFancierRaceResultsRequest>
{
    private readonly RaceDbContext _db;
    public GetFancierRaceResultsConsumer(RaceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetFancierRaceResultsRequest> ctx)
    {
        var msg = ctx.Message;

        var q = _db.RaceResults
            .Include(r => r.Race)
            .Include(r => r.Category)
            .Where(r =>
                r.UserId == msg.UserId &&
                r.Race.ClubId == msg.ClubId &&
                r.Status == ResultStatus.Published &&
                !r.IsDeleted)
            .OrderByDescending(r => r.Race.ActualReleaseTime);

        var total = await q.CountAsync(ctx.CancellationToken);
        var items = await q
            .Skip((msg.Page - 1) * msg.PageSize)
            .Take(msg.PageSize)
            .ToListAsync(ctx.CancellationToken);

        var mapped = items.Select(r => new FancierRaceResultItem(
            r.RingNumber,
            r.PigeonName,
            r.PigeonSex,
            r.PigeonYearOfBirth,
            r.Race.Name,
            r.Race.ReleaseLocation ?? "",
            r.Race.ActualReleaseTime ?? r.Race.ScheduledReleaseTime ?? r.CreatedAt,
            r.DistanceKm,
            r.SpeedMperMin,
            r.ClubRank,
            r.CategoryRank,
            r.Category?.Name
        )).ToList();

        await ctx.RespondAsync(new FancierRaceResultsResponse(true, total, mapped));
    }
}
