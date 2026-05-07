using MediatR;
using Microsoft.EntityFrameworkCore;
using PigeonRacing.Application.Common;
using PigeonRacing.Application.Common.Interfaces;
using PigeonRacing.Domain.Entities;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.Application.Features.Results;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record RaceResultDto(
    Guid Id,
    Guid RaceId,
    string RaceName,
    Guid? CategoryId,
    string? CategoryName,
    Guid? UserId,
    string? FancierName,
    string RingNumber,
    string? PigeonName,
    string? PigeonSex,
    int? PigeonYearOfBirth,
    DateTime ArrivalTime,
    double DistanceKm,
    double VelocityMperMin,
    double VelocityKmH,
    int? ClubRank,
    int? CategoryRank,
    ResultStatus Status,
    bool IsDuplicate,
    bool IsLateArrival,
    bool HasInvalidTimestamp,
    string? ValidationNotes,
    DataIngestionType IngestionType);

public record RaceResultSummaryDto(
    Guid Id,
    string RingNumber,
    string? FancierName,
    double VelocityMperMin,
    double DistanceKm,
    int? ClubRank,
    int? CategoryRank,
    string? CategoryName,
    ResultStatus Status);

public record IngestionLogDto(
    Guid Id,
    Guid RaceId,
    DataIngestionType IngestionType,
    string? FileName,
    int TotalRowsRead,
    int SuccessfulRows,
    int FailedRows,
    int DuplicateRows,
    string? ErrorSummary,
    DateTime ProcessedAt,
    bool IsSuccess);

// ── Commands ──────────────────────────────────────────────────────────────────

public record AddManualResultCommand(
    Guid RaceId,
    Guid? CategoryId,
    string RingNumber,
    DateTime ArrivalTime,
    string? PigeonName,
    string? PigeonSex,
    int? PigeonYearOfBirth) : IRequest<Result<RaceResultDto>>;

public record IngestETSFileCommand(
    Guid RaceId,
    Guid? CategoryId,
    Stream FileStream,
    string FileName) : IRequest<Result<IngestionLogDto>>;

public record ProcessRaceResultsCommand(Guid RaceId) : IRequest<Result<ProcessingResultDto>>;

public record ProcessingResultDto(
    int TotalProcessed,
    int RankedEntries,
    int InvalidEntries,
    int DuplicateEntries);

public record DeleteRaceResultCommand(Guid ResultId) : IRequest<Result>;

public record LinkResultToFancierCommand(
    Guid ResultId,
    Guid UserId) : IRequest<Result<RaceResultDto>>;

// ── Queries ───────────────────────────────────────────────────────────────────

public record GetRaceResultsQuery(
    Guid RaceId,
    Guid? CategoryId,
    PagedQuery Paged) : IRequest<Result<PagedResult<RaceResultDto>>>;

public record GetFancierResultsQuery(
    Guid UserId,
    PagedQuery Paged) : IRequest<Result<PagedResult<RaceResultDto>>>;

public record GetIngestionLogsQuery(Guid RaceId) : IRequest<Result<List<IngestionLogDto>>>;

// ── Add Manual Result Handler ─────────────────────────────────────────────────

public class AddManualResultHandler : IRequestHandler<AddManualResultCommand, Result<RaceResultDto>>
{
    private readonly IAppDbContext _db;
    private readonly IVelocityCalculator _calc;
    private readonly ICurrentUserService _currentUser;

    public AddManualResultHandler(IAppDbContext db, IVelocityCalculator calc, ICurrentUserService currentUser)
    {
        _db = db;
        _calc = calc;
        _currentUser = currentUser;
    }

    public async Task<Result<RaceResultDto>> Handle(AddManualResultCommand cmd, CancellationToken ct)
    {
        var race = await _db.Races
            .Include(r => r.Club)
            .FirstOrDefaultAsync(r => r.Id == cmd.RaceId, ct);

        if (race == null) return Result.NotFound<RaceResultDto>("Race");
        if (race.Status == RaceStatus.Published)
            return Result.Failure<RaceResultDto>("Cannot add results to a published race.", "RACE_PUBLISHED");
        if (race.ActualReleaseTime == null)
            return Result.Failure<RaceResultDto>("Race has not been started (no release time set).", "NO_RELEASE_TIME");

        // Duplicate check
        var isDuplicate = await _db.RaceResults
            .AnyAsync(r => r.RaceId == cmd.RaceId && r.RingNumber == cmd.RingNumber, ct);

        // Distance from club loft to release point
        double distanceKm = 0;
        if (race.Club.Latitude.HasValue && race.Club.Longitude.HasValue)
        {
            distanceKm = _calc.CalculateDistance(
                race.Club.Latitude.Value, race.Club.Longitude.Value,
                race.ReleaseLatitude, race.ReleaseLongitude);
        }

        var flightDuration = cmd.ArrivalTime - race.ActualReleaseTime.Value;
        var isLate = flightDuration.TotalHours > 24;
        var hasInvalidTs = cmd.ArrivalTime < race.ActualReleaseTime;
        var velocity = (!hasInvalidTs && distanceKm > 0)
            ? _calc.Calculate(distanceKm, flightDuration)
            : 0;

        var result = new RaceResult
        {
            RaceId = cmd.RaceId,
            CategoryId = cmd.CategoryId,
            RingNumber = cmd.RingNumber,
            PigeonName = cmd.PigeonName,
            PigeonSex = cmd.PigeonSex,
            PigeonYearOfBirth = cmd.PigeonYearOfBirth,
            ArrivalTime = cmd.ArrivalTime,
            FlightDuration = hasInvalidTs ? null : flightDuration,
            DistanceKm = distanceKm,
            VelocityMperMin = velocity,
            Status = hasInvalidTs || isDuplicate ? ResultStatus.Rejected : ResultStatus.Validated,
            IsDuplicate = isDuplicate,
            IsLateArrival = isLate,
            HasInvalidTimestamp = hasInvalidTs,
            ValidationNotes = BuildValidationNotes(isDuplicate, isLate, hasInvalidTs),
            IngestionType = DataIngestionType.Manual,
            CreatedBy = _currentUser.UserId
        };

        _db.RaceResults.Add(result);
        await _db.SaveChangesAsync(ct);

        return Result.Success(await BuildDtoAsync(result.Id, ct));
    }

    private static string? BuildValidationNotes(bool dup, bool late, bool invalidTs)
    {
        var notes = new List<string>();
        if (dup) notes.Add("Duplicate ring number");
        if (late) notes.Add("Late arrival (>24h)");
        if (invalidTs) notes.Add("Arrival time before release time");
        return notes.Count > 0 ? string.Join("; ", notes) : null;
    }

    private async Task<RaceResultDto> BuildDtoAsync(Guid id, CancellationToken ct)
    {
        var r = await _db.RaceResults
            .Include(x => x.Race)
            .Include(x => x.Category)
            .Include(x => x.User)
            .FirstAsync(x => x.Id == id, ct);
        return r.ToDto();
    }
}

// ── ETS File Ingestion Handler ────────────────────────────────────────────────

public class IngestETSFileHandler : IRequestHandler<IngestETSFileCommand, Result<IngestionLogDto>>
{
    private readonly IAppDbContext _db;
    private readonly IETSFileParser _parser;
    private readonly IVelocityCalculator _calc;
    private readonly IFileStorageService _storage;
    private readonly ICurrentUserService _currentUser;

    public IngestETSFileHandler(IAppDbContext db, IETSFileParser parser,
        IVelocityCalculator calc, IFileStorageService storage, ICurrentUserService currentUser)
    {
        _db = db;
        _parser = parser;
        _calc = calc;
        _storage = storage;
        _currentUser = currentUser;
    }

    public async Task<Result<IngestionLogDto>> Handle(IngestETSFileCommand cmd, CancellationToken ct)
    {
        var race = await _db.Races
            .Include(r => r.Club)
            .FirstOrDefaultAsync(r => r.Id == cmd.RaceId, ct);

        if (race == null) return Result.NotFound<IngestionLogDto>("Race");
        if (race.ActualReleaseTime == null)
            return Result.Failure<IngestionLogDto>("Race has not been started.", "NO_RELEASE_TIME");

        // Store the original file
        cmd.FileStream.Position = 0;
        var rawFileUrl = await _storage.UploadAsync(
            cmd.FileStream, cmd.FileName, "application/octet-stream", $"ets/{cmd.RaceId}", ct);

        // Parse
        cmd.FileStream.Position = 0;
        var parseResult = await _parser.ParseAsync(cmd.FileStream, cmd.FileName, ct);

        // Get existing ring numbers to detect duplicates
        var existingRings = await _db.RaceResults
            .Where(r => r.RaceId == cmd.RaceId)
            .Select(r => r.RingNumber)
            .ToHashSetAsync(ct);

        // Compute club distance to release point once
        double baseDistanceKm = 0;
        if (race.Club.Latitude.HasValue && race.Club.Longitude.HasValue)
        {
            baseDistanceKm = _calc.CalculateDistance(
                race.Club.Latitude.Value, race.Club.Longitude.Value,
                race.ReleaseLatitude, race.ReleaseLongitude);
        }

        var newResults = new List<RaceResult>();
        var inSessionDuplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in parseResult.Rows.Where(r => !r.HasError))
        {
            var isDuplicate = existingRings.Contains(row.RingNumber)
                           || inSessionDuplicates.Contains(row.RingNumber);

            var flightDuration = row.ArrivalTime - race.ActualReleaseTime.Value;
            var hasInvalidTs = row.ArrivalTime < race.ActualReleaseTime;
            var isLate = flightDuration.TotalHours > 24;
            var velocity = (!hasInvalidTs && !isDuplicate && baseDistanceKm > 0)
                ? _calc.Calculate(baseDistanceKm, flightDuration)
                : 0;

            newResults.Add(new RaceResult
            {
                RaceId = cmd.RaceId,
                CategoryId = cmd.CategoryId,
                RingNumber = row.RingNumber,
                PigeonName = row.PigeonName,
                PigeonSex = row.Sex,
                PigeonYearOfBirth = row.YearOfBirth,
                ArrivalTime = row.ArrivalTime,
                FlightDuration = hasInvalidTs ? null : flightDuration,
                DistanceKm = baseDistanceKm,
                VelocityMperMin = velocity,
                IsDuplicate = isDuplicate,
                IsLateArrival = isLate,
                HasInvalidTimestamp = hasInvalidTs,
                Status = (hasInvalidTs || isDuplicate) ? ResultStatus.Rejected : ResultStatus.Validated,
                IngestionType = DataIngestionType.ETSFile,
                CreatedBy = _currentUser.UserId
            });

            inSessionDuplicates.Add(row.RingNumber);
        }

        if (newResults.Count > 0)
            await _db.RaceResults.AddRangeAsync(newResults, ct);

        var log = new DataIngestionLog
        {
            RaceId = cmd.RaceId,
            IngestionType = DataIngestionType.ETSFile,
            FileName = cmd.FileName,
            TotalRowsRead = parseResult.TotalRows,
            SuccessfulRows = parseResult.SuccessfulRows,
            FailedRows = parseResult.FailedRows,
            DuplicateRows = parseResult.DuplicateRows,
            ErrorSummary = parseResult.Errors.Count > 0
                ? System.Text.Json.JsonSerializer.Serialize(parseResult.Errors)
                : null,
            RawFileUrl = rawFileUrl,
            IsSuccess = parseResult.IsSuccess,
            ProcessedByUserId = _currentUser.UserId!.Value
        };

        _db.DataIngestionLogs.Add(log);
        await _db.SaveChangesAsync(ct);

        return Result.Success(log.ToDto());
    }
}

// ── Process Race Results (Ranking Engine) ─────────────────────────────────────

public class ProcessRaceResultsHandler : IRequestHandler<ProcessRaceResultsCommand, Result<ProcessingResultDto>>
{
    private readonly IAppDbContext _db;

    public ProcessRaceResultsHandler(IAppDbContext db) => _db = db;

    public async Task<Result<ProcessingResultDto>> Handle(ProcessRaceResultsCommand cmd, CancellationToken ct)
    {
        var race = await _db.Races.FirstOrDefaultAsync(r => r.Id == cmd.RaceId, ct);
        if (race == null) return Result.NotFound<ProcessingResultDto>("Race");

        var results = await _db.RaceResults
            .Where(r => r.RaceId == cmd.RaceId && !r.IsDeleted)
            .ToListAsync(ct);

        var validResults = results
            .Where(r => r.Status == ResultStatus.Validated || r.Status == ResultStatus.Published)
            .Where(r => !r.IsDuplicate && !r.HasInvalidTimestamp)
            .ToList();

        // ── Club-wide ranking (by velocity descending, then arrival time ascending for ties) ──
        var clubRanked = validResults
            .OrderByDescending(r => r.VelocityMperMin)
            .ThenBy(r => r.ArrivalTime)
            .ToList();

        for (int i = 0; i < clubRanked.Count; i++)
            clubRanked[i].ClubRank = i + 1;

        // ── Category ranking ──────────────────────────────────────────────────
        var categories = validResults
            .Where(r => r.CategoryId.HasValue)
            .GroupBy(r => r.CategoryId!.Value);

        foreach (var group in categories)
        {
            var catRanked = group
                .OrderByDescending(r => r.VelocityMperMin)
                .ThenBy(r => r.ArrivalTime)
                .ToList();

            for (int i = 0; i < catRanked.Count; i++)
                catRanked[i].CategoryRank = i + 1;
        }

        await _db.SaveChangesAsync(ct);

        return Result.Success(new ProcessingResultDto(
            results.Count,
            validResults.Count,
            results.Count(r => r.HasInvalidTimestamp),
            results.Count(r => r.IsDuplicate)));
    }
}

// ── Get Race Results Handler ──────────────────────────────────────────────────

public class GetRaceResultsHandler : IRequestHandler<GetRaceResultsQuery, Result<PagedResult<RaceResultDto>>>
{
    private readonly IAppDbContext _db;

    public GetRaceResultsHandler(IAppDbContext db) => _db = db;

    public async Task<Result<PagedResult<RaceResultDto>>> Handle(GetRaceResultsQuery query, CancellationToken ct)
    {
        var q = _db.RaceResults
            .Include(r => r.Race)
            .Include(r => r.Category)
            .Include(r => r.User)
            .Where(r => r.RaceId == query.RaceId && !r.IsDeleted);

        if (query.CategoryId.HasValue)
            q = q.Where(r => r.CategoryId == query.CategoryId);

        if (!string.IsNullOrEmpty(query.Paged.Search))
            q = q.Where(r => r.RingNumber.Contains(query.Paged.Search)
                           || (r.PigeonName != null && r.PigeonName.Contains(query.Paged.Search)));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(r => r.ClubRank ?? int.MaxValue)
            .ThenByDescending(r => r.VelocityMperMin)
            .Skip(query.Paged.Skip)
            .Take(query.Paged.PageSize)
            .Select(r => r.ToDto())
            .ToListAsync(ct);

        return Result.Success(new PagedResult<RaceResultDto>
        {
            Items = items,
            TotalCount = total,
            Page = query.Paged.Page,
            PageSize = query.Paged.PageSize
        });
    }
}

// ── Get Fancier Results Handler ───────────────────────────────────────────────

public class GetFancierResultsHandler : IRequestHandler<GetFancierResultsQuery, Result<PagedResult<RaceResultDto>>>
{
    private readonly IAppDbContext _db;

    public GetFancierResultsHandler(IAppDbContext db) => _db = db;

    public async Task<Result<PagedResult<RaceResultDto>>> Handle(GetFancierResultsQuery query, CancellationToken ct)
    {
        var q = _db.RaceResults
            .Include(r => r.Race)
            .Include(r => r.Category)
            .Include(r => r.User)
            .Where(r => r.UserId == query.UserId && r.Status == ResultStatus.Published);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(r => r.Race.PublishedAt)
            .Skip(query.Paged.Skip)
            .Take(query.Paged.PageSize)
            .Select(r => r.ToDto())
            .ToListAsync(ct);

        return Result.Success(new PagedResult<RaceResultDto>
        {
            Items = items,
            TotalCount = total,
            Page = query.Paged.Page,
            PageSize = query.Paged.PageSize
        });
    }
}

// ── Link Result to Fancier Handler ────────────────────────────────────────────

public class LinkResultToFancierHandler : IRequestHandler<LinkResultToFancierCommand, Result<RaceResultDto>>
{
    private readonly IAppDbContext _db;

    public LinkResultToFancierHandler(IAppDbContext db) => _db = db;

    public async Task<Result<RaceResultDto>> Handle(LinkResultToFancierCommand cmd, CancellationToken ct)
    {
        var result = await _db.RaceResults
            .Include(r => r.Race).Include(r => r.Category).Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == cmd.ResultId, ct);

        if (result == null) return Result.NotFound<RaceResultDto>("RaceResult");

        var userExists = await _db.Users.AnyAsync(u => u.Id == cmd.UserId, ct);
        if (!userExists) return Result.NotFound<RaceResultDto>("User");

        result.UserId = cmd.UserId;
        result.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result.Success(result.ToDto());
    }
}

// ── Delete Result Handler ─────────────────────────────────────────────────────

public class DeleteRaceResultHandler : IRequestHandler<DeleteRaceResultCommand, Result>
{
    private readonly IAppDbContext _db;

    public DeleteRaceResultHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(DeleteRaceResultCommand cmd, CancellationToken ct)
    {
        var result = await _db.RaceResults
            .Include(r => r.Race)
            .FirstOrDefaultAsync(r => r.Id == cmd.ResultId, ct);

        if (result == null) return Result.NotFound("RaceResult");
        if (result.Race.Status == RaceStatus.Published)
            return Result.Failure("Cannot delete results from a published race.", "RACE_PUBLISHED");

        result.IsDeleted = true;
        result.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── Ingestion Logs Handler ────────────────────────────────────────────────────

public class GetIngestionLogsHandler : IRequestHandler<GetIngestionLogsQuery, Result<List<IngestionLogDto>>>
{
    private readonly IAppDbContext _db;

    public GetIngestionLogsHandler(IAppDbContext db) => _db = db;

    public async Task<Result<List<IngestionLogDto>>> Handle(GetIngestionLogsQuery query, CancellationToken ct)
    {
        var logs = await _db.DataIngestionLogs
            .Where(l => l.RaceId == query.RaceId)
            .OrderByDescending(l => l.ProcessedAt)
            .Select(l => l.ToDto())
            .ToListAsync(ct);

        return Result.Success(logs);
    }
}

// ── Mapping extensions ────────────────────────────────────────────────────────

public static class ResultMappingExtensions
{
    public static RaceResultDto ToDto(this RaceResult r) => new(
        r.Id, r.RaceId, r.Race?.Name ?? string.Empty,
        r.CategoryId, r.Category?.Name,
        r.UserId, r.User?.FullName,
        r.RingNumber, r.PigeonName, r.PigeonSex, r.PigeonYearOfBirth,
        r.ArrivalTime, r.DistanceKm, r.VelocityMperMin, r.VelocityKmH,
        r.ClubRank, r.CategoryRank, r.Status,
        r.IsDuplicate, r.IsLateArrival, r.HasInvalidTimestamp,
        r.ValidationNotes, r.IngestionType);

    public static IngestionLogDto ToDto(this DataIngestionLog l) => new(
        l.Id, l.RaceId, l.IngestionType, l.FileName,
        l.TotalRowsRead, l.SuccessfulRows, l.FailedRows, l.DuplicateRows,
        l.ErrorSummary, l.ProcessedAt, l.IsSuccess);
}
