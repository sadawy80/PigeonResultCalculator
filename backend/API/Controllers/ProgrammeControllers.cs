using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PigeonRacing.Application.Common;
using PigeonRacing.Application.Features.Programmes;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.API.Controllers;

// ─────────────────────────────────────────────────────────────────────────────
//  Programmes Controller
//  Manages club programmes (seasons/series) and the races that belong to them.
// ─────────────────────────────────────────────────────────────────────────────

[Route("api/programmes")]
[Authorize]
public class ProgrammesController : ApiControllerBase
{
    // ── Programme CRUD ────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateProgrammeCommand cmd, CancellationToken ct)
        => FromResult(await Mediator.Send(cmd, ct));

    [HttpPut("{programmeId:guid}")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> Update(Guid programmeId, [FromBody] UpdateProgrammeCommand cmd, CancellationToken ct)
        => FromResult(await Mediator.Send(cmd with { ProgrammeId = programmeId }, ct));

    [HttpGet("{programmeId:guid}")]
    public async Task<IActionResult> Get(Guid programmeId, CancellationToken ct)
        => FromResult(await Mediator.Send(new GetProgrammeQuery(programmeId), ct));

    [HttpGet("club/{clubId:guid}")]
    public async Task<IActionResult> GetByClub(
        Guid clubId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
        => FromResult(await Mediator.Send(
            new GetClubProgrammesQuery(clubId,
                new PagedQuery { Page = page, PageSize = pageSize, Search = search }), ct));

    [HttpDelete("{programmeId:guid}")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> Delete(Guid programmeId, CancellationToken ct)
        => FromResult(await Mediator.Send(new DeleteProgrammeCommand(programmeId), ct));

    // ── Race membership ───────────────────────────────────────────────────────

    [HttpPost("{programmeId:guid}/races")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> AddRace(
        Guid programmeId,
        [FromBody] AddRaceToProgrammeRequest req,
        CancellationToken ct)
        => FromResult(await Mediator.Send(
            new AddRaceToProgrammeCommand(programmeId, req.RaceId, req.ScoreWeight, req.SortOrder), ct));

    [HttpDelete("{programmeId:guid}/races/{raceId:guid}")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> RemoveRace(Guid programmeId, Guid raceId, CancellationToken ct)
        => FromResult(await Mediator.Send(new RemoveRaceFromProgrammeCommand(programmeId, raceId), ct));

    // ── Calculation & publishing ──────────────────────────────────────────────

    /// <summary>
    /// Runs all four engines: Race Results ranking (already done per-race),
    /// Best Loft, Ace Pigeon, Super Ace Pigeon.
    /// Safe to call multiple times — clears and recalculates.
    /// </summary>
    [HttpPost("{programmeId:guid}/calculate")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> Calculate(Guid programmeId, CancellationToken ct)
        => FromResult(await Mediator.Send(new CalculateProgrammeResultsCommand(programmeId), ct));

    /// <summary>
    /// Marks the programme as Published — results become publicly visible.
    /// Requires at least one calculation to have been run first.
    /// </summary>
    [HttpPost("{programmeId:guid}/publish")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> Publish(Guid programmeId, CancellationToken ct)
        => FromResult(await Mediator.Send(new PublishProgrammeCommand(programmeId), ct));
}

// ─────────────────────────────────────────────────────────────────────────────
//  Race Results Controller
//  Per-race results — already existed, but now has a dedicated page endpoint
//  that returns the full race detail including weather, categories, and results.
// ─────────────────────────────────────────────────────────────────────────────
// (Race results are handled in the existing ResultsController — see Controllers.cs)

// ─────────────────────────────────────────────────────────────────────────────
//  Best Loft Results Controller
// ─────────────────────────────────────────────────────────────────────────────

[Route("api/best-loft")]
[Authorize]
public class BestLoftController : ApiControllerBase
{
    /// <summary>
    /// Paginated Best Loft leaderboard for a programme.
    /// Ordered by LoftRank ascending (1 = best loft).
    /// </summary>
    [HttpGet("programme/{programmeId:guid}")]
    public async Task<IActionResult> Get(
        Guid programmeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
        => FromResult(await Mediator.Send(
            new GetBestLoftResultsQuery(programmeId,
                new PagedQuery { Page = page, PageSize = pageSize, Search = search }), ct));
}

// ─────────────────────────────────────────────────────────────────────────────
//  Ace Pigeon Results Controller
// ─────────────────────────────────────────────────────────────────────────────

[Route("api/ace-pigeon")]
[Authorize]
public class AcePigeonController : ApiControllerBase
{
    /// <summary>
    /// Paginated Ace Pigeon leaderboard for a programme.
    /// Ordered by AceRank ascending (1 = top ace pigeon).
    /// </summary>
    [HttpGet("programme/{programmeId:guid}")]
    public async Task<IActionResult> Get(
        Guid programmeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
        => FromResult(await Mediator.Send(
            new GetAcePigeonResultsQuery(programmeId,
                new PagedQuery { Page = page, PageSize = pageSize, Search = search }), ct));
}

// ─────────────────────────────────────────────────────────────────────────────
//  Super Ace Pigeon Results Controller
// ─────────────────────────────────────────────────────────────────────────────

[Route("api/super-ace-pigeon")]
[Authorize]
public class SuperAcePigeonController : ApiControllerBase
{
    /// <summary>
    /// Paginated Super Ace Pigeon leaderboard for a programme.
    /// Only pigeons meeting the programme's SuperAceQualification criteria appear here.
    /// Ordered by SuperAceRank ascending (1 = top super ace).
    /// </summary>
    [HttpGet("programme/{programmeId:guid}")]
    public async Task<IActionResult> Get(
        Guid programmeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
        => FromResult(await Mediator.Send(
            new GetSuperAcePigeonResultsQuery(programmeId,
                new PagedQuery { Page = page, PageSize = pageSize, Search = search }), ct));
}

// ── Request models ────────────────────────────────────────────────────────────

public record AddRaceToProgrammeRequest(Guid RaceId, double ScoreWeight = 1.0, int SortOrder = 0);
