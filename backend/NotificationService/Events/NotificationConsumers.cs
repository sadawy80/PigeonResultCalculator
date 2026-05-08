using MassTransit;
using PRC.Common.Messages;
using PRC.NotificationService.Services;

namespace PRC.NotificationService.Events;


// ── Direct email relay (from IdentityService: password reset, verify email) ──

public class SendEmailEventConsumer : IConsumer<SendEmailEvent>
{
    private readonly IEmailService _email;
    private readonly ILogger<SendEmailEventConsumer> _logger;

    public SendEmailEventConsumer(IEmailService email, ILogger<SendEmailEventConsumer> logger)
    {
        _email  = email;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SendEmailEvent> ctx)
    {
        var msg = ctx.Message;
        _logger.LogInformation("Relaying email to {To}: {Subject}", msg.To, msg.Subject);
        await _email.SendAsync(msg.To, msg.Subject, msg.HtmlBody, ctx.CancellationToken);
    }
}

// ── Club member invitation ────────────────────────────────────────────────────

public class MemberInvitedConsumer : IConsumer<MemberInvited>
{
    private readonly IEmailService _email;
    private readonly ILogger<MemberInvitedConsumer> _logger;

    public MemberInvitedConsumer(IEmailService email, ILogger<MemberInvitedConsumer> logger)
    {
        _email  = email;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MemberInvited> ctx)
    {
        var msg  = ctx.Message;
        var html = $@"
            <h2>You're invited to join {msg.ClubName}!</h2>
            <p>{msg.InviterName} has invited you to join their pigeon racing club on Pigeon Result Calculator.</p>
            <p><a href='{msg.AcceptLink}' style='background:#1E3A5F;color:white;padding:12px 24px;
               border-radius:4px;text-decoration:none;display:inline-block;'>Accept Invitation</a></p>
            <p>This link expires in 7 days.</p>";

        await _email.SendAsync(msg.Email, $"Invitation to join {msg.ClubName}", html, ctx.CancellationToken);
    }
}

// ── Race published — notify fanciers who have results in this race ────────────

public class RaceResultsPublishedConsumer : IConsumer<RaceResultsPublished>
{
    private readonly IEmailService _email;
    private readonly IRequestClient<GetUserEmailsRequest> _emailsClient;
    private readonly ILogger<RaceResultsPublishedConsumer> _logger;

    public RaceResultsPublishedConsumer(
        IEmailService email,
        IRequestClient<GetUserEmailsRequest> emailsClient,
        ILogger<RaceResultsPublishedConsumer> logger)
    {
        _email        = email;
        _emailsClient = emailsClient;
        _logger       = logger;
    }

    public async Task Consume(ConsumeContext<RaceResultsPublished> ctx)
    {
        var msg = ctx.Message;
        _logger.LogInformation(
            "Race '{RaceName}' (Id={RaceId}) published by club {ClubId} with {Count} results",
            msg.RaceName, msg.RaceId, msg.ClubId, msg.Results.Count);

        var userIds = msg.Results
            .Where(r => r.UserId.HasValue)
            .Select(r => r.UserId!.Value)
            .Distinct()
            .ToList();

        if (!userIds.Any()) return;

        var emailsResp = await _emailsClient.GetResponse<UserEmailsResult>(
            new GetUserEmailsRequest(userIds), ctx.CancellationToken);
        var emails = emailsResp.Message.Emails;

        foreach (var result in msg.Results.Where(r => r.UserId.HasValue))
        {
            if (!emails.TryGetValue(result.UserId!.Value, out var to)) continue;

            var rank = result.UserFullName ?? "your pigeon";
            var html = $@"
                <h2>Your race results are published!</h2>
                <p>Dear fancier,</p>
                <p>Results for race <strong>{msg.RaceName}</strong> organised by
                   <strong>{msg.ClubName}</strong> have been published.</p>
                <table style='border-collapse:collapse;width:100%;max-width:480px'>
                  <tr><td style='padding:8px;border:1px solid #ddd'><strong>Ring number</strong></td>
                      <td style='padding:8px;border:1px solid #ddd'>{result.RingNumber}</td></tr>
                  <tr><td style='padding:8px;border:1px solid #ddd'><strong>Speed</strong></td>
                      <td style='padding:8px;border:1px solid #ddd'>{result.SpeedMperMin:F1} m/min</td></tr>
                  <tr><td style='padding:8px;border:1px solid #ddd'><strong>Distance</strong></td>
                      <td style='padding:8px;border:1px solid #ddd'>{result.DistanceKm:F2} km</td></tr>
                </table>
                <p>Log in to Pigeon Result Calculator to view full standings.</p>";

            try
            {
                await _email.SendAsync(to, $"Results published: {msg.RaceName}", html, ctx.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send result email to {Email}", to);
            }
        }
    }
}

// ── Subscription lifecycle ────────────────────────────────────────────────────

public class SubscriptionConfirmedEmailConsumer : IConsumer<SubscriptionConfirmedEmail>
{
    private readonly IEmailService _email;
    private readonly ILogger<SubscriptionConfirmedEmailConsumer> _logger;

    public SubscriptionConfirmedEmailConsumer(IEmailService email, ILogger<SubscriptionConfirmedEmailConsumer> logger)
    {
        _email  = email;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SubscriptionConfirmedEmail> ctx)
    {
        var msg  = ctx.Message;
        var html = $@"
            <h2>Subscription Confirmed</h2>
            <p>Dear {msg.ContactName},</p>
            <p>Your <strong>{msg.SubscriptionType}</strong> subscription for <strong>{msg.EntityName}</strong>
               has been activated.</p>
            <table style='border-collapse:collapse;width:100%;max-width:500px'>
              <tr><td style='padding:8px;border:1px solid #ddd'><strong>Plan</strong></td>
                  <td style='padding:8px;border:1px solid #ddd'>{msg.PlanName}</td></tr>
              <tr><td style='padding:8px;border:1px solid #ddd'><strong>Billing cycle</strong></td>
                  <td style='padding:8px;border:1px solid #ddd'>{msg.BillingCycle}</td></tr>
              <tr><td style='padding:8px;border:1px solid #ddd'><strong>Expires</strong></td>
                  <td style='padding:8px;border:1px solid #ddd'>{msg.ExpiresAt:dd MMM yyyy}</td></tr>
            </table>
            <p>Thank you for using Pigeon Result Calculator.</p>";

        await _email.SendAsync(msg.To, $"Subscription confirmed — {msg.PlanName}", html, ctx.CancellationToken);
        _logger.LogInformation("Subscription confirmation sent to {To} for {Entity}", msg.To, msg.EntityName);
    }
}

public class SubscriptionExpiredEmailConsumer : IConsumer<SubscriptionExpiredEmail>
{
    private readonly IEmailService _email;
    private readonly ILogger<SubscriptionExpiredEmailConsumer> _logger;

    public SubscriptionExpiredEmailConsumer(IEmailService email, ILogger<SubscriptionExpiredEmailConsumer> logger)
    {
        _email  = email;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SubscriptionExpiredEmail> ctx)
    {
        var msg  = ctx.Message;
        var html = $@"
            <h2>Subscription Expired</h2>
            <p>Dear {msg.ContactName},</p>
            <p>Your <strong>{msg.SubscriptionType}</strong> subscription for <strong>{msg.EntityName}</strong>
               has expired. Your plan was: <strong>{msg.PlanName}</strong>.</p>
            <p>To continue using all features, please renew your subscription through the admin panel.</p>";

        await _email.SendAsync(msg.To, $"Subscription expired — {msg.EntityName}", html, ctx.CancellationToken);
        _logger.LogInformation("Subscription expiry notice sent to {To} for {Entity}", msg.To, msg.EntityName);
    }
}

public class SubscriptionCancelledEmailConsumer : IConsumer<SubscriptionCancelledEmail>
{
    private readonly IEmailService _email;
    private readonly ILogger<SubscriptionCancelledEmailConsumer> _logger;

    public SubscriptionCancelledEmailConsumer(IEmailService email, ILogger<SubscriptionCancelledEmailConsumer> logger)
    {
        _email  = email;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SubscriptionCancelledEmail> ctx)
    {
        var msg  = ctx.Message;
        var html = $@"
            <h2>Subscription Cancelled</h2>
            <p>Dear {msg.ContactName},</p>
            <p>Your <strong>{msg.SubscriptionType}</strong> subscription for <strong>{msg.EntityName}</strong>
               has been cancelled.</p>
            <p><strong>Reason:</strong> {msg.Reason}</p>
            <p>If you believe this is an error or wish to reactivate, please contact support.</p>";

        await _email.SendAsync(msg.To, $"Subscription cancelled — {msg.EntityName}", html, ctx.CancellationToken);
        _logger.LogInformation("Subscription cancellation notice sent to {To} for {Entity}", msg.To, msg.EntityName);
    }
}

// ── Legacy event stubs (kept for backward-compat; new code uses typed email events above) ──

public class SubscriptionActivatedConsumer : IConsumer<SubscriptionActivated>
{
    private readonly ILogger<SubscriptionActivatedConsumer> _logger;
    public SubscriptionActivatedConsumer(ILogger<SubscriptionActivatedConsumer> logger) => _logger = logger;

    public Task Consume(ConsumeContext<SubscriptionActivated> ctx)
    {
        var msg = ctx.Message;
        _logger.LogInformation("Subscription {Id} activated for {Type} {EntityName} — plan {Plan}",
            msg.SubscriptionId, msg.SubscriptionType, msg.EntityName, msg.PlanName);
        return Task.CompletedTask;
    }
}

public class SubscriptionExpiredConsumer : IConsumer<SubscriptionExpiredEvent>
{
    private readonly ILogger<SubscriptionExpiredConsumer> _logger;
    public SubscriptionExpiredConsumer(ILogger<SubscriptionExpiredConsumer> logger) => _logger = logger;

    public Task Consume(ConsumeContext<SubscriptionExpiredEvent> ctx)
    {
        var msg = ctx.Message;
        _logger.LogWarning("Subscription {Id} expired for {Type} {EntityName}",
            msg.SubscriptionId, msg.SubscriptionType, msg.EntityName);
        return Task.CompletedTask;
    }
}

public class SubscriptionCancelledConsumer : IConsumer<SubscriptionCancelledEvent>
{
    private readonly ILogger<SubscriptionCancelledConsumer> _logger;
    public SubscriptionCancelledConsumer(ILogger<SubscriptionCancelledConsumer> logger) => _logger = logger;

    public Task Consume(ConsumeContext<SubscriptionCancelledEvent> ctx)
    {
        var msg = ctx.Message;
        _logger.LogWarning("Subscription {Id} cancelled for {Type} {EntityName} — reason: {Reason}",
            msg.SubscriptionId, msg.SubscriptionType, msg.EntityName, msg.Reason);
        return Task.CompletedTask;
    }
}

// ── External link requested ───────────────────────────────────────────────────

public class ExternalLinkRequestedConsumer : IConsumer<ExternalLinkRequested>
{
    private readonly ILogger<ExternalLinkRequestedConsumer> _logger;
    public ExternalLinkRequestedConsumer(ILogger<ExternalLinkRequestedConsumer> logger) => _logger = logger;

    public Task Consume(ConsumeContext<ExternalLinkRequested> ctx)
    {
        var msg = ctx.Message;
        _logger.LogInformation("External link requested for club {ClubId} from {Platform} loft '{Loft}'",
            msg.ClubId, msg.ExternalPlatformName, msg.ExternalLoftName);
        return Task.CompletedTask;
    }
}
