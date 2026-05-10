using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PRC.Common.Consul;
using PRC.AdminService.Data;
using PRC.AdminService.Events;
using PRC.AdminService.Middleware;
using PRC.AdminService.Services;
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
    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ} [{Level:u3}] {SourceContext} {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .WriteTo.Conditional(
        _ => !string.IsNullOrEmpty(seqUrl),
        wt => wt.Seq(seqUrl ?? "http://localhost:5341", restrictedToMinimumLevel: LogEventLevel.Information))
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .CreateLogger();

builder.Host.UseSerilog();

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AdminDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── JWT Authentication — Admin key (separate from IdentityService) ────────────
var adminKey = builder.Configuration["Jwt:AdminKey"]
    ?? throw new InvalidOperationException("Jwt:AdminKey is required.");

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(adminKey)),
        ValidateIssuer   = true, ValidIssuer   = builder.Configuration["Jwt:AdminIssuer"]   ?? "PRC.AdminService",
        ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:AdminAudience"] ?? "PRC.Admin",
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
        .AllowAnyHeader().WithMethods("GET","POST","PUT","DELETE","PATCH","OPTIONS").AllowCredentials()));

// ── MassTransit — all cross-service communication goes through RabbitMQ ──────
builder.Services.AddMassTransit(x =>
{
    x.AddRequestClient<GetIdentityStatsRequest>();
    x.AddRequestClient<GetClubStatsRequest>();
    x.AddRequestClient<GetRaceStatsRequest>();
    x.AddRequestClient<GetFederationStatsRequest>();
    x.AddRequestClient<GetSubscriptionStatsRequest>();
    x.AddRequestClient<GetUsersRequest>();
    x.AddRequestClient<ValidateAdminCredentialsRequest>();
    x.AddRequestClient<ToggleUserActiveRequest>();
    x.AddRequestClient<AssignRoleRequest>();
    x.AddRequestClient<SetUserLimitsRequest>();
    x.AddRequestClient<DeleteUserRequest>();
    x.AddRequestClient<GetAllClubsRequest>();
    x.AddRequestClient<ToggleClubActiveRequest>();
    x.AddRequestClient<AdminCreateClubRequest>();
    x.AddRequestClient<AdminAssignClubManagerRequest>();
    x.AddRequestClient<AdminDeleteClubRequest>();
    x.AddRequestClient<SetClubSubscriptionExpiryRequest>();
    x.AddRequestClient<GetFederationsRequest>();
    x.AddRequestClient<CreateFederationRequest>();
    x.AddRequestClient<ToggleFederationActiveRequest>();
    x.AddRequestClient<AdminDeleteFederationRequest>();
    x.AddRequestClient<GetSubscriptionPlansRequest>();
    x.AddRequestClient<UpdateSubscriptionPlanBusRequest>();
    x.AddRequestClient<GetFederationSubscriptionsRequest>();
    x.AddRequestClient<CreateFederationSubscriptionRequest>();
    x.AddRequestClient<GetActiveSubscriptionCountRequest>();
    x.AddRequestClient<GetUpgradeRequestsRequest>();
    x.AddRequestClient<ReviewUpgradeRequestRequest>();
    x.AddRequestClient<RevokeUpgradeRequestRequest>();
    x.AddRequestClient<GetAdminRacesRequest>();
    x.AddRequestClient<AdminDeleteRaceRequest>();
    x.AddRequestClient<GetAdminProgrammesRequest>();
    x.AddRequestClient<GetAdminAcePigeonResultsRequest>();
    x.AddRequestClient<GetAdminSuperAceResultsRequest>();
    x.AddRequestClient<GetAdminBestLoftResultsRequest>();
    x.AddRequestClient<NotifyClubManagersRequest>();
    x.AddRequestClient<GetAdminPigeonsRequest>();
    x.AddRequestClient<AdminUpdatePigeonRequest>();
    x.AddRequestClient<AdminDeletePigeonRequest>();
    x.AddRequestClient<GetAdminFanciersRequest>();
    x.AddRequestClient<LinkFancierToUserRequest>();
    x.AddRequestClient<UnlinkFancierRequest>();
    x.AddRequestClient<AdminDeleteProgrammeRequest>();
    x.AddRequestClient<GetAdminExternalLinksRequest>();
    x.AddRequestClient<AdminApproveLinkBusRequest>();
    x.AddRequestClient<AdminRejectLinkBusRequest>();
    x.AddRequestClient<AdminRevokeLinkBusRequest>();
    x.AddRequestClient<GetAdminNotificationsRequest>();
    x.AddRequestClient<AdminSendNotificationBusRequest>();
    x.AddRequestClient<AdminCreateSubscriptionPlanBusRequest>();
    x.AddRequestClient<AdminDeleteSubscriptionPlanBusRequest>();
    x.AddRequestClient<GetAuditLogsRequest>();

    x.AddConsumer<UpgradeRequestSubmittedConsumer>();
    x.AddConsumer<ExternalLinkRequestedConsumer>();

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
builder.Services.AddHttpClient("GeoIp");
builder.Services.AddSingleton<IGeoIpService, GeoIpService>();
builder.Services.AddScoped<IBusAdminClient, BusAdminClient>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAdminTokenService, AdminTokenService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PRC Admin Service", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header,
        Description = "Admin JWT (issued by POST /api/admin/auth/login)"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddConsulServiceRegistration(builder.Configuration, "admin-service", 9507);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(opts =>
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} {StatusCode} in {Elapsed:0.0000}ms");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PRC Admin Service v1");
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
app.MapMetrics("/metrics");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "AdminService" }));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    for (var attempt = 1; ; attempt++)
    {
        try { await db.Database.MigrateAsync(); break; }
        catch (Exception ex) when (attempt < 6)
        { Log.Warning("AdminService DB init attempt {Attempt} failed, retrying in 5s: {Error}", attempt, ex.Message); await Task.Delay(5000); }
    }
}

app.Run();
