using System.ComponentModel.DataAnnotations.Schema;
using PRC.Common;

namespace PRC.FederationService.Models;

public class FederationResult : AuditableEntity
{
    public Guid FederationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public FederationResultStatus Status { get; set; } = FederationResultStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public Guid? PublishedByUserId { get; set; }
    public string? Notes { get; set; }
    public int TotalEntriesCount { get; set; }
    public int TotalClubsCount { get; set; }

    public Federation Federation { get; set; } = null!;
    public ICollection<FederationResultRace> IncludedRaces { get; set; } = new List<FederationResultRace>();
    public ICollection<FederationResultEntry> Entries { get; set; } = new List<FederationResultEntry>();
}

// Cross-service refs: RaceId and ClubId are plain Guids — no EF FK constraints
public class FederationResultRace : BaseEntity
{
    public Guid FederationResultId { get; set; }
    public Guid RaceId { get; set; }
    public Guid ClubId { get; set; }

    public FederationResult FederationResult { get; set; } = null!;
}

public class FederationResultEntry : BaseEntity
{
    public Guid FederationResultId { get; set; }
    public Guid RaceResultId { get; set; }
    public Guid ClubId { get; set; }
    public string RingNumber { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? UserFullName { get; set; }
    public string? ClubName { get; set; }
    [Column("VelocityMperMin")]
    public double SpeedMperMin { get; set; }
    public double DistanceKm { get; set; }
    public int NationalRank { get; set; }
    public int? NationalCategoryRank { get; set; }

    public FederationResult FederationResult { get; set; } = null!;
}
