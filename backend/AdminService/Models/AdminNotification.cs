namespace PRC.AdminService.Models;

public class AdminNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;      // UpgradeRequest | LinkRequest | System
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public string? SourceId { get; set; }                  // RequestId / LinkId for deep-link
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
