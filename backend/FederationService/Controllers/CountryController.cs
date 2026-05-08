using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.Common;
using PRC.FederationService.DTOs;
using PRC.FederationService.Services;

namespace PRC.FederationService.Controllers;

[ApiController]
[Route("api/federation")]
public class FederationController : FederationControllerBase
{
    private readonly IFederationService _federation;
    private readonly ICurrentUserService _currentUser;

    public FederationController(IFederationService federation, ICurrentUserService currentUser)
    {
        _federation  = federation;
        _currentUser = currentUser;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => FromResult(await _federation.GetAllFederationsAsync(ct));

    [HttpGet("page")]
    [Authorize(Roles = "FederationManager,SuperAdmin")]
    public async Task<IActionResult> GetPage(CancellationToken ct)
    {
        var federationId = _currentUser.FederationId;
        if (federationId == null) return Forbid();
        return FromResult(await _federation.GetFederationPageAsync(federationId.Value, ct));
    }

    [HttpPut("page")]
    [Authorize(Roles = "FederationManager,SuperAdmin")]
    public async Task<IActionResult> UpdatePage([FromBody] UpdateFederationPageRequest req, CancellationToken ct)
    {
        var federationId = _currentUser.FederationId;
        if (federationId == null) return Forbid();
        return FromResult(await _federation.UpdateFederationPageAsync(federationId.Value, req, ct));
    }
}
