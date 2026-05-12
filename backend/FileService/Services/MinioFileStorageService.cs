using Minio;
using Minio.DataModel.Args;

namespace PRC.FileService.Services;

public class MinioSettings
{
    public string Endpoint     { get; set; } = "minio:9000";
    public string AccessKey    { get; set; } = "minioadmin";
    public string SecretKey    { get; set; } = "minioadmin";
    /// <summary>Public read bucket — logos, photos, anything safe to serve directly.</summary>
    public string Bucket        { get; set; } = "pigeon-files";
    /// <summary>Private bucket — backups + anything that requires presigned access.</summary>
    public string PrivateBucket { get; set; } = "pigeon-private";
    public bool   UseSsl        { get; set; } = false;
    /// <summary>External URL the public bucket is reachable on (used to build returned URLs).</summary>
    public string PublicUrl     { get; set; } = "http://localhost:9000";
}

public class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minio;
    private readonly string _publicBucket;
    private readonly string _privateBucket;
    private readonly string _publicUrl;
    private readonly ILogger<MinioFileStorageService> _log;

    public MinioFileStorageService(IConfiguration config, ILogger<MinioFileStorageService> log)
    {
        _log = log;
        var s = config.GetSection("Minio").Get<MinioSettings>() ?? new MinioSettings();
        _publicBucket  = s.Bucket;
        _privateBucket = s.PrivateBucket;
        _publicUrl     = s.PublicUrl.TrimEnd('/');

        var builder = new MinioClient()
            .WithEndpoint(s.Endpoint)
            .WithCredentials(s.AccessKey, s.SecretKey);
        if (s.UseSsl) builder = builder.WithSSL();
        _minio = builder.Build();
    }

    public async Task<string> UploadAsync(string fileName, string contentType, Stream data, long size, CancellationToken ct = default)
    {
        await EnsureBucketAsync(_publicBucket, anonymousRead: true, ct);
        var key = $"{Guid.NewGuid():N}/{fileName}";
        await _minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_publicBucket)
            .WithObject(key)
            .WithStreamData(data)
            .WithObjectSize(size)
            .WithContentType(contentType), ct);

        var url = $"{_publicUrl}/{_publicBucket}/{key}";
        _log.LogInformation("Stored public object {Key} ({Size:N0} bytes) at {Url}", key, size, url);
        return url;
    }

    public async Task<string> UploadKeyedAsync(string objectKey, string contentType, Stream data, long size, CancellationToken ct = default)
    {
        await EnsureBucketAsync(_privateBucket, anonymousRead: false, ct);
        await _minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_privateBucket)
            .WithObject(objectKey)
            .WithStreamData(data)
            .WithObjectSize(size)
            .WithContentType(contentType), ct);

        _log.LogInformation("Stored private object {Key} ({Size:N0} bytes)", objectKey, size);
        return objectKey;
    }

    public Task<string> GetPresignedUrlAsync(string objectKey, TimeSpan expiry, CancellationToken ct = default)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(_privateBucket)
            .WithObject(objectKey)
            .WithExpiry((int)expiry.TotalSeconds);
        return _minio.PresignedGetObjectAsync(args);
    }

    public async Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        try
        {
            await _minio.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(_publicBucket)
                .WithObject(objectKey), ct);
            _log.LogInformation("Deleted public object {Key}", objectKey);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not delete public object {Key}", objectKey);
        }
    }

    public async Task DeletePrivateAsync(string objectKey, CancellationToken ct = default)
    {
        try
        {
            await _minio.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(_privateBucket)
                .WithObject(objectKey), ct);
            _log.LogInformation("Deleted private object {Key}", objectKey);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not delete private object {Key}", objectKey);
        }
    }

    private async Task EnsureBucketAsync(string bucket, bool anonymousRead, CancellationToken ct)
    {
        var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), ct);
        if (exists) return;

        await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), ct);
        if (anonymousRead)
        {
            var policy = $$"""{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"AWS":["*"]},"Action":["s3:GetObject"],"Resource":["arn:aws:s3:::{{bucket}}/*"]}]}""";
            await _minio.SetPolicyAsync(new SetPolicyArgs().WithBucket(bucket).WithPolicy(policy), ct);
        }
    }
}
