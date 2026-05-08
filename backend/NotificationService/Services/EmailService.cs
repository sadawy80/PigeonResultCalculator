using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace PRC.NotificationService.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var from = _config["Email:From"] ?? "noreply@pigeonresultcalculator.com";
        var host = _config["Email:Host"] ?? "localhost";
        var port = int.Parse(_config["Email:Port"] ?? "587");

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, ct);

        var username = _config["Email:Username"];
        if (!string.IsNullOrEmpty(username))
            await client.AuthenticateAsync(username, _config["Email:Password"], ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
    }
}
