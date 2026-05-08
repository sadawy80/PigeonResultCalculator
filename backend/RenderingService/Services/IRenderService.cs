using PRC.Common;
using PRC.RenderingService.DTOs;

namespace PRC.RenderingService.Services;

public record RenderResult(string Html, TemplatePaperSize PaperSize, string TemplateName);

public interface IRenderService
{
    Task<Result<RenderResult>> RenderAsync(RenderRequest req, CancellationToken ct);
}
