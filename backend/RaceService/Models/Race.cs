using PRC.Common;

namespace PRC.RaceService.Models;

public class Race : AuditableEntity
{
    public Guid ClubId { get; set; }
    public Guid? FederationId { get; set; }                  // cached from club's federation
    public Guid? ProgrammeId { get; set; }                   // cross-service ref to ClubService programme
    public string? ProgrammeName { get; set; }               // cached
    public string ClubName { get; set; } = string.Empty;    // cached from ClubService
    public double? ClubLatitude { get; set; }               // cached for speed calculation
    public double? ClubLongitude { get; set; }              // cached for speed calculation
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RaceStatus Status { get; set; } = RaceStatus.Draft;

    public string ReleaseLocation { get; set; } = string.Empty;
    public double ReleaseLongitude { get; set; }
    public double ReleaseLatitude { get; set; }
    public DateTime? ScheduledReleaseTime { get; set; }
    public DateTime? ActualReleaseTime { get; set; }

    public double? WindSpeedKmh { get; set; }
    public WindDirection? WindDirection { get; set; }
    public double? TemperatureCelsius { get; set; }
    public string? WeatherNotes { get; set; }

    public int? TotalPigeonsEntered { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsLiveTracking { get; set; } = false;
    public double? NominatedDistanceKm { get; set; }

    public ICollection<RaceCategory> Categories { get; set; } = new List<RaceCategory>();
    public ICollection<RaceResult> Results { get; set; } = new List<RaceResult>();
    public ICollection<DataIngestionLog> IngestionLogs { get; set; } = new List<DataIngestionLog>();
}

public class RaceCategory : BaseEntity
{
    public Guid RaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; } = 0;
    public string? EligibilityCriteria { get; set; }

    public Race Race { get; set; } = null!;
    public ICollection<RaceResult> Results { get; set; } = new List<RaceResult>();
}

public class RaceResult : AuditableEntity
{
    public Guid RaceId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? FancierId { get; set; }
    public string? FancierName { get; set; }
    public string RingNumber { get; set; } = string.Empty;
    public string? PigeonName { get; set; }
    public string? PigeonSex { get; set; }
    public int? PigeonYearOfBirth { get; set; }
    public DataIngestionType IngestionType { get; set; }

    public DateTime ArrivalTime { get; set; }
    public TimeSpan? FlightDuration { get; set; }
    public double DistanceKm { get; set; }

    public double SpeedMperMin { get; set; }
    public double SpeedKmH => SpeedMperMin * 60 / 1000;

    public int? ClubRank { get; set; }
    public int? CategoryRank { get; set; }
    public ResultStatus Status { get; set; } = ResultStatus.Pending;

    public bool IsDuplicate { get; set; } = false;
    public bool IsLateArrival { get; set; } = false;
    public bool HasInvalidTimestamp { get; set; } = false;
    public string? ValidationNotes { get; set; }

    // Cached pigeon info (from Pigeon table or ETS file metadata)
    public Guid? PigeonId { get; set; }

    public Race Race { get; set; } = null!;
    public RaceCategory? Category { get; set; }
}

public class DataIngestionLog : BaseEntity
{
    public Guid RaceId { get; set; }
    public DataIngestionType IngestionType { get; set; }
    public string? FileName { get; set; }
    public int TotalRowsRead { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }
    public int DuplicateRows { get; set; }
    public string? ErrorSummary { get; set; }
    public string? RawFileUrl { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public Guid ProcessedByUserId { get; set; }
    public bool IsSuccess { get; set; }

    public Race Race { get; set; } = null!;
}
