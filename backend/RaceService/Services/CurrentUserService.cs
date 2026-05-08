using System.Security.Claims;

namespace PRC.RaceService.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserService(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? User => _http.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(
        User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    public Guid? FederationId => Guid.TryParse(
        User?.FindFirstValue("FederationId"), out var id) ? id : null;

    public Guid? ClubId => Guid.TryParse(
        User?.FindFirstValue("clubId"), out var id) ? id : null;
}
