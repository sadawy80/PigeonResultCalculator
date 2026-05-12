using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.RenderingService.Models;
using PRC.RenderingService.Services;

namespace PRC.RenderingService.Controllers;

/// <summary>
/// File-based renderer for the four result table types. Each type has both
/// a PDF endpoint (returns the rendered multi-page A4 PDF) and an Excel
/// endpoint (returns the same data as an .xlsx workbook).
/// </summary>
[ApiController]
[Route("api/result-tables")]
[Authorize]
public class ResultsRenderController : ControllerBase
{
    private readonly IResultRenderer _renderer;
    private readonly IResultExcelExporter _excel;

    public ResultsRenderController(IResultRenderer renderer, IResultExcelExporter excel)
    {
        _renderer = renderer;
        _excel    = excel;
    }

    // ── PDF endpoints ─────────────────────────────────────────────────────
    [HttpPost("race/pdf")]
    public Task<IActionResult> RacePdf([FromBody] ResultRenderRequest req, CancellationToken ct)
        => PdfAsync(ResultType.Race, req, ct);

    [HttpPost("ace/pdf")]
    public Task<IActionResult> AcePdf([FromBody] ResultRenderRequest req, CancellationToken ct)
        => PdfAsync(ResultType.Ace, req, ct);

    [HttpPost("super-ace/pdf")]
    public Task<IActionResult> SuperAcePdf([FromBody] ResultRenderRequest req, CancellationToken ct)
        => PdfAsync(ResultType.SuperAce, req, ct);

    [HttpPost("best-loft/pdf")]
    public Task<IActionResult> BestLoftPdf([FromBody] ResultRenderRequest req, CancellationToken ct)
        => PdfAsync(ResultType.BestLoft, req, ct);

    // ── Excel endpoints ───────────────────────────────────────────────────
    [HttpPost("race/excel")]
    public IActionResult RaceExcel([FromBody] ResultRenderRequest req)
        => Excel(ResultType.Race, req);

    [HttpPost("ace/excel")]
    public IActionResult AceExcel([FromBody] ResultRenderRequest req)
        => Excel(ResultType.Ace, req);

    [HttpPost("super-ace/excel")]
    public IActionResult SuperAceExcel([FromBody] ResultRenderRequest req)
        => Excel(ResultType.SuperAce, req);

    [HttpPost("best-loft/excel")]
    public IActionResult BestLoftExcel([FromBody] ResultRenderRequest req)
        => Excel(ResultType.BestLoft, req);

    // ── helpers ───────────────────────────────────────────────────────────
    private async Task<IActionResult> PdfAsync(ResultType type, ResultRenderRequest req, CancellationToken ct)
    {
        var pdf  = await _renderer.RenderAsync(type, req, ct);
        var slug = type.ToString().ToLowerInvariant();
        return File(pdf, "application/pdf", $"{slug}-result-{req.DesignId}-{req.Language}.pdf");
    }

    private IActionResult Excel(ResultType type, ResultRenderRequest req)
    {
        var bytes = _excel.Export(type, req);
        var slug  = type.ToString().ToLowerInvariant();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{slug}-result-{req.DesignId}-{req.Language}.xlsx");
    }
}
