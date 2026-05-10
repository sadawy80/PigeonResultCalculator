using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PRC.FederationService.Hubs;

[Authorize]
public class FederationHub : Hub
{
    public async Task JoinFederation(string federationId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"federation-{federationId}");

    public async Task LeaveFederation(string federationId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"federation-{federationId}");
}
