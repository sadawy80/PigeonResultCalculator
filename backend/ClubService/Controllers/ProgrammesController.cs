using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.Common;
using PRC.Common.Authorization;
using PRC.ClubService.DTOs;
using PRC.ClubService.Services;

namespace PRC.ClubService.Controllers;

[Route("api/programmes")]
[Authorize]
public class ProgrammesController : ClubControllerBase
{
    private readonly IProgrammeService _programmes;
    private readonly ICurrentUserService _user;

    public ProgrammesController(IProgrammeService programmes, ICurrentUserService user)
    {
        _programmes = programmes;
        _user = user;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProgrammeRequest req, CancellationToken ct)
        => FromResult(await _programmes.CreateAsync(req, _user.UserId.GetValueOrDefault(), ct));

    [HttpPut("{programmeId:guid}")]
    public async Task<IActionResult> Update(Guid programmeId, [FromBody] UpdateProgrammeRequest req, CancellationToken ct)
        => FromResult(await _programmes.UpdateAsync(programmeId, req, ct));

    [HttpGet("{programmeId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid programmeId, CancellationToken ct)
        => FromResult(await _programmes.GetAsync(programmeId, ct));

    [HttpGet("by-club/{clubId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByClub(Guid clubId, [FromQuery] PagedQuery paged, CancellationToken ct)
        => FromResult(await _programmes.GetByClubAsync(clubId, paged, ct));

    [HttpDelete("{programmeId:guid}")]
    public async Task<IActionResult> Delete(Guid programmeId, CancellationToken ct)
        => FromResult(await _programmes.DeleteAsync(programmeId, ct));

    [HttpPost("{programmeId:guid}/races")]
    public async Task<IActionResult> AddRace(Guid programmeId, [FromBody] AddRaceToProgrammeRequest req, CancellationToken ct)
        => FromResult(await _programmes.AddRaceAsync(programmeId, req, ct));

    [HttpDelete("{programmeId:guid}/races/{raceId:guid}")]
    public async Task<IActionResult> RemoveRace(Guid programmeId, Guid raceId, CancellationToken ct)
        => FromResult(await _programmes.RemoveRaceAsync(programmeId, raceId, ct));

    [HttpPost("{programmeId:guid}/calculate")]
    [RequiresPlan]
    public async Task<IActionResult> Calculate(Guid programmeId, CancellationToken ct)
        => FromResult(await _programmes.CalculateAsync(programmeId, ct));

    [HttpPost("{programmeId:guid}/publish")]
    [RequiresPlan]
    public async Task<IActionResult> Publish(Guid programmeId, CancellationToken ct)
        => FromResult(await _programmes.PublishAsync(programmeId, _user.UserId.GetValueOrDefault(), ct));
}
