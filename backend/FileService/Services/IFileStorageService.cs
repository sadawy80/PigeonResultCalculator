namespace PRC.FileService.Services;

/// <summary>
/// MinIO-backed object storage shared by every service that needs to upload
/// user files (logos, photos) or service-internal blobs (database backups).
///
/// Two distinct flows live behind this one interface:
///  - <see cref="UploadAsync"/> stores into the <b>public</b> bucket. The
///    service assigns a GUID-prefixed object key and returns a URL that
///    anyone can GET — the bucket policy permits anonymous reads. This is
///    the path used by the user-facing <c>POST /api/files/upload</c>.
///  - <see cref="UploadKeyedAsync"/> stores into the <b>private</b> bucket
///    using a caller-controlled object key (e.g. "PRC_Identity/20260512.bak.gz").
///    Used by the BackupService; private objects are only retrievable via
///    a short-lived <see cref="GetPresignedUrlAsync"/>.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Upload to the public bucket. Returns an absolute, publicly-readable URL.
    /// </summary>
    Task<string> UploadAsync(string fileName, string contentType, Stream data, long size, CancellationToken ct = default);

    /// <summary>
    /// Upload to the private bucket with a caller-controlled key. Returns the
    /// object key (same value the caller supplied) so callers can persist it.
    /// </summary>
    Task<string> UploadKeyedAsync(string objectKey, string contentType, Stream data, long size, CancellationToken ct = default);

    /// <summary>
    /// Generate a short-lived presigned GET URL for an object in the private
    /// bucket. Used to hand admins a one-off download link for backups.
    /// </summary>
    Task<string> GetPresignedUrlAsync(string objectKey, TimeSpan expiry, CancellationToken ct = default);

    /// <summary>Delete from the public bucket.</summary>
    Task DeleteAsync(string objectKey, CancellationToken ct = default);

    /// <summary>Delete from the private bucket.</summary>
    Task DeletePrivateAsync(string objectKey, CancellationToken ct = default);
}
