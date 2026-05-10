using System.IO.Compression;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PRC.BackupService.Data;
using PRC.BackupService.Models;

namespace PRC.BackupService.Services;

public class BackupOrchestrator
{
    private readonly BackupDbContext    _db;
    private readonly MinioStorageService  _minio;
    private readonly PCloudStorageService _pCloud;
    private readonly IConfiguration     _config;
    private readonly ILogger<BackupOrchestrator> _log;

    private static readonly string[] Databases =
    [
        "PRC_Identity", "PRC_Club", "PRC_Race", "PRC_Federation",
        "PRC_Rendering", "PRC_Integration", "PRC_Admin", "PRC_Subscription"
    ];

    public BackupOrchestrator(
        BackupDbContext db,
        MinioStorageService minio,
        PCloudStorageService pCloud,
        IConfiguration config,
        ILogger<BackupOrchestrator> log)
    {
        _db     = db;
        _minio  = minio;
        _pCloud = pCloud;
        _config = config;
        _log    = log;
    }

    public async Task RunAllAsync(string triggeredBy, CancellationToken ct = default)
    {
        _log.LogInformation("Backup run started — trigger: {Trigger}", triggeredBy);
        foreach (var db in Databases)
            await BackupDatabaseAsync(db, triggeredBy, ct);

        await PruneOldBackupsAsync(ct);
        _log.LogInformation("Backup run complete");
    }

    public async Task<BackupEntry> BackupDatabaseAsync(string databaseName, string triggeredBy, CancellationToken ct = default)
    {
        var entry = new BackupEntry { DatabaseName = databaseName, TriggeredBy = triggeredBy };
        _db.Backups.Add(entry);
        await _db.SaveChangesAsync(ct);

        var stagingRoot = _config["Backup:StagingPath"] ?? "/sqldata/backup";
        var ts          = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var bakFile     = Path.Combine(stagingRoot, $"{databaseName}_{ts}.bak");
        var gzFile      = bakFile + ".gz";

        try
        {
            Directory.CreateDirectory(stagingRoot);

            var masterConn = _config.GetConnectionString("MasterConnection")
                ?? _config.GetConnectionString("DefaultConnection")!
                    .Replace("PRC_Backup", "master", StringComparison.OrdinalIgnoreCase);

            _log.LogInformation("Backing up {Database}...", databaseName);
            await RunSqlBackupAsync(masterConn, databaseName, bakFile, ct);

            _log.LogInformation("Compressing {File}...", Path.GetFileName(bakFile));
            await GzipAsync(bakFile, gzFile, ct);
            File.Delete(bakFile);

            var fi        = new FileInfo(gzFile);
            var objectKey = $"{databaseName}/{ts}.bak.gz";

            await using (var fs = fi.OpenRead())
                await _minio.UploadAsync(objectKey, fs, fi.Length, ct);

            entry.UploadedToMinIO = true;
            entry.ObjectKey       = objectKey;
            entry.SizeBytes       = fi.Length;

            if (_pCloud.IsConfigured)
            {
                await using var fs2 = fi.OpenRead();
                await _pCloud.UploadAsync(Path.GetFileName(gzFile), fs2, ct);
                entry.UploadedToPCloud = true;
            }

            entry.Status      = BackupStatus.Completed;
            entry.CompletedAt = DateTime.UtcNow;
            _log.LogInformation("Backup completed: {Key} ({Size:N0} bytes)", objectKey, fi.Length);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Backup failed for {Database}", databaseName);
            entry.Status       = BackupStatus.Failed;
            entry.ErrorMessage = ex.Message;
        }
        finally
        {
            if (File.Exists(gzFile))  File.Delete(gzFile);
            if (File.Exists(bakFile)) File.Delete(bakFile);
            await _db.SaveChangesAsync(CancellationToken.None);
        }

        return entry;
    }

    private static async Task RunSqlBackupAsync(string connStr, string dbName, string outputPath, CancellationToken ct)
    {
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 1800;
        cmd.CommandText    = $"BACKUP DATABASE [{dbName}] TO DISK = N'{outputPath}' WITH COMPRESSION, STATS = 10";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task GzipAsync(string source, string dest, CancellationToken ct)
    {
        await using var input  = File.OpenRead(source);
        await using var output = File.Create(dest);
        await using var gz     = new GZipStream(output, CompressionLevel.Optimal);
        await input.CopyToAsync(gz, ct);
    }

    private async Task PruneOldBackupsAsync(CancellationToken ct)
    {
        var days   = _config.GetValue<int>("Backup:RetentionDays", 30);
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var old    = await _db.Backups
            .Where(b => b.CreatedAt < cutoff && b.Status == BackupStatus.Completed)
            .ToListAsync(ct);

        foreach (var e in old)
        {
            try
            {
                if (!string.IsNullOrEmpty(e.ObjectKey))
                    await _minio.DeleteAsync(e.ObjectKey, ct);
                _db.Backups.Remove(e);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to prune backup {Id}", e.Id);
            }
        }

        if (old.Count > 0)
            await _db.SaveChangesAsync(ct);

        _log.LogInformation("Pruned {Count} backup(s) older than {Days} days", old.Count, days);
    }
}
