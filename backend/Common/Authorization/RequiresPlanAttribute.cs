using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace PRC.Common.Authorization;

/// <summary>
/// Blocks the action with 402 if the calling federation has no active subscription.
/// SuperAdmins bypass the check entirely. Background/bus operations are unaffected.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequiresPlanAttribute : Attribute, IFilterFactory
{
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider services)
        => services.GetRequiredService<RequiresPlanFilter>();
}

/// <summary>
/// Registered as scoped — inject ISubscriptionChecker which does the bus call.
/// </summary>
public sealed class RequiresPlanFilter : IAsyncActionFilter
{
    private readonly ISubscriptionChecker _checker;

    public RequiresPlanFilter(ISubscriptionChecker checker) => _checker = checker;

    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        var user = ctx.HttpContext.User;

        // SuperAdmin bypasses plan check
        if (user.IsInRole("SuperAdmin")) { await next(); return; }

        var fedId = user.FindFirstValue("federationId");
        if (!Guid.TryParse(fedId, out var federationId))
        {
            ctx.Result = new ObjectResult(
                new ProblemDetails { Status = 403, Title = "Forbidden", Detail = "No federation associated with your account." })
            { StatusCode = 403 };
            return;
        }

        var active = await _checker.HasActivePlanAsync(federationId, ctx.HttpContext.RequestAborted);
        if (!active)
        {
            ctx.Result = new ObjectResult(
                new ProblemDetails { Status = 402, Title = "Subscription required", Detail = "An active subscription is required for this operation." })
            { StatusCode = 402 };
            return;
        }

        await next();
    }
}

/// <summary>
/// Abstraction so each service can wire up its own bus client.
/// </summary>
public interface ISubscriptionChecker
{
    Task<bool> HasActivePlanAsync(Guid federationId, CancellationToken ct = default);
}

/// <summary>
/// Wraps the bus request with a 5-minute memory cache to avoid per-request roundtrips.
/// Concrete implementation lives in each service that registers the bus client.
/// </summary>
public abstract class CachingSubscriptionCheckerBase : ISubscriptionChecker
{
    private readonly IMemoryCache _cache;

    protected CachingSubscriptionCheckerBase(IMemoryCache cache) => _cache = cache;

    protected abstract Task<bool> FetchFromBusAsync(Guid federationId, CancellationToken ct);

    public async Task<bool> HasActivePlanAsync(Guid federationId, CancellationToken ct = default)
    {
        var key = $"plan:{federationId}";
        if (_cache.TryGetValue(key, out bool cached)) return cached;

        var active = await FetchFromBusAsync(federationId, ct);
        _cache.Set(key, active, TimeSpan.FromMinutes(5));
        return active;
    }
}
