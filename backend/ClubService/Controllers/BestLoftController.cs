using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.Common;
using PRC.ClubService.Services;

namespace PRC.ClubService.Controllers;

[Route("api/best-loft")]
[Authorize]
public class BestLoftController : ClubControllerBase
{
    private readonly IProgrammeService _programmes;

    public BestLoftController(IProgrammeService programmes) => _programmes = programmes;

    [HttpGet("programme/{programmeId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid programmeId, [FromQuery] PagedQuery paged, CancellationToken ct)
        => FromResult(await _programmes.GetBestLoftAsync(programmeId, paged, ct));
}
