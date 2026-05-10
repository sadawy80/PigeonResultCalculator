using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.ClubService.Data;
using PRC.ClubService.DTOs;
using PRC.ClubService.Models;

namespace PRC.ClubService.Services;

public interface IProgrammeService
{
    Task<Result<ProgrammeDto>> CreateAsync(CreateProgrammeRequest req, Guid createdBy, CancellationToken ct);
    Task<Result<ProgrammeDto>> UpdateAsync(Guid programmeId, UpdateProgrammeRequest req, CancellationToken ct);
    Task<Result<ProgrammeDto>> GetAsync(Guid programmeId, CancellationToken ct);
    Task<Result<PagedResult<ProgrammeSummaryDto>>> GetByClubAsync(Guid clubId, PagedQuery paged, CancellationToken ct);
    Task<Result> DeleteAsync(Guid programmeId, CancellationToken ct);
    Task<Result<ProgrammeDto>> AddRaceAsync(Guid programmeId, AddRaceToProgrammeRequest req, CancellationToken ct);
    Task<Result> RemoveRaceAsync(Guid programmeId, Guid raceId, CancellationToken ct);
    Task<Result<CalculationSummaryDto>> CalculateAsync(Guid programmeId, CancellationToken ct);
    Task<Result<ProgrammeDto>> PublishAsync(Guid programmeId, Guid publishedBy, CancellationToken ct);
    Task<Result<PagedResult<BestLoftResultDto>>> GetBestLoftAsync(Guid programmeId, PagedQuery paged, CancellationToken ct);
    Task<Result<PagedResult<AcePigeonResultDto>>> GetAcePigeonAsync(Guid programmeId, PagedQuery paged, CancellationToken ct);
    Task<Result<PagedResult<SuperAcePigeonResultDto>>> GetSuperAcePigeonAsync(Guid programmeId, PagedQuery paged, CancellationToken ct);
}

public record CalculationSummaryDto(
    int BestLoftEntriesCalculated,
    int AcePigeonEntriesCalculated,
    int SuperAcePigeonEntriesCalculated,
    int RacesIncluded,
    string ScoringMethod,
    string? Warnings);

public class ProgrammeService : IProgrammeService
{
    private readonly ClubDbContext _db;
    private readonly IRaceServiceClient _raceClient;

    public ProgrammeService(ClubDbContext db, IRaceServiceClient raceClient)
    {
        _db = db;
        _raceClient = raceClient;
    }

    public async Task<Result<ProgrammeDto>> CreateAsync(CreateProgrammeRequest req, Guid createdBy, CancellationToken ct)
    {
        if (req.ClubId == null && req.FederationId == null)
            return Result.Failure<ProgrammeDto>("Either ClubId or FederationId must be provided.", "INVALID_OWNER");

        if (req.ClubId.HasValue)
        {
            var club = await _db.Clubs.FirstOrDefaultAsync(c => c.Id == req.ClubId && !c.IsDeleted, ct);
            if (club == null) return Result.NotFound<ProgrammeDto>("Club");
        }

        var prog = new ClubProgramme
        {
            ClubId = req.ClubId, FederationId = req.FederationId, FederationName = req.FederationName,
            Name = req.Name, Description = req.Description,
            Year = req.Year, StartDate = req.StartDate, EndDate = req.EndDate,
            ScoringMethod = req.ScoringMethod, PointsForFirst = req.PointsForFirst,
            MaxPointPositions = req.MaxPointPositions,
            BestLoftPigeonsPerRace = req.BestLoftPigeonsPerRace,
            BestLoftMinRaces = req.BestLoftMinRaces,
            AcePigeonMinRaces = req.AcePigeonMinRaces,
            SuperAceQualification = req.SuperAceQualification,
            SuperAceMinRaceCount = req.SuperAceMinRaceCount,
            SuperAceMinRacePercentage = req.SuperAceMinRacePercentage,
            Status = ProgrammeStatus.Draft, CreatedBy = createdBy
        };

        _db.ClubProgrammes.Add(prog);
        await _db.SaveChangesAsync(ct);
        return Result.Success(await LoadDtoAsync(prog.Id, ct));
    }

    public async Task<Result<ProgrammeDto>> UpdateAsync(Guid programmeId, UpdateProgrammeRequest req, CancellationToken ct)
    {
        var prog = await LoadProgrammeWithRacesAsync(programmeId, ct);
        if (prog == null) return Result.NotFound<ProgrammeDto>("Programme");
        if (prog.Status == ProgrammeStatus.Published)
            return Result.Failure<ProgrammeDto>("Cannot edit a published programme.", "PROGRAMME_PUBLISHED");

        prog.Name = req.Name; prog.Description = req.Description;
        prog.StartDate = req.StartDate; prog.EndDate = req.EndDate;
        prog.ScoringMethod = req.ScoringMethod; prog.PointsForFirst = req.PointsForFirst;
        prog.MaxPointPositions = req.MaxPointPositions;
        prog.BestLoftPigeonsPerRace = req.BestLoftPigeonsPerRace;
        prog.BestLoftMinRaces = req.BestLoftMinRaces;
        prog.AcePigeonMinRaces = req.AcePigeonMinRaces;
        prog.SuperAceQualification = req.SuperAceQualification;
        prog.SuperAceMinRaceCount = req.SuperAceMinRaceCount;
        prog.SuperAceMinRacePercentage = req.SuperAceMinRacePercentage;
        prog.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result.Success(prog.ToDto());
    }

    public async Task<Result<ProgrammeDto>> GetAsync(Guid programmeId, CancellationToken ct)
    {
        var prog = await LoadProgrammeWithRacesAsync(programmeId, ct);
        return prog == null ? Result.NotFound<ProgrammeDto>("Programme") : Result.Success(prog.ToDto());
    }

    public async Task<Result<PagedResult<ProgrammeSummaryDto>>> GetByClubAsync(Guid clubId, PagedQuery paged, CancellationToken ct)
    {
        var club = await _db.Clubs.FirstOrDefaultAsync(c => c.Id == clubId, ct);
        var federationId = club?.FederationId;

        var q = _db.ClubProgrammes
            .Include(p => p.ProgrammeRaces)
            .Where(p => p.ClubId == clubId
                     || (federationId.HasValue && p.FederationId == federationId));

        if (!string.IsNullOrEmpty(paged.Search))
            q = q.Where(p => p.Name.Contains(paged.Search));
        if (paged.Year.HasValue)
            q = q.Where(p => p.Year == paged.Year.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(p => p.Year).ThenBy(p => p.Name)
            .Skip(paged.Skip).Take(paged.PageSize)
            .ToListAsync(ct);

        return Result.Success(new PagedResult<ProgrammeSummaryDto>
        {
            Items = items.Select(p => p.ToSummaryDto()).ToList(),
            TotalCount = total, Page = paged.Page, PageSize = paged.PageSize
        });
    }

    public async Task<Result> DeleteAsync(Guid programmeId, CancellationToken ct)
    {
        var prog = await _db.ClubProgrammes.FindAsync(new object[] { programmeId }, ct);
        if (prog == null) return Result.NotFound("Programme");
        prog.IsDeleted = true;
        prog.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<ProgrammeDto>> AddRaceAsync(Guid programmeId, AddRaceToProgrammeRequest req, CancellationToken ct)
    {
        var prog = await LoadProgrammeWithRacesAsync(programmeId, ct);
        if (prog == null) return Result.NotFound<ProgrammeDto>("Programme");

        if (prog.ProgrammeRaces.Any(r => r.RaceId == req.RaceId && !r.IsDeleted))
            return Result.Conflict<ProgrammeDto>("Race is already in this programme.");

        var raceName = await _raceClient.GetRaceNameAsync(req.RaceId, ct);
        if (raceName == null)
            return Result.NotFound<ProgrammeDto>("Race");

        var releaseTime = await _raceClient.GetRaceActualReleaseTimeAsync(req.RaceId, ct);
        var entryCount = await _raceClient.GetRaceResultCountAsync(req.RaceId, ct);

        prog.ProgrammeRaces.Add(new ProgrammeRace
        {
            RaceId = req.RaceId,
            RaceName = raceName,
            ActualReleaseTime = releaseTime,
            TotalEntries = entryCount,
            ScoreWeight = req.ScoreWeight,
            SortOrder = req.SortOrder
        });

        await _db.SaveChangesAsync(ct);
        return Result.Success(prog.ToDto());
    }

    public async Task<Result> RemoveRaceAsync(Guid programmeId, Guid raceId, CancellationToken ct)
    {
        var pr = await _db.ProgrammeRaces
            .FirstOrDefaultAsync(r => r.ProgrammeId == programmeId && r.RaceId == raceId, ct);
        if (pr == null) return Result.NotFound("ProgrammeRace");

        _db.ProgrammeRaces.Remove(pr);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<CalculationSummaryDto>> CalculateAsync(Guid programmeId, CancellationToken ct)
    {
        var programme = await _db.ClubProgrammes
            .Include(p => p.ProgrammeRaces.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == programmeId && !p.IsDeleted, ct);

        if (programme == null) return Result.NotFound<CalculationSummaryDto>("Programme");

        var programmeRaces = programme.ProgrammeRaces.OrderBy(r => r.SortOrder).ToList();

        if (programmeRaces.Count == 0)
            return Result.Failure<CalculationSummaryDto>("No races in this programme.", "NO_RACES");

        var raceIds = programmeRaces.Select(pr => pr.RaceId).ToList();
        var allResults = await _raceClient.GetPublishedResultsForProgrammeAsync(raceIds, ct);
        var warnings = new List<string>();

        // Clear old calculations
        _db.BestLoftResults.RemoveRange(_db.BestLoftResults.Where(x => x.ProgrammeId == programmeId));
        _db.AcePigeonResults.RemoveRange(_db.AcePigeonResults.Where(x => x.ProgrammeId == programmeId));
        _db.SuperAcePigeonResults.RemoveRange(_db.SuperAcePigeonResults.Where(x => x.ProgrammeId == programmeId));
        await _db.SaveChangesAsync(ct);

        var bestLoftEntries = CalculateBestLoft(programme, programmeRaces, allResults, warnings);
        await _db.BestLoftResults.AddRangeAsync(bestLoftEntries, ct);

        var acePigeonEntries = CalculateAcePigeon(programme, programmeRaces, allResults, warnings, programmeRaces.Count);
        await _db.AcePigeonResults.AddRangeAsync(acePigeonEntries, ct);

        var superAceEntries = CalculateSuperAce(programme, acePigeonEntries, programmeRaces.Count, warnings);
        await _db.SuperAcePigeonResults.AddRangeAsync(superAceEntries, ct);

        if (programme.Status == ProgrammeStatus.Draft)
        {
            programme.Status = ProgrammeStatus.Active;
            programme.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        return Result.Success(new CalculationSummaryDto(
            bestLoftEntries.Count, acePigeonEntries.Count, superAceEntries.Count,
            programmeRaces.Count, programme.ScoringMethod.ToString(),
            warnings.Count > 0 ? string.Join("; ", warnings) : null));
    }

    public async Task<Result<ProgrammeDto>> PublishAsync(Guid programmeId, Guid publishedBy, CancellationToken ct)
    {
        var prog = await LoadProgrammeWithRacesAsync(programmeId, ct);
        if (prog == null) return Result.NotFound<ProgrammeDto>("Programme");
        if (prog.Status == ProgrammeStatus.Published)
            return Result.Conflict<ProgrammeDto>("Programme already published.");

        var hasBestLoft = await _db.BestLoftResults.AnyAsync(r => r.ProgrammeId == programmeId, ct);
        if (!hasBestLoft)
            return Result.Failure<ProgrammeDto>("Run calculations before publishing.", "CALCULATION_REQUIRED");

        prog.Status = ProgrammeStatus.Published;
        prog.PublishedAt = DateTime.UtcNow;
        prog.PublishedByUserId = publishedBy;
        await _db.SaveChangesAsync(ct);
        return Result.Success(prog.ToDto());
    }

    public async Task<Result<PagedResult<BestLoftResultDto>>> GetBestLoftAsync(Guid programmeId, PagedQuery paged, CancellationToken ct)
    {
        var programme = await _db.ClubProgrammes.FirstOrDefaultAsync(p => p.Id == programmeId, ct);
        if (programme == null) return Result.NotFound<PagedResult<BestLoftResultDto>>("Programme");

        var q = _db.BestLoftResults.Where(r => r.ProgrammeId == programmeId);
        if (!string.IsNullOrEmpty(paged.Search))
            q = q.Where(r => r.FancierName.Contains(paged.Search));

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(r => r.LoftRank).Skip(paged.Skip).Take(paged.PageSize).ToListAsync(ct);

        return Result.Success(new PagedResult<BestLoftResultDto>
        {
            Items = items.Select(r => r.ToDto(programme.Name)).ToList(),
            TotalCount = total, Page = paged.Page, PageSize = paged.PageSize
        });
    }

    public async Task<Result<PagedResult<AcePigeonResultDto>>> GetAcePigeonAsync(Guid programmeId, PagedQuery paged, CancellationToken ct)
    {
        var programme = await _db.ClubProgrammes.FirstOrDefaultAsync(p => p.Id == programmeId, ct);
        if (programme == null) return Result.NotFound<PagedResult<AcePigeonResultDto>>("Programme");

        var q = _db.AcePigeonResults.Where(r => r.ProgrammeId == programmeId);
        if (!string.IsNullOrEmpty(paged.Search))
            q = q.Where(r => r.RingNumber.Contains(paged.Search) || r.FancierName.Contains(paged.Search));

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(r => r.AceRank).Skip(paged.Skip).Take(paged.PageSize).ToListAsync(ct);

        return Result.Success(new PagedResult<AcePigeonResultDto>
        {
            Items = items.Select(r => r.ToDto(programme.Name)).ToList(),
            TotalCount = total, Page = paged.Page, PageSize = paged.PageSize
        });
    }

    public async Task<Result<PagedResult<SuperAcePigeonResultDto>>> GetSuperAcePigeonAsync(Guid programmeId, PagedQuery paged, CancellationToken ct)
    {
        var programme = await _db.ClubProgrammes.FirstOrDefaultAsync(p => p.Id == programmeId, ct);
        if (programme == null) return Result.NotFound<PagedResult<SuperAcePigeonResultDto>>("Programme");

        var q = _db.SuperAcePigeonResults.Where(r => r.ProgrammeId == programmeId);
        if (!string.IsNullOrEmpty(paged.Search))
            q = q.Where(r => r.RingNumber.Contains(paged.Search) || r.FancierName.Contains(paged.Search));

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(r => r.SuperAceRank).Skip(paged.Skip).Take(paged.PageSize).ToListAsync(ct);

        return Result.Success(new PagedResult<SuperAcePigeonResultDto>
        {
            Items = items.Select(r => r.ToDto(programme.Name)).ToList(),
            TotalCount = total, Page = paged.Page, PageSize = paged.PageSize
        });
    }

    // ── Calculation Engines ───────────────────────────────────────────────────

    private List<BestLoftResult> CalculateBestLoft(
        ClubProgramme programme,
        List<ProgrammeRace> programmeRaces,
        List<RaceResultForCalculation> allResults,
        List<string> warnings)
    {
        var byFancier = allResults.Where(r => r.UserId.HasValue).GroupBy(r => r.UserId!.Value);
        var fancierData = byFancier.Select(g => new
        {
            UserId = g.Key,
            Name = g.First().UserFullName ?? "Unknown",
            Races = g.GroupBy(r => r.RaceId)
                     .Select(rg => (RaceId: rg.Key, Pigeons: rg.OrderByDescending(r => r.SpeedMperMin).ToList()))
                     .ToList()
        }).Where(f => f.Races.Count >= programme.BestLoftMinRaces).ToList();

        return fancierData.Select(f =>
        {
            double totalScore = 0; double bestSpeed = 0;
            double totalSpeed = 0; int speedCount = 0; int totalPigeons = 0;
            var breakdown = new List<RaceBreakdownItem>();

            foreach (var pr in programmeRaces)
            {
                var raceEntry = f.Races.FirstOrDefault(r => r.RaceId == pr.RaceId);
                if (raceEntry == default)
                {
                    breakdown.Add(new RaceBreakdownItem(pr.RaceId, pr.RaceName, 0, 0, 0, 0, true));
                    continue;
                }
                var pigeons = programme.BestLoftPigeonsPerRace > 0
                    ? raceEntry.Pigeons.Take(programme.BestLoftPigeonsPerRace).ToList()
                    : raceEntry.Pigeons;

                double score = ComputeScore(programme, pigeons, pr.ScoreWeight);
                double bestRaceVel = pigeons.Max(p => p.SpeedMperMin);
                int bestRank = pigeons.Min(p => p.ClubRank);
                totalScore += score; totalPigeons += pigeons.Count;
                totalSpeed += pigeons.Average(p => p.SpeedMperMin); speedCount++;
                bestSpeed = Math.Max(bestSpeed, bestRaceVel);
                breakdown.Add(new RaceBreakdownItem(pr.RaceId, pr.RaceName, score, bestRaceVel, bestRank, pigeons.Count, false));
            }

            return new { f.UserId, f.Name, TotalScore = totalScore, AvgScore = f.Races.Count > 0 ? totalScore / f.Races.Count : 0,
                RacesEntered = f.Races.Count, TotalPigeons = totalPigeons,
                BestSpeed = bestSpeed, AvgSpeed = speedCount > 0 ? totalSpeed / speedCount : 0, Breakdown = breakdown };
        })
        .OrderByDescending(x => x.TotalScore).ThenByDescending(x => x.BestSpeed)
        .Select((x, i) => new BestLoftResult
        {
            ProgrammeId = programme.Id, UserId = x.UserId, FancierName = x.Name,
            LoftRank = i + 1, TotalScore = Math.Round(x.TotalScore, 4),
            AverageScore = Math.Round(x.AvgScore, 4), RacesEntered = x.RacesEntered,
            PigeonsEntered = x.TotalPigeons, BestSingleSpeedMperMin = Math.Round(x.BestSpeed, 4),
            AverageSpeedMperMin = Math.Round(x.AvgSpeed, 4),
            RaceBreakdownJson = JsonSerializer.Serialize(x.Breakdown)
        }).ToList();
    }

    private List<AcePigeonResult> CalculateAcePigeon(
        ClubProgramme programme, List<ProgrammeRace> programmeRaces,
        List<RaceResultForCalculation> allResults, List<string> warnings, int totalRaces)
    {
        var byPigeon = allResults.GroupBy(r => r.RingNumber.ToUpperInvariant());
        var qualified = byPigeon.Where(g => g.Count() >= programme.AcePigeonMinRaces).ToList();

        return qualified.Select(g =>
        {
            var first = g.First();
            double totalScore = 0; double bestSpeed = 0; double totalSpeed = 0;
            int bestRank = int.MaxValue;
            var breakdown = new List<RaceBreakdownItem>();

            foreach (var pr in programmeRaces)
            {
                var entry = g.FirstOrDefault(r => r.RaceId == pr.RaceId);
                if (entry == null) { breakdown.Add(new RaceBreakdownItem(pr.RaceId, pr.RaceName, 0, 0, 0, 0, true)); continue; }
                double score = ComputeScore(programme, new[] { entry }, pr.ScoreWeight);
                totalScore += score; bestSpeed = Math.Max(bestSpeed, entry.SpeedMperMin);
                totalSpeed += entry.SpeedMperMin;
                bestRank = Math.Min(bestRank, entry.ClubRank);
                breakdown.Add(new RaceBreakdownItem(pr.RaceId, pr.RaceName, score, entry.SpeedMperMin, entry.ClubRank, 1, false));
            }

            int entered = g.Count();
            return new { RingNumber = g.Key, first.PigeonName, first.PigeonSex, first.PigeonYearOfBirth,
                first.UserId, FancierName = first.UserFullName ?? "Unlinked", first.PigeonId,
                TotalScore = totalScore, AvgScore = entered > 0 ? totalScore / entered : 0,
                RacesEntered = entered, ParticipationRate = totalRaces > 0 ? (double)entered / totalRaces * 100 : 0,
                BestSpeed = bestSpeed, AvgSpeed = entered > 0 ? totalSpeed / entered : 0,
                BestRank = bestRank == int.MaxValue ? 0 : bestRank, Breakdown = breakdown };
        })
        .OrderByDescending(x => x.TotalScore).ThenByDescending(x => x.BestSpeed)
        .Select((x, i) => new AcePigeonResult
        {
            ProgrammeId = programme.Id, UserId = x.UserId, PigeonId = x.PigeonId,
            RingNumber = x.RingNumber, PigeonName = x.PigeonName, PigeonSex = x.PigeonSex,
            PigeonYearOfBirth = x.PigeonYearOfBirth, FancierName = x.FancierName,
            AceRank = i + 1, TotalScore = Math.Round(x.TotalScore, 4),
            AverageScore = Math.Round(x.AvgScore, 4), RacesEntered = x.RacesEntered,
            RacesInProgramme = totalRaces, ParticipationRate = Math.Round(x.ParticipationRate, 2),
            BestSpeedMperMin = Math.Round(x.BestSpeed, 4),
            AverageSpeedMperMin = Math.Round(x.AvgSpeed, 4),
            BestClubRank = x.BestRank,
            RaceBreakdownJson = JsonSerializer.Serialize(x.Breakdown)
        }).ToList();
    }

    private static List<SuperAcePigeonResult> CalculateSuperAce(
        ClubProgramme programme, List<AcePigeonResult> aceResults,
        int totalRaces, List<string> warnings)
    {
        var qualifiers = programme.SuperAceQualification switch
        {
            SuperAceQualification.AllRacesRequired => aceResults.Where(a => a.RacesEntered == totalRaces).ToList(),
            SuperAceQualification.MinimumRaceCount => aceResults.Where(a => a.RacesEntered >= programme.SuperAceMinRaceCount).ToList(),
            SuperAceQualification.MinimumRacePercentage => aceResults.Where(a => a.ParticipationRate >= programme.SuperAceMinRacePercentage).ToList(),
            _ => aceResults
        };

        if (qualifiers.Count == 0) warnings.Add("No pigeons qualified for Super Ace.");

        return qualifiers.OrderByDescending(a => a.TotalScore).ThenByDescending(a => a.BestSpeedMperMin)
            .Select((a, i) => new SuperAcePigeonResult
            {
                ProgrammeId = programme.Id, UserId = a.UserId, PigeonId = a.PigeonId,
                RingNumber = a.RingNumber, PigeonName = a.PigeonName, PigeonSex = a.PigeonSex,
                PigeonYearOfBirth = a.PigeonYearOfBirth, FancierName = a.FancierName,
                SuperAceRank = i + 1, TotalScore = a.TotalScore, AverageScore = a.AverageScore,
                RacesEntered = a.RacesEntered, RacesInProgramme = a.RacesInProgramme,
                ParticipationRate = a.ParticipationRate, BestSpeedMperMin = a.BestSpeedMperMin,
                AverageSpeedMperMin = a.AverageSpeedMperMin, BestClubRank = a.BestClubRank,
                AcePigeonResultId = a.Id, RaceBreakdownJson = a.RaceBreakdownJson
            }).ToList();
    }

    private static double ComputeScore(ClubProgramme p, IEnumerable<RaceResultForCalculation> pigeons, double weight)
    {
        return p.ScoringMethod switch
        {
            ScoringMethod.AverageSpeed => pigeons.Average(x => x.SpeedMperMin) * weight,
            ScoringMethod.TotalSpeed   => pigeons.Sum(x => x.SpeedMperMin) * weight,
            ScoringMethod.PointsByRank    => pigeons.Sum(x => ComputeRankPoints(x.ClubRank, p)) * weight,
            _                             => pigeons.Average(x => x.SpeedMperMin) * weight
        };
    }

    private static double ComputeRankPoints(int rank, ClubProgramme p)
    {
        if (p.MaxPointPositions > 0 && rank > p.MaxPointPositions) return 0;
        return Math.Max(0, p.PointsForFirst - (rank - 1));
    }

    private Task<ClubProgramme?> LoadProgrammeWithRacesAsync(Guid id, CancellationToken ct) =>
        _db.ClubProgrammes
            .Include(p => p.Club)
            .Include(p => p.ProgrammeRaces.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

    private async Task<ProgrammeDto> LoadDtoAsync(Guid id, CancellationToken ct)
    {
        var p = await LoadProgrammeWithRacesAsync(id, ct);
        return p!.ToDto();
    }
}

public static class ProgrammeMappingExtensions
{
    public static ProgrammeDto ToDto(this ClubProgramme p) => new(
        p.Id, p.ClubId, p.Club?.Name, p.FederationId, p.FederationName,
        p.Name, p.Description, p.Year, p.StartDate, p.EndDate,
        p.Status, p.ScoringMethod, p.PointsForFirst, p.MaxPointPositions,
        p.BestLoftPigeonsPerRace, p.BestLoftMinRaces, p.AcePigeonMinRaces,
        p.SuperAceQualification, p.SuperAceMinRaceCount, p.SuperAceMinRacePercentage,
        p.PublishedAt, p.CreatedAt,
        p.ProgrammeRaces.Where(r => !r.IsDeleted).OrderBy(r => r.SortOrder)
            .Select(r => new ProgrammeRaceDto(r.Id, r.RaceId, r.RaceName, r.ActualReleaseTime, r.ScoreWeight, r.SortOrder, r.TotalEntries))
            .ToList());

    public static ProgrammeSummaryDto ToSummaryDto(this ClubProgramme p) => new(
        p.Id, p.Name, p.Year, p.Status, p.ScoringMethod,
        p.ProgrammeRaces.Count(r => !r.IsDeleted), p.StartDate, p.EndDate);

    public static BestLoftResultDto ToDto(this BestLoftResult r, string programmeName)
    {
        var breakdown = r.RaceBreakdownJson != null
            ? JsonSerializer.Deserialize<List<RaceBreakdownItem>>(r.RaceBreakdownJson) ?? new()
            : new List<RaceBreakdownItem>();
        return new(r.Id, r.ProgrammeId, programmeName, r.UserId, r.FancierName,
            r.LoftRank, r.TotalScore, r.AverageScore, r.RacesEntered, r.PigeonsEntered,
            r.BestSingleSpeedMperMin, r.AverageSpeedMperMin, breakdown);
    }

    public static AcePigeonResultDto ToDto(this AcePigeonResult r, string programmeName)
    {
        var breakdown = r.RaceBreakdownJson != null
            ? JsonSerializer.Deserialize<List<RaceBreakdownItem>>(r.RaceBreakdownJson) ?? new()
            : new List<RaceBreakdownItem>();
        return new(r.Id, r.ProgrammeId, programmeName, r.UserId, r.FancierName,
            r.PigeonId, r.RingNumber, r.PigeonName, r.PigeonSex, r.PigeonYearOfBirth,
            r.AceRank, r.TotalScore, r.AverageScore, r.RacesEntered, r.RacesInProgramme,
            r.ParticipationRate, r.BestSpeedMperMin, r.AverageSpeedMperMin, r.BestClubRank, breakdown);
    }

    public static SuperAcePigeonResultDto ToDto(this SuperAcePigeonResult r, string programmeName)
    {
        var breakdown = r.RaceBreakdownJson != null
            ? JsonSerializer.Deserialize<List<RaceBreakdownItem>>(r.RaceBreakdownJson) ?? new()
            : new List<RaceBreakdownItem>();
        return new(r.Id, r.ProgrammeId, programmeName, r.UserId, r.FancierName,
            r.PigeonId, r.RingNumber, r.PigeonName, r.PigeonSex, r.PigeonYearOfBirth,
            r.SuperAceRank, r.TotalScore, r.AverageScore, r.RacesEntered, r.RacesInProgramme,
            r.ParticipationRate, r.BestSpeedMperMin, r.AverageSpeedMperMin, r.BestClubRank, breakdown);
    }
}
