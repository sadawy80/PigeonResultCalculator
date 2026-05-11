using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using PRC.Common.Authorization;
using PRC.Common.Messages;

namespace PRC.ClubService.Services;

public class ClubSubscriptionChecker : CachingSubscriptionCheckerBase
{
    private readonly IRequestClient<GetFederationSubscriptionLimitsRequest> _client;

    public ClubSubscriptionChecker(
        IRequestClient<GetFederationSubscriptionLimitsRequest> client,
        IMemoryCache cache) : base(cache)
        => _client = client;

    protected override async Task<bool> FetchFromBusAsync(Guid federationId, CancellationToken ct)
    {
        try
        {
            var resp = await _client.GetResponse<GetFederationSubscriptionLimitsResult>(
                new GetFederationSubscriptionLimitsRequest(federationId), ct);
            return resp.Message.HasActiveSubscription;
        }
        catch
        {
            // Fail open — if the subscription service is down, don't block users
            return true;
        }
    }
}
