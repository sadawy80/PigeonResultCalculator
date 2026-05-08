using PRC.Common;

namespace PRC.FederationService.DTOs;

// ── Federation DTOs ───────────────────────────────────────────────────────────

public record FederationDto(
    Guid Id,
    string Name,
    string Code,
    string Slug,
    string FlagUrl,
    string DefaultLanguage,
    string DefaultTimezone,
    string DefaultDistanceUnit,
    bool IsActive);

// ── Federation Result DTOs ────────────────────────────────────────────────────

public record FederationResultDto(
    Guid Id,
    Guid FederationId,
    string FederationName,
    string Name,
    string? Description,
    FederationResultStatus Status,
    int TotalEntriesCount,
    int TotalClubsCount,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    List<FederationResultEntryDto> TopEntries);

public record FederationResultEntryDto(
    Guid Id,
    int NationalRank,
    int? NationalCategoryRank,
    string RingNumber,
    string? FancierName,
    string ClubName,
    double VelocityMperMin,
    double DistanceKm);

// ── Requests from RaceService (race result data for aggregation) ──────────────

public record RaceResultSnapshot(
    Guid Id,
    Guid RaceId,
    Guid ClubId,
    string ClubName,
    string RingNumber,
    Guid? UserId,
    string? UserFullName,
    double VelocityMperMin,
    double DistanceKm,
    DateTime? ArrivalTime);

public record RaceSnapshot(
    Guid Id,
    Guid ClubId,
    string ClubName,
    string Name,
    RaceStatus Status);

// ── Requests ──────────────────────────────────────────────────────────────────

public record CreateFederationResultRequest(
    Guid FederationId,
    string Name,
    string? Description,
    List<Guid> RaceIds);

public record UpdateFederationPageRequest(
    SiteTheme? Theme,
    bool? IsPublished,
    string? AnnouncementsJson,
    string? HeaderHtml);
