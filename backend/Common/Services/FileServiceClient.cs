using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PRC.Common.Services;

/// <summary>
/// Thin typed-HttpClient wrapper that other PRC services use to talk to the
/// FileService. Uses the same <c>ServiceConfig:InternalApiKey</c> + <c>X-Service-Key</c>
/// pattern the rest of the PRC stack already uses for service-to-service calls.
///
/// Two flows:
///  - <see cref="UploadPublicAsync"/> = public bucket, returns a publicly-readable URL.
///  - <see cref="UploadKeyedAsync"/> + <see cref="GetPresignedUrlAsync"/> = private
///    bucket, used by BackupService and anything else that needs deterministic keys.
/// </summary>
public interface IFileServiceClient
{
    Task<string> UploadPublicAsync(string fileName, string contentType, Stream data, long size, CancellationToken ct = default);
    Task<string> UploadKeyedAsync(string objectKey, string contentType, Stream data, long size, CancellationToken ct = default);
    Task<string> GetPresignedUrlAsync(string objectKey, TimeSpan expiry, CancellationToken ct = default);
    Task DeletePublicAsync(string objectKey, CancellationToken ct = default);
    Task DeletePrivateAsync(string objectKey, CancellationToken ct = default);
}

public class FileServiceClient : IFileServiceClient
{
    private readonly HttpClient _http;
    public FileServiceClient(HttpClient http) => _http = http;

    public async Task<string> UploadPublicAsync(string fileName, string contentType, Stream data, long size, CancellationToken ct = default)
    {
        using var form    = new MultipartFormDataContent();
        var streamContent = new StreamContent(data);
        streamContent.Headers.ContentType   = MediaTypeHeaderValue.Parse(contentType);
        streamContent.Headers.ContentLength = size;
        form.Add(streamContent, "file", fileName);

        var resp = await _http.PostAsync("/api/files/upload", form, ct);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<UploadResultDto>>(cancellationToken: ct);
        return body?.Data?.Url ?? throw new InvalidOperationException("FileService returned no URL.");
    }

    public async Task<string> UploadKeyedAsync(string objectKey, string contentType, Stream data, long size, CancellationToken ct = default)
    {
        using var content = new StreamContent(data);
        content.Headers.ContentType   = MediaTypeHeaderValue.Parse(contentType);
        content.Headers.ContentLength = size;

        var resp = await _http.PutAsync($"/api/files/internal/{Uri.EscapeDataString(objectKey)}", content, ct);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<UploadResultDto>>(cancellationToken: ct);
        return body?.Data?.Url ?? objectKey;
    }

    public async Task<string> GetPresignedUrlAsync(string objectKey, TimeSpan expiry, CancellationToken ct = default)
    {
        var minutes = Math.Max(1, (int)expiry.TotalMinutes);
        var resp = await _http.GetAsync(
            $"/api/files/internal/presigned-url?objectKey={Uri.EscapeDataString(objectKey)}&expiryMinutes={minutes}", ct);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PresignedUrlDto>>(cancellationToken: ct);
        return body?.Data?.Url ?? throw new InvalidOperationException("FileService returned no presigned URL.");
    }

    public async Task DeletePublicAsync(string objectKey, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/api/files?objectKey={Uri.EscapeDataString(objectKey)}", ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task DeletePrivateAsync(string objectKey, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/api/files/internal?objectKey={Uri.EscapeDataString(objectKey)}", ct);
        resp.EnsureSuccessStatusCode();
    }

    private record UploadResultDto(string Url);
    private record PresignedUrlDto(string Url, int ExpiresInMinutes);
}

public static class FileServiceClientExtensions
{
    /// <summary>
    /// Registers <see cref="IFileServiceClient"/> backed by a typed HttpClient
    /// that points at FileService and forwards the shared <c>X-Service-Key</c>.
    /// Reads <c>FileService:BaseUrl</c> and <c>ServiceConfig:InternalApiKey</c>
    /// from configuration.
    /// </summary>
    public static IServiceCollection AddFileServiceClient(this IServiceCollection services, IConfiguration config)
    {
        services.TryAddSingleton<IFileServiceClient>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("FileService");
            return new FileServiceClient(http);
        });

        services.AddHttpClient("FileService", c =>
            {
                c.BaseAddress = new Uri(config["FileService:BaseUrl"] ?? "http://file-service:9512");
                c.Timeout     = TimeSpan.FromMinutes(30); // backups can be large
                var key = config["ServiceConfig:InternalApiKey"];
                if (!string.IsNullOrEmpty(key))
                    c.DefaultRequestHeaders.Add("X-Service-Key", key);
            });

        return services;
    }
}
