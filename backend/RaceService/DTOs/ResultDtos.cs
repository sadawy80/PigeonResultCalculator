using PRC.Common;

namespace PRC.RaceService.DTOs;

public record RaceResultDto(
    Guid Id, Guid RaceId, string RaceName,
    Guid? CategoryId, string? CategoryName,
    Guid? UserId, string? FancierName,
    string RingNumber, string? PigeonName, string? PigeonSex, int? PigeonYearOfBirth,
    DateTime ArrivalTime, double DistanceKm,
    double SpeedMperMin, double SpeedKmH,
    int? ClubRank, int? CategoryRank,
    ResultStatus Status, bool IsDuplicate, bool IsLateArrival, bool HasInvalidTimestamp,
    string? ValidationNotes, DataIngestionType IngestionType);

public record IngestionLogDto(
    Guid Id, Guid RaceId, DataIngestionType IngestionType, string? FileName,
    int TotalRowsRead, int SuccessfulRows, int FailedRows, int DuplicateRows,
    string? ErrorSummary, DateTime ProcessedAt, bool IsSuccess);

public record ProcessingResultDto(
    int TotalProcessed, int RankedEntries, int InvalidEntries, int DuplicateEntries);

// Cross-service: used by ClubService for programme calculations
public record RaceResultForProgramme(
    Guid RaceId, string RaceName,
    Guid ResultId, string RingNumber,
    Guid? UserId, string? UserFullName,
    double SpeedMperMin, double DistanceKm,
    DateTime? ArrivalTime, int ClubRank,
    Guid? PigeonId, string? PigeonName, string? PigeonSex, int? PigeonYearOfBirth);

// Requests
public record AddManualResultRequest(
    Guid RaceId, Guid? CategoryId, string RingNumber, DateTime ArrivalTime,
    string? PigeonName, string? PigeonSex, int? PigeonYearOfBirth);

public record LinkFancierRequest(Guid UserId);
