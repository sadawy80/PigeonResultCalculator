using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PRC.IntegrationService.Services;

public interface IExternalPlatformCallbackService
{
    Task NotifyLinkReviewedAsync(
        string callbackUrl, string linkToken, string status,
        string? accessToken, string? rejectionReason, CancellationToken ct);
}

public class ExternalPlatformCallbackService : IExternalPlatformCallbackService
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<ExternalPlatformCallbackService> _logger;
    private readonly string? _webhookSecret;

    public ExternalPlatformCallbackService(
        IHttpClientFactory http,
        ILogger<ExternalPlatformCallbackService> logger,
        IConfiguration config)
    {
        _http = http;
        _logger = logger;
        _webhookSecret = config["Integration:WebhookSecret"];
    }

    public async Task NotifyLinkReviewedAsync(
        string callbackUrl, string linkToken, string status,
        string? accessToken, string? rejectionReason, CancellationToken ct)
    {
        try
        {
            var payload = new
            {
                linkToken,
                status,
                accessToken,
                rejectionReason,
                reviewedAt = DateTime.UtcNow.ToString("O"),
                apiBaseUrl = "https://pigeonresultcalculator.com/api/integrations"
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var client = _http.CreateClient("PlatformCallback");
            var request = new HttpRequestMessage(HttpMethod.Post, callbackUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("X-PRC-Link-Token", linkToken);

            if (!string.IsNullOrEmpty(_webhookSecret))
            {
                var sig = ComputeHmacSha256(json, _webhookSecret);
                request.Headers.Add("X-PRC-Signature", $"sha256={sig}");
            }

            var response = await client.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Webhook to {Url} returned {Status}. Token: {Token}",
                    callbackUrl, (int)response.StatusCode, linkToken);
            else
                _logger.LogInformation("Webhook to {Url} succeeded. Status: {Status}, Token: {Token}",
                    callbackUrl, status, linkToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook to {Url} failed. Token: {Token}", callbackUrl, linkToken);
        }
    }

    private static string ComputeHmacSha256(string data, string secret)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
