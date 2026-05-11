using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PRC.Common.Authorization;
using PRC.Common.Consul;
using PRC.Common.Correlation;
using PRC.Common.Tenancy;
using PRC.RaceService.Data;
using PRC.Common.Messages;
using PRC.RaceService.Events;
using PRC.RaceService.Hubs;
using PRC.RaceService.Services;
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
    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
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
builder.Services.AddDbContext<RaceDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required.");

builder.Services.AddAuthentication(opts =>
{
    opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opts =>
{
    opts.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer   = true, ValidIssuer   = builder.Configuration["Jwt:Issuer"]   ?? "PRC",
        ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"] ?? "PRC",
        ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30)
    };
    opts.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            var token = ctx.Request.Query["access_token"];
            var path = ctx.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/hubs/live-race"))
                ctx.Token = token;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opts =>
    opts.AddPolicy("Gateway", p => p
        .WithOrigins(
            builder.Configuration["App:GatewayUrl"] ?? "http://localhost:9500",
            builder.Configuration["App:FrontendUrl"] ?? "http://localhost:4300")
        .AllowAnyHeader().WithMethods("GET","POST","PUT","DELETE","PATCH","OPTIONS").AllowCredentials()));

// ── MassTransit ───────────────────────────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<GetRaceStatsConsumer>();
    x.AddConsumer<GetRaceForRenderConsumer>();
    x.AddConsumer<GetRaceResultForRenderConsumer>();
    x.AddConsumer<GetFancierRaceResultsConsumer>();
    x.AddConsumer<GetRaceSnapshotConsumer>();
    x.AddConsumer<GetPublishedResultsForProgrammeConsumer>();
    x.AddConsumer<GetPigeonLookupConsumer>();
    x.AddConsumer<GetPublishedRacesForPublicConsumer>();
    x.AddConsumer<GetAdminRacesConsumer>();
    x.AddConsumer<AdminDeleteRaceConsumer>();
    x.AddConsumer<GetAdminPigeonsConsumer>();
    x.AddConsumer<AdminUpdatePigeonConsumer>();
    x.AddConsumer<AdminDeletePigeonConsumer>();
    x.AddConsumer<GetAdminFanciersConsumer>();
    x.AddConsumer<LinkFancierToUserConsumer>();
    x.AddConsumer<UnlinkFancierConsumer>();

    x.AddRequestClient<CheckResultLimitRequest>();
    x.AddRequestClient<IncrementResultUsageRequest>();
    x.AddRequestClient<GetFederationSubscriptionLimitsRequest>();

    x.AddEntityFrameworkOutbox<RaceDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();
    });

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

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IRaceService, RaceService>();
builder.Services.AddScoped<IResultService, ResultService>();
builder.Services.AddScoped<ISpeedCalculator, SpeedCalculator>();
builder.Services.AddScoped<IETSFileParser, ETSFileParser>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ISubscriptionChecker, RaceSubscriptionChecker>();
builder.Services.AddScoped<RequiresPlanFilter>();

builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCorrelationIdHandler();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PRC Race Service", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddConsulServiceRegistration(builder.Configuration, "race-service", 9503);

var app = builder.Build();

app.UseCorrelationId();
app.UseSerilogRequestLogging(opts =>
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} {StatusCode} in {Elapsed:0.0000}ms");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PRC Race Service v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("Gateway");
app.UseHttpMetrics();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<LiveRaceHub>("/hubs/live-race");
app.MapMetrics("/metrics");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "RaceService" }));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDbContext>();
    for (var attempt = 1; ; attempt++)
    {
        try { await db.Database.MigrateAsync(); break; }
        catch (Exception ex) when (attempt < 6)
        { Log.Warning("RaceService DB init attempt {Attempt} failed, retrying in 5s: {Error}", attempt, ex.Message); await Task.Delay(5000); }
    }
    await PRC.RaceService.Data.DemoSeeder.SeedAsync(db);
}

app.Run();
