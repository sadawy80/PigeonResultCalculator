using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.Common;
using PRC.RaceService.DTOs;
using PRC.RaceService.Services;

namespace PRC.RaceService.Controllers;

[Route("api/results")]
[Authorize]
public class ResultsController : RaceControllerBase
{
    private readonly IResultService _results;
    private readonly ICurrentUserService _user;

    public ResultsController(IResultService results, ICurrentUserService user)
    {
        _results = results;
        _user = user;
    }

    [HttpPost("manual")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> AddManual([FromBody] AddManualResultRequest req, CancellationToken ct)
        => FromResult(await _results.AddManualAsync(req, _user.UserId.GetValueOrDefault(), ct));

    [HttpPost("ingest-ets")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> IngestETS(
        [FromForm] Guid raceId, [FromForm] Guid? categoryId,
        IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object?>.Fail("No file provided."));

        return FromResult(await _results.IngestETSFileAsync(
            raceId, categoryId, file.OpenReadStream(), file.FileName,
            _user.UserId.GetValueOrDefault(), ct));
    }

    [HttpPost("{raceId:guid}/process")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> Process(Guid raceId, CancellationToken ct)
        => FromResult(await _results.ProcessAsync(raceId, ct));

    [HttpGet("race/{raceId:guid}")]
    public async Task<IActionResult> GetByRace(
        Guid raceId, [FromQuery] Guid? categoryId, [FromQuery] PagedQuery paged, CancellationToken ct)
        => FromResult(await _results.GetByRaceAsync(raceId, categoryId, paged, ct));

    [HttpGet("fancier/{userId:guid}")]
    public async Task<IActionResult> GetByFancier(Guid userId, [FromQuery] PagedQuery paged, CancellationToken ct)
        => FromResult(await _results.GetByFancierAsync(userId, paged, ct));

    [HttpPut("{resultId:guid}/link-fancier")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> LinkFancier(Guid resultId, [FromBody] LinkFancierRequest req, CancellationToken ct)
        => FromResult(await _results.LinkFancierAsync(resultId, req.UserId, ct));

    [HttpDelete("{resultId:guid}")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> Delete(Guid resultId, CancellationToken ct)
        => FromResult(await _results.DeleteAsync(resultId, ct));

    [HttpGet("race/{raceId:guid}/ingestion-logs")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> GetIngestionLogs(Guid raceId, CancellationToken ct)
        => FromResult(await _results.GetIngestionLogsAsync(raceId, ct));

    // ── Cross-service endpoint for ClubService programme calculations ─────────
    [HttpGet("for-programme")]
    [AllowAnonymous]
    public async Task<IActionResult> ForProgramme([FromQuery] string raceIds, CancellationToken ct)
    {
        var ids = raceIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(id => Guid.TryParse(id, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();

        return FromResult(await _results.GetPublishedForProgrammeAsync(ids, ct));
    }

    // ── Cross-service batch snapshots (used by FederationService) ────────────
    [HttpGet("snapshots")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSnapshots([FromQuery] string raceIds, CancellationToken ct)
    {
        var ids = raceIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(id => Guid.TryParse(id, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();

        return FromResult(await _results.GetPublishedForProgrammeAsync(ids, ct));
    }
}
