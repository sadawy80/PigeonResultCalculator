using MassTransit;
using PRC.ClubService.DTOs;
using PRC.Common.Messages;

namespace PRC.ClubService.Services;

public class BusRaceServiceClient : IRaceServiceClient
{
    private readonly IRequestClient<GetRaceSnapshotRequest> _snapshotClient;
    private readonly IRequestClient<GetPublishedResultsForProgrammeRequest> _resultsClient;
    private readonly IRequestClient<GetPigeonLookupRequest> _pigeonClient;
    private readonly ILogger<BusRaceServiceClient> _logger;

    public BusRaceServiceClient(
        IRequestClient<GetRaceSnapshotRequest> snapshotClient,
        IRequestClient<GetPublishedResultsForProgrammeRequest> resultsClient,
        IRequestClient<GetPigeonLookupRequest> pigeonClient,
        ILogger<BusRaceServiceClient> logger)
    {
        _snapshotClient = snapshotClient;
        _resultsClient  = resultsClient;
        _pigeonClient   = pigeonClient;
        _logger         = logger;
    }

    public async Task<bool> RaceExistsAsync(Guid raceId, Guid clubId, CancellationToken ct)
    {
        var snap = await AskSnapshot(raceId, ct);
        return snap?.Found == true && snap.ClubId == clubId;
    }

    public async Task<string?> GetRaceNameAsync(Guid raceId, CancellationToken ct)
    {
        var snap = await AskSnapshot(raceId, ct);
        return snap?.Found == true ? snap.Name : null;
    }

    public async Task<int> GetRaceResultCountAsync(Guid raceId, CancellationToken ct)
    {
        var snap = await AskSnapshot(raceId, ct);
        return snap?.ResultCount ?? 0;
    }

    public async Task<DateTime?> GetRaceActualReleaseTimeAsync(Guid raceId, CancellationToken ct)
    {
        var snap = await AskSnapshot(raceId, ct);
        return snap?.Found == true ? snap.ActualReleaseTime : null;
    }

    public async Task<List<RaceResultForCalculation>> GetPublishedResultsForProgrammeAsync(
        List<Guid> raceIds, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            var resp = await _resultsClient.GetResponse<PublishedResultsForProgrammeResult>(
                new GetPublishedResultsForProgrammeRequest(raceIds), cts.Token);
            return resp.Message.Items.Select(i => new RaceResultForCalculation(
                i.RaceId, i.RaceName, i.ResultId, i.RingNumber,
                i.UserId, null, i.SpeedMperMin, i.DistanceKm,
                i.ArrivalTime, i.ClubRank, i.PigeonId, i.PigeonName, i.PigeonSex, i.PigeonYearOfBirth
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch results for programme calculation");
            return new List<RaceResultForCalculation>();
        }
    }

    public async Task<bool> PigeonExistsAsync(string ringNumber, CancellationToken ct)
    {
        var result = await AskPigeon(ringNumber, ct);
        return result?.Found == true;
    }

    public async Task<Guid?> GetPigeonIdAsync(string ringNumber, CancellationToken ct)
    {
        var result = await AskPigeon(ringNumber, ct);
        return result?.Found == true ? result.PigeonId : null;
    }

    private async Task<RaceSnapshotResult?> AskSnapshot(Guid raceId, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            var resp = await _snapshotClient.GetResponse<RaceSnapshotResult>(
                new GetRaceSnapshotRequest(raceId), cts.Token);
            return resp.Message;
        }
        catch { return null; }
    }

    private async Task<PigeonLookupResult?> AskPigeon(string ringNumber, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            var resp = await _pigeonClient.GetResponse<PigeonLookupResult>(
                new GetPigeonLookupRequest(ringNumber), cts.Token);
            return resp.Message;
        }
        catch { return null; }
    }
}
