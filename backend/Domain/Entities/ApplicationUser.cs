using Microsoft.AspNetCore.Identity;
using PigeonRacing.Domain.Common;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public UserRole Role { get; set; }
    public Guid? CountryId { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public string PreferredTimezone { get; set; } = "UTC";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? ExternalLoftSystemId { get; set; }   // PigeonLoftManager integration

    // Navigation
    public Country? Country { get; set; }
    public ICollection<ClubMembership> ClubMemberships { get; set; } = new List<ClubMembership>();
    public ICollection<Invitation> SentInvitations { get; set; } = new List<Invitation>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? ReplacedByToken { get; set; }
    public string? RevokedReason { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}
