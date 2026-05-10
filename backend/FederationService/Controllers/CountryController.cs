using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.FederationService.DTOs;
using PRC.FederationService.Services;
using PRC.FederationService.Data;

namespace PRC.FederationService.Controllers;

[ApiController]
[Route("api/federation")]
public class FederationController : FederationControllerBase
{
    private readonly IFederationService _federation;
    private readonly ICurrentUserService _currentUser;
    private readonly FederationDbContext _db;

    public FederationController(IFederationService federation, ICurrentUserService currentUser, FederationDbContext db)
    {
        _federation  = federation;
        _currentUser = currentUser;
        _db = db;
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

    [HttpGet("stats")]
    [Authorize(Roles = "FederationManager,SuperAdmin")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var federationId = _currentUser.FederationId;
        if (federationId == null) return Forbid();
        var thisYear = DateTime.UtcNow.Year;
        var totalResults    = await _db.FederationResults.CountAsync(r => r.FederationId == federationId, ct);
        var resultsThisYear = await _db.FederationResults.CountAsync(r => r.FederationId == federationId && r.CreatedAt.Year == thisYear, ct);
        return Ok(ApiResponse<object>.Ok(new { totalResults, resultsThisYear }));
    }
}
