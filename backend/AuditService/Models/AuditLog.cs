using PRC.Common;

namespace PRC.AuditService.Models;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public AuditSeverity Severity { get; set; } = AuditSeverity.Info;
    public string? Details { get; set; }
    public Guid? TriggeredByUserId { get; set; }
    public string? TriggeredByName { get; set; }
    public string? CorrelationId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Country { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
