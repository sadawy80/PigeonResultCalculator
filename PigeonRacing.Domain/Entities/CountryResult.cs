using PigeonRacing.Domain.Common;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.Domain.Entities;

/// <summary>
/// National/country-level aggregated result, derived from multiple club results.
/// </summary>
public class CountryResult : AuditableEntity
{
    public Guid CountryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CountryResultStatus Status { get; set; } = CountryResultStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public Guid? PublishedByUserId { get; set; }
    public string? Notes { get; set; }
    public int TotalEntriesCount { get; set; }
    public int TotalClubsCount { get; set; }

    // Navigation
    public Country Country { get; set; } = null!;
    public ApplicationUser? PublishedByUser { get; set; }
    public ICollection<CountryResultRace> IncludedRaces { get; set; } = new List<CountryResultRace>();
    public ICollection<CountryResultEntry> Entries { get; set; } = new List<CountryResultEntry>();
}

/// <summary>
/// Tracks which club races are included in a country result.
/// </summary>
public class CountryResultRace : BaseEntity
{
    public Guid CountryResultId { get; set; }
    public Guid RaceId { get; set; }
    public Guid ClubId { get; set; }

    // Navigation
    public CountryResult CountryResult { get; set; } = null!;
    public Race Race { get; set; } = null!;
    public Club Club { get; set; } = null!;
}

/// <summary>
/// Individual entry in a country result, derived from club RaceResult records.
/// </summary>
public class CountryResultEntry : BaseEntity
{
    public Guid CountryResultId { get; set; }
    public Guid RaceResultId { get; set; }
    public Guid ClubId { get; set; }
    public string RingNumber { get; set; } = string.Empty;
    public Guid? UserId { get; set; }

    // Normalized velocity (used for cross-club ranking)
    public double VelocityMperMin { get; set; }
    public double DistanceKm { get; set; }

    // National ranking
    public int NationalRank { get; set; }
    public int? NationalCategoryRank { get; set; }

    // Navigation
    public CountryResult CountryResult { get; set; } = null!;
    public RaceResult RaceResult { get; set; } = null!;
    public Club Club { get; set; } = null!;
    public ApplicationUser? User { get; set; }
}
