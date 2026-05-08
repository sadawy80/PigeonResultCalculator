using PRC.Common;
using PRC.IntegrationService.Models;

namespace PRC.IntegrationService.DTOs;

public record ExternalLinkDto(
    Guid Id,
    Guid UserId,
    Guid ClubId,
    string ExternalPlatformName,
    string ExternalUserId,
    string ExternalLoftId,
    string ExternalLoftName,
    string LinkToken,
    ExternalLinkStatus Status,
    string StatusName,
    string? RejectionReason,
    string? RevokedReason,
    DateTime RequestedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    DateTime? RevokedAt,
    DateTime? LastDataAccessAt);

public record IntegrationRaceResultDto(
    string RingNumber,
    string? PigeonName,
    string? PigeonSex,
    int? PigeonYearOfBirth,
    string RaceName,
    string ReleaseLocation,
    DateTime RaceDate,
    double DistanceKm,
    double VelocityMperMin,
    double VelocityKmH,
    int? ClubRank,
    int? CategoryRank,
    string? CategoryName);

public record IntegrationAcePigeonDto(
    string RingNumber,
    string? PigeonName,
    string? PigeonSex,
    int? PigeonYearOfBirth,
    string ProgrammeName,
    int ProgrammeYear,
    int AceRank,
    double TotalScore,
    double AverageScore,
    int RacesEntered,
    int RacesInProgramme,
    double ParticipationRate,
    double BestVelocityMperMin,
    double AverageVelocityMperMin,
    int BestClubRank);

public record IntegrationSuperAceDto(
    string RingNumber,
    string? PigeonName,
    string? PigeonSex,
    int? PigeonYearOfBirth,
    string ProgrammeName,
    int ProgrammeYear,
    int SuperAceRank,
    double TotalScore,
    double AverageScore,
    int RacesEntered,
    int RacesInProgramme,
    double ParticipationRate,
    double BestVelocityMperMin,
    double AverageVelocityMperMin,
    int BestClubRank);

public record IntegrationBestLoftDto(
    string ProgrammeName,
    int ProgrammeYear,
    int LoftRank,
    double TotalScore,
    double AverageScore,
    int RacesEntered,
    int PigeonsEntered,
    double BestSingleVelocityMperMin,
    double AverageVelocityMperMin);

public record IntegrationSummaryDto(
    int TotalRaceResults,
    int TotalAcePigeonResults,
    int TotalSuperAcePigeonResults,
    int TotalBestLoftResults,
    DateTime? LastRaceDate);

// Request body models
public record LinkRequestBody(
    string ExternalUserId,
    string ExternalLoftId,
    string ExternalLoftName,
    string CallbackUrl,
    Guid ClubId,
    string? ExternalPlatformName,
    Guid? PrcUserId,
    string? MetadataJson);

public record RevokeByTokenBody(string LinkToken, string? Reason);
public record RejectLinkBody(string? Reason);
public record RevokeBody(string? Reason);
