using MassTransit;
using PRC.ClubService.Data;
using PRC.ClubService.Models;
using PRC.Common;
using PRC.Common.Messages;

namespace PRC.ClubService.Events;

public class CreateInAppNotificationConsumer : IConsumer<CreateInAppNotification>
{
    private readonly ClubDbContext _db;
    private readonly ILogger<CreateInAppNotificationConsumer> _logger;

    public CreateInAppNotificationConsumer(ClubDbContext db, ILogger<CreateInAppNotificationConsumer> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateInAppNotification> ctx)
    {
        var msg = ctx.Message;
        _db.Notifications.Add(new Notification
        {
            UserId    = msg.UserId,
            Type      = msg.Type,
            Channel   = NotificationChannel.InApp,
            Status    = NotificationStatus.Sent,
            Title     = msg.Title,
            Body      = msg.Body,
            ActionUrl = msg.ActionUrl,
            SentAt    = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ctx.CancellationToken);
        _logger.LogInformation("In-app notification created for user {UserId}: {Title}", msg.UserId, msg.Title);
    }
}
