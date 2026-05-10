using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.Common.Messages;
using PRC.ClubService.Data;

namespace PRC.ClubService.Events;

public class GetClubBrandingConsumer : IConsumer<GetClubBrandingRequest>
{
    private readonly ClubDbContext _db;
    public GetClubBrandingConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetClubBrandingRequest> ctx)
    {
        var club = await _db.Clubs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == ctx.Message.ClubId);

        if (club == null)
        {
            await ctx.RespondAsync(new ClubBrandingResult(false, "", null, "#1E3A5F", "#C9A84C"));
            return;
        }

        await ctx.RespondAsync(new ClubBrandingResult(
            true, club.Name, club.LogoUrl,
            club.PrimaryColor ?? "#1E3A5F",
            club.SecondaryColor ?? "#C9A84C"));
    }
}

public class GetProgrammeForRenderConsumer : IConsumer<GetProgrammeForRenderRequest>
{
    private readonly ClubDbContext _db;
    public GetProgrammeForRenderConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetProgrammeForRenderRequest> ctx)
    {
        var prog = await _db.ClubProgrammes
            .Include(p => p.Club)
            .Include(p => p.ProgrammeRaces)
            .Include(p => p.BestLoftResults)
            .Include(p => p.AcePigeonResults)
            .Include(p => p.SuperAcePigeonResults)
            .FirstOrDefaultAsync(p => p.Id == ctx.Message.ProgrammeId);

        if (prog == null)
        {
            await ctx.RespondAsync(new ProgrammeForRenderResult(
                false, Guid.Empty, "", 0, "", 0, "", 0,
                Guid.Empty, "", null, "#1E3A5F", "#C9A84C",
                Array.Empty<BestLoftRenderItem>(),
                Array.Empty<AcePigeonRenderItem>(),
                Array.Empty<SuperAceRenderItem>()));
            return;
        }

        var bestLoft = prog.BestLoftResults
            .OrderBy(r => r.LoftRank)
            .Select(r => new BestLoftRenderItem(
                r.LoftRank, r.FancierName, r.RacesEntered, r.PigeonsEntered,
                r.BestSingleSpeedMperMin, r.AverageSpeedMperMin, r.TotalScore, r.AverageScore))
            .ToList();

        var acePigeon = prog.AcePigeonResults
            .OrderBy(r => r.AceRank)
            .Select(r => new AcePigeonRenderItem(
                r.AceRank, r.RingNumber, r.PigeonName, r.PigeonSex, r.PigeonYearOfBirth,
                r.FancierName, r.RacesEntered, r.RacesInProgramme, r.ParticipationRate,
                r.BestSpeedMperMin, r.AverageSpeedMperMin, r.TotalScore, r.AverageScore, r.BestClubRank))
            .ToList();

        var superAce = prog.SuperAcePigeonResults
            .OrderBy(r => r.SuperAceRank)
            .Select(r => new SuperAceRenderItem(
                r.SuperAceRank, r.RingNumber, r.PigeonName, r.PigeonSex, r.PigeonYearOfBirth,
                r.FancierName, r.RacesEntered, r.RacesInProgramme, r.ParticipationRate,
                r.BestSpeedMperMin, r.AverageSpeedMperMin, r.TotalScore, r.AverageScore, r.BestClubRank))
            .ToList();

        await ctx.RespondAsync(new ProgrammeForRenderResult(
            true, prog.Id, prog.Name, prog.Year, prog.ScoringMethod.ToString(),
            prog.AcePigeonMinRaces, prog.SuperAceQualification.ToString(),
            prog.ProgrammeRaces.Count(r => !r.IsDeleted),
            prog.ClubId ?? Guid.Empty,
            prog.Club?.Name ?? prog.FederationName ?? "Federation",
            prog.Club?.LogoUrl,
            prog.Club?.PrimaryColor ?? "#1E3A5F",
            prog.Club?.SecondaryColor ?? "#C9A84C",
            bestLoft, acePigeon, superAce));
    }
}
