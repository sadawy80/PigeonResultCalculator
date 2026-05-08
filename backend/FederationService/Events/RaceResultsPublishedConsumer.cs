using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PRC.Common.Messages;
using PRC.FederationService.Data;
using PRC.FederationService.Hubs;
using PRC.FederationService.Models;

namespace PRC.FederationService.Events;

public class RaceResultsPublishedConsumer : IConsumer<RaceResultsPublished>
{
    private readonly FederationDbContext _db;
    private readonly IPublishEndpoint _bus;
    private readonly IHubContext<FederationHub> _hub;
    private readonly ILogger<RaceResultsPublishedConsumer> _log;

    public RaceResultsPublishedConsumer(
        FederationDbContext db,
        IPublishEndpoint bus,
        IHubContext<FederationHub> hub,
        ILogger<RaceResultsPublishedConsumer> log)
    {
        _db  = db;
        _bus = bus;
        _hub = hub;
        _log = log;
    }

    public async Task Consume(ConsumeContext<RaceResultsPublished> context)
    {
        var msg = context.Message;

        var existing = await _db.RaceSnapshotCaches
            .FirstOrDefaultAsync(r => r.RaceId == msg.RaceId);

        if (existing != null)
        {
            existing.ClubName    = msg.ClubName;
            existing.RaceName    = msg.RaceName;
            existing.Status      = msg.RaceStatus;
            existing.FederationId = msg.FederationId;
            existing.CachedAt    = DateTime.UtcNow;

            var old = _db.RaceResultSnapshotCaches.Where(r => r.RaceSnapshotCacheId == existing.Id);
            _db.RaceResultSnapshotCaches.RemoveRange(old);
        }
        else
        {
            existing = new RaceSnapshotCache
            {
                RaceId       = msg.RaceId,
                ClubId       = msg.ClubId,
                FederationId = msg.FederationId,
                ClubName     = msg.ClubName,
                RaceName     = msg.RaceName,
                Status       = msg.RaceStatus,
                CachedAt     = DateTime.UtcNow
            };
            _db.RaceSnapshotCaches.Add(existing);
            await _db.SaveChangesAsync();
        }

        foreach (var r in msg.Results)
        {
            _db.RaceResultSnapshotCaches.Add(new RaceResultSnapshotCache
            {
                RaceSnapshotCacheId = existing.Id,
                ResultId     = r.ResultId,
                ClubId       = msg.ClubId,
                ClubName     = msg.ClubName,
                RingNumber   = r.RingNumber,
                UserId       = r.UserId,
                UserFullName = r.UserFullName,
                SpeedMperMin = r.SpeedMperMin,
                DistanceKm   = r.DistanceKm,
                ArrivalTime  = r.ArrivalTime
            });
        }

        await _db.SaveChangesAsync();
        _log.LogInformation("Cached {Count} results for race {RaceId}", msg.Results.Count, msg.RaceId);

        if (msg.FederationId.HasValue)
        {
            var federation = await _db.Federations
                .FirstOrDefaultAsync(c => c.Id == msg.FederationId.Value);

            if (federation != null)
            {
                await _hub.Clients
                    .Group($"federation-{federation.Id}")
                    .SendAsync("FederationResultUpdated", new
                    {
                        federationId = federation.Id,
                        clubName     = msg.ClubName,
                        raceName     = msg.RaceName,
                        resultCount  = msg.Results.Count,
                        updatedAt    = DateTime.UtcNow
                    }, context.CancellationToken);

                if (!string.IsNullOrEmpty(federation.ManagerEmail))
                {
                    var html = $@"
                        <h2>Race Results Published</h2>
                        <p>Dear {federation.ManagerName ?? "Federation Manager"},</p>
                        <p><strong>{msg.ClubName}</strong> has published results for race
                           <strong>{msg.RaceName}</strong> with {msg.Results.Count} result(s).</p>
                        <p>Visit the Pigeon Result Calculator admin panel to review and include
                           these results in a federation result.</p>";

                    await _bus.Publish(
                        new SendEmailEvent(
                            federation.ManagerEmail,
                            $"Race published: {msg.RaceName} — {msg.ClubName}",
                            html),
                        context.CancellationToken);

                    _log.LogInformation(
                        "Notified federation manager {Email} about race {RaceId}",
                        federation.ManagerEmail, msg.RaceId);
                }
            }
        }
    }
}
