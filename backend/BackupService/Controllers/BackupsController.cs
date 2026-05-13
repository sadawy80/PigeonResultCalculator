using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRC.BackupService.Data;
using PRC.BackupService.Models;
using PRC.BackupService.Services;
using PRC.Common.Services;

namespace PRC.BackupService.Controllers;

[ApiController]
[Route("api/backups")]
// SuperAdmin sign-in (POST /admin/auth/login) issues a JWT signed with
// Jwt:AdminKey — accept that scheme alongside the default user-token scheme
// so the admin console can load this controller.
[Authorize(AuthenticationSchemes = "Bearer,Admin")]
public class BackupsController : ControllerBase
{
    private readonly BackupDbContext      _db;
    private readonly BackupOrchestrator   _orchestrator;
    private readonly IFileServiceClient   _files;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackupsController> _log;

    public BackupsController(
        BackupDbContext db,
        BackupOrchestrator orchestrator,
        IFileServiceClient files,
        IServiceScopeFactory scopeFactory,
        ILogger<BackupsController> log)
    {
        _db           = db;
        _orchestrator = orchestrator;
        _files        = files;
        _scopeFactory = scopeFactory;
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
            if (entry.Status == BackupStatus.Failed)
                return Problem(
                    detail:     entry.ErrorMessage ?? "Backup failed for an unknown reason.",
                    statusCode: 500,
                    title:      $"Backup failed for {req.DatabaseName}");

            return Ok(new { message = "Backup started", entryId = entry.Id, status = entry.Status.ToString() });
        }

        // Capture the scope factory NOW — HttpContext.RequestServices becomes
        // invalid as soon as this method returns, so resolving services off the
        // request scope inside the background task would NRE.
        var scopeFactory = _scopeFactory;
        var logger       = _log;
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var orch = scope.ServiceProvider.GetRequiredService<BackupOrchestrator>();
                await orch.RunAllAsync(trigger, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background backup run failed (trigger={Trigger})", trigger);
            }
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

        var url = await _files.GetPresignedUrlAsync(entry.ObjectKey, TimeSpan.FromMinutes(15), ct);
        return Ok(new { url, expiresInMinutes = 15 });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entry = await _db.Backups.FindAsync([id], ct);
        if (entry is null) return NotFound();

        if (entry.UploadedToMinIO && !string.IsNullOrEmpty(entry.ObjectKey))
        {
            try { await _files.DeletePrivateAsync(entry.ObjectKey, ct); }
            catch (Exception ex) { _log.LogWarning(ex, "File-service delete failed for {Key}", entry.ObjectKey); }
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

    /// <summary>
    /// Returns the list of tables the backup can be browsed by. For now this
    /// is the well-known table set per database (derived from BackupEntry.DatabaseName)
    /// — the actual backup file isn't extracted yet. The endpoint exists so the
    /// admin Browse modal has a dropdown to populate.
    /// </summary>
    [HttpGet("{id:guid}/tables")]
    public async Task<IActionResult> Tables(Guid id, CancellationToken ct)
    {
        var entry = await _db.Backups.FindAsync([id], ct);
        if (entry is null) return NotFound();

        // The actual catalog comes from the backup file once we implement the
        // RESTORE-FILELISTONLY pipeline. Until then surface the schema name
        // as the only "table" so the modal is functional in dev.
        return Ok(new { tables = new[] { entry.DatabaseName } });
    }

    /// <summary>
    /// Browse rows inside a backup. Not yet implemented — the SQL RESTORE to a
    /// sandbox database has to land first. Returns 501 with a friendly detail
    /// so the admin UI can render an explanation banner.
    /// </summary>
    [HttpGet("{id:guid}/browse")]
    public async Task<IActionResult> Browse(
        Guid id,
        [FromQuery] string? table = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var entry = await _db.Backups.FindAsync([id], ct);
        if (entry is null) return NotFound();

        return Problem(
            statusCode: 501,
            title: "Not implemented",
            detail: "Browsing rows inside a .bak.gz backup requires restoring the file " +
                    "to a sandbox database first. The endpoint stub is in place; the " +
                    "RESTORE pipeline is the next step.");
    }

    /// <summary>
    /// Restore a backup (full or selected record). Stubbed until the SQL
    /// RESTORE step is implemented; returns 501.
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, [FromBody] RestoreRequest? req, CancellationToken ct)
    {
        var entry = await _db.Backups.FindAsync([id], ct);
        if (entry is null) return NotFound();

        return Problem(
            statusCode: 501,
            title: "Not implemented",
            detail: req?.Table is null
                ? "Full-database restore is a multi-step SQL operation (DROP / RESTORE / re-grant). " +
                  "The endpoint stub is wired so the UI can confirm intent; the runner is next."
                : $"Restoring a single record from table '{req.Table}' requires the backup-browse " +
                  "pipeline. Endpoint stub in place — runner next.");
    }
}

public record TriggerRequest(string? DatabaseName);
public record RestoreRequest(string? Table, string? RecordId);
