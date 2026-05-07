using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PigeonRacing.Application.Common;
using PigeonRacing.Application.Features.Templates;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.API.Controllers;

// ─────────────────────────────────────────────────────────────────────────────
//  TemplatesController
//  Provides template browsing, HTML rendering, and print job management.
//  PDF generation is done client-side (browser print dialog) or via
//  the /render endpoint + a headless print service.
// ─────────────────────────────────────────────────────────────────────────────

[Route("api/templates")]
[Authorize]
public class TemplatesController : ApiControllerBase
{
    /// <summary>
    /// List all available templates, optionally filtered by category or style.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] TemplateCategory? category = null,
        [FromQuery] TemplateStyle? style = null,
        CancellationToken ct = default)
        => FromResult(await Mediator.Send(new GetTemplatesQuery(category, style), ct));

    /// <summary>
    /// Get a single template's metadata (not the HTML — use /render for that).
    /// </summary>
    [HttpGet("{templateId:guid}")]
    public async Task<IActionResult> Get(Guid templateId, CancellationToken ct)
        => FromResult(await Mediator.Send(new GetTemplateQuery(templateId), ct));

    /// <summary>
    /// Render a template with real data and return the substituted HTML.
    /// The client embeds this in an iframe and triggers window.print().
    /// </summary>
    [HttpPost("{templateId:guid}/render")]
    public async Task<IActionResult> Render(
        Guid templateId,
        [FromBody] RenderTemplateRequest req,
        CancellationToken ct)
    {
        var result = await Mediator.Send(
            new RenderTemplateCommand(req with { TemplateId = templateId }), ct);

        if (!result.IsSuccess) return FromResult(result);

        // Return raw HTML for iframe embedding
        return Content(result.Data!.Html, "text/html");
    }

    /// <summary>
    /// Render and return the full HTML page ready for printing.
    /// Sets Content-Disposition to trigger browser print on load.
    /// </summary>
    [HttpGet("{templateId:guid}/print")]
    public async Task<IActionResult> PrintPreview(
        Guid templateId,
        [FromQuery] Guid? raceId,
        [FromQuery] Guid? programmeId,
        [FromQuery] Guid? raceResultId,
        [FromQuery] TemplateCategory category = TemplateCategory.RaceResults,
        [FromQuery] string? recipientName = null,
        [FromQuery] string? rank = null,
        [FromQuery] string? locale = null,
        CancellationToken ct = default)
    {
        var req = new RenderTemplateRequest(
            templateId, category, raceId, programmeId, raceResultId,
            recipientName, rank, null, locale);

        var result = await Mediator.Send(new RenderTemplateCommand(req), ct);
        if (!result.IsSuccess) return NotFound();

        var dir = locale == "ar" ? "rtl" : "ltr";
        var html = result.Data!.Html.Replace("</body>",
            $"<script>window.onload = function() {{ window.print(); }};</script></body>");

        // Inject dir attribute for RTL locales
        html = html.Replace("<html>", $"<html dir=\"{dir}\" lang=\"{locale ?? "en"}\">");
        html = html.Replace("<html ", $"<html dir=\"{dir}\" lang=\"{locale ?? "en"}\" ");

        return Content(html, "text/html");
    }

    // ── Print Jobs ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Create a print job record (audit trail).
    /// </summary>
    [HttpPost("jobs")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> CreateJob(
        [FromBody] RenderTemplateRequest req,
        CancellationToken ct)
        => FromResult(await Mediator.Send(new CreatePrintJobCommand(req), ct));

    /// <summary>
    /// List print jobs for a club.
    /// </summary>
    [HttpGet("jobs/club/{clubId:guid}")]
    public async Task<IActionResult> GetJobs(
        Guid clubId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => FromResult(await Mediator.Send(new GetPrintJobsQuery(clubId, page, pageSize), ct));
}
