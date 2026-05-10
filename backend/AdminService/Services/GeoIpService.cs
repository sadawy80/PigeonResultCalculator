using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace PRC.AdminService.Services;

public interface IGeoIpService
{
    Task<string?> GetCountryAsync(string? ipAddress, CancellationToken ct = default);
}

public class GeoIpService : IGeoIpService
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<GeoIpService> _log;
    private readonly ConcurrentDictionary<string, string?> _cache = new();

    public GeoIpService(IHttpClientFactory http, ILogger<GeoIpService> log)
    {
        _http = http;
        _log  = log;
    }

    public async Task<string?> GetCountryAsync(string? ipAddress, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return null;

        var ip = ipAddress.Split(',')[0].Trim();

        if (IsPrivate(ip))
            return "Local";

        if (_cache.TryGetValue(ip, out var cached))
            return cached;

        try
        {
            var client   = _http.CreateClient("GeoIp");
            var response = await client.GetStringAsync($"http://ip-api.com/json/{ip}?fields=country,status", ct);
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            string? country = null;
            if (root.TryGetProperty("status", out var status) && status.GetString() == "success"
                && root.TryGetProperty("country", out var countryEl))
            {
                country = countryEl.GetString();
            }

            _cache.TryAdd(ip, country);
            return country;
        }
        catch (Exception ex)
        {
            _log.LogWarning("GeoIP lookup failed for {Ip}: {Error}", ip, ex.Message);
            return null;
        }
    }

    private static bool IsPrivate(string ip)
    {
        if (ip == "::1" || ip == "127.0.0.1" || ip.StartsWith("::ffff:127."))
            return true;

        if (!IPAddress.TryParse(ip, out var addr))
            return false;

        var bytes = addr.GetAddressBytes();
        if (bytes.Length == 4)
        {
            return bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168);
        }

        return false;
    }
}
