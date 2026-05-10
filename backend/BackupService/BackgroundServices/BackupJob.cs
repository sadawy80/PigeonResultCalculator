using PRC.BackupService.Services;

namespace PRC.BackupService.BackgroundServices;

public class BackupJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackupJob>   _log;

    public BackupJob(IServiceScopeFactory scopeFactory, ILogger<BackupJob> log)
    {
        _scopeFactory = scopeFactory;
        _log          = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNext2AmUtc();
            _log.LogInformation("Next scheduled backup in {Minutes:F0} minutes", delay.TotalMinutes);
            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orch = scope.ServiceProvider.GetRequiredService<BackupOrchestrator>();
                await orch.RunAllAsync("schedule", stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _log.LogError(ex, "Scheduled backup run failed");
            }
        }
    }

    private static TimeSpan TimeUntilNext2AmUtc()
    {
        var now  = DateTime.UtcNow;
        var next = now.Date.AddHours(2);
        if (next <= now) next = next.AddDays(1);
        return next - now;
    }
}
