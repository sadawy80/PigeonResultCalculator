using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PRC.RaceService.Hubs;

[Authorize]
public class LiveRaceHub : Hub
{
    private readonly ILogger<LiveRaceHub> _logger;

    public LiveRaceHub(ILogger<LiveRaceHub> logger) => _logger = logger;

    public async Task JoinRaceGroup(string raceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"race-{raceId}");
        _logger.LogInformation("Client {ConnectionId} joined race group {RaceId}", Context.ConnectionId, raceId);
    }

    public async Task LeaveRaceGroup(string raceId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"race-{raceId}");

    public async Task JoinClubGroup(string clubId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"club-{clubId}");

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
