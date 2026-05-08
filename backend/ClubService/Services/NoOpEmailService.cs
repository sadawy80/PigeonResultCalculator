namespace PRC.ClubService.Services;

public class NoOpEmailService : IEmailService
{
    private readonly ILogger<NoOpEmailService> _logger;

    public NoOpEmailService(ILogger<NoOpEmailService> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogInformation("NoOp email to {To}: {Subject}", to, subject);
        return Task.CompletedTask;
    }

    public Task SendInvitationAsync(string to, string inviterName, string clubName, string acceptLink, CancellationToken ct = default)
    {
        _logger.LogInformation("NoOp invitation email to {To} for club {ClubName}", to, clubName);
        return Task.CompletedTask;
    }
}
