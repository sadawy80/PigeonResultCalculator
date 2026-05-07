// ── Add to PigeonRacing.Domain/Enums/Enums.cs ────────────────────────────────
// These are appended to the same namespace

namespace PigeonRacing.Domain.Enums;

/// <summary>
/// Scoring method used when calculating Best Loft, Ace Pigeon, and Super Ace results.
/// </summary>
public enum ScoringMethod
{
    /// Average velocity (m/min) across all qualifying races.
    AverageVelocity = 1,

    /// Points awarded per race rank (1st = MaxPoints, 2nd = MaxPoints-1, …).
    PointsByRank = 2,

    /// Points as a percentage of the winner's velocity in each race.
    PointsByVelocityPercentage = 3,

    /// Sum of raw velocities across all qualifying races.
    TotalVelocity = 4
}

/// <summary>
/// Qualification requirement for Super Ace status.
/// </summary>
public enum SuperAceQualification
{
    /// Pigeon must have entered every race in the programme.
    AllRacesRequired = 1,

    /// Pigeon must have entered at least N races (configurable threshold).
    MinimumRaceCount = 2,

    /// Pigeon must have entered at least X% of races in the programme.
    MinimumRacePercentage = 3
}

/// <summary>
/// Lifecycle status of a Club Programme.
/// </summary>
public enum ProgrammeStatus
{
    Draft = 1,
    Active = 2,
    Completed = 3,
    Published = 4,
    Cancelled = 5
}

/// <summary>
/// Which aggregate result table a calculation record belongs to.
/// </summary>
public enum AggregateResultType
{
    BestLoft = 1,
    AcePigeon = 2,
    SuperAcePigeon = 3
}

/// <summary>
/// Scoring method used when calculating Best Loft, Ace Pigeon, and Super Ace results.
/// </summary>
public enum ScoringMethod
{
    /// Average velocity (m/min) across all qualifying races.
    AverageVelocity = 1,

    /// Points awarded per race rank (1st = MaxPoints, 2nd = MaxPoints-1, …).
    PointsByRank = 2,

    /// Points as a percentage of the winner's velocity in each race.
    PointsByVelocityPercentage = 3,

    /// Sum of raw velocities across all qualifying races.
    TotalVelocity = 4
}

/// <summary>
/// Qualification requirement for Super Ace status.
/// </summary>
public enum SuperAceQualification
{
    /// Pigeon must have entered every race in the programme.
    AllRacesRequired = 1,

    /// Pigeon must have entered at least N races (configurable threshold).
    MinimumRaceCount = 2,

    /// Pigeon must have entered at least X% of races in the programme.
    MinimumRacePercentage = 3
}

/// <summary>
/// Lifecycle status of a Club Programme.
/// </summary>
public enum ProgrammeStatus
{
    Draft = 1,
    Active = 2,
    Completed = 3,
    Published = 4,
    Cancelled = 5
}

/// <summary>
/// Which aggregate result table a calculation record belongs to.
/// </summary>
public enum AggregateResultType
{
    BestLoft = 1,
    AcePigeon = 2,
    SuperAcePigeon = 3
}
