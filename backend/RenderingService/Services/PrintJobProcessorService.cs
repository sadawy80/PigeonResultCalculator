using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.RenderingService.Data;
using PRC.RenderingService.DTOs;
using PRC.RenderingService.Models;

namespace PRC.RenderingService.Services;

public class PrintJobProcessorService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PrintJobProcessorService> _logger;

    public PrintJobProcessorService(IServiceProvider services, ILogger<PrintJobProcessorService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await ProcessPendingJobsAsync(ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { _logger.LogError(ex, "Error processing print jobs"); }

            await Task.Delay(TimeSpan.FromSeconds(15), ct);
        }
    }

    private async Task ProcessPendingJobsAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db     = scope.ServiceProvider.GetRequiredService<RenderingDbContext>();
        var render = scope.ServiceProvider.GetRequiredService<IRenderService>();
        var pdf    = scope.ServiceProvider.GetRequiredService<IPdfGeneratorService>();

        var jobs = await db.PrintJobs
            .Where(j => j.Status == PrintJobStatus.Pending)
            .Take(5)
            .ToListAsync(ct);

        foreach (var job in jobs)
            await ProcessJobAsync(job, db, render, pdf, ct);
    }

    private async Task ProcessJobAsync(
        PrintJob job, RenderingDbContext db,
        IRenderService render, IPdfGeneratorService pdf, CancellationToken ct)
    {
        job.Status = PrintJobStatus.Rendering;
        await db.SaveChangesAsync(ct);

        try
        {
            var req = System.Text.Json.JsonSerializer.Deserialize<RenderRequest>(
                job.DataPayloadJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (req == null) throw new InvalidOperationException("Null render request in print job.");

            var renderResult = await render.RenderAsync(req, ct);
            if (!renderResult.IsSuccess || renderResult.Value == null)
                throw new InvalidOperationException($"Render failed: {renderResult.Error}");

            var pdfBytes = await pdf.GenerateFromHtmlAsync(renderResult.Value.Html, ct);

            job.PdfUrl        = $"/pdfs/{job.Id:N}.pdf";
            job.FileSizeBytes = pdfBytes.LongLength;
            job.Status        = PrintJobStatus.Complete;
            job.CompletedAt   = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process print job {JobId}", job.Id);
            job.Status       = PrintJobStatus.Failed;
            job.ErrorMessage = ex.Message;
        }

        await db.SaveChangesAsync(ct);
    }
}
