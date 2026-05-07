using PigeonRacing.Domain.Common;

namespace PigeonRacing.Domain.Entities;

// ─────────────────────────────────────────────────────────────────────────────
//  ExternalLink
//  Represents one approved (or pending) link between a fancier account on
//  PigeonResultCalculator.com and their loft on PigeonLoftManager.com.
//  One fancier can have one active link per external platform.
// ─────────────────────────────────────────────────────────────────────────────

public class ExternalLink : BaseEntity
{
    // ── PRC side ─────────────────────────────────────────────────────────────

    /// The fancier whose results are being shared.
    public Guid UserId { get; set; }

    /// The club whose data the fancier belongs to (approver's club).
    public Guid ClubId { get; set; }

    // ── External platform side ────────────────────────────────────────────────

    /// Name of the external platform (e.g. "PigeonLoftManager").
    public string ExternalPlatformName { get; set; } = string.Empty;

    /// External platform's user ID for this fancier.
    public string ExternalUserId { get; set; } = string.Empty;

    /// External platform's loft ID for this fancier.
    public string ExternalLoftId { get; set; } = string.Empty;

    /// Human-readable loft name from the external platform.
    public string ExternalLoftName { get; set; } = string.Empty;

    /// URL on PigeonLoftManager that PRC calls when the request is approved/rejected.
    public string CallbackUrl { get; set; } = string.Empty;

    // ── Tokens ───────────────────────────────────────────────────────────────

    /// Shared secret sent with the callback so PLM can verify authenticity.
    /// Also shown to the fancier on PLM so they can confirm the right request.
    public string LinkToken { get; set; } = Guid.NewGuid().ToString("N");

    /// Issued on approval. PLM uses this as Bearer token for all data API calls.
    /// Null until approved.
    public string? AccessToken { get; set; }

    /// When the access token expires. Null = never expires (revoke instead).
    public DateTime? AccessTokenExpiresAt { get; set; }

    // ── Status ────────────────────────────────────────────────────────────────

    public ExternalLinkStatus Status { get; set; } = ExternalLinkStatus.Pending;
    public string? RejectionReason { get; set; }
    public string? RevokedReason { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? LastDataAccessAt { get; set; }

    /// The club manager who approved or rejected this request.
    public Guid? ReviewedByUserId { get; set; }

    // ── PLM-supplied metadata ─────────────────────────────────────────────────

    /// JSON blob with any extra metadata PLM sent with the request.
    public string? RequestMetadataJson { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────

    public ApplicationUser User { get; set; } = null!;
    public Club Club { get; set; } = null!;
    public ApplicationUser? ReviewedBy { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
//  ExternalLinkStatus enum
// ─────────────────────────────────────────────────────────────────────────────

public enum ExternalLinkStatus
{
    Pending  = 1,   // Request sent by PLM, awaiting club manager approval
    Approved = 2,   // Club manager approved; AccessToken issued to PLM
    Rejected = 3,   // Club manager rejected the request
    Revoked  = 4    // Was approved, then revoked by either side
}
