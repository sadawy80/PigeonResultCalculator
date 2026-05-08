using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.Common;
using PRC.FederationService.DTOs;
using PRC.FederationService.Services;

namespace PRC.FederationService.Controllers;

[ApiController]
[Route("api/federation-results")]
[Authorize]
public class FederationResultsController : FederationControllerBase
{
    private readonly IFederationService _federation;
    private readonly ICurrentUserService _currentUser;

    public FederationResultsController(IFederationService federation, ICurrentUserService currentUser)
    {
        _federation  = federation;
        _currentUser = currentUser;
    }

    [HttpPost]
    [Authorize(Roles = "FederationManager,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateFederationResultRequest req, CancellationToken ct)
        => FromResult(await _federation.CreateFederationResultAsync(req, _currentUser.UserId ?? Guid.Empty, ct));

    [HttpPost("{federationResultId:guid}/publish")]
    [Authorize(Roles = "FederationManager,SuperAdmin")]
    public async Task<IActionResult> Publish(Guid federationResultId, CancellationToken ct)
        => FromResult(await _federation.PublishFederationResultAsync(federationResultId, _currentUser.UserId ?? Guid.Empty, ct));

    [HttpGet("{federationResultId:guid}")]
    public async Task<IActionResult> Get(Guid federationResultId, CancellationToken ct)
        => FromResult(await _federation.GetFederationResultAsync(federationResultId, ct));

    [HttpGet("federation/{federationId:guid}")]
    public async Task<IActionResult> GetByFederation(
        Guid federationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => FromResult(await _federation.GetFederationResultsAsync(federationId,
            new PagedQuery { Page = page, PageSize = pageSize }, ct));
}
