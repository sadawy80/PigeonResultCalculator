using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.RenderingService.Data;
using PRC.RenderingService.DTOs;
using PRC.RenderingService.Models;

namespace PRC.RenderingService.Services;

public class TemplateService : ITemplateService
{
    private readonly RenderingDbContext _db;
    public TemplateService(RenderingDbContext db) => _db = db;

    public async Task<Result<List<PrintTemplateDto>>> GetTemplatesAsync(
        TemplateCategory? category, TemplateStyle? style, bool includeInactive, CancellationToken ct)
    {
        var q = _db.PrintTemplates.AsQueryable();
        if (!includeInactive) q = q.Where(t => t.IsActive);
        if (category.HasValue) q = q.Where(t => t.Category == category.Value);
        if (style.HasValue) q = q.Where(t => t.Style == style.Value);
        var templates = await q.OrderBy(t => t.SortOrder).ToListAsync(ct);
        return Result.Success(templates.Select(MapToDto).ToList());
    }

    public async Task<Result<PrintTemplateDto>> GetTemplateAsync(Guid id, CancellationToken ct)
    {
        var t = await _db.PrintTemplates.FirstOrDefaultAsync(x => x.Id == id, ct);
        return t == null ? Result.NotFound<PrintTemplateDto>("Template") : Result.Success(MapToDto(t));
    }

    public async Task<Result<PagedResult<PrintJobDto>>> GetJobsAsync(
        Guid clubId, int page, int pageSize, CancellationToken ct)
    {
        var q = _db.PrintJobs.Where(j => j.ClubId == clubId).OrderByDescending(j => j.CreatedAt);
        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var templateIds = items.Select(j => j.TemplateId).Distinct().ToList();
        var templateNames = await _db.PrintTemplates
            .Where(t => templateIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Name })
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        var dtos = items.Select(j => new PrintJobDto(
            j.Id, j.TemplateId, templateNames.GetValueOrDefault(j.TemplateId, ""),
            j.Category, j.Status, j.PdfUrl, j.FileSizeBytes, j.CreatedAt, j.CompletedAt)).ToList();

        return Result.Success(new PagedResult<PrintJobDto>
        {
            Items = dtos, TotalCount = total, Page = page, PageSize = pageSize
        });
    }

    public async Task<Result<PrintJobDto>> CreateJobAsync(
        RenderRequest req, Guid clubId, Guid userId, CancellationToken ct)
    {
        var template = await _db.PrintTemplates.FirstOrDefaultAsync(t => t.Id == req.TemplateId, ct);
        if (template == null) return Result.NotFound<PrintJobDto>("Template");

        var job = new PrintJob
        {
            TemplateId        = req.TemplateId,
            ClubId            = clubId,
            Category          = req.Category,
            Status            = PrintJobStatus.Pending,
            RaceId            = req.RaceId,
            ProgrammeId       = req.ProgrammeId,
            RaceResultId      = req.RaceResultId,
            GeneratedByUserId = userId,
            DataPayloadJson   = System.Text.Json.JsonSerializer.Serialize(req)
        };

        _db.PrintJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        return Result.Success(new PrintJobDto(
            job.Id, job.TemplateId, template.Name, job.Category,
            job.Status, null, null, job.CreatedAt, null));
    }

    private static PrintTemplateDto MapToDto(PrintTemplate t) => new(
        t.Id, t.Name, t.Description, t.Category, t.Category.ToString(),
        t.Style, t.Style.ToString(), t.PaperSize, t.PaperSize.ToString(),
        t.ColourScheme, t.PrimaryColour, t.SecondaryColour, t.ThumbnailUrl,
        t.MaxRows, t.IsMultiPage, t.IsSystem, t.SortOrder, t.VariableSchemaJson);
}
