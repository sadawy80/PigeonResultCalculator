using System.Net.Http.Json;
using PRC.Common;
using PRC.ClubService.DTOs;

namespace PRC.ClubService.Services;

public class RaceServiceClient : IRaceServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<RaceServiceClient> _logger;

    public RaceServiceClient(HttpClient http, ILogger<RaceServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<bool> RaceExistsAsync(Guid raceId, Guid clubId, CancellationToken ct)
    {
        try
        {
            var resp = await _http.GetAsync($"/api/races/{raceId}/exists?clubId={clubId}", ct);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify race {RaceId} from RaceService", raceId);
            return false;
        }
    }

    public async Task<string?> GetRaceNameAsync(Guid raceId, CancellationToken ct)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<ApiResponse<RaceSnapshotDto>>($"/api/races/{raceId}/snapshot", ct);
            return result?.Data?.Name;
        }
        catch { return null; }
    }

    public async Task<int> GetRaceResultCountAsync(Guid raceId, CancellationToken ct)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<ApiResponse<int>>($"/api/races/{raceId}/result-count", ct);
            return result?.Data ?? 0;
        }
        catch { return 0; }
    }

    public async Task<DateTime?> GetRaceActualReleaseTimeAsync(Guid raceId, CancellationToken ct)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<ApiResponse<RaceSnapshotDto>>($"/api/races/{raceId}/snapshot", ct);
            return result?.Data?.ActualReleaseTime;
        }
        catch { return null; }
    }

    public async Task<List<RaceResultForCalculation>> GetPublishedResultsForProgrammeAsync(List<Guid> raceIds, CancellationToken ct)
    {
        try
        {
            var ids = string.Join(",", raceIds);
            var result = await _http.GetFromJsonAsync<ApiResponse<List<RaceResultForCalculation>>>(
                $"/api/results/for-programme?raceIds={ids}", ct);
            return result?.Data ?? new List<RaceResultForCalculation>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch results for programme calculation");
            return new List<RaceResultForCalculation>();
        }
    }

    public async Task<bool> PigeonExistsAsync(string ringNumber, CancellationToken ct)
    {
        try
        {
            var resp = await _http.GetAsync($"/api/pigeons/exists?ringNumber={Uri.EscapeDataString(ringNumber)}", ct);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<Guid?> GetPigeonIdAsync(string ringNumber, CancellationToken ct)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<ApiResponse<Guid?>>($"/api/pigeons/id?ringNumber={Uri.EscapeDataString(ringNumber)}", ct);
            return result?.Data;
        }
        catch { return null; }
    }

    private record RaceSnapshotDto(Guid Id, string Name, DateTime? ActualReleaseTime);
}
