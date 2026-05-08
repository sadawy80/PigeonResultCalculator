using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.Common;
using PRC.SubscriptionService.DTOs;
using PRC.SubscriptionService.Services;
using System.Security.Claims;

namespace PRC.SubscriptionService.Controllers;

[Route("api/subscription-plans")]
[Authorize]
public class SubscriptionPlansController : SubscriptionControllerBase
{
    private readonly ISubscriptionService _svc;

    public SubscriptionPlansController(ISubscriptionService svc) => _svc = svc;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans(
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        // Only SuperAdmin can see inactive plans
        var canSeeInactive = User.IsInRole("SuperAdmin");
        var result = await _svc.GetPlansAsync(includeInactive && canSeeInactive, ct);
        return FromResult(result);
    }

    [HttpGet("{planId:guid}")]
    public async Task<IActionResult> GetPlan(Guid planId, CancellationToken ct)
    {
        var result = await _svc.GetPlanAsync(planId, ct);
        return FromResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CreatePlan([FromBody] CreateSubscriptionPlanRequest req, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
        var result = await _svc.CreatePlanAsync(req, userId, ct);
        return FromResult(result);
    }

    [HttpPut("{planId:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdatePlan(Guid planId, [FromBody] UpdateSubscriptionPlanRequest req, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
        var result = await _svc.UpdatePlanAsync(planId, req, userId, ct);
        return FromResult(result);
    }
}
