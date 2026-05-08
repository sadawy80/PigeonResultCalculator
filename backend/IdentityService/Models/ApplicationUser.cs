using Microsoft.AspNetCore.Identity;
using PRC.Common;

namespace PRC.IdentityService.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public UserRole Role { get; set; }
    public Guid? FederationId { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public string PreferredTimezone { get; set; } = "UTC";
    public bool IsActive { get; set; } = true;
    public int? MaxResultsOverride { get; set; }
    public int? MaxClubsOverride { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? ExternalLoftSystemId { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
