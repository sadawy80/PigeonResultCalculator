using PRC.Common;

namespace PRC.ClubService.Models;

public class Club : AuditableEntity
{
    public Guid? FederationId { get; set; }
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
    public DateTime? SubscriptionExpiresAt { get; set; }

    // FederationName is cached here to avoid cross-service calls on every DTO projection
    public string? FederationName { get; set; }

    public ICollection<ClubMembership> Memberships { get; set; } = new List<ClubMembership>();
    public ClubPage? ClubPage { get; set; }
    public ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
    public ICollection<ClubProgramme> Programmes { get; set; } = new List<ClubProgramme>();
}
