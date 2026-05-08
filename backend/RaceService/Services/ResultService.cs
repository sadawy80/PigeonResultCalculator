using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.Common.Messages;
using PRC.RaceService.Data;
using PRC.RaceService.DTOs;
using PRC.RaceService.Hubs;
using PRC.RaceService.Models;

namespace PRC.RaceService.Services;

public interface IResultService
{
    Task<Result<RaceResultDto>> AddManualAsync(AddManualResultRequest req, Guid addedBy, CancellationToken ct);
    Task<Result<IngestionLogDto>> IngestETSFileAsync(Guid raceId, Guid? categoryId, Stream fileStream, string fileName, Guid processedBy, CancellationToken ct);
    Task<Result<ProcessingResultDto>> ProcessAsync(Guid raceId, CancellationToken ct);
    Task<Result<PagedResult<RaceResultDto>>> GetByRaceAsync(Guid raceId, Guid? categoryId, PagedQuery paged, CancellationToken ct);
    Task<Result<PagedResult<RaceResultDto>>> GetByFancierAsync(Guid userId, PagedQuery paged, CancellationToken ct);
    Task<Result<RaceResultDto>> LinkFancierAsync(Guid resultId, Guid userId, CancellationToken ct);
    Task<Result> DeleteAsync(Guid resultId, CancellationToken ct);
    Task<Result<List<IngestionLogDto>>> GetIngestionLogsAsync(Guid raceId, CancellationToken ct);
    Task<Result<List<RaceResultForProgramme>>> GetPublishedForProgrammeAsync(List<Guid> raceIds, CancellationToken ct);
}

public class ResultService : IResultService
{
    private readonly RaceDbContext _db;
    private readonly ISpeedCalculator _speed;
    private readonly IETSFileParser _parser;
    private readonly IHubContext<LiveRaceHub> _hub;
    private readonly IRequestClient<CheckResultLimitRequest> _limitClient;
    private readonly IRequestClient<IncrementResultUsageRequest> _incrementClient;

    public ResultService(
        RaceDbContext db,
        ISpeedCalculator speed,
        IETSFileParser parser,
        IHubContext<LiveRaceHub> hub,
        IRequestClient<CheckResultLimitRequest> limitClient,
        IRequestClient<IncrementResultUsageRequest> incrementClient)
    {
        _db              = db;
        _speed           = speed;
        _parser          = parser;
        _hub             = hub;
        _limitClient     = limitClient;
        _incrementClient = incrementClient;
    }

    public async Task<Result<RaceResultDto>> AddManualAsync(AddManualResultRequest req, Guid addedBy, CancellationToken ct)
    {
        var race = await _db.Races.Include(r => r.Categories)
            .FirstOrDefaultAsync(r => r.Id == req.RaceId, ct);
        if (race == null) return Result.NotFound<RaceResultDto>("Race");
        if (race.Status == RaceStatus.Published)
            return Result.Failure<RaceResultDto>("Cannot add results to a published race.", "RACE_PUBLISHED");
        if (race.ActualReleaseTime == null)
            return Result.Failure<RaceResultDto>("Race has not been started (no release time set).", "NO_RELEASE_TIME");

        // ── Subscription limit check ──────────────────────────────────────────
        try
        {
            var limitResp = await _limitClient.GetResponse<CheckResultLimitResult>(
                new CheckResultLimitRequest(race.ClubId, 1), ct);
            if (!limitResp.Message.Allowed)
                return Result.Failure<RaceResultDto>(limitResp.Message.Error ?? "Result limit reached.", "RESULT_LIMIT_EXCEEDED");
        }
        catch (RequestTimeoutException) { /* allow if subscription service unavailable */ }

        var isDuplicate = await _db.RaceResults.AnyAsync(r => r.RaceId == req.RaceId && r.RingNumber == req.RingNumber, ct);

        double distanceKm = 0;
        if (race.ClubLatitude.HasValue && race.ClubLongitude.HasValue)
            distanceKm = _speed.CalculateDistance(race.ClubLatitude.Value, race.ClubLongitude.Value, race.ReleaseLatitude, race.ReleaseLongitude);

        var flightDuration = req.ArrivalTime - race.ActualReleaseTime.Value;
        var isLate = flightDuration.TotalHours > 24;
        var hasInvalidTs = req.ArrivalTime < race.ActualReleaseTime;
        var speedMperMin = (!hasInvalidTs && distanceKm > 0) ? _speed.Calculate(distanceKm, flightDuration) : 0;

        var result = new RaceResult
        {
            RaceId = req.RaceId, CategoryId = req.CategoryId,
            RingNumber = req.RingNumber, PigeonName = req.PigeonName,
            PigeonSex = req.PigeonSex, PigeonYearOfBirth = req.PigeonYearOfBirth,
            ArrivalTime = req.ArrivalTime,
            FlightDuration = hasInvalidTs ? null : flightDuration,
            DistanceKm = distanceKm, SpeedMperMin = speedMperMin,
            Status = (hasInvalidTs || isDuplicate) ? ResultStatus.Rejected : ResultStatus.Validated,
            IsDuplicate = isDuplicate, IsLateArrival = isLate, HasInvalidTimestamp = hasInvalidTs,
            ValidationNotes = BuildValidationNotes(isDuplicate, isLate, hasInvalidTs),
            IngestionType = DataIngestionType.Manual, CreatedBy = addedBy
        };

        _db.RaceResults.Add(result);
        await _db.SaveChangesAsync(ct);

        // Fire-and-forget increment (don't block the response)
        _ = _incrementClient.GetResponse<IncrementResultUsageResult>(
            new IncrementResultUsageRequest(race.ClubId, 1));

        var dto = await BuildResultDtoAsync(result.Id, ct);
        await _hub.Clients.Group($"race-{req.RaceId}").SendAsync("NewResult", dto, ct);
        return Result.Success(dto);
    }

    public async Task<Result<IngestionLogDto>> IngestETSFileAsync(
        Guid raceId, Guid? categoryId, Stream fileStream, string fileName, Guid processedBy, CancellationToken ct)
    {
        var race = await _db.Races.FirstOrDefaultAsync(r => r.Id == raceId, ct);
        if (race == null) return Result.NotFound<IngestionLogDto>("Race");
        if (race.ActualReleaseTime == null)
            return Result.Failure<IngestionLogDto>("Race has not been started.", "NO_RELEASE_TIME");

        fileStream.Position = 0;
        var parseResult = await _parser.ParseAsync(fileStream, fileName, ct);

        var existingRings = await _db.RaceResults
            .Where(r => r.RaceId == raceId)
            .Select(r => r.RingNumber)
            .ToHashSetAsync(ct);

        double baseDistanceKm = 0;
        if (race.ClubLatitude.HasValue && race.ClubLongitude.HasValue)
            baseDistanceKm = _speed.CalculateDistance(race.ClubLatitude.Value, race.ClubLongitude.Value, race.ReleaseLatitude, race.ReleaseLongitude);

        var newResults = new List<RaceResult>();
        var inSessionDuplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in parseResult.Rows.Where(r => !r.HasError))
        {
            var isDuplicate = existingRings.Contains(row.RingNumber) || inSessionDuplicates.Contains(row.RingNumber);
            var flightDuration = row.ArrivalTime - race.ActualReleaseTime.Value;
            var hasInvalidTs = row.ArrivalTime < race.ActualReleaseTime;
            var isLate = flightDuration.TotalHours > 24;
            var speedMperMin = (!hasInvalidTs && !isDuplicate && baseDistanceKm > 0)
                ? _speed.Calculate(baseDistanceKm, flightDuration) : 0;

            newResults.Add(new RaceResult
            {
                RaceId = raceId, CategoryId = categoryId,
                RingNumber = row.RingNumber, PigeonName = row.PigeonName,
                PigeonSex = row.Sex, PigeonYearOfBirth = row.YearOfBirth,
                ArrivalTime = row.ArrivalTime,
                FlightDuration = hasInvalidTs ? null : flightDuration,
                DistanceKm = baseDistanceKm, SpeedMperMin = speedMperMin,
                IsDuplicate = isDuplicate, IsLateArrival = isLate, HasInvalidTimestamp = hasInvalidTs,
                Status = (hasInvalidTs || isDuplicate) ? ResultStatus.Rejected : ResultStatus.Validated,
                IngestionType = DataIngestionType.ETSFile, CreatedBy = processedBy
            });

            inSessionDuplicates.Add(row.RingNumber);
        }

        if (newResults.Count > 0)
        {
            // ── Bulk subscription limit check ─────────────────────────────────
            var validNewCount = newResults.Count(r => r.Status == ResultStatus.Validated);
            if (validNewCount > 0)
            {
                try
                {
                    var limitResp = await _limitClient.GetResponse<CheckResultLimitResult>(
                        new CheckResultLimitRequest(race.ClubId, validNewCount), ct);
                    if (!limitResp.Message.Allowed)
                        return Result.Failure<IngestionLogDto>(
                            limitResp.Message.Error ?? "Result limit reached.", "RESULT_LIMIT_EXCEEDED");
                }
                catch (RequestTimeoutException) { /* allow if unavailable */ }
            }

            await _db.RaceResults.AddRangeAsync(newResults, ct);
        }

        var log = new DataIngestionLog
        {
            RaceId = raceId, IngestionType = DataIngestionType.ETSFile,
            FileName = fileName, TotalRowsRead = parseResult.TotalRows,
            SuccessfulRows = parseResult.SuccessfulRows, FailedRows = parseResult.FailedRows,
            DuplicateRows = parseResult.DuplicateRows,
            ErrorSummary = parseResult.Errors.Count > 0
                ? System.Text.Json.JsonSerializer.Serialize(parseResult.Errors) : null,
            IsSuccess = parseResult.IsSuccess, ProcessedByUserId = processedBy
        };

        _db.DataIngestionLogs.Add(log);
        await _db.SaveChangesAsync(ct);

        var savedValidCount = newResults.Count(r => r.Status == ResultStatus.Validated);
        if (savedValidCount > 0)
            _ = _incrementClient.GetResponse<IncrementResultUsageResult>(
                new IncrementResultUsageRequest(race.ClubId, savedValidCount));

        if (newResults.Count > 0)
            await _hub.Clients.Group($"club-{race.ClubId}").SendAsync("ResultsBatchUploaded",
                new { raceId, count = newResults.Count }, ct);

        return Result.Success(log.ToDto());
    }

    public async Task<Result<ProcessingResultDto>> ProcessAsync(Guid raceId, CancellationToken ct)
    {
        var race = await _db.Races.FirstOrDefaultAsync(r => r.Id == raceId, ct);
        if (race == null) return Result.NotFound<ProcessingResultDto>("Race");

        var results = await _db.RaceResults.Where(r => r.RaceId == raceId && !r.IsDeleted).ToListAsync(ct);

        var validResults = results
            .Where(r => (r.Status == ResultStatus.Validated || r.Status == ResultStatus.Published)
                     && !r.IsDuplicate && !r.HasInvalidTimestamp)
            .ToList();

        var clubRanked = validResults.OrderByDescending(r => r.SpeedMperMin).ThenBy(r => r.ArrivalTime).ToList();
        for (int i = 0; i < clubRanked.Count; i++) clubRanked[i].ClubRank = i + 1;

        foreach (var group in validResults.Where(r => r.CategoryId.HasValue).GroupBy(r => r.CategoryId!.Value))
        {
            var catRanked = group.OrderByDescending(r => r.SpeedMperMin).ThenBy(r => r.ArrivalTime).ToList();
            for (int i = 0; i < catRanked.Count; i++) catRanked[i].CategoryRank = i + 1;
        }

        await _db.SaveChangesAsync(ct);
        await _hub.Clients.Group($"race-{raceId}").SendAsync("RaceStatusChanged", new { raceId, status = "ResultsProcessed" }, ct);

        return Result.Success(new ProcessingResultDto(
            results.Count, validResults.Count,
            results.Count(r => r.HasInvalidTimestamp),
            results.Count(r => r.IsDuplicate)));
    }

    public async Task<Result<PagedResult<RaceResultDto>>> GetByRaceAsync(Guid raceId, Guid? categoryId, PagedQuery paged, CancellationToken ct)
    {
        var q = _db.RaceResults.Include(r => r.Race).Include(r => r.Category)
            .Where(r => r.RaceId == raceId && !r.IsDeleted);

        if (categoryId.HasValue) q = q.Where(r => r.CategoryId == categoryId);
        if (!string.IsNullOrEmpty(paged.Search))
            q = q.Where(r => r.RingNumber.Contains(paged.Search) || (r.PigeonName != null && r.PigeonName.Contains(paged.Search)));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(r => r.ClubRank ?? int.MaxValue).ThenByDescending(r => r.SpeedMperMin)
            .Skip(paged.Skip).Take(paged.PageSize)
            .Select(r => r.ToDto())
            .ToListAsync(ct);

        return Result.Success(new PagedResult<RaceResultDto>
        {
            Items = items, TotalCount = total, Page = paged.Page, PageSize = paged.PageSize
        });
    }

    public async Task<Result<PagedResult<RaceResultDto>>> GetByFancierAsync(Guid userId, PagedQuery paged, CancellationToken ct)
    {
        var q = _db.RaceResults.Include(r => r.Race).Include(r => r.Category)
            .Where(r => r.UserId == userId && r.Status == ResultStatus.Published);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(r => r.Race.PublishedAt)
            .Skip(paged.Skip).Take(paged.PageSize)
            .Select(r => r.ToDto())
            .ToListAsync(ct);

        return Result.Success(new PagedResult<RaceResultDto>
        {
            Items = items, TotalCount = total, Page = paged.Page, PageSize = paged.PageSize
        });
    }

    public async Task<Result<RaceResultDto>> LinkFancierAsync(Guid resultId, Guid userId, CancellationToken ct)
    {
        var result = await _db.RaceResults.Include(r => r.Race).Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == resultId, ct);
        if (result == null) return Result.NotFound<RaceResultDto>("RaceResult");

        result.UserId = userId;
        result.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success(result.ToDto());
    }

    public async Task<Result> DeleteAsync(Guid resultId, CancellationToken ct)
    {
        var result = await _db.RaceResults.Include(r => r.Race)
            .FirstOrDefaultAsync(r => r.Id == resultId, ct);
        if (result == null) return Result.NotFound("RaceResult");
        if (result.Race.Status == RaceStatus.Published)
            return Result.Failure("Cannot delete results from a published race.", "RACE_PUBLISHED");

        result.IsDeleted = true;
        result.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<List<IngestionLogDto>>> GetIngestionLogsAsync(Guid raceId, CancellationToken ct)
    {
        var logs = await _db.DataIngestionLogs
            .Where(l => l.RaceId == raceId)
            .OrderByDescending(l => l.ProcessedAt)
            .Select(l => l.ToDto())
            .ToListAsync(ct);
        return Result.Success(logs);
    }

    public async Task<Result<List<RaceResultForProgramme>>> GetPublishedForProgrammeAsync(List<Guid> raceIds, CancellationToken ct)
    {
        var results = await _db.RaceResults
            .Include(r => r.Race)
            .Where(r => raceIds.Contains(r.RaceId) && r.Status == ResultStatus.Published && !r.IsDeleted)
            .ToListAsync(ct);

        var dtos = results.Select(r => new RaceResultForProgramme(
            r.RaceId, r.Race.Name, r.Id, r.RingNumber,
            r.UserId, null,
            r.SpeedMperMin, r.DistanceKm, r.ArrivalTime,
            r.ClubRank ?? 0,
            r.PigeonId, r.PigeonName, r.PigeonSex, r.PigeonYearOfBirth
        )).ToList();

        return Result.Success(dtos);
    }

    private async Task<RaceResultDto> BuildResultDtoAsync(Guid id, CancellationToken ct)
    {
        var r = await _db.RaceResults.Include(x => x.Race).Include(x => x.Category)
            .FirstAsync(x => x.Id == id, ct);
        return r.ToDto();
    }

    private static string? BuildValidationNotes(bool dup, bool late, bool invalidTs)
    {
        var notes = new List<string>();
        if (dup) notes.Add("Duplicate ring number");
        if (late) notes.Add("Late arrival (>24h)");
        if (invalidTs) notes.Add("Arrival time before release time");
        return notes.Count > 0 ? string.Join("; ", notes) : null;
    }
}

public static class ResultMappingExtensions
{
    public static RaceResultDto ToDto(this RaceResult r) => new(
        r.Id, r.RaceId, r.Race?.Name ?? string.Empty,
        r.CategoryId, r.Category?.Name,
        r.UserId, null,
        r.RingNumber, r.PigeonName, r.PigeonSex, r.PigeonYearOfBirth,
        r.ArrivalTime, r.DistanceKm, r.SpeedMperMin, r.SpeedKmH,
        r.ClubRank, r.CategoryRank, r.Status,
        r.IsDuplicate, r.IsLateArrival, r.HasInvalidTimestamp,
        r.ValidationNotes, r.IngestionType);

    public static IngestionLogDto ToDto(this DataIngestionLog l) => new(
        l.Id, l.RaceId, l.IngestionType, l.FileName,
        l.TotalRowsRead, l.SuccessfulRows, l.FailedRows, l.DuplicateRows,
        l.ErrorSummary, l.ProcessedAt, l.IsSuccess);
}
