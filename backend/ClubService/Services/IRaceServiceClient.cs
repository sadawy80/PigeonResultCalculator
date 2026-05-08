using PRC.ClubService.DTOs;

namespace PRC.ClubService.Services;

public interface IRaceServiceClient
{
    Task<bool> RaceExistsAsync(Guid raceId, Guid clubId, CancellationToken ct);
    Task<string?> GetRaceNameAsync(Guid raceId, CancellationToken ct);
    Task<int> GetRaceResultCountAsync(Guid raceId, CancellationToken ct);
    Task<DateTime?> GetRaceActualReleaseTimeAsync(Guid raceId, CancellationToken ct);
    Task<List<RaceResultForCalculation>> GetPublishedResultsForProgrammeAsync(List<Guid> raceIds, CancellationToken ct);
    Task<bool> PigeonExistsAsync(string ringNumber, CancellationToken ct);
    Task<Guid?> GetPigeonIdAsync(string ringNumber, CancellationToken ct);
}
