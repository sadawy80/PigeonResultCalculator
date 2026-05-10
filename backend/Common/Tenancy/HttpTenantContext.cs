using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace PRC.Common.Tenancy;

public class HttpTenantContext : ITenantContext
{
    public HttpTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        var value = httpContextAccessor.HttpContext?.User.FindFirstValue("federationId");
        TenantId = Guid.TryParse(value, out var id) ? id : null;
    }

    public Guid? TenantId { get; }
}
