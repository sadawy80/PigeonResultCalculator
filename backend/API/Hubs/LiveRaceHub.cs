using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PigeonRacing.API;

/// <summary>
/// Real-time hub for live race tracking.
/// Clients join race-specific groups to receive instant result pushes.
/// </summary>
[Authorize]
public class LiveRaceHub : Hub
{
    private readonly ILogger<LiveRaceHub> _logger;

    public LiveRaceHub(ILogger<LiveRaceHub> logger) => _logger = logger;

    /// <summary>Client calls this to start receiving updates for a race.</summary>
    public async Task JoinRaceGroup(string raceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"race-{raceId}");
        _logger.LogInformation("Client {ConnectionId} joined race group {RaceId}",
            Context.ConnectionId, raceId);
    }

    /// <summary>Client calls this to stop receiving updates for a race.</summary>
    public async Task LeaveRaceGroup(string raceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"race-{raceId}");
    }

    /// <summary>Join a club group to receive all club-level updates.</summary>
    public async Task JoinClubGroup(string clubId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"club-{clubId}");
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogDebug("SignalR client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("SignalR client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// Service used by result processing to push updates to connected clients.
/// </summary>
public class LiveResultsBroadcaster
{
    private readonly IHubContext<LiveRaceHub> _hub;

    public LiveResultsBroadcaster(IHubContext<LiveRaceHub> hub) => _hub = hub;

    public Task BroadcastNewResultAsync(Guid raceId, object resultDto) =>
        _hub.Clients.Group($"race-{raceId}").SendAsync("NewResult", resultDto);

    public Task BroadcastRaceStatusAsync(Guid raceId, string status) =>
        _hub.Clients.Group($"race-{raceId}").SendAsync("RaceStatusChanged", new { raceId, status });

    public Task BroadcastClubEventAsync(Guid clubId, string eventName, object payload) =>
        _hub.Clients.Group($"club-{clubId}").SendAsync(eventName, payload);
}
