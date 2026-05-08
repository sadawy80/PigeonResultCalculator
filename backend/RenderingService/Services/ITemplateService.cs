using PRC.Common;
using PRC.RenderingService.DTOs;

namespace PRC.RenderingService.Services;

public interface ITemplateService
{
    Task<Result<List<PrintTemplateDto>>> GetTemplatesAsync(TemplateCategory? category, TemplateStyle? style, bool includeInactive, CancellationToken ct);
    Task<Result<PrintTemplateDto>> GetTemplateAsync(Guid id, CancellationToken ct);
    Task<Result<PagedResult<PrintJobDto>>> GetJobsAsync(Guid clubId, int page, int pageSize, CancellationToken ct);
    Task<Result<PrintJobDto>> CreateJobAsync(RenderRequest req, Guid clubId, Guid userId, CancellationToken ct);
}
