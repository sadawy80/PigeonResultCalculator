using PigeonRacing.Domain.Common;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.Domain.Entities;

public class Country : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;        // ISO 3166-1 alpha-2
    public string FlagUrl { get; set; } = string.Empty;
    public string DefaultLanguage { get; set; } = "en";
    public string DefaultTimezone { get; set; } = "UTC";
    public string DefaultDistanceUnit { get; set; } = "km"; // km or miles
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Club> Clubs { get; set; } = new List<Club>();
    public ICollection<ApplicationUser> Managers { get; set; } = new List<ApplicationUser>();
    public ICollection<CountryResult> CountryResults { get; set; } = new List<CountryResult>();
    public ICollection<CountrySubscription> Subscriptions { get; set; } = new List<CountrySubscription>();
    public CountryPage? CountryPage { get; set; }
}

public class Club : AuditableEntity
{
    public Guid CountryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Country Country { get; set; } = null!;
    public ICollection<ApplicationUser> Managers { get; set; } = new List<ApplicationUser>();
    public ICollection<ClubMembership> Memberships { get; set; } = new List<ClubMembership>();
    public ICollection<Race> Races { get; set; } = new List<Race>();
    public ICollection<ClubPage> ClubPages { get; set; } = new List<ClubPage>();
    public ClubSubscription? Subscription { get; set; }
}

public class ClubMembership : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation
    public Club Club { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public ICollection<PigeonLink> PigeonLinks { get; set; } = new List<PigeonLink>();
}
