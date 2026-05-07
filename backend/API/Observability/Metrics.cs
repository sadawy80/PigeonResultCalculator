using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

namespace PigeonRacing.API.Observability;

// ─────────────────────────────────────────────────────────────────────────────
//  PigeonRacingMetrics
//  All custom business + infrastructure counters and histograms.
//  Registered as singleton; injected wherever measurements are taken.
// ─────────────────────────────────────────────────────────────────────────────

public sealed class PigeonRacingMetrics
{
    // ── HTTP / ASP.NET ────────────────────────────────────────────────────────
    // (standard dotnet Prometheus library provides http_requests_received_total
    //  and http_request_duration_seconds automatically via UseHttpMetrics())

    // ── Auth ──────────────────────────────────────────────────────────────────
    public readonly Counter AuthFailures = Metrics
        .CreateCounter("pigeon_auth_failures_total",
            "Number of failed authentication attempts",
            new CounterConfiguration { LabelNames = ["reason"] });

    public readonly Counter Logins = Metrics
        .CreateCounter("pigeon_logins_total",
            "Number of successful logins");

    public readonly Counter TokenRefreshes = Metrics
        .CreateCounter("pigeon_token_refreshes_total",
            "Number of JWT token refresh operations");

    public readonly Gauge ActiveSessions = Metrics
        .CreateGauge("pigeon_active_sessions",
            "Estimated number of active user sessions (token refresh activity)");

    // ── Races ─────────────────────────────────────────────────────────────────
    public readonly Counter RacesCreated = Metrics
        .CreateCounter("pigeon_races_created_total",
            "Number of races created");

    public readonly Counter RacesPublished = Metrics
        .CreateCounter("pigeon_races_published_total",
            "Number of races published");

    public readonly Counter RacePublishFailures = Metrics
        .CreateCounter("pigeon_race_publish_failures_total",
            "Number of race publish operations that failed");

    public readonly Gauge LiveRacesActive = Metrics
        .CreateGauge("pigeon_live_races_active",
            "Number of currently live (in-progress) races");

    // ── Results ───────────────────────────────────────────────────────────────
    public readonly Counter ResultsIngested = Metrics
        .CreateCounter("pigeon_results_ingested_total",
            "Number of race results ingested",
            new CounterConfiguration { LabelNames = ["source"] }); // ets | manual

    public readonly Counter ETSParseTotal = Metrics
        .CreateCounter("pigeon_ets_parse_total",
            "Number of ETS files successfully parsed");

    public readonly Counter ETSParseFailures = Metrics
        .CreateCounter("pigeon_ets_parse_failures_total",
            "Number of ETS file parse failures");

    public readonly Histogram ETSParseDuration = Metrics
        .CreateHistogram("pigeon_ets_parse_duration_seconds",
            "Duration of ETS file parsing in seconds",
            new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.05, 2, 10) });

    public readonly Histogram ETSParseRowCount = Metrics
        .CreateHistogram("pigeon_ets_parse_rows",
            "Number of rows parsed per ETS file",
            new HistogramConfiguration { Buckets = new double[] { 10, 50, 100, 200, 500, 1000, 2000, 5000 } });

    // ── Programmes ────────────────────────────────────────────────────────────
    public readonly Counter ProgrammeCalculationsTotal = Metrics
        .CreateCounter("pigeon_programme_calculations_total",
            "Number of programme result calculations triggered");

    public readonly Counter ProgrammeCalculationErrors = Metrics
        .CreateCounter("pigeon_programme_calculation_errors_total",
            "Number of programme calculations that failed");

    public readonly Histogram ProgrammeCalculationDuration = Metrics
        .CreateHistogram("pigeon_programme_calculation_duration_seconds",
            "Duration of programme calculation in seconds",
            new HistogramConfiguration
            {
                Buckets = new double[] { 0.1, 0.5, 1, 2, 5, 10, 30, 60, 120 },
                LabelNames = ["scoring_method"]
            });

    // ── Users & Club ──────────────────────────────────────────────────────────
    public readonly Counter InvitationsSent = Metrics
        .CreateCounter("pigeon_invitations_sent_total",
            "Number of member invitations sent");

    public readonly Counter NotificationsSent = Metrics
        .CreateCounter("pigeon_notifications_sent_total",
            "Number of in-app notifications sent",
            new CounterConfiguration { LabelNames = ["type"] });

    // ── Database ──────────────────────────────────────────────────────────────
    public readonly Histogram DBQueryDuration = Metrics
        .CreateHistogram("pigeon_db_query_duration_seconds",
            "Duration of database queries",
            new HistogramConfiguration
            {
                Buckets = new double[] { 0.001, 0.005, 0.01, 0.05, 0.1, 0.25, 0.5, 1, 2, 5 },
                LabelNames = ["operation"]
            });

    public readonly Gauge DBConnectionPoolSize = Metrics
        .CreateGauge("pigeon_db_connection_pool_size",
            "Total DB connection pool size");

    public readonly Gauge DBConnectionPoolAvailable = Metrics
        .CreateGauge("pigeon_db_connection_pool_available",
            "Available DB connections in the pool");

    // ── SignalR ───────────────────────────────────────────────────────────────
    public readonly Gauge SignalRConnectionsActive = Metrics
        .CreateGauge("pigeon_signalr_connections_active",
            "Number of active SignalR connections");

    public readonly Counter SignalRMessagesTotal = Metrics
        .CreateCounter("pigeon_signalr_messages_total",
            "Total SignalR messages broadcast",
            new CounterConfiguration { LabelNames = ["event"] });
}

// ─────────────────────────────────────────────────────────────────────────────
//  MetricsMiddleware
//  Records high-level request metrics not captured by the default middleware.
// ─────────────────────────────────────────────────────────────────────────────

public class MetricsMiddleware
{
    private readonly RequestDelegate _next;

    public MetricsMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Health checks configuration
// ─────────────────────────────────────────────────────────────────────────────

public static class HealthCheckConfiguration
{
    public static IServiceCollection AddPigeonHealthChecks(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddHealthChecks()
            .AddSqlServer(
                config.GetConnectionString("DefaultConnection")!,
                name: "sqlserver",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "sql"])
            .AddRedis(
                config.GetConnectionString("Redis") ?? "localhost:6379",
                name: "redis",
                failureStatus: HealthStatus.Degraded,
                tags: ["cache", "redis"])
            .AddCheck("api-self", () => HealthCheckResult.Healthy("API is running"),
                tags: ["self"]);

        return services;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Observability extension — wires everything into Program.cs
// ─────────────────────────────────────────────────────────────────────────────

public static class ObservabilityExtensions
{
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration config)
    {
        // Register metrics singleton
        services.AddSingleton<PigeonRacingMetrics>();

        // Health checks
        services.AddPigeonHealthChecks(config);

        return services;
    }

    public static IApplicationBuilder UseObservability(this IApplicationBuilder app)
    {
        // Prometheus HTTP metrics middleware (records http_requests_received_total etc.)
        app.UseHttpMetrics(options =>
        {
            options.AddCustomLabel("route", context =>
                context.GetEndpoint()?.Metadata?.GetMetadata<Microsoft.AspNetCore.Routing.RouteNameMetadata>()?.RouteName
                ?? context.Request.Path.Value
                ?? "unknown");
        });

        app.UseMiddleware<MetricsMiddleware>();

        return app;
    }

    public static IEndpointRouteBuilder MapObservability(this IEndpointRouteBuilder endpoints)
    {
        // Prometheus scrape endpoint
        endpoints.MapMetrics("/metrics");

        // Health check endpoints
        endpoints.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = async (ctx, report) =>
            {
                ctx.Response.ContentType = "application/json";
                var result = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.TotalMilliseconds
                    }),
                    totalDuration = report.TotalDuration.TotalMilliseconds
                };
                await ctx.Response.WriteAsJsonAsync(result);
            }
        });

        endpoints.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("db")
        });

        endpoints.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("self")
        });

        return endpoints;
    }
}
