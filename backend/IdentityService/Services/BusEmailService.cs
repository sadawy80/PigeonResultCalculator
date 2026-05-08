using MassTransit;
using PRC.Common.Messages;

namespace PRC.IdentityService.Services;

public class BusEmailService : IEmailService
{
    private readonly IPublishEndpoint _bus;
    private readonly ILogger<BusEmailService> _logger;

    public BusEmailService(IPublishEndpoint bus, ILogger<BusEmailService> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        await _bus.Publish(new SendEmailEvent(to, subject, htmlBody), ct);
        _logger.LogInformation("SendEmailEvent published for {To}: {Subject}", to, subject);
    }
}
