using PRC.Common;

namespace PRC.IdentityService.Models;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? ReplacedByToken { get; set; }
    public string? RevokedReason { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
