using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.Common;
using PRC.FileService.Filters;
using PRC.FileService.Services;

namespace PRC.FileService.Controllers;

/// <summary>
/// User-facing upload + service-to-service backup storage.
///
/// Public bucket: anyone may GET the returned URL directly from MinIO — the
/// bucket policy is open-read. Uploads still require a JWT.
///
/// Private bucket: only reachable through a presigned URL; never served
/// directly. Internal endpoints are gated by <see cref="ServiceKeyFilter"/>
/// (X-Service-Key header) because they're invoked by other PRC services,
/// not by end users.
/// </summary>
[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private static readonly string[] AllowedImageTypes =
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif"
    };
    private const long MaxImageBytes = 10 * 1024 * 1024;     // 10 MB
    private const long MaxBackupBytes = 5L * 1024 * 1024 * 1024; // 5 GB

    private readonly IFileStorageService _storage;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IFileStorageService storage, ILogger<FilesController> logger)
    {
        _storage = storage;
        _logger  = logger;
    }

    // ── User-facing image upload ─────────────────────────────────────────────
    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(MaxImageBytes)]
    public async Task<ActionResult<ApiResponse<UploadResult>>> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<UploadResult>.Fail("No file provided."));
        if (file.Length > MaxImageBytes)
            return BadRequest(ApiResponse<UploadResult>.Fail("File exceeds 10 MB limit."));
        if (!AllowedImageTypes.Contains(file.ContentType.ToLowerInvariant()))
            return BadRequest(ApiResponse<UploadResult>.Fail("Only JPEG, PNG, WebP, and GIF images are allowed."));

        var ext      = Path.GetExtension(file.FileName).ToLowerInvariant();
        var safeName = $"{Guid.NewGuid():N}{ext}";

        await using var stream = file.OpenReadStream();
        var url = await _storage.UploadAsync(safeName, file.ContentType, stream, file.Length, ct);

        _logger.LogInformation("File uploaded: {Url}", url);
        return Ok(ApiResponse<UploadResult>.Ok(new UploadResult(url), "File uploaded successfully."));
    }

    [HttpDelete]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object?>>> Delete([FromQuery] string objectKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return BadRequest(ApiResponse<object?>.Fail("objectKey is required."));
        await _storage.DeleteAsync(objectKey, ct);
        return Ok(ApiResponse<object?>.Ok(null, "File deleted."));
    }

    // ── Internal: service-to-service (X-Service-Key) ────────────────────────
    /// <summary>
    /// Upload a blob to the private bucket with a caller-controlled key.
    /// Body is streamed raw; <c>Content-Type</c> and <c>Content-Length</c>
    /// headers must be set by the caller.
    /// </summary>
    [HttpPut("internal/{**objectKey}")]
    [ServiceFilter(typeof(ServiceKeyFilter))]
    [RequestSizeLimit(MaxBackupBytes)]
    public async Task<ActionResult<ApiResponse<UploadResult>>> UploadKeyed(
        string objectKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return BadRequest(ApiResponse<UploadResult>.Fail("objectKey path segment is required."));

        var contentType = Request.ContentType ?? "application/octet-stream";
        var size        = Request.ContentLength ?? 0;
        if (size <= 0)
            return BadRequest(ApiResponse<UploadResult>.Fail("Content-Length header is required for keyed uploads."));

        var stored = await _storage.UploadKeyedAsync(objectKey, contentType, Request.Body, size, ct);
        return Ok(ApiResponse<UploadResult>.Ok(new UploadResult(stored)));
    }

    [HttpGet("internal/presigned-url")]
    [ServiceFilter(typeof(ServiceKeyFilter))]
    public async Task<ActionResult<ApiResponse<PresignedUrlResult>>> PresignedUrl(
        [FromQuery] string objectKey,
        [FromQuery] int    expiryMinutes = 15,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return BadRequest(ApiResponse<PresignedUrlResult>.Fail("objectKey is required."));
        var minutes = Math.Clamp(expiryMinutes, 1, 24 * 60);

        var url = await _storage.GetPresignedUrlAsync(objectKey, TimeSpan.FromMinutes(minutes), ct);
        return Ok(ApiResponse<PresignedUrlResult>.Ok(new PresignedUrlResult(url, minutes)));
    }

    [HttpDelete("internal")]
    [ServiceFilter(typeof(ServiceKeyFilter))]
    public async Task<ActionResult<ApiResponse<object?>>> DeletePrivate(
        [FromQuery] string objectKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return BadRequest(ApiResponse<object?>.Fail("objectKey is required."));
        await _storage.DeletePrivateAsync(objectKey, ct);
        return Ok(ApiResponse<object?>.Ok(null, "File deleted."));
    }
}

public record UploadResult(string Url);
public record PresignedUrlResult(string Url, int ExpiresInMinutes);
