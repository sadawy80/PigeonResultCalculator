using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.Common;
using PRC.ClubService.Services;

namespace PRC.ClubService.Controllers;

[Route("api/ace-pigeon")]
[Authorize]
public class AcePigeonController : ClubControllerBase
{
    private readonly IProgrammeService _programmes;

    public AcePigeonController(IProgrammeService programmes) => _programmes = programmes;

    [HttpGet("programme/{programmeId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid programmeId, [FromQuery] PagedQuery paged, CancellationToken ct)
        => FromResult(await _programmes.GetAcePigeonAsync(programmeId, paged, ct));
}
