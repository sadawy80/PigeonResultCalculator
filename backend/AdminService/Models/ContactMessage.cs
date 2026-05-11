namespace PRC.AdminService.Models;

public class ContactMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Fancier | ClubManager | FederationManager | Anonymous</summary>
    public string SenderRole { get; set; } = "Anonymous";

    public Guid? UserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string? SenderPhone { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public string Status { get; set; } = "Open"; // Open | Replied | Closed
    public Guid? AssignedAdminId { get; set; }
    public string? AdminReply { get; set; }
    public DateTime? RepliedAt { get; set; }
    public Guid? RepliedByAdminId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
