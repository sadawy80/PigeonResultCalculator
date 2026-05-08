using PRC.Common;

namespace PRC.ClubService.Models;

public class ClubMembership : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid UserId { get; set; }
    public string? UserFullName { get; set; }
    public string? UserEmail { get; set; }
    public UserRole UserRole { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public Club Club { get; set; } = null!;
    public ICollection<PigeonLink> PigeonLinks { get; set; } = new List<PigeonLink>();
}
