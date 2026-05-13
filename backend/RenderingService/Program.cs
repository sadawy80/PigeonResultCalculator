using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using MassTransit;
using Prometheus;
using PRC.Common.Logging;
using Serilog;
using System.Text;
using PRC.Common.Consul;
using PRC.Common.Correlation;
using PRC.Common.Messages;
using PRC.RenderingService.Data;
using PRC.RenderingService.Services;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Destructure.With<PiiDestructuringPolicy>()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341")
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddDbContext<RenderingDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var key = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuer           = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidateAudience         = true,
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.FromSeconds(30)
        };
    });

// Also accept AdminService tokens so SuperAdmin can render result PDFs from the admin pages.
var adminKey = builder.Configuration["Jwt:AdminKey"];
if (!string.IsNullOrEmpty(adminKey))
{
    builder.Services.AddAuthentication()
        .AddJwtBearer("Admin", opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(adminKey)),
                ValidateIssuer           = true,
                ValidIssuer              = builder.Configuration["Jwt:AdminIssuer"]  ?? "PRC.AdminService",
                ValidateAudience         = true,
                ValidAudience            = builder.Configuration["Jwt:AdminAudience"] ?? "PRC.Admin",
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.FromSeconds(30)
            };
        });
}

builder.Services.AddAuthorization(opt =>
{
    // Default policy accepts BOTH user and admin schemes — print endpoints are
    // [Authorize] without a specific scheme, so listing them here is what lets
    // SuperAdmin tokens through alongside ordinary user tokens.
    var schemes = string.IsNullOrEmpty(adminKey)
        ? new[] { JwtBearerDefaults.AuthenticationScheme }
        : new[] { JwtBearerDefaults.AuthenticationScheme, "Admin" };
    opt.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(schemes)
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddRequestClient<GetRaceForRenderRequest>();
    x.AddRequestClient<GetRaceResultForRenderRequest>();
    x.AddRequestClient<GetClubBrandingRequest>();
    x.AddRequestClient<GetUserNamesRequest>();
    x.AddRequestClient<GetProgrammeForRenderRequest>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });
        cfg.UseCorrelationIdFilters(ctx);
        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddCorrelationIdHandler();

builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IRenderService, RenderService>();
builder.Services.AddSingleton<IPdfGeneratorService, PdfGeneratorService>();
builder.Services.AddHostedService<PrintJobProcessorService>();

// File-based renderers for the certificate + result packages.
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IPuppeteerBrowserHost, PuppeteerBrowserHost>();
builder.Services.AddSingleton<ICertRenderer, CertRenderer>();
builder.Services.AddSingleton<IResultRenderer, ResultRenderer>();
builder.Services.AddSingleton<IResultExcelExporter, ResultExcelExporter>();
builder.Services.AddScoped<IPrintOrchestrator, PrintOrchestrator>();
builder.Services.AddHostedService<FontBootstrapService>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddConsulServiceRegistration(builder.Configuration, "rendering-service", 9505);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RenderingDbContext>();
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            var creator = db.Database.GetInfrastructure()
                .GetRequiredService<IRelationalDatabaseCreator>();
            try { await creator.CreateTablesAsync(); } catch { /* tables already exist */ }
            break;
        }
        catch (Exception ex) when (attempt < 6)
        { Log.Warning("RenderingService DB init attempt {Attempt} failed, retrying in 5s: {Error}", attempt, ex.Message); await Task.Delay(5000); }
    }
    await TemplateSeeder.SeedAsync(db);
}

app.UseCorrelationId();
app.UseStaticFiles();   // serves wwwroot/fonts/* used by the cert / result templates
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpMetrics();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapMetrics("/metrics");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "RenderingService" }));

await app.RunAsync();
