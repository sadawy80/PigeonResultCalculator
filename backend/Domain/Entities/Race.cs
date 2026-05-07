using PigeonRacing.Domain.Common;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.Domain.Entities;

public class Race : AuditableEntity
{
    public Guid ClubId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RaceStatus Status { get; set; } = RaceStatus.Draft;

    // Release info
    public string ReleaseLocation { get; set; } = string.Empty;
    public double ReleaseLongitude { get; set; }
    public double ReleaseLatitude { get; set; }
    public DateTime? ScheduledReleaseTime { get; set; }
    public DateTime? ActualReleaseTime { get; set; }

    // Weather at release
    public double? WindSpeedKmh { get; set; }
    public WindDirection? WindDirection { get; set; }
    public double? TemperatureCelsius { get; set; }
    public string? WeatherNotes { get; set; }

    // Race metadata
    public int? TotalPigeonsEntered { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsLiveTracking { get; set; } = false;

    /// Nominated distance in km (set before race, may differ from per-pigeon distances).
    public double? NominatedDistanceKm { get; set; }

    // ── Computed display helpers ──────────────────────────────────────────────
    public string WindDescription =>
        WindSpeedKmh.HasValue
            ? $"{WindDirection?.ToString() ?? ""} {WindSpeedKmh:F0} km/h".Trim()
            : WeatherNotes ?? "";

    public string TemperatureDescription =>
        TemperatureCelsius.HasValue ? $"{TemperatureCelsius:F1}°C" : "";

    // Navigation
    public Club Club { get; set; } = null!;
    public ICollection<RaceCategory> Categories { get; set; } = new List<RaceCategory>();
    public ICollection<RaceResult> Results { get; set; } = new List<RaceResult>();
    public ICollection<DataIngestionLog> IngestionLogs { get; set; } = new List<DataIngestionLog>();
    public ICollection<CountryResultRace> CountryResultRaces { get; set; } = new List<CountryResultRace>();
}

public class RaceCategory : BaseEntity
{
    public Guid RaceId { get; set; }
    public string Name { get; set; } = string.Empty;        // e.g. "Young Birds", "Old Birds", "Section A"
    public string? Description { get; set; }
    public int SortOrder { get; set; } = 0;
    public string? EligibilityCriteria { get; set; }

    // Navigation
    public Race Race { get; set; } = null!;
    public ICollection<RaceResult> Results { get; set; } = new List<RaceResult>();
}

public class RaceResult : AuditableEntity
{
    public Guid RaceId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? UserId { get; set; }              // linked fancier (nullable: may not be linked)
    public string RingNumber { get; set; } = string.Empty;
    public string? PigeonName { get; set; }
    public string? PigeonSex { get; set; }        // M/F/U
    public int? PigeonYearOfBirth { get; set; }
    public DataIngestionType IngestionType { get; set; }

    // Timing
    public DateTime ArrivalTime { get; set; }
    public TimeSpan? FlightDuration { get; set; }

    // Distance (from club loft to release point)
    public double DistanceKm { get; set; }
    public double DistanceMeters => DistanceKm * 1000;

    // Velocity (m/min — standard pigeon racing unit)
    public double VelocityMperMin { get; set; }
    public double VelocityKmH => VelocityMperMin * 60 / 1000;

    // Ranking
    public int? ClubRank { get; set; }
    public int? CategoryRank { get; set; }
    public ResultStatus Status { get; set; } = ResultStatus.Pending;

    // Validation flags
    public bool IsDuplicate { get; set; } = false;
    public bool IsLateArrival { get; set; } = false;
    public bool HasInvalidTimestamp { get; set; } = false;
    public string? ValidationNotes { get; set; }

    // Navigation
    public Race Race { get; set; } = null!;
    public RaceCategory? Category { get; set; }
    public ApplicationUser? User { get; set; }
    public ICollection<CountryResultEntry> CountryResultEntries { get; set; } = new List<CountryResultEntry>();

    // ── Computed helpers ──────────────────────────────────────────────────────
    public string CategoryName => Category?.Name ?? "Open";
    public string? FancierName => User?.FullName;
    public Guid? PigeonId => null; // Pigeon not directly linked on RaceResult — linked via ring number
}
