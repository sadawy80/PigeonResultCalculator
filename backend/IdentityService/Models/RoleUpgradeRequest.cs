using PRC.Common;

namespace PRC.IdentityService.Models;

public class RoleUpgradeRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public UserRole RequestedRole { get; set; }
    public Guid? FederationId { get; set; }
    public UpgradeRequestStatus Status { get; set; } = UpgradeRequestStatus.Pending;
    public string? ClubName { get; set; }
    public string? Notes { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
