using Microsoft.EntityFrameworkCore;
using PRC.ClubService.Data;

namespace PRC.ClubService.BackgroundServices;

public class ClubExpiryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClubExpiryJob> _logger;

    public ClubExpiryJob(IServiceScopeFactory scopeFactory, ILogger<ClubExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunAsync(stoppingToken);

            // sleep until next midnight UTC
            var now  = DateTime.UtcNow;
            var next = now.Date.AddDays(1);
            await Task.Delay(next - now, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();

            var expired = await db.Clubs
                .Where(c => !c.IsDeleted && c.IsActive
                         && c.SubscriptionExpiresAt.HasValue
                         && c.SubscriptionExpiresAt.Value < DateTime.UtcNow)
                .ToListAsync(ct);

            if (expired.Count == 0) return;

            var now = DateTime.UtcNow;
            foreach (var club in expired)
            {
                club.IsActive  = false;
                club.UpdatedAt = now;
            }

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("ClubExpiryJob: suspended {Count} expired club(s).", expired.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "ClubExpiryJob: error during expiry check.");
        }
    }
}
