using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PRC.Common.Services;

public interface IFileStorageService
{
    /// <summary>Uploads a stream and returns the relative storage path.</summary>
    Task<string> UploadAsync(Stream content, string fileName, string contentType, string folder, CancellationToken ct = default);
    /// <summary>Downloads the file at the given storage path, or null if not found.</summary>
    Task<Stream?> DownloadAsync(string fileUrl, CancellationToken ct = default);
    Task DeleteAsync(string fileUrl, CancellationToken ct = default);
    /// <summary>Returns a URL the caller can use to download the file (may be signed).</summary>
    string GetAccessUrl(string fileUrl);
}

// ── Local-disk implementation (development / on-premise) ─────────────────────

public class LocalDiskFileStorageService : IFileStorageService
{
    private readonly string _baseDir;
    private readonly ILogger<LocalDiskFileStorageService> _logger;

    public LocalDiskFileStorageService(IConfiguration config, ILogger<LocalDiskFileStorageService> logger)
    {
        _baseDir = config["FileStorage:BasePath"] ?? "/app/uploads";
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, string folder, CancellationToken ct = default)
    {
        var dir = Path.Combine(_baseDir, folder);
        Directory.CreateDirectory(dir);

        var unique = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(dir, unique);

        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);

        var relPath = $"{folder}/{unique}";
        _logger.LogInformation("Stored file {RelPath}", relPath);
        return relPath;
    }

    public Task<Stream?> DownloadAsync(string fileUrl, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_baseDir, fileUrl);
        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        return Task.FromResult<Stream?>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_baseDir, fileUrl);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string GetAccessUrl(string fileUrl)
        => $"/api/files/{Uri.EscapeDataString(fileUrl)}";
}
