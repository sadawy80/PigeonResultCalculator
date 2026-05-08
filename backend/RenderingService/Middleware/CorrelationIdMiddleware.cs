using Serilog.Context;

namespace PRC.RenderingService.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var correlationId = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");
        ctx.Response.Headers["X-Correlation-Id"] = correlationId;
        using (LogContext.PushProperty("CorrelationId", correlationId))
            await _next(ctx);
    }
}
