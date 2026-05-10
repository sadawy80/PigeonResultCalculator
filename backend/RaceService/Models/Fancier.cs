namespace PRC.RaceService.Models;

public class Fancier
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    public Guid ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public Guid? FederationId { get; set; }
    public string? FederationName { get; set; }
    public string? Country { get; set; }

    public Guid? LinkedUserId { get; set; }
    public string? LinkedUserName { get; set; }
    public string? LinkedUserEmail { get; set; }
    public DateTime? LinkedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
