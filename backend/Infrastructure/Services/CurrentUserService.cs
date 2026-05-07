using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PigeonRacing.Application.Common.Interfaces;

namespace PigeonRacing.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var id = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return id != null ? Guid.Parse(id) : null;
        }
    }

    public string? UserName => User?.FindFirstValue(ClaimTypes.Name);
    public string? Email => User?.FindFirstValue(ClaimTypes.Email);
    public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    public Guid? CountryId
    {
        get
        {
            var id = User?.FindFirstValue("countryId");
            return id != null ? Guid.Parse(id) : null;
        }
    }

    public Guid? ClubId
    {
        get
        {
            var id = User?.FindFirstValue("clubId");
            return id != null ? Guid.Parse(id) : null;
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
}
