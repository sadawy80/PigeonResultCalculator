using PRC.Common;

namespace PRC.FederationService.Models;

public class Federation : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string FlagUrl { get; set; } = string.Empty;
    public string DefaultLanguage { get; set; } = "en";
    public string DefaultTimezone { get; set; } = "UTC";
    public string DefaultDistanceUnit { get; set; } = "km";
    public bool IsActive { get; set; } = true;

    // Cached from IdentityService when a FederationManager is assigned
    public string? ManagerEmail { get; set; }
    public string? ManagerName { get; set; }

    public FederationPage? FederationPage { get; set; }
    public ICollection<FederationResult> FederationResults { get; set; } = new List<FederationResult>();
}
