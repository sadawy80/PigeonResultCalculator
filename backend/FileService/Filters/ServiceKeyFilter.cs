using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PRC.FileService.Filters;

/// <summary>
/// Gate for internal-only endpoints — checks for the shared
/// <c>X-Service-Key</c> header matching <c>ServiceConfig:InternalApiKey</c>.
/// Same pattern other PRC services use for service-to-service calls.
/// </summary>
public class ServiceKeyFilter : IActionFilter
{
    private readonly string _key;

    public ServiceKeyFilter(IConfiguration config) =>
        _key = config["ServiceConfig:InternalApiKey"] ?? "";

    public void OnActionExecuting(ActionExecutingContext ctx)
    {
        if (!ctx.HttpContext.Request.Headers.TryGetValue("X-Service-Key", out var key) || key != _key)
            ctx.Result = new UnauthorizedResult();
    }

    public void OnActionExecuted(ActionExecutedContext ctx) { }
}
