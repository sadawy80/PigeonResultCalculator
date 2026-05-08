namespace PRC.ClubService.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
    Task SendInvitationAsync(string to, string inviterName, string clubName, string acceptLink, CancellationToken ct = default);
}
