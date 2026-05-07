using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PigeonRacing.Application.Features.Integration;

namespace PigeonRacing.Infrastructure.Services;

// ─────────────────────────────────────────────────────────────────────────────
//  ExternalPlatformCallbackService
//  Fires the webhook to PigeonLoftManager.com when the club manager
//  approves or rejects a link request.
//
//  Security: each callback includes:
//    - X-PRC-Link-Token: the LinkToken shared at request time
//    - X-PRC-Signature: HMAC-SHA256(body, webhookSecret) — if configured
//    - The accessToken (only on approval)
// ─────────────────────────────────────────────────────────────────────────────

public class ExternalPlatformCallbackService : IExternalPlatformCallbackService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExternalPlatformCallbackService> _logger;
    private readonly string? _webhookSecret;

    public ExternalPlatformCallbackService(
        IHttpClientFactory httpClientFactory,
        ILogger<ExternalPlatformCallbackService> logger,
        Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _webhookSecret = config["Integration:WebhookSecret"];
    }

    public async Task NotifyLinkReviewedAsync(
        string callbackUrl,
        string linkToken,
        string status,
        string? accessToken,
        string? rejectionReason,
        CancellationToken ct)
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

            var client = _httpClientFactory.CreateClient("PlatformCallback");

            var request = new HttpRequestMessage(HttpMethod.Post, callbackUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Add security headers
            request.Headers.Add("X-PRC-Link-Token", linkToken);

            if (!string.IsNullOrEmpty(_webhookSecret))
            {
                var signature = ComputeHmacSha256(json, _webhookSecret);
                request.Headers.Add("X-PRC-Signature", $"sha256={signature}");
            }

            var response = await client.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Webhook callback to {Url} returned {Status}. Link token: {Token}",
                    callbackUrl, (int)response.StatusCode, linkToken);
            }
            else
            {
                _logger.LogInformation(
                    "Webhook callback to {Url} succeeded. Status: {Status}, LinkToken: {Token}",
                    callbackUrl, status, linkToken);
            }
        }
        catch (Exception ex)
        {
            // Callback failure is non-fatal — log and continue
            _logger.LogError(ex,
                "Failed to fire webhook callback to {Url}. The club manager's decision was saved. LinkToken: {Token}",
                callbackUrl, linkToken);
        }
    }

    private static string ComputeHmacSha256(string data, string secret)
    {
        var keyBytes  = Encoding.UTF8.GetBytes(secret);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hash = HMACSHA256.HashData(keyBytes, dataBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
