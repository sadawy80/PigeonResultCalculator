using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.AuditService.Data;
using PRC.AuditService.Events;
using PRC.Common.Correlation;
using Prometheus;
using PRC.Common.Logging;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

var seqUrl = builder.Configuration["Serilog:SeqUrl"];

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ} [{Level:u3}] {SourceContext} {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .WriteTo.Conditional(
        _ => !string.IsNullOrEmpty(seqUrl),
        wt => wt.Seq(seqUrl ?? "http://localhost:5341", restrictedToMinimumLevel: LogEventLevel.Information))
    .Destructure.With<PiiDestructuringPolicy>()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .CreateLogger();

builder.Host.UseSerilog();

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AuditDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── MassTransit ───────────────────────────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AuditEntryConsumer>();
    x.AddConsumer<GetAuditLogsConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/",
            h =>
            {
                h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
                h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
            });
        cfg.UseCorrelationIdFilters(ctx);
        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddCorrelationIdHandler();

var app = builder.Build();

app.UseCorrelationId();
app.UseSerilogRequestLogging(opts =>
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} {StatusCode} in {Elapsed:0.0000}ms");

app.UseHttpMetrics();
app.MapMetrics("/metrics");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "AuditService" }));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    for (var attempt = 1; ; attempt++)
    {
        try { await db.Database.MigrateAsync(); break; }
        catch (Exception ex) when (attempt < 6)
        {
            Log.Warning(ex, "DB migration attempt {Attempt} failed, retrying in 5s", attempt);
            await Task.Delay(5000);
        }
    }
}

app.Run();
