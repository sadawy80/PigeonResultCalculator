using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PRC.Common.Consul;
using PRC.Common.Correlation;
using PRC.Common.Messages;
using PRC.IdentityService.Data;
using PRC.IdentityService.Events;
using PRC.IdentityService.Models;
using PRC.IdentityService.Services;
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
builder.Services.AddDbContext<IdentityDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── ASP.NET Identity ──────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(opts =>
{
    opts.Password.RequiredLength = 8;
    opts.Password.RequireNonAlphanumeric = false;
    opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    opts.Lockout.MaxFailedAccessAttempts = 5;
    opts.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<IdentityDbContext>()
.AddDefaultTokenProviders();

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
            builder.Configuration["App:GatewayUrl"]   ?? "http://localhost:9500",
            builder.Configuration["App:FrontendUrl"] ?? "http://localhost:4300")
        .AllowAnyHeader().WithMethods("GET","POST","PUT","DELETE","PATCH","OPTIONS").AllowCredentials()));

// ── MassTransit ───────────────────────────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<GetIdentityStatsConsumer>();
    x.AddConsumer<ValidateAdminCredentialsConsumer>();
    x.AddConsumer<GetUsersConsumer>();
    x.AddConsumer<ToggleUserActiveConsumer>();
    x.AddConsumer<AssignRoleConsumer>();
    x.AddConsumer<SetUserLimitsConsumer>();
    x.AddConsumer<GetUserNamesConsumer>();
    x.AddConsumer<GetUserEmailsConsumer>();
    x.AddConsumer<GetUpgradeRequestsConsumer>();
    x.AddConsumer<ReviewUpgradeRequestConsumer>();
    x.AddConsumer<RevokeUpgradeRequestConsumer>();
    x.AddConsumer<DeleteUserConsumer>();

    x.AddRequestClient<GetFederationSubscriptionLimitsRequest>();
    x.AddRequestClient<GetActiveClubCountForFederationRequest>();

    x.AddEntityFrameworkOutbox<IdentityDbContext>(o =>
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
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, BusEmailService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddCorrelationIdHandler();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PRC Identity Service", Version = "v1" });
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

builder.Services.AddConsulServiceRegistration(builder.Configuration, "identity-service", 9501);

var app = builder.Build();

app.UseCorrelationId();
app.UseSerilogRequestLogging(opts =>
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} {StatusCode} in {Elapsed:0.0000}ms");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PRC Identity v1");
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
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "IdentityService" }));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    for (var attempt = 1; ; attempt++)
    {
        try { await db.Database.MigrateAsync(); break; }
        catch (Exception ex) when (attempt < 6)
        { Log.Warning("IdentityService DB init attempt {Attempt} failed, retrying in 5s: {Error}", attempt, ex.Message); await Task.Delay(5000); }
    }
    await AdminSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

app.Run();
