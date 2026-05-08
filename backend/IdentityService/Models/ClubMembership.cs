using PRC.Common;

namespace PRC.IdentityService.Models;

// Minimal local copy — owned by ClubService, synced here via events for JWT claim resolution.
public class ClubMembership : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid UserId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
