using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.Common.Messages;
using PRC.ClubService.Data;

namespace PRC.ClubService.Events;

public class GetFancierProgrammeResultsConsumer : IConsumer<GetFancierProgrammeResultsRequest>
{
    private readonly ClubDbContext _db;
    public GetFancierProgrammeResultsConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetFancierProgrammeResultsRequest> ctx)
    {
        var msg = ctx.Message;

        var programmeIds = await _db.ClubProgrammes
            .Where(p => p.ClubId == msg.ClubId &&
                        p.Status == ProgrammeStatus.Published &&
                        !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(ctx.CancellationToken);

        var aceResults = await _db.AcePigeonResults
            .Include(a => a.Programme)
            .Where(a => programmeIds.Contains(a.ProgrammeId) && a.UserId == msg.UserId)
            .OrderBy(a => a.Programme.Year).ThenBy(a => a.AceRank)
            .ToListAsync(ctx.CancellationToken);

        var superAceResults = await _db.SuperAcePigeonResults
            .Include(a => a.Programme)
            .Where(a => programmeIds.Contains(a.ProgrammeId) && a.UserId == msg.UserId)
            .OrderBy(a => a.Programme.Year).ThenBy(a => a.SuperAceRank)
            .ToListAsync(ctx.CancellationToken);

        var bestLoftResults = await _db.BestLoftResults
            .Include(b => b.Programme)
            .Where(b => programmeIds.Contains(b.ProgrammeId) && b.UserId == msg.UserId)
            .OrderBy(b => b.Programme.Year).ThenBy(b => b.LoftRank)
            .ToListAsync(ctx.CancellationToken);

        var ace = aceResults.Select(a => new FancierAcePigeonItem(
            a.RingNumber, a.PigeonName, a.PigeonSex, a.PigeonYearOfBirth,
            a.Programme.Name, a.Programme.Year, a.AceRank,
            a.TotalScore, a.AverageScore, a.RacesEntered, a.RacesInProgramme,
            a.ParticipationRate, a.BestSpeedMperMin, a.AverageSpeedMperMin, a.BestClubRank
        )).ToList();

        var superAce = superAceResults.Select(a => new FancierSuperAceItem(
            a.RingNumber, a.PigeonName, a.PigeonSex, a.PigeonYearOfBirth,
            a.Programme.Name, a.Programme.Year, a.SuperAceRank,
            a.TotalScore, a.AverageScore, a.RacesEntered, a.RacesInProgramme,
            a.ParticipationRate, a.BestSpeedMperMin, a.AverageSpeedMperMin, a.BestClubRank
        )).ToList();

        var bestLoft = bestLoftResults.Select(b => new FancierBestLoftItem(
            b.Programme.Name, b.Programme.Year, b.LoftRank,
            b.TotalScore, b.AverageScore, b.RacesEntered, b.PigeonsEntered,
            b.BestSingleSpeedMperMin, b.AverageSpeedMperMin
        )).ToList();

        await ctx.RespondAsync(new FancierProgrammeResultsResponse(ace, superAce, bestLoft));
    }
}
