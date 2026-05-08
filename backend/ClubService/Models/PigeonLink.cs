using PRC.Common;

namespace PRC.ClubService.Models;

public class PigeonLink : BaseEntity
{
    public Guid MembershipId { get; set; }
    public string RingNumber { get; set; } = string.Empty;
    public Guid? PigeonId { get; set; }
    public bool IsVerified { get; set; } = false;
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    public Guid LinkedByUserId { get; set; }

    public ClubMembership Membership { get; set; } = null!;
}
