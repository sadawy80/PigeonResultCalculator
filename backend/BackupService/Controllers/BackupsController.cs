using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRC.BackupService.Data;
using PRC.BackupService.Models;
using PRC.BackupService.Services;

namespace PRC.BackupService.Controllers;

[ApiController]
[Route("api/backups")]
[Authorize]
public class BackupsController : ControllerBase
{
    private readonly BackupDbContext      _db;
    private readonly BackupOrchestrator   _orchestrator;
    private readonly MinioStorageService  _minio;
    private readonly ILogger<BackupsController> _log;

    public BackupsController(
        BackupDbContext db,
        BackupOrchestrator orchestrator,
        MinioStorageService minio,
        ILogger<BackupsController> log)
    {
        _db           = db;
        _orchestrator = orchestrator;
        _minio        = minio;
        _log          = log;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? database,
        [FromQuery] string? status,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var q = _db.Backups.AsQueryable();

        if (!string.IsNullOrEmpty(database))
            q = q.Where(b => b.DatabaseName == database);

        if (Enum.TryParse<BackupStatus>(status, ignoreCase: true, out var parsed))
            q = q.Where(b => b.Status == parsed);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                b.Id, b.DatabaseName, b.ObjectKey, b.SizeBytes,
                b.CreatedAt, b.CompletedAt, b.Status, b.ErrorMessage,
                b.UploadedToMinIO, b.UploadedToPCloud, b.TriggeredBy
            })
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize, items });
    }

    [HttpPost("trigger")]
    public async Task<IActionResult> Trigger([FromBody] TriggerRequest? req, CancellationToken ct)
    {
        var userId  = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var trigger = $"manual:{userId}";

        if (!string.IsNullOrEmpty(req?.DatabaseName))
        {
            var entry = await _orchestrator.BackupDatabaseAsync(req.DatabaseName, trigger, ct);
            return Ok(new { message = "Backup started", entryId = entry.Id, status = entry.Status.ToString() });
        }

        _ = Task.Run(async () =>
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var orch = scope.ServiceProvider.GetRequiredService<BackupOrchestrator>();
            await orch.RunAllAsync(trigger, CancellationToken.None);
        });

        return Accepted(new { message = "Full backup run started in background" });
    }

    [HttpGet("{id:guid}/download-url")]
    public async Task<IActionResult> DownloadUrl(Guid id, CancellationToken ct)
    {
        var entry = await _db.Backups.FindAsync([id], ct);
        if (entry is null) return NotFound();
        if (!entry.UploadedToMinIO || string.IsNullOrEmpty(entry.ObjectKey))
            return BadRequest(new { error = "Backup file not available in MinIO." });

        var url = await _minio.GetPresignedUrlAsync(entry.ObjectKey, TimeSpan.FromMinutes(15), ct);
        return Ok(new { url, expiresInMinutes = 15 });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entry = await _db.Backups.FindAsync([id], ct);
        if (entry is null) return NotFound();

        if (entry.UploadedToMinIO && !string.IsNullOrEmpty(entry.ObjectKey))
        {
            try { await _minio.DeleteAsync(entry.ObjectKey, ct); }
            catch (Exception ex) { _log.LogWarning(ex, "MinIO delete failed for {Key}", entry.ObjectKey); }
        }

        _db.Backups.Remove(entry);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("databases")]
    public IActionResult GetDatabases()
    {
        string[] dbs =
        [
            "PRC_Identity", "PRC_Club", "PRC_Race", "PRC_Federation",
            "PRC_Rendering", "PRC_Integration", "PRC_Admin", "PRC_Subscription"
        ];
        return Ok(dbs);
    }
}

public record TriggerRequest(string? DatabaseName);
