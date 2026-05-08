using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PRC.AdminService.Filters;

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
