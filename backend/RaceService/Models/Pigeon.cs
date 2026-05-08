using PRC.Common;

namespace PRC.RaceService.Models;

public class Pigeon : AuditableEntity
{
    public string RingNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Sex { get; set; }
    public int? YearOfBirth { get; set; }
    public string? Color { get; set; }
    public string? Strain { get; set; }
    public Guid? FederationId { get; set; }
    public string? ExternalLoftSystemId { get; set; }
    public string? PhotoUrl { get; set; }

    public ICollection<PigeonLink> Links { get; set; } = new List<PigeonLink>();
}

public class PigeonLink : BaseEntity
{
    public Guid MembershipId { get; set; }       // cross-service ref (ClubService)
    public string RingNumber { get; set; } = string.Empty;
    public Guid? PigeonId { get; set; }
    public bool IsVerified { get; set; } = false;
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    public Guid LinkedByUserId { get; set; }

    public Pigeon? Pigeon { get; set; }
}
