using ClosedXML.Excel;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.Common;
using PRC.Common.Messages;
using PRC.RenderingService.DTOs;
using PRC.RenderingService.Services;

namespace PRC.RenderingService.Controllers;

[Route("api/templates")]
[Authorize]
[ApiController]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateService _templates;
    private readonly IRenderService _render;
    private readonly IRequestClient<GetRaceForRenderRequest> _raceClient;

    public TemplatesController(
        ITemplateService templates,
        IRenderService render,
        IRequestClient<GetRaceForRenderRequest> raceClient)
    {
        _templates  = templates;
        _render     = render;
        _raceClient = raceClient;
    }

    private Guid CurrentUserId => Guid.TryParse(
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    private Guid? CurrentClubId
    {
        get
        {
            var v = User.FindFirst("club_id")?.Value;
            return Guid.TryParse(v, out var id) ? id : null;
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] TemplateCategory? category = null,
        [FromQuery] TemplateStyle? style = null,
        CancellationToken ct = default)
    {
        var result = await _templates.GetTemplatesAsync(category, style, false, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("{templateId:guid}")]
    public async Task<IActionResult> Get(Guid templateId, CancellationToken ct)
    {
        var result = await _templates.GetTemplateAsync(templateId, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost("{templateId:guid}/render")]
    public async Task<IActionResult> Render(
        Guid templateId,
        [FromBody] RenderRequest req,
        CancellationToken ct)
    {
        var result = await _render.RenderAsync(req with { TemplateId = templateId }, ct);
        if (!result.IsSuccess) return NotFound(result.Error);
        return Content(result.Value!.Html, "text/html");
    }

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
        var req = new RenderRequest(templateId, category, raceId, programmeId, raceResultId,
            recipientName, rank, null, locale);
        var result = await _render.RenderAsync(req, ct);
        if (!result.IsSuccess) return NotFound();

        var dir = locale == "ar" ? "rtl" : "ltr";
        var html = result.Value!.Html
            .Replace("</body>", "<script>window.onload=function(){window.print();};</script></body>")
            .Replace("<html>", $"<html dir=\"{dir}\" lang=\"{locale ?? "en"}\">")
            .Replace("<html ", $"<html dir=\"{dir}\" lang=\"{locale ?? "en"}\" ");

        return Content(html, "text/html");
    }

    [HttpPost("jobs")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> CreateJob([FromBody] RenderRequest req, CancellationToken ct)
    {
        var clubId = CurrentClubId ?? Guid.Empty;
        var result = await _templates.CreateJobAsync(req, clubId, CurrentUserId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("jobs/club/{clubId:guid}")]
    public async Task<IActionResult> GetJobs(
        Guid clubId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _templates.GetJobsAsync(clubId, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("export/race/{raceId:guid}")]
    public async Task<IActionResult> ExportRaceResults(Guid raceId, CancellationToken ct)
    {
        Response<RaceForRenderResult> resp;
        try
        {
            resp = await _raceClient.GetResponse<RaceForRenderResult>(
                new GetRaceForRenderRequest(raceId), ct);
        }
        catch
        {
            return StatusCode(503, "Race data unavailable");
        }

        var race = resp.Message;
        if (!race.Found) return NotFound("Race not found");

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Results");

        ws.Cell(1, 1).Value = "Race";
        ws.Cell(1, 2).Value = race.RaceName;
        ws.Cell(2, 1).Value = "Release Location";
        ws.Cell(2, 2).Value = race.ReleaseLocation ?? string.Empty;
        ws.Cell(3, 1).Value = "Release Time";
        ws.Cell(3, 2).Value = race.ActualReleaseTime?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty;
        ws.Cell(4, 1).Value = "Total Pigeons";
        ws.Cell(4, 2).Value = race.TotalPigeonsEntered;

        var headers = new[] { "Rank", "Ring Number", "Pigeon Name", "Sex", "Year", "Fancier",
                              "Distance (km)", "Speed (m/min)", "Speed (km/h)", "Arrival Time", "Category" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(6, i + 1).Value = headers[i];

        ws.Row(6).Style.Font.Bold = true;
        ws.Row(6).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

        for (int i = 0; i < race.Results.Count; i++)
        {
            var r   = race.Results[i];
            var row = i + 7;
            ws.Cell(row, 1).Value  = r.ClubRank?.ToString() ?? string.Empty;
            ws.Cell(row, 2).Value  = r.RingNumber;
            ws.Cell(row, 3).Value  = r.PigeonName ?? string.Empty;
            ws.Cell(row, 4).Value  = r.PigeonSex ?? string.Empty;
            ws.Cell(row, 5).Value  = r.PigeonYearOfBirth?.ToString() ?? string.Empty;
            ws.Cell(row, 6).Value  = string.Empty;
            ws.Cell(row, 7).Value  = Math.Round(r.DistanceKm, 3);
            ws.Cell(row, 8).Value  = Math.Round(r.SpeedMperMin, 2);
            ws.Cell(row, 9).Value  = Math.Round(r.SpeedMperMin * 60 / 1000, 3);
            ws.Cell(row, 10).Value = r.ArrivalTime.ToString("yyyy-MM-dd HH:mm:ss");
            ws.Cell(row, 11).Value = r.CategoryName;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"race-results-{raceId:N}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
