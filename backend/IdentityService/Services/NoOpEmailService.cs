namespace PRC.IdentityService.Services;

// Stub for local development — replace with MassTransit event publisher in production.
public class NoOpEmailService : IEmailService
{
    private readonly ILogger<NoOpEmailService> _logger;

    public NoOpEmailService(ILogger<NoOpEmailService> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogInformation("NoOp email to {To}: {Subject}", to, subject);
        return Task.CompletedTask;
    }
}
