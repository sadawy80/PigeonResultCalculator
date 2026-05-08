using PRC.Common;

namespace PRC.IdentityService.Models;

// Minimal invitation record — full ownership belongs to ClubService.
// IdentityService keeps this table to validate invitation tokens at registration time.
public class Invitation : BaseEntity
{
    public Guid InvitedByUserId { get; set; }
    public Guid ClubId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = Guid.NewGuid().ToString("N");
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
    public DateTime? AcceptedAt { get; set; }
    public Guid? AcceptedByUserId { get; set; }
}
