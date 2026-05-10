using Minio;
using Minio.DataModel.Args;

namespace PRC.BackupService.Services;

public class MinioStorageService
{
    private readonly IMinioClient _minio;
    private readonly string _bucket;
    private readonly ILogger<MinioStorageService> _log;

    public MinioStorageService(IMinioClient minio, IConfiguration config, ILogger<MinioStorageService> log)
    {
        _minio  = minio;
        _bucket = config["Minio:Bucket"] ?? "prc-backups";
        _log    = log;
    }

    public async Task UploadAsync(string objectKey, Stream data, long size, CancellationToken ct = default)
    {
        await EnsureBucketAsync(ct);
        var args = new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectKey)
            .WithStreamData(data)
            .WithObjectSize(size)
            .WithContentType("application/gzip");
        await _minio.PutObjectAsync(args, ct);
        _log.LogInformation("Uploaded {Key} ({Size:N0} bytes) to MinIO", objectKey, size);
    }

    public async Task<string> GetPresignedUrlAsync(string objectKey, TimeSpan expiry, CancellationToken ct = default)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectKey)
            .WithExpiry((int)expiry.TotalSeconds);
        return await _minio.PresignedGetObjectAsync(args);
    }

    public async Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        var args = new RemoveObjectArgs().WithBucket(_bucket).WithObject(objectKey);
        await _minio.RemoveObjectAsync(args, ct);
        _log.LogInformation("Deleted MinIO object {Key}", objectKey);
    }

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucket), ct);
        if (!exists)
            await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket), ct);
    }
}
