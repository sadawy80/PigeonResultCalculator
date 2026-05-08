using PRC.Common;

namespace PRC.IntegrationService.Models;

public class ExternalLink : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid ClubId { get; set; }

    public string ExternalPlatformName { get; set; } = string.Empty;
    public string ExternalUserId { get; set; } = string.Empty;
    public string ExternalLoftId { get; set; } = string.Empty;
    public string ExternalLoftName { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;

    public string LinkToken { get; set; } = Guid.NewGuid().ToString("N");
    public string? AccessToken { get; set; }
    public DateTime? AccessTokenExpiresAt { get; set; }

    public ExternalLinkStatus Status { get; set; } = ExternalLinkStatus.Pending;
    public string? RejectionReason { get; set; }
    public string? RevokedReason { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? LastDataAccessAt { get; set; }

    public Guid? ReviewedByUserId { get; set; }
    public string? RequestMetadataJson { get; set; }
}
