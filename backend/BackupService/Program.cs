using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Minio;
using PRC.BackupService.BackgroundServices;
using PRC.BackupService.Data;
using PRC.BackupService.Services;
using PRC.Common.Consul;
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
builder.Services.AddDbContext<BackupDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── JWT ───────────────────────────────────────────────────────────────────────
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

var adminKey = builder.Configuration["Jwt:AdminKey"];
if (!string.IsNullOrEmpty(adminKey))
{
    builder.Services.AddAuthentication()
        .AddJwtBearer("Admin", opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(adminKey)),
                ValidateIssuer   = true, ValidIssuer   = builder.Configuration["Jwt:AdminIssuer"]  ?? "PRC.AdminService",
                ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:AdminAudience"] ?? "PRC.Admin",
                ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30)
            };
        });
}

builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opts =>
    opts.AddPolicy("Gateway", p => p
        .WithOrigins(
            builder.Configuration["App:GatewayUrl"] ?? "http://localhost:9500",
            builder.Configuration["App:FrontendUrl"] ?? "http://localhost:4300")
        .AllowAnyHeader().WithMethods("GET","POST","PUT","DELETE","PATCH","OPTIONS").AllowCredentials()));

// ── MinIO ─────────────────────────────────────────────────────────────────────
var minioEndpoint  = builder.Configuration["Minio:Endpoint"]  ?? "localhost:9000";
var minioAccessKey = builder.Configuration["Minio:AccessKey"] ?? "minioadmin";
var minioSecretKey = builder.Configuration["Minio:SecretKey"] ?? "minioadmin";

builder.Services.AddMinio(cfg => cfg
    .WithEndpoint(minioEndpoint)
    .WithCredentials(minioAccessKey, minioSecretKey)
    .Build());

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddCorrelationIdHandler();
builder.Services.AddHttpClient();
builder.Services.AddScoped<MinioStorageService>();
builder.Services.AddScoped<PCloudStorageService>();
builder.Services.AddScoped<BackupOrchestrator>();
builder.Services.AddHostedService<BackupJob>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PRC Backup Service", Version = "v1" });
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

builder.Services.AddConsulServiceRegistration(builder.Configuration, "backup-service", 9510);

var app = builder.Build();

app.UseCorrelationId();
app.UseSerilogRequestLogging(opts =>
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} {StatusCode} in {Elapsed:0.0000}ms");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PRC Backup Service v1");
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
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "BackupService" }));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BackupDbContext>();
    for (var attempt = 1; ; attempt++)
    {
        try { await db.Database.MigrateAsync(); break; }
        catch (Exception ex) when (attempt < 6)
        {
            Log.Warning("BackupService DB init attempt {Attempt} failed, retrying in 5s: {Error}", attempt, ex.Message);
            await Task.Delay(5000);
        }
    }
}

app.Run();
