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
    private readonly IGeoIpService  _geoIp;

    public AuditService(AdminDbContext db, IGeoIpService geoIp)
    {
        _db    = db;
        _geoIp = geoIp;
    }

    public async Task LogAsync(string action, string entityType, Guid? entityId,
        AuditSeverity severity, string? details,
        Guid? triggeredByUserId, string? triggeredByName,
        string? correlationId, string? ipAddress,
        CancellationToken ct = default)
    {
        var country = await _geoIp.GetCountryAsync(ipAddress, ct);

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
            IpAddress           = ipAddress,
            Country             = country
        });
        await _db.SaveChangesAsync(ct);
    }
}
