using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.RenderingService.Models;
using PRC.RenderingService.Services;

namespace PRC.RenderingService.Controllers;

/// <summary>
/// File-based renderer for the four award certificate types. Each endpoint
/// accepts the JSON payload defined by the matching <c>_PROD_SPEC.md</c> and
/// returns a single-page A4 PDF. Best Loft has a different schema (loft hero,
/// federation strip) so it needs its own endpoint; the other three share a
/// schema shape but are kept separate for clarity and per-type validation.
/// </summary>
[ApiController]
[Route("api/certificates")]
[Authorize]
public class CertificatesController : ControllerBase
{
    private readonly ICertRenderer _renderer;

    public CertificatesController(ICertRenderer renderer) => _renderer = renderer;

    [HttpPost("race")]
    public Task<IActionResult> Race([FromBody] CertRenderRequest req, CancellationToken ct)
        => RenderAsync(CertType.Race, req, ct);

    [HttpPost("ace")]
    public Task<IActionResult> Ace([FromBody] CertRenderRequest req, CancellationToken ct)
        => RenderAsync(CertType.Ace, req, ct);

    [HttpPost("super-ace")]
    public Task<IActionResult> SuperAce([FromBody] CertRenderRequest req, CancellationToken ct)
        => RenderAsync(CertType.SuperAce, req, ct);

    [HttpPost("best-loft")]
    public Task<IActionResult> BestLoft([FromBody] CertRenderRequest req, CancellationToken ct)
        => RenderAsync(CertType.BestLoft, req, ct);

    private async Task<IActionResult> RenderAsync(CertType type, CertRenderRequest req, CancellationToken ct)
    {
        var pdf = await _renderer.RenderAsync(type, req, ct);
        var slug = type.ToString().ToLowerInvariant();
        var fileName = $"{slug}-cert-{req.DesignId}-{req.Language}.pdf";
        return File(pdf, "application/pdf", fileName);
    }
}
