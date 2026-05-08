using System.Security.Claims;

namespace PRC.FederationService.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _ctx;

    public CurrentUserService(IHttpContextAccessor ctx) => _ctx = ctx;

    public Guid? UserId
    {
        get
        {
            var val = _ctx.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }

    public string? Role => _ctx.HttpContext?.User.FindFirstValue(ClaimTypes.Role);

    public Guid? FederationId
    {
        get
        {
            var val = _ctx.HttpContext?.User.FindFirstValue("federationId");
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }
}
