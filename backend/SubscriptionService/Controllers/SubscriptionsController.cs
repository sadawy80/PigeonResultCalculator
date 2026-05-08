using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.Common;
using PRC.SubscriptionService.DTOs;
using PRC.SubscriptionService.Events;
using PRC.SubscriptionService.Services;
using System.Security.Claims;

namespace PRC.SubscriptionService.Controllers;

[Route("api/subscriptions")]
[Authorize(Roles = "SuperAdmin")]
public class SubscriptionsController : SubscriptionControllerBase
{
    private readonly ISubscriptionService _svc;
    private readonly IPublishEndpoint _bus;

    public SubscriptionsController(ISubscriptionService svc, IPublishEndpoint bus)
    {
        _svc = svc;
        _bus = bus;
    }

    [HttpGet("country")]
    public async Task<IActionResult> GetFederationSubscriptions(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _svc.GetFederationSubscriptionsAsync(page, pageSize, ct);
        return FromResult(result);
    }

    [HttpGet("country/{FederationId:guid}/active")]
    [Authorize]
    public async Task<IActionResult> GetActiveFederationSubscription(Guid FederationId, CancellationToken ct)
    {
        var result = await _svc.GetActiveFederationSubscriptionAsync(FederationId, ct);
        return FromResult(result);
    }

    [HttpPost("country")]
    public async Task<IActionResult> CreateFederationSubscription(
        [FromBody] CreateFederationSubscriptionRequest req, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
        var result = await _svc.CreateFederationSubscriptionAsync(req, userId, ct);
        if (result.IsSuccess && result.Value is not null)
        {
            await _bus.Publish(new SubscriptionCreated(
                result.Value.Id, result.Value.FederationId, result.Value.FederationName,
                result.Value.PlanName, result.Value.BillingCycle.ToString(),
                result.Value.ExpiresAt, DateTime.UtcNow));
        }
        return FromResult(result);
    }

    [HttpGet("club/{clubId:guid}/active")]
    [Authorize]
    public async Task<IActionResult> GetActiveClubSubscription(Guid clubId, CancellationToken ct)
    {
        var result = await _svc.GetActiveClubSubscriptionAsync(clubId, ct);
        return FromResult(result);
    }

    [HttpPost("club")]
    public async Task<IActionResult> CreateClubSubscription(
        [FromBody] CreateClubSubscriptionRequest req, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
        var result = await _svc.CreateClubSubscriptionAsync(req, userId, ct);
        return FromResult(result);
    }
}
