using PRC.AdminService.Data;
using PRC.AdminService.Models;
using PRC.Common;

namespace PRC.AdminService.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entityType, Guid? entityId,
        AuditSeverity severity, string? details,
        Guid? triggeredByUserId, string? triggeredByName,
        string? correlationId, string? ipAddress,
        CancellationToken ct = default);
}

public class AuditService : IAuditService
{
    private readonly AdminDbContext _db;

    public AuditService(AdminDbContext db) => _db = db;

    public async Task LogAsync(string action, string entityType, Guid? entityId,
        AuditSeverity severity, string? details,
        Guid? triggeredByUserId, string? triggeredByName,
        string? correlationId, string? ipAddress,
        CancellationToken ct = default)
    {
        _db.AuditEvents.Add(new AuditEvent
        {
            Action              = action,
            EntityType          = entityType,
            EntityId            = entityId,
            Severity            = severity,
            Details             = details,
            TriggeredByUserId   = triggeredByUserId,
            TriggeredByName     = triggeredByName,
            CorrelationId       = correlationId,
            ServiceName         = "AdminService",
            IpAddress           = ipAddress
        });
        await _db.SaveChangesAsync(ct);
    }
}
