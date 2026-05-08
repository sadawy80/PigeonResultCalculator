using MassTransit;
using PRC.Common;
using PRC.Common.Messages;
using PRC.IntegrationService.DTOs;

namespace PRC.IntegrationService.Services;

public interface IIntegrationDataService
{
    Task<Result<PagedResult<IntegrationRaceResultDto>>> GetRaceResultsAsync(
        Guid userId, Guid clubId, int page, int pageSize, CancellationToken ct);

    Task<Result<List<IntegrationAcePigeonDto>>> GetAcePigeonAsync(
        Guid userId, Guid clubId, CancellationToken ct);

    Task<Result<List<IntegrationSuperAceDto>>> GetSuperAceAsync(
        Guid userId, Guid clubId, CancellationToken ct);

    Task<Result<List<IntegrationBestLoftDto>>> GetBestLoftAsync(
        Guid userId, Guid clubId, CancellationToken ct);

    Task<Result<IntegrationSummaryDto>> GetSummaryAsync(
        Guid userId, Guid clubId, CancellationToken ct);
}

public class IntegrationDataService : IIntegrationDataService
{
    private readonly IRequestClient<GetFancierRaceResultsRequest> _raceClient;
    private readonly IRequestClient<GetFancierProgrammeResultsRequest> _progClient;

    public IntegrationDataService(
        IRequestClient<GetFancierRaceResultsRequest> raceClient,
        IRequestClient<GetFancierProgrammeResultsRequest> progClient)
    {
        _raceClient = raceClient;
        _progClient = progClient;
    }

    public async Task<Result<PagedResult<IntegrationRaceResultDto>>> GetRaceResultsAsync(
        Guid userId, Guid clubId, int page, int pageSize, CancellationToken ct)
    {
        var resp = await Ask<GetFancierRaceResultsRequest, FancierRaceResultsResponse>(
            _raceClient, new GetFancierRaceResultsRequest(userId, clubId, page, pageSize), ct);

        if (resp == null)
            return Result.Failure<PagedResult<IntegrationRaceResultDto>>("Could not reach RaceService.", "SERVICE_UNAVAILABLE");

        var dtos = resp.Items.Select(r => new IntegrationRaceResultDto(
            r.RingNumber, r.PigeonName, r.PigeonSex, r.PigeonYearOfBirth,
            r.RaceName, r.ReleaseLocation, r.RaceDate,
            r.DistanceKm,
            Math.Round(r.SpeedMperMin, 4),
            Math.Round(r.SpeedMperMin * 60.0 / 1000.0, 3),
            r.ClubRank, r.CategoryRank, r.CategoryName
        )).ToList();

        return Result.Success(new PagedResult<IntegrationRaceResultDto>
        {
            Items      = dtos,
            TotalCount = resp.TotalCount,
            Page       = page,
            PageSize   = pageSize
        });
    }

    public async Task<Result<List<IntegrationAcePigeonDto>>> GetAcePigeonAsync(
        Guid userId, Guid clubId, CancellationToken ct)
    {
        var resp = await Ask<GetFancierProgrammeResultsRequest, FancierProgrammeResultsResponse>(
            _progClient, new GetFancierProgrammeResultsRequest(userId, clubId), ct);

        if (resp == null)
            return Result.Failure<List<IntegrationAcePigeonDto>>("Could not reach ClubService.", "SERVICE_UNAVAILABLE");

        var dtos = resp.AcePigeonResults.Select(a => new IntegrationAcePigeonDto(
            a.RingNumber, a.PigeonName, a.PigeonSex, a.PigeonYearOfBirth,
            a.ProgrammeName, a.ProgrammeYear, a.AceRank,
            Math.Round(a.TotalScore, 2), Math.Round(a.AverageScore, 2),
            a.RacesEntered, a.RacesInProgramme,
            Math.Round(a.ParticipationRate, 1),
            Math.Round(a.BestSpeedMperMin, 4),
            Math.Round(a.AverageSpeedMperMin, 4),
            a.BestClubRank
        )).ToList();

        return Result.Success(dtos);
    }

    public async Task<Result<List<IntegrationSuperAceDto>>> GetSuperAceAsync(
        Guid userId, Guid clubId, CancellationToken ct)
    {
        var resp = await Ask<GetFancierProgrammeResultsRequest, FancierProgrammeResultsResponse>(
            _progClient, new GetFancierProgrammeResultsRequest(userId, clubId), ct);

        if (resp == null)
            return Result.Failure<List<IntegrationSuperAceDto>>("Could not reach ClubService.", "SERVICE_UNAVAILABLE");

        var dtos = resp.SuperAceResults.Select(a => new IntegrationSuperAceDto(
            a.RingNumber, a.PigeonName, a.PigeonSex, a.PigeonYearOfBirth,
            a.ProgrammeName, a.ProgrammeYear, a.SuperAceRank,
            Math.Round(a.TotalScore, 2), Math.Round(a.AverageScore, 2),
            a.RacesEntered, a.RacesInProgramme,
            Math.Round(a.ParticipationRate, 1),
            Math.Round(a.BestSpeedMperMin, 4),
            Math.Round(a.AverageSpeedMperMin, 4),
            a.BestClubRank
        )).ToList();

        return Result.Success(dtos);
    }

    public async Task<Result<List<IntegrationBestLoftDto>>> GetBestLoftAsync(
        Guid userId, Guid clubId, CancellationToken ct)
    {
        var resp = await Ask<GetFancierProgrammeResultsRequest, FancierProgrammeResultsResponse>(
            _progClient, new GetFancierProgrammeResultsRequest(userId, clubId), ct);

        if (resp == null)
            return Result.Failure<List<IntegrationBestLoftDto>>("Could not reach ClubService.", "SERVICE_UNAVAILABLE");

        var dtos = resp.BestLoftResults.Select(b => new IntegrationBestLoftDto(
            b.ProgrammeName, b.ProgrammeYear, b.LoftRank,
            Math.Round(b.TotalScore, 2), Math.Round(b.AverageScore, 2),
            b.RacesEntered, b.PigeonsEntered,
            Math.Round(b.BestSingleSpeedMperMin, 4),
            Math.Round(b.AverageSpeedMperMin, 4)
        )).ToList();

        return Result.Success(dtos);
    }

    public async Task<Result<IntegrationSummaryDto>> GetSummaryAsync(
        Guid userId, Guid clubId, CancellationToken ct)
    {
        var raceResp = await Ask<GetFancierRaceResultsRequest, FancierRaceResultsResponse>(
            _raceClient, new GetFancierRaceResultsRequest(userId, clubId, 1, 1), ct);

        var progResp = await Ask<GetFancierProgrammeResultsRequest, FancierProgrammeResultsResponse>(
            _progClient, new GetFancierProgrammeResultsRequest(userId, clubId), ct);

        DateTime? lastRaceDate = raceResp?.Items.Count > 0 ? raceResp.Items[0].RaceDate : null;

        return Result.Success(new IntegrationSummaryDto(
            TotalRaceResults            : raceResp?.TotalCount ?? 0,
            TotalAcePigeonResults       : progResp?.AcePigeonResults.Count ?? 0,
            TotalSuperAcePigeonResults  : progResp?.SuperAceResults.Count ?? 0,
            TotalBestLoftResults        : progResp?.BestLoftResults.Count ?? 0,
            LastRaceDate                : lastRaceDate
        ));
    }

    private static async Task<T?> Ask<TRequest, T>(
        IRequestClient<TRequest> client, TRequest request, CancellationToken ct)
        where TRequest : class
        where T : class
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            var response = await client.GetResponse<T>(request, cts.Token);
            return response.Message;
        }
        catch { return null; }
    }
}
