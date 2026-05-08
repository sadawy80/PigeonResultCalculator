using PRC.Common;

namespace PRC.FederationService.Models;

/// Local cache of published race metadata from RaceService (populated via event bus).
public class RaceSnapshotCache : BaseEntity
{
    public Guid RaceId    { get; set; }
    public Guid ClubId    { get; set; }
    public Guid? FederationId { get; set; }
    public string ClubName  { get; set; } = string.Empty;
    public string RaceName  { get; set; } = string.Empty;
    public RaceStatus Status { get; set; }
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RaceResultSnapshotCache> Results { get; set; } = new List<RaceResultSnapshotCache>();
}

/// Local cache of published race results from RaceService.
public class RaceResultSnapshotCache : BaseEntity
{
    public Guid RaceSnapshotCacheId { get; set; }
    public Guid ResultId    { get; set; }
    public Guid ClubId      { get; set; }
    public string ClubName  { get; set; } = string.Empty;
    public string RingNumber{ get; set; } = string.Empty;
    public Guid? UserId     { get; set; }
    public string? UserFullName { get; set; }
    public double SpeedMperMin  { get; set; }
    public double DistanceKm    { get; set; }
    public DateTime? ArrivalTime { get; set; }

    public RaceSnapshotCache RaceSnapshot { get; set; } = null!;
}
