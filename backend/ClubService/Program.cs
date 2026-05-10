using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PRC.Common.Consul;
using PRC.ClubService.Data;
using PRC.ClubService.Events;
using PRC.ClubService.Services;
using PRC.Common.Messages;
using Prometheus;
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
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .CreateLogger();

builder.Host.UseSerilog();

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ClubDbContext>(opts =>
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
});

builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opts =>
    opts.AddPolicy("Gateway", p => p
        .WithOrigins(
            builder.Configuration["App:GatewayUrl"] ?? "http://localhost:9500",
            builder.Configuration["App:FrontendUrl"] ?? "http://localhost:4300")
        .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// ── MassTransit ───────────────────────────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<GetClubStatsConsumer>();
    x.AddConsumer<GetAllClubsConsumer>();
    x.AddConsumer<ToggleClubActiveConsumer>();
    x.AddConsumer<GetClubBrandingConsumer>();
    x.AddConsumer<GetProgrammeForRenderConsumer>();
    x.AddConsumer<GetFancierProgrammeResultsConsumer>();
    x.AddConsumer<GetPublicClubBySlugConsumer>();
    x.AddConsumer<ListPublishedClubsForPublicConsumer>();
    x.AddConsumer<GetActiveClubCountForFederationConsumer>();
    x.AddConsumer<CreateInAppNotificationConsumer>();
    x.AddConsumer<GetAdminProgrammesConsumer>();
    x.AddConsumer<GetAdminAcePigeonResultsConsumer>();
    x.AddConsumer<GetAdminSuperAceResultsConsumer>();
    x.AddConsumer<GetAdminBestLoftResultsConsumer>();
    x.AddConsumer<NotifyClubManagersConsumer>();
    x.AddConsumer<AdminDeleteProgrammeConsumer>();
    x.AddConsumer<AdminCreateClubConsumer>();
    x.AddConsumer<AdminDeleteClubConsumer>();
    x.AddConsumer<AdminAssignClubManagerConsumer>();
    x.AddConsumer<SetClubSubscriptionExpiryConsumer>();
    x.AddConsumer<GetAdminNotificationsConsumer>();
    x.AddConsumer<AdminSendNotificationConsumer>();

    x.AddRequestClient<GetRaceSnapshotRequest>();
    x.AddRequestClient<GetPublishedResultsForProgrammeRequest>();
    x.AddRequestClient<GetPigeonLookupRequest>();

    x.AddEntityFrameworkOutbox<ClubDbContext>(o =>
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
        cfg.ConfigureEndpoints(ctx);
    });
});

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddHostedService<PRC.ClubService.BackgroundServices.ClubExpiryJob>();
builder.Services.AddScoped<IClubService, ClubService>();
builder.Services.AddScoped<IProgrammeService, ProgrammeService>();
builder.Services.AddScoped<IEmailService, NoOpEmailService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IRaceServiceClient, BusRaceServiceClient>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PRC Club Service", Version = "v1" });
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

builder.Services.AddConsulServiceRegistration(builder.Configuration, "club-service", 9502);

var app = builder.Build();

app.UseMiddleware<PRC.ClubService.Middleware.CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(opts =>
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} {StatusCode} in {Elapsed:0.0000}ms");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PRC Club Service v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("Gateway");
app.UseHttpMetrics();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapMetrics("/metrics");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ClubService" }));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
    for (var attempt = 1; ; attempt++)
    {
        try { await db.Database.MigrateAsync(); break; }
        catch (Exception ex) when (attempt < 6)
        { Log.Warning("ClubService DB init attempt {Attempt} failed, retrying in 5s: {Error}", attempt, ex.Message); await Task.Delay(5000); }
    }
    await PRC.ClubService.Data.DemoSeeder.SeedAsync(db);
}

app.Run();
