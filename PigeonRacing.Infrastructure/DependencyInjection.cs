using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PigeonRacing.Application.Common.Interfaces;
using PigeonRacing.Domain.Entities;
using PigeonRacing.Infrastructure.Persistence;
using PigeonRacing.Infrastructure.Services;
using StackExchange.Redis;

namespace PigeonRacing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ─────────────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql =>
                {
                    sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    sql.EnableRetryOnFailure(maxRetryCount: 3);
                    sql.CommandTimeout(60);
                }));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // ── Identity ─────────────────────────────────────────────────────────
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // ── Redis cache ──────────────────────────────────────────────────────
        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConn))
        {
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(redisConn));
            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddScoped<ICacheService, MemoryCacheService>();
        }

        // ── App services ─────────────────────────────────────────────────────
        services.AddScoped<IVelocityCalculator, VelocityCalculator>();
        services.AddScoped<IETSFileParser, ETSFileParser>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<PigeonRacing.Application.Features.Integration.IExternalPlatformCallbackService,
                           ExternalPlatformCallbackService>();

        // ── HttpClient for external platform callbacks ─────────────────────
        services.AddHttpClient("PlatformCallback", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Add("User-Agent", "PigeonResultCalculator/1.0");
        });

        // ── Template seeder (idempotent — runs on startup) ───────────────────
        services.AddHostedService<TemplateSeedHostedService>();

        return services;
    }
}

/// <summary>
/// Seeds the 160 print templates once on startup via the hosted service pattern.
/// </summary>
public class TemplateSeedHostedService : IHostedService
{
    private readonly IServiceProvider _services;

    public TemplateSeedHostedService(IServiceProvider services) => _services = services;

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PigeonRacing.Infrastructure.Persistence.AppDbContext>();
        await PigeonRacing.Infrastructure.Templates.TemplateSeeder.SeedAsync(db);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
