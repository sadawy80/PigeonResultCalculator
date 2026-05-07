using PigeonRacing.Domain.Common;

namespace PigeonRacing.Domain.Entities;

/// <summary>
/// Lightweight pigeon identity record.
/// Full loft/pedigree data lives in external PigeonLoftManager system.
/// </summary>
public class Pigeon : AuditableEntity
{
    public string RingNumber { get; set; } = string.Empty;   // National ring number (unique per country+year)
    public string? Name { get; set; }
    public string? Sex { get; set; }                         // M/F/U
    public int? YearOfBirth { get; set; }
    public string? Color { get; set; }
    public string? Strain { get; set; }
    public Guid? CountryId { get; set; }
    public string? ExternalLoftSystemId { get; set; }        // PigeonLoftManager integration ID
    public string? PhotoUrl { get; set; }

    // Navigation
    public Country? Country { get; set; }
    public ICollection<PigeonLink> Links { get; set; } = new List<PigeonLink>();
}

/// <summary>
/// Links a pigeon (by ring number) to a fancier within a club.
/// Managers create these links; fanciers can view linked pigeons.
/// </summary>
public class PigeonLink : BaseEntity
{
    public Guid MembershipId { get; set; }
    public string RingNumber { get; set; } = string.Empty;
    public Guid? PigeonId { get; set; }            // resolved pigeon record (if exists)
    public bool IsVerified { get; set; } = false;
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    public Guid LinkedByUserId { get; set; }

    // Navigation
    public ClubMembership Membership { get; set; } = null!;
    public Pigeon? Pigeon { get; set; }
    public ApplicationUser LinkedByUser { get; set; } = null!;
}
