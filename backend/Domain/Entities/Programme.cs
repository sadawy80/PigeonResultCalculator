using PigeonRacing.Domain.Common;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.Domain.Entities;

// ─────────────────────────────────────────────────────────────────────────────
//  ClubProgramme
//  A named season or series (e.g. "2025 Sprint Series", "2025 Long Distance Cup")
//  that groups races together and drives Best Loft, Ace Pigeon, Super Ace calcs.
// ─────────────────────────────────────────────────────────────────────────────

public class ClubProgramme : BaseEntity
{
    public Guid ClubId { get; set; }

    /// Display name, e.g. "2025 Club Season" or "Sprint Series Spring 2025"
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int Year { get; set; } = DateTime.UtcNow.Year;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public ProgrammeStatus Status { get; set; } = ProgrammeStatus.Draft;

    // ── Scoring configuration ─────────────────────────────────────────────────

    /// How scores are computed for Best Loft and Ace Pigeon.
    public ScoringMethod ScoringMethod { get; set; } = ScoringMethod.AverageVelocity;

    /// When ScoringMethod = PointsByRank: points awarded to 1st place (scales down from here).
    public int PointsForFirst { get; set; } = 10;

    /// When ScoringMethod = PointsByRank: how many positions earn points (0 = unlimited).
    public int MaxPointPositions { get; set; } = 0;

    // ── Best Loft configuration ───────────────────────────────────────────────

    /// Number of pigeons per race counted toward a fancier's loft score (0 = all).
    public int BestLoftPigeonsPerRace { get; set; } = 0;

    /// Minimum races a fancier must enter to qualify for Best Loft ranking.
    public int BestLoftMinRaces { get; set; } = 1;

    // ── Ace Pigeon configuration ──────────────────────────────────────────────

    /// Minimum races a pigeon must enter to qualify as Ace Pigeon.
    public int AcePigeonMinRaces { get; set; } = 3;

    // ── Super Ace Pigeon configuration ────────────────────────────────────────

    public SuperAceQualification SuperAceQualification { get; set; } = SuperAceQualification.AllRacesRequired;

    /// Used when SuperAceQualification = MinimumRaceCount.
    public int SuperAceMinRaceCount { get; set; } = 0;

    /// Used when SuperAceQualification = MinimumRacePercentage (0–100).
    public double SuperAceMinRacePercentage { get; set; } = 100.0;

    // ── Publication ───────────────────────────────────────────────────────────

    public DateTime? PublishedAt { get; set; }
    public Guid? PublishedByUserId { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────

    public Club Club { get; set; } = null!;
    public ApplicationUser? PublishedByUser { get; set; }
    public ICollection<ProgrammeRace> ProgrammeRaces { get; set; } = new List<ProgrammeRace>();
    public ICollection<BestLoftResult> BestLoftResults { get; set; } = new List<BestLoftResult>();
    public ICollection<AcePigeonResult> AcePigeonResults { get; set; } = new List<AcePigeonResult>();
    public ICollection<SuperAcePigeonResult> SuperAcePigeonResults { get; set; } = new List<SuperAcePigeonResult>();
}

// ─────────────────────────────────────────────────────────────────────────────
//  ProgrammeRace
//  Join table — which races belong to a programme, and their weighting.
// ─────────────────────────────────────────────────────────────────────────────

public class ProgrammeRace : BaseEntity
{
    public Guid ProgrammeId { get; set; }
    public Guid RaceId { get; set; }

    /// Optional multiplier applied to the score from this race (default 1.0).
    /// Allows certain races to count double, e.g. championship finals.
    public double ScoreWeight { get; set; } = 1.0;

    /// Display order within the programme.
    public int SortOrder { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────

    public ClubProgramme Programme { get; set; } = null!;
    public Race Race { get; set; } = null!;
}

// ─────────────────────────────────────────────────────────────────────────────
//  BestLoftResult
//  Aggregate result per fancier across all races in a programme.
//  "Best Loft" = the fancier/owner whose pigeons performed best overall.
// ─────────────────────────────────────────────────────────────────────────────

public class BestLoftResult : BaseEntity
{
    public Guid ProgrammeId { get; set; }
    public Guid? UserId { get; set; }

    /// Fancier name — kept denormalised in case the account was later deleted.
    public string FancierName { get; set; } = string.Empty;

    // ── Score fields ──────────────────────────────────────────────────────────

    public int LoftRank { get; set; }
    public double TotalScore { get; set; }        // sum of per-race scores
    public double AverageScore { get; set; }      // TotalScore / RacesEntered
    public int RacesEntered { get; set; }
    public int PigeonsEntered { get; set; }       // total across all races

    /// Best single-race velocity achieved by any pigeon from this loft.
    public double BestSingleVelocityMperMin { get; set; }

    /// Average velocity across all pigeons/races counted.
    public double AverageVelocityMperMin { get; set; }

    // ── Per-race breakdown (JSON) ─────────────────────────────────────────────
    /// JSON array of { raceId, raceName, score, bestVelocity, pigeonsEntered, bestRank }
    public string? RaceBreakdownJson { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────

    public ClubProgramme Programme { get; set; } = null!;
    public ApplicationUser? User { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
//  AcePigeonResult
//  Aggregate result per individual pigeon across all races in a programme.
//  "Ace Pigeon" = the individual bird with the best overall performance.
// ─────────────────────────────────────────────────────────────────────────────

public class AcePigeonResult : BaseEntity
{
    public Guid ProgrammeId { get; set; }
    public Guid? UserId { get; set; }       // owning fancier
    public Guid? PigeonId { get; set; }     // nullable — pigeon may not be registered

    /// Ring number — primary identifier, always populated.
    public string RingNumber { get; set; } = string.Empty;
    public string? PigeonName { get; set; }
    public string? PigeonSex { get; set; }
    public int? PigeonYearOfBirth { get; set; }

    /// Name of the owning fancier — denormalised.
    public string FancierName { get; set; } = string.Empty;

    // ── Score fields ──────────────────────────────────────────────────────────

    public int AceRank { get; set; }
    public double TotalScore { get; set; }
    public double AverageScore { get; set; }      // TotalScore / RacesEntered
    public int RacesEntered { get; set; }
    public int RacesInProgramme { get; set; }     // total races pigeon could have entered
    public double ParticipationRate { get; set; } // RacesEntered / RacesInProgramme * 100

    public double BestVelocityMperMin { get; set; }
    public double AverageVelocityMperMin { get; set; }
    public int BestClubRank { get; set; }         // best rank achieved in any single race

    /// JSON array of { raceId, raceName, velocity, clubRank, score, dnf }
    public string? RaceBreakdownJson { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────

    public ClubProgramme Programme { get; set; } = null!;
    public ApplicationUser? User { get; set; }
    public Pigeon? Pigeon { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
//  SuperAcePigeonResult
//  Elite subset of Ace Pigeon — pigeons meeting stricter participation criteria.
//  Computed from AcePigeonResult records that satisfy the programme's
//  SuperAceQualification rules.
// ─────────────────────────────────────────────────────────────────────────────

public class SuperAcePigeonResult : BaseEntity
{
    public Guid ProgrammeId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? PigeonId { get; set; }

    public string RingNumber { get; set; } = string.Empty;
    public string? PigeonName { get; set; }
    public string? PigeonSex { get; set; }
    public int? PigeonYearOfBirth { get; set; }
    public string FancierName { get; set; } = string.Empty;

    // ── Score fields ──────────────────────────────────────────────────────────

    public int SuperAceRank { get; set; }
    public double TotalScore { get; set; }
    public double AverageScore { get; set; }
    public int RacesEntered { get; set; }          // must equal RacesInProgramme for AllRacesRequired
    public int RacesInProgramme { get; set; }
    public double ParticipationRate { get; set; }  // will be 100% for AllRacesRequired

    public double BestVelocityMperMin { get; set; }
    public double AverageVelocityMperMin { get; set; }
    public int BestClubRank { get; set; }

    /// Reference back to the underlying AcePigeonResult for traceability.
    public Guid? AcePigeonResultId { get; set; }

    /// JSON array — same structure as AcePigeonResult.RaceBreakdownJson.
    public string? RaceBreakdownJson { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────

    public ClubProgramme Programme { get; set; } = null!;
    public ApplicationUser? User { get; set; }
    public Pigeon? Pigeon { get; set; }
    public AcePigeonResult? AcePigeonResult { get; set; }
}
