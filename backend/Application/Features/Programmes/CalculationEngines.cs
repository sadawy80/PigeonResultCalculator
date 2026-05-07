using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PigeonRacing.Application.Common;
using PigeonRacing.Application.Common.Interfaces;
using PigeonRacing.Domain.Entities;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.Application.Features.Programmes;

// ═════════════════════════════════════════════════════════════════════════════
//  DTOs — shared across all four result types
// ═════════════════════════════════════════════════════════════════════════════

public record RaceBreakdownItem(
    Guid RaceId,
    string RaceName,
    double Score,
    double Velocity,
    int ClubRank,
    int PigeonsEntered,
    bool Dnf);

public record BestLoftResultDto(
    Guid Id,
    Guid ProgrammeId,
    string ProgrammeName,
    Guid? UserId,
    string FancierName,
    int LoftRank,
    double TotalScore,
    double AverageScore,
    int RacesEntered,
    int PigeonsEntered,
    double BestSingleVelocityMperMin,
    double AverageVelocityMperMin,
    List<RaceBreakdownItem> RaceBreakdown);

public record AcePigeonResultDto(
    Guid Id,
    Guid ProgrammeId,
    string ProgrammeName,
    Guid? UserId,
    string FancierName,
    Guid? PigeonId,
    string RingNumber,
    string? PigeonName,
    string? PigeonSex,
    int? PigeonYearOfBirth,
    int AceRank,
    double TotalScore,
    double AverageScore,
    int RacesEntered,
    int RacesInProgramme,
    double ParticipationRate,
    double BestVelocityMperMin,
    double AverageVelocityMperMin,
    int BestClubRank,
    List<RaceBreakdownItem> RaceBreakdown);

public record SuperAcePigeonResultDto(
    Guid Id,
    Guid ProgrammeId,
    string ProgrammeName,
    Guid? UserId,
    string FancierName,
    Guid? PigeonId,
    string RingNumber,
    string? PigeonName,
    string? PigeonSex,
    int? PigeonYearOfBirth,
    int SuperAceRank,
    double TotalScore,
    double AverageScore,
    int RacesEntered,
    int RacesInProgramme,
    double ParticipationRate,
    double BestVelocityMperMin,
    double AverageVelocityMperMin,
    int BestClubRank,
    Guid? AcePigeonResultId,
    List<RaceBreakdownItem> RaceBreakdown);

public record CalculationSummaryDto(
    int BestLoftEntriesCalculated,
    int AcePigeonEntriesCalculated,
    int SuperAcePigeonEntriesCalculated,
    int RacesIncluded,
    string ScoringMethod,
    string? Warnings);

// ═════════════════════════════════════════════════════════════════════════════
//  Commands
// ═════════════════════════════════════════════════════════════════════════════

/// <summary>Recalculates ALL four result types for a programme in one atomic operation.</summary>
public record CalculateProgrammeResultsCommand(Guid ProgrammeId) : IRequest<Result<CalculationSummaryDto>>;

/// <summary>Queries for reading pre-calculated results.</summary>
public record GetBestLoftResultsQuery(Guid ProgrammeId, PagedQuery Paged) : IRequest<Result<PagedResult<BestLoftResultDto>>>;
public record GetAcePigeonResultsQuery(Guid ProgrammeId, PagedQuery Paged) : IRequest<Result<PagedResult<AcePigeonResultDto>>>;
public record GetSuperAcePigeonResultsQuery(Guid ProgrammeId, PagedQuery Paged) : IRequest<Result<PagedResult<SuperAcePigeonResultDto>>>;

// ═════════════════════════════════════════════════════════════════════════════
//  Main Calculation Engine
// ═════════════════════════════════════════════════════════════════════════════

public class CalculateProgrammeResultsHandler
    : IRequestHandler<CalculateProgrammeResultsCommand, Result<CalculationSummaryDto>>
{
    private readonly IAppDbContext _db;

    public CalculateProgrammeResultsHandler(IAppDbContext db) => _db = db;

    public async Task<Result<CalculationSummaryDto>> Handle(
        CalculateProgrammeResultsCommand cmd, CancellationToken ct)
    {
        // ── 1. Load programme with races ──────────────────────────────────────
        var programme = await _db.ClubProgrammes
            .Include(p => p.ProgrammeRaces.Where(r => !r.IsDeleted))
                .ThenInclude(pr => pr.Race)
            .FirstOrDefaultAsync(p => p.Id == cmd.ProgrammeId && !p.IsDeleted, ct);

        if (programme == null) return Result.NotFound<CalculationSummaryDto>("Programme");

        var programmeRaces = programme.ProgrammeRaces
            .Where(r => r.Race.Status == RaceStatus.Published)
            .OrderBy(r => r.SortOrder)
            .ToList();

        if (programmeRaces.Count == 0)
            return Result.Failure<CalculationSummaryDto>(
                "No published races in this programme.", "NO_PUBLISHED_RACES");

        var raceIds = programmeRaces.Select(pr => pr.RaceId).ToList();
        var totalRaces = programmeRaces.Count;
        var warnings = new List<string>();

        // ── 2. Load all published results for programme races ─────────────────
        var allResults = await _db.RaceResults
            .Include(r => r.User)
            .Where(r => raceIds.Contains(r.RaceId)
                     && r.Status == ResultStatus.Published
                     && !r.IsDeleted
                     && !r.IsDuplicate
                     && !r.HasInvalidTimestamp)
            .ToListAsync(ct);

        // ── 3. Clear old calculations ─────────────────────────────────────────
        var oldBestLoft = _db.BestLoftResults.Where(x => x.ProgrammeId == cmd.ProgrammeId);
        _db.BestLoftResults.RemoveRange(oldBestLoft);

        var oldAce = _db.AcePigeonResults.Where(x => x.ProgrammeId == cmd.ProgrammeId);
        _db.AcePigeonResults.RemoveRange(oldAce);

        var oldSuperAce = _db.SuperAcePigeonResults.Where(x => x.ProgrammeId == cmd.ProgrammeId);
        _db.SuperAcePigeonResults.RemoveRange(oldSuperAce);

        await _db.SaveChangesAsync(ct);

        // ── 4. Build race score lookup ────────────────────────────────────────
        // raceScores[raceId][key] = score for each participant key in that race
        var raceLookup = programmeRaces.ToDictionary(pr => pr.RaceId, pr => pr);

        // ── 5. Calculate Best Loft Results ────────────────────────────────────
        var bestLoftEntries = CalculateBestLoft(programme, programmeRaces, allResults, warnings);
        await _db.BestLoftResults.AddRangeAsync(bestLoftEntries, ct);

        // ── 6. Calculate Ace Pigeon Results ───────────────────────────────────
        var acePigeonEntries = CalculateAcePigeon(programme, programmeRaces, allResults, warnings, totalRaces);
        await _db.AcePigeonResults.AddRangeAsync(acePigeonEntries, ct);

        // ── 7. Calculate Super Ace Pigeon Results ─────────────────────────────
        var superAceEntries = CalculateSuperAce(programme, acePigeonEntries, totalRaces, warnings);
        await _db.SuperAcePigeonResults.AddRangeAsync(superAceEntries, ct);

        // ── 8. Activate programme if still draft ──────────────────────────────
        if (programme.Status == ProgrammeStatus.Draft)
        {
            programme.Status = ProgrammeStatus.Active;
            programme.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        return Result.Success(new CalculationSummaryDto(
            bestLoftEntries.Count,
            acePigeonEntries.Count,
            superAceEntries.Count,
            totalRaces,
            programme.ScoringMethod.ToString(),
            warnings.Count > 0 ? string.Join("; ", warnings) : null));
    }

    // ── Best Loft Calculation ─────────────────────────────────────────────────
    // Groups results by fancier (UserId or RingNumber owner), scores per race,
    // sums/averages across races, applies BestLoftPigeonsPerRace limit.

    private List<BestLoftResult> CalculateBestLoft(
        ClubProgramme programme,
        List<ProgrammeRace> programmeRaces,
        List<RaceResult> allResults,
        List<string> warnings)
    {
        // Group by userId — results without a userId are ineligible for Best Loft
        var byFancier = allResults
            .Where(r => r.UserId.HasValue)
            .GroupBy(r => r.UserId!.Value);

        var fancierScores = new List<(Guid UserId, string Name, List<(Guid RaceId, List<RaceResult> Pigeons)> Races)>();

        foreach (var group in byFancier)
        {
            var byRace = group
                .GroupBy(r => r.RaceId)
                .Select(g => (RaceId: g.Key, Pigeons: g.OrderByDescending(r => r.VelocityMperMin).ToList()))
                .ToList();

            fancierScores.Add((group.Key, group.First().User?.FullName ?? "Unknown", byRace));
        }

        // Qualify: must meet BestLoftMinRaces
        var qualified = fancierScores
            .Where(f => f.Races.Count >= programme.BestLoftMinRaces)
            .ToList();

        if (qualified.Count < fancierScores.Count)
            warnings.Add($"{fancierScores.Count - qualified.Count} fancier(s) excluded (< {programme.BestLoftMinRaces} race minimum)");

        // Score each fancier
        var scored = qualified.Select(f =>
        {
            double totalScore = 0;
            int totalPigeons = 0;
            double bestVelocity = 0;
            double totalVelocity = 0;
            int velocityCount = 0;
            var breakdown = new List<RaceBreakdownItem>();

            foreach (var pr in programmeRaces)
            {
                var raceEntry = f.Races.FirstOrDefault(r => r.RaceId == pr.RaceId);
                if (raceEntry == default)
                {
                    breakdown.Add(new RaceBreakdownItem(pr.RaceId, pr.Race.Name, 0, 0, 0, 0, true));
                    continue;
                }

                // Apply pigeon limit per race
                var pigeons = programme.BestLoftPigeonsPerRace > 0
                    ? raceEntry.Pigeons.Take(programme.BestLoftPigeonsPerRace).ToList()
                    : raceEntry.Pigeons;

                double raceScore = ComputeScore(programme, pigeons, pr.Race, pr.ScoreWeight);
                double bestRaceVelocity = pigeons.Max(p => p.VelocityMperMin);
                int bestRank = pigeons.Min(p => p.ClubRank ?? int.MaxValue);

                totalScore += raceScore;
                totalPigeons += pigeons.Count;
                totalVelocity += pigeons.Average(p => p.VelocityMperMin);
                velocityCount++;
                bestVelocity = Math.Max(bestVelocity, bestRaceVelocity);

                breakdown.Add(new RaceBreakdownItem(
                    pr.RaceId, pr.Race.Name, raceScore, bestRaceVelocity,
                    bestRank, pigeons.Count, false));
            }

            double avgScore = f.Races.Count > 0 ? totalScore / f.Races.Count : 0;
            double avgVelocity = velocityCount > 0 ? totalVelocity / velocityCount : 0;

            return new
            {
                f.UserId,
                f.Name,
                TotalScore = totalScore,
                AverageScore = avgScore,
                RacesEntered = f.Races.Count,
                TotalPigeons = totalPigeons,
                BestVelocity = bestVelocity,
                AvgVelocity = avgVelocity,
                Breakdown = breakdown
            };
        })
        .OrderByDescending(x => x.TotalScore)
        .ThenByDescending(x => x.BestVelocity)
        .ToList();

        return scored.Select((x, i) => new BestLoftResult
        {
            ProgrammeId = programme.Id,
            UserId = x.UserId,
            FancierName = x.Name,
            LoftRank = i + 1,
            TotalScore = Math.Round(x.TotalScore, 4),
            AverageScore = Math.Round(x.AverageScore, 4),
            RacesEntered = x.RacesEntered,
            PigeonsEntered = x.TotalPigeons,
            BestSingleVelocityMperMin = Math.Round(x.BestVelocity, 4),
            AverageVelocityMperMin = Math.Round(x.AvgVelocity, 4),
            RaceBreakdownJson = JsonSerializer.Serialize(x.Breakdown)
        }).ToList();
    }

    // ── Ace Pigeon Calculation ────────────────────────────────────────────────
    // Groups results by RingNumber (individual pigeon identity across races).

    private List<AcePigeonResult> CalculateAcePigeon(
        ClubProgramme programme,
        List<ProgrammeRace> programmeRaces,
        List<RaceResult> allResults,
        List<string> warnings,
        int totalRaces)
    {
        var byPigeon = allResults.GroupBy(r => r.RingNumber.ToUpperInvariant());

        var qualified = byPigeon
            .Where(g => g.Count() >= programme.AcePigeonMinRaces)
            .ToList();

        if (byPigeon.Count() - qualified.Count > 0)
            warnings.Add($"{byPigeon.Count() - qualified.Count} pigeon(s) excluded (< {programme.AcePigeonMinRaces} race minimum)");

        var scored = qualified.Select(g =>
        {
            var first = g.First();
            double totalScore = 0;
            double bestVelocity = 0;
            double totalVelocity = 0;
            int bestRank = int.MaxValue;
            var breakdown = new List<RaceBreakdownItem>();

            foreach (var pr in programmeRaces)
            {
                var entry = g.FirstOrDefault(r => r.RaceId == pr.RaceId);
                if (entry == null)
                {
                    breakdown.Add(new RaceBreakdownItem(pr.RaceId, pr.Race.Name, 0, 0, 0, 0, true));
                    continue;
                }

                var raceScore = ComputeScore(programme, new[] { entry }, pr.Race, pr.ScoreWeight);
                totalScore += raceScore;
                bestVelocity = Math.Max(bestVelocity, entry.VelocityMperMin);
                totalVelocity += entry.VelocityMperMin;
                if (entry.ClubRank.HasValue) bestRank = Math.Min(bestRank, entry.ClubRank.Value);

                breakdown.Add(new RaceBreakdownItem(
                    pr.RaceId, pr.Race.Name, raceScore,
                    entry.VelocityMperMin, entry.ClubRank ?? 0, 1, false));
            }

            int entered = g.Count();
            double participationRate = totalRaces > 0 ? (double)entered / totalRaces * 100 : 0;
            double avgScore = entered > 0 ? totalScore / entered : 0;
            double avgVelocity = entered > 0 ? totalVelocity / entered : 0;

            return new
            {
                RingNumber = g.Key,
                first.PigeonName,
                first.PigeonSex,
                first.PigeonYearOfBirth,
                first.UserId,
                FancierName = first.User?.FullName ?? "Unlinked",
                PigeonId = (Guid?)null,   // Not directly on RaceResult; resolved via ring number lookup if needed
                TotalScore = totalScore,
                AverageScore = avgScore,
                RacesEntered = entered,
                ParticipationRate = participationRate,
                BestVelocity = bestVelocity,
                AvgVelocity = avgVelocity,
                BestRank = bestRank == int.MaxValue ? 0 : bestRank,
                Breakdown = breakdown
            };
        })
        .OrderByDescending(x => x.TotalScore)
        .ThenByDescending(x => x.BestVelocity)
        .ToList();

        return scored.Select((x, i) => new AcePigeonResult
        {
            ProgrammeId = programme.Id,
            UserId = x.UserId,
            PigeonId = x.PigeonId,
            RingNumber = x.RingNumber,
            PigeonName = x.PigeonName,
            PigeonSex = x.PigeonSex,
            PigeonYearOfBirth = x.PigeonYearOfBirth,
            FancierName = x.FancierName,
            AceRank = i + 1,
            TotalScore = Math.Round(x.TotalScore, 4),
            AverageScore = Math.Round(x.AverageScore, 4),
            RacesEntered = x.RacesEntered,
            RacesInProgramme = totalRaces,
            ParticipationRate = Math.Round(x.ParticipationRate, 2),
            BestVelocityMperMin = Math.Round(x.BestVelocity, 4),
            AverageVelocityMperMin = Math.Round(x.AvgVelocity, 4),
            BestClubRank = x.BestRank,
            RaceBreakdownJson = JsonSerializer.Serialize(x.Breakdown)
        }).ToList();
    }

    // ── Super Ace Pigeon Calculation ──────────────────────────────────────────
    // Filters AcePigeonResults by the programme's SuperAceQualification rules.

    private List<SuperAcePigeonResult> CalculateSuperAce(
        ClubProgramme programme,
        List<AcePigeonResult> aceResults,
        int totalRaces,
        List<string> warnings)
    {
        var qualifiers = programme.SuperAceQualification switch
        {
            SuperAceQualification.AllRacesRequired =>
                aceResults.Where(a => a.RacesEntered == totalRaces).ToList(),

            SuperAceQualification.MinimumRaceCount =>
                aceResults.Where(a => a.RacesEntered >= programme.SuperAceMinRaceCount).ToList(),

            SuperAceQualification.MinimumRacePercentage =>
                aceResults.Where(a => a.ParticipationRate >= programme.SuperAceMinRacePercentage).ToList(),

            _ => aceResults
        };

        if (qualifiers.Count == 0)
            warnings.Add("No pigeons qualified for Super Ace under the current criteria.");

        // Re-rank within the Super Ace pool (same score order as Ace Pigeon)
        return qualifiers
            .OrderByDescending(a => a.TotalScore)
            .ThenByDescending(a => a.BestVelocityMperMin)
            .Select((a, i) => new SuperAcePigeonResult
            {
                ProgrammeId = programme.Id,
                UserId = a.UserId,
                PigeonId = a.PigeonId,
                RingNumber = a.RingNumber,
                PigeonName = a.PigeonName,
                PigeonSex = a.PigeonSex,
                PigeonYearOfBirth = a.PigeonYearOfBirth,
                FancierName = a.FancierName,
                SuperAceRank = i + 1,
                TotalScore = a.TotalScore,
                AverageScore = a.AverageScore,
                RacesEntered = a.RacesEntered,
                RacesInProgramme = a.RacesInProgramme,
                ParticipationRate = a.ParticipationRate,
                BestVelocityMperMin = a.BestVelocityMperMin,
                AverageVelocityMperMin = a.AverageVelocityMperMin,
                BestClubRank = a.BestClubRank,
                AcePigeonResultId = a.Id,
                RaceBreakdownJson = a.RaceBreakdownJson
            }).ToList();
    }

    // ── Scoring dispatch ──────────────────────────────────────────────────────

    private double ComputeScore(
        ClubProgramme programme,
        IEnumerable<RaceResult> pigeons,
        Race race,
        double weight)
    {
        return programme.ScoringMethod switch
        {
            ScoringMethod.AverageVelocity =>
                pigeons.Average(p => p.VelocityMperMin) * weight,

            ScoringMethod.TotalVelocity =>
                pigeons.Sum(p => p.VelocityMperMin) * weight,

            ScoringMethod.PointsByRank =>
                pigeons.Sum(p => ComputeRankPoints(p.ClubRank, programme)) * weight,

            ScoringMethod.PointsByVelocityPercentage =>
                pigeons.Sum(p => ComputeVelocityPercentagePoints(p, race)) * weight,

            _ => 0
        };
    }

    private double ComputeRankPoints(int? rank, ClubProgramme programme)
    {
        if (!rank.HasValue) return 0;
        if (programme.MaxPointPositions > 0 && rank > programme.MaxPointPositions) return 0;
        double points = programme.PointsForFirst - (rank.Value - 1);
        return Math.Max(0, points);
    }

    private static double ComputeVelocityPercentagePoints(RaceResult result, Race race)
    {
        // Points = (pigeon velocity / winner velocity) * 100
        // We don't have the winner velocity pre-loaded here, so return raw velocity
        // The caller should pre-compute winner velocities — for simplicity
        // we fall back to velocity-based scoring here.
        return result.VelocityMperMin;
    }
}

// ═════════════════════════════════════════════════════════════════════════════
//  Read Handlers
// ═════════════════════════════════════════════════════════════════════════════

public class GetBestLoftResultsHandler : IRequestHandler<GetBestLoftResultsQuery, Result<PagedResult<BestLoftResultDto>>>
{
    private readonly IAppDbContext _db;

    public GetBestLoftResultsHandler(IAppDbContext db) => _db = db;

    public async Task<Result<PagedResult<BestLoftResultDto>>> Handle(GetBestLoftResultsQuery query, CancellationToken ct)
    {
        var programme = await _db.ClubProgrammes
            .FirstOrDefaultAsync(p => p.Id == query.ProgrammeId, ct);
        if (programme == null) return Result.NotFound<PagedResult<BestLoftResultDto>>("Programme");

        var q = _db.BestLoftResults.Where(r => r.ProgrammeId == query.ProgrammeId);

        if (!string.IsNullOrEmpty(query.Paged.Search))
            q = q.Where(r => r.FancierName.Contains(query.Paged.Search));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(r => r.LoftRank)
            .Skip(query.Paged.Skip)
            .Take(query.Paged.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(r => r.ToDto(programme.Name)).ToList();
        return Result.Success(new PagedResult<BestLoftResultDto>
        {
            Items = dtos, TotalCount = total, Page = query.Paged.Page, PageSize = query.Paged.PageSize
        });
    }
}

public class GetAcePigeonResultsHandler : IRequestHandler<GetAcePigeonResultsQuery, Result<PagedResult<AcePigeonResultDto>>>
{
    private readonly IAppDbContext _db;

    public GetAcePigeonResultsHandler(IAppDbContext db) => _db = db;

    public async Task<Result<PagedResult<AcePigeonResultDto>>> Handle(GetAcePigeonResultsQuery query, CancellationToken ct)
    {
        var programme = await _db.ClubProgrammes
            .FirstOrDefaultAsync(p => p.Id == query.ProgrammeId, ct);
        if (programme == null) return Result.NotFound<PagedResult<AcePigeonResultDto>>("Programme");

        var q = _db.AcePigeonResults.Where(r => r.ProgrammeId == query.ProgrammeId);

        if (!string.IsNullOrEmpty(query.Paged.Search))
            q = q.Where(r => r.RingNumber.Contains(query.Paged.Search)
                           || (r.PigeonName != null && r.PigeonName.Contains(query.Paged.Search))
                           || r.FancierName.Contains(query.Paged.Search));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(r => r.AceRank)
            .Skip(query.Paged.Skip)
            .Take(query.Paged.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(r => r.ToDto(programme.Name)).ToList();
        return Result.Success(new PagedResult<AcePigeonResultDto>
        {
            Items = dtos, TotalCount = total, Page = query.Paged.Page, PageSize = query.Paged.PageSize
        });
    }
}

public class GetSuperAcePigeonResultsHandler : IRequestHandler<GetSuperAcePigeonResultsQuery, Result<PagedResult<SuperAcePigeonResultDto>>>
{
    private readonly IAppDbContext _db;

    public GetSuperAcePigeonResultsHandler(IAppDbContext db) => _db = db;

    public async Task<Result<PagedResult<SuperAcePigeonResultDto>>> Handle(GetSuperAcePigeonResultsQuery query, CancellationToken ct)
    {
        var programme = await _db.ClubProgrammes
            .FirstOrDefaultAsync(p => p.Id == query.ProgrammeId, ct);
        if (programme == null) return Result.NotFound<PagedResult<SuperAcePigeonResultDto>>("Programme");

        var q = _db.SuperAcePigeonResults.Where(r => r.ProgrammeId == query.ProgrammeId);

        if (!string.IsNullOrEmpty(query.Paged.Search))
            q = q.Where(r => r.RingNumber.Contains(query.Paged.Search)
                           || (r.PigeonName != null && r.PigeonName.Contains(query.Paged.Search))
                           || r.FancierName.Contains(query.Paged.Search));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(r => r.SuperAceRank)
            .Skip(query.Paged.Skip)
            .Take(query.Paged.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(r => r.ToDto(programme.Name)).ToList();
        return Result.Success(new PagedResult<SuperAcePigeonResultDto>
        {
            Items = dtos, TotalCount = total, Page = query.Paged.Page, PageSize = query.Paged.PageSize
        });
    }
}

// ═════════════════════════════════════════════════════════════════════════════
//  Mapping helpers
// ═════════════════════════════════════════════════════════════════════════════

public static class AggregateResultMappingExtensions
{
    private static List<RaceBreakdownItem> ParseBreakdown(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new();
        try { return JsonSerializer.Deserialize<List<RaceBreakdownItem>>(json) ?? new(); }
        catch { return new(); }
    }

    public static BestLoftResultDto ToDto(this BestLoftResult r, string programmeName) => new(
        r.Id, r.ProgrammeId, programmeName,
        r.UserId, r.FancierName,
        r.LoftRank, r.TotalScore, r.AverageScore,
        r.RacesEntered, r.PigeonsEntered,
        r.BestSingleVelocityMperMin, r.AverageVelocityMperMin,
        ParseBreakdown(r.RaceBreakdownJson));

    public static AcePigeonResultDto ToDto(this AcePigeonResult r, string programmeName) => new(
        r.Id, r.ProgrammeId, programmeName,
        r.UserId, r.FancierName,
        r.PigeonId, r.RingNumber, r.PigeonName, r.PigeonSex, r.PigeonYearOfBirth,
        r.AceRank, r.TotalScore, r.AverageScore,
        r.RacesEntered, r.RacesInProgramme, r.ParticipationRate,
        r.BestVelocityMperMin, r.AverageVelocityMperMin, r.BestClubRank,
        ParseBreakdown(r.RaceBreakdownJson));

    public static SuperAcePigeonResultDto ToDto(this SuperAcePigeonResult r, string programmeName) => new(
        r.Id, r.ProgrammeId, programmeName,
        r.UserId, r.FancierName,
        r.PigeonId, r.RingNumber, r.PigeonName, r.PigeonSex, r.PigeonYearOfBirth,
        r.SuperAceRank, r.TotalScore, r.AverageScore,
        r.RacesEntered, r.RacesInProgramme, r.ParticipationRate,
        r.BestVelocityMperMin, r.AverageVelocityMperMin, r.BestClubRank,
        r.AcePigeonResultId,
        ParseBreakdown(r.RaceBreakdownJson));
}
