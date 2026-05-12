using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.RenderingService.Services;

namespace PRC.RenderingService.Controllers;

/// <summary>
/// High-level print endpoints. The front-end calls these with entity IDs and
/// receives a PDF (or XLSX for results). All data assembly happens inside
/// <see cref="IPrintOrchestrator"/>; the front-end never has to build the
/// template-specific JSON shape itself.
/// </summary>
[ApiController]
[Route("api/print")]
[Authorize]
public class PrintController : ControllerBase
{
    private readonly IPrintOrchestrator _orch;
    public PrintController(IPrintOrchestrator orch) => _orch = orch;

    // ── Design catalogue ──────────────────────────────────────────────────
    [HttpGet("designs/cert/{certType}")]
    [AllowAnonymous]
    public ActionResult<DesignCatalogue> CertDesigns(string certType)
    {
        try { return DesignCatalog.GetCert(certType); }
        catch (ArgumentException ex) { return Problem(detail: ex.Message, statusCode: 400); }
    }

    [HttpGet("designs/result/{resultType}")]
    [AllowAnonymous]
    public ActionResult<IReadOnlyList<DesignInfo>> ResultDesigns(string resultType)
    {
        try { return Ok(DesignCatalog.GetResult(resultType)); }
        catch (ArgumentException ex) { return Problem(detail: ex.Message, statusCode: 400); }
    }

    // ── Certificates ──────────────────────────────────────────────────────
    public record RaceCertReq(Guid RaceResultId, string DesignId, string Language);
    public record AcePigeonCertReq(Guid ProgrammeId, string RingNumber, string DesignId, string Language);
    public record BestLoftCertReq(Guid ProgrammeId, Guid FancierUserId, string DesignId, string Language);

    [HttpPost("cert/race")]
    public async Task<IActionResult> RaceCert([FromBody] RaceCertReq r, CancellationToken ct)
        => Pdf(await _orch.RenderRaceCertAsync(r.RaceResultId, r.DesignId, r.Language, ct), $"race-cert-{r.DesignId}-{r.Language}.pdf");

    [HttpPost("cert/ace")]
    public async Task<IActionResult> AceCert([FromBody] AcePigeonCertReq r, CancellationToken ct)
        => Pdf(await _orch.RenderAceCertAsync(r.ProgrammeId, r.RingNumber, r.DesignId, r.Language, ct), $"ace-cert-{r.DesignId}-{r.Language}.pdf");

    [HttpPost("cert/super-ace")]
    public async Task<IActionResult> SuperAceCert([FromBody] AcePigeonCertReq r, CancellationToken ct)
        => Pdf(await _orch.RenderSuperAceCertAsync(r.ProgrammeId, r.RingNumber, r.DesignId, r.Language, ct), $"super-ace-cert-{r.DesignId}-{r.Language}.pdf");

    [HttpPost("cert/best-loft")]
    public async Task<IActionResult> BestLoftCert([FromBody] BestLoftCertReq r, CancellationToken ct)
        => Pdf(await _orch.RenderBestLoftCertAsync(r.ProgrammeId, r.FancierUserId, r.DesignId, r.Language, ct), $"best-loft-cert-{r.DesignId}-{r.Language}.pdf");

    // ── Result tables ─────────────────────────────────────────────────────
    public record RaceResultsReq(Guid RaceId, string DesignId, string Language);
    public record ProgrammeResultsReq(Guid ProgrammeId, string DesignId, string Language);
    public record RaceExcelReq(Guid RaceId, string Language);
    public record ProgrammeExcelReq(Guid ProgrammeId, string Language);

    [HttpPost("result/race/pdf")]
    public async Task<IActionResult> RaceResultsPdf([FromBody] RaceResultsReq r, CancellationToken ct)
        => Pdf(await _orch.RenderRaceResultsPdfAsync(r.RaceId, r.DesignId, r.Language, ct), $"race-result-{r.DesignId}-{r.Language}.pdf");

    [HttpPost("result/ace/pdf")]
    public async Task<IActionResult> AceResultsPdf([FromBody] ProgrammeResultsReq r, CancellationToken ct)
        => Pdf(await _orch.RenderAceResultsPdfAsync(r.ProgrammeId, r.DesignId, r.Language, ct), $"ace-result-{r.DesignId}-{r.Language}.pdf");

    [HttpPost("result/super-ace/pdf")]
    public async Task<IActionResult> SuperAceResultsPdf([FromBody] ProgrammeResultsReq r, CancellationToken ct)
        => Pdf(await _orch.RenderSuperAceResultsPdfAsync(r.ProgrammeId, r.DesignId, r.Language, ct), $"super-ace-result-{r.DesignId}-{r.Language}.pdf");

    [HttpPost("result/best-loft/pdf")]
    public async Task<IActionResult> BestLoftResultsPdf([FromBody] ProgrammeResultsReq r, CancellationToken ct)
        => Pdf(await _orch.RenderBestLoftResultsPdfAsync(r.ProgrammeId, r.DesignId, r.Language, ct), $"best-loft-result-{r.DesignId}-{r.Language}.pdf");

    [HttpPost("result/race/excel")]
    public async Task<IActionResult> RaceResultsExcel([FromBody] RaceExcelReq r, CancellationToken ct)
        => Excel(await _orch.RenderRaceResultsExcelAsync(r.RaceId, r.Language, ct), $"race-result-{r.Language}.xlsx");

    [HttpPost("result/ace/excel")]
    public async Task<IActionResult> AceResultsExcel([FromBody] ProgrammeExcelReq r, CancellationToken ct)
        => Excel(await _orch.RenderAceResultsExcelAsync(r.ProgrammeId, r.Language, ct), $"ace-result-{r.Language}.xlsx");

    [HttpPost("result/super-ace/excel")]
    public async Task<IActionResult> SuperAceResultsExcel([FromBody] ProgrammeExcelReq r, CancellationToken ct)
        => Excel(await _orch.RenderSuperAceResultsExcelAsync(r.ProgrammeId, r.Language, ct), $"super-ace-result-{r.Language}.xlsx");

    [HttpPost("result/best-loft/excel")]
    public async Task<IActionResult> BestLoftResultsExcel([FromBody] ProgrammeExcelReq r, CancellationToken ct)
        => Excel(await _orch.RenderBestLoftResultsExcelAsync(r.ProgrammeId, r.Language, ct), $"best-loft-result-{r.Language}.xlsx");

    // ── helpers ───────────────────────────────────────────────────────────
    private IActionResult Pdf(byte[] data, string fileName)   => File(data, "application/pdf", fileName);
    private IActionResult Excel(byte[] data, string fileName) => File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
}
