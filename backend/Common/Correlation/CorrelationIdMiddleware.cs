using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace PRC.Common.Correlation;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemsKey = "CorrelationId";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var correlationId = ctx.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        ctx.Items[ItemsKey] = correlationId;
        ctx.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty(ItemsKey, correlationId))
        {
            await _next(ctx);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
