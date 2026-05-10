using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.Common;
using PRC.RaceService.DTOs;
using PRC.RaceService.Services;

namespace PRC.RaceService.Controllers;

[Route("api/races")]
[Authorize]
public class RacesController : RaceControllerBase
{
    private readonly IRaceService _races;
    private readonly ICurrentUserService _user;

    public RacesController(IRaceService races, ICurrentUserService user)
    {
        _races = races;
        _user = user;
    }

    [HttpPost]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateRaceRequest req, CancellationToken ct)
        => FromResult(await _races.CreateAsync(req, _user.UserId.GetValueOrDefault(), ct));

    [HttpPut("{raceId:guid}")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> Update(Guid raceId, [FromBody] UpdateRaceRequest req, CancellationToken ct)
        => FromResult(await _races.UpdateAsync(raceId, req, ct));

    [HttpPost("{raceId:guid}/start")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> Start(Guid raceId, [FromBody] StartRaceRequest req, CancellationToken ct)
        => FromResult(await _races.StartAsync(raceId, req.ActualReleaseTime, ct));

    [HttpPost("{raceId:guid}/complete")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> Complete(Guid raceId, CancellationToken ct)
        => FromResult(await _races.CompleteAsync(raceId, ct));

    [HttpPost("{raceId:guid}/publish")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> Publish(Guid raceId, CancellationToken ct)
        => FromResult(await _races.PublishAsync(raceId, ct));

    [HttpDelete("{raceId:guid}")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> Delete(Guid raceId, CancellationToken ct)
        => FromResult(await _races.DeleteAsync(raceId, ct));

    [HttpGet("{raceId:guid}")]
    public async Task<IActionResult> Get(Guid raceId, CancellationToken ct)
        => FromResult(await _races.GetAsync(raceId, ct));

    [HttpGet("club/{clubId:guid}")]
    public async Task<IActionResult> GetByClub(Guid clubId, [FromQuery] PagedQuery paged, CancellationToken ct)
        => FromResult(await _races.GetByClubAsync(clubId, paged, ct));

    [HttpGet("club/{clubId:guid}/live")]
    public async Task<IActionResult> GetLive(Guid clubId, CancellationToken ct)
        => FromResult(await _races.GetLiveAsync(clubId, ct));

    // ── Cross-service endpoints ───────────────────────────────────────────────

    [HttpGet("{raceId:guid}/exists")]
    [AllowAnonymous]
    public async Task<IActionResult> Exists(Guid raceId, [FromQuery] Guid clubId, CancellationToken ct)
    {
        var exists = await _races.ExistsAsync(raceId, clubId, ct);
        return exists ? Ok() : NotFound();
    }

    [HttpGet("{raceId:guid}/snapshot")]
    [AllowAnonymous]
    public async Task<IActionResult> Snapshot(Guid raceId, CancellationToken ct)
    {
        var snapshot = await _races.GetSnapshotAsync(raceId, ct);
        return snapshot == null
            ? NotFound()
            : Ok(ApiResponse<RaceSnapshotDto>.Ok(snapshot));
    }

    [HttpGet("{raceId:guid}/result-count")]
    [AllowAnonymous]
    public async Task<IActionResult> ResultCount(Guid raceId, CancellationToken ct)
    {
        var count = await _races.GetResultCountAsync(raceId, ct);
        return Ok(ApiResponse<int>.Ok(count));
    }
}
