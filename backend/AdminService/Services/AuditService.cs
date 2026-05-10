using MassTransit;
using PRC.Common;
using PRC.Common.Messages;

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
    private readonly IPublishEndpoint _publish;
    private readonly IGeoIpService    _geoIp;

    public AuditService(IPublishEndpoint publish, IGeoIpService geoIp)
    {
        _publish = publish;
        _geoIp   = geoIp;
    }

    public async Task LogAsync(string action, string entityType, Guid? entityId,
        AuditSeverity severity, string? details,
        Guid? triggeredByUserId, string? triggeredByName,
        string? correlationId, string? ipAddress,
        CancellationToken ct = default)
    {
        var country = await _geoIp.GetCountryAsync(ipAddress, ct);

        await _publish.Publish(new AuditEntryEvent(
            action, entityType, entityId, severity, details,
            triggeredByUserId, triggeredByName, correlationId,
            "AdminService", ipAddress, country, DateTime.UtcNow), ct);
    }
}
