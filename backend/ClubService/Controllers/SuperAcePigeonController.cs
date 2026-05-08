using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.Common;
using PRC.ClubService.Services;

namespace PRC.ClubService.Controllers;

[Route("api/super-ace-pigeon")]
[Authorize]
public class SuperAcePigeonController : ClubControllerBase
{
    private readonly IProgrammeService _programmes;

    public SuperAcePigeonController(IProgrammeService programmes) => _programmes = programmes;

    [HttpGet("programme/{programmeId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid programmeId, [FromQuery] PagedQuery paged, CancellationToken ct)
        => FromResult(await _programmes.GetSuperAcePigeonAsync(programmeId, paged, ct));
}
