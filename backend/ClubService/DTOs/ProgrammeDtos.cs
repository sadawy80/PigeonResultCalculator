using PRC.Common;

namespace PRC.ClubService.DTOs;

public record ProgrammeDto(
    Guid Id, Guid ClubId, string ClubName,
    string Name, string? Description,
    int Year, DateTime? StartDate, DateTime? EndDate,
    ProgrammeStatus Status, ScoringMethod ScoringMethod,
    int PointsForFirst, int MaxPointPositions,
    int BestLoftPigeonsPerRace, int BestLoftMinRaces, int AcePigeonMinRaces,
    SuperAceQualification SuperAceQualification,
    int SuperAceMinRaceCount, double SuperAceMinRacePercentage,
    DateTime? PublishedAt, DateTime CreatedAt,
    List<ProgrammeRaceDto> Races);

public record ProgrammeRaceDto(
    Guid ProgrammeRaceId, Guid RaceId, string RaceName,
    DateTime? ActualReleaseTime, double ScoreWeight, int SortOrder, int TotalEntries);

public record ProgrammeSummaryDto(
    Guid Id, string Name, int Year, ProgrammeStatus Status,
    ScoringMethod ScoringMethod, int RaceCount,
    DateTime? StartDate, DateTime? EndDate);

public record RaceBreakdownItem(
    Guid RaceId, string RaceName, double Score, double Speed,
    int ClubRank, int PigeonsEntered, bool Dnf);

public record BestLoftResultDto(
    Guid Id, Guid ProgrammeId, string ProgrammeName,
    Guid? UserId, string FancierName,
    int LoftRank, double TotalScore, double AverageScore,
    int RacesEntered, int PigeonsEntered,
    double BestSingleSpeedMperMin, double AverageSpeedMperMin,
    List<RaceBreakdownItem> RaceBreakdown);

public record AcePigeonResultDto(
    Guid Id, Guid ProgrammeId, string ProgrammeName,
    Guid? UserId, string FancierName,
    Guid? PigeonId, string RingNumber,
    string? PigeonName, string? PigeonSex, int? PigeonYearOfBirth,
    int AceRank, double TotalScore, double AverageScore,
    int RacesEntered, int RacesInProgramme, double ParticipationRate,
    double BestSpeedMperMin, double AverageSpeedMperMin, int BestClubRank,
    List<RaceBreakdownItem> RaceBreakdown);

public record SuperAcePigeonResultDto(
    Guid Id, Guid ProgrammeId, string ProgrammeName,
    Guid? UserId, string FancierName,
    Guid? PigeonId, string RingNumber,
    string? PigeonName, string? PigeonSex, int? PigeonYearOfBirth,
    int SuperAceRank, double TotalScore, double AverageScore,
    int RacesEntered, int RacesInProgramme, double ParticipationRate,
    double BestSpeedMperMin, double AverageSpeedMperMin, int BestClubRank,
    List<RaceBreakdownItem> RaceBreakdown);

// Requests
public record CreateProgrammeRequest(
    Guid ClubId, string Name, string? Description,
    int Year, DateTime? StartDate, DateTime? EndDate,
    ScoringMethod ScoringMethod, int PointsForFirst, int MaxPointPositions,
    int BestLoftPigeonsPerRace, int BestLoftMinRaces, int AcePigeonMinRaces,
    SuperAceQualification SuperAceQualification,
    int SuperAceMinRaceCount, double SuperAceMinRacePercentage);

public record UpdateProgrammeRequest(
    string Name, string? Description,
    DateTime? StartDate, DateTime? EndDate,
    ScoringMethod ScoringMethod, int PointsForFirst, int MaxPointPositions,
    int BestLoftPigeonsPerRace, int BestLoftMinRaces, int AcePigeonMinRaces,
    SuperAceQualification SuperAceQualification,
    int SuperAceMinRaceCount, double SuperAceMinRacePercentage);

public record AddRaceToProgrammeRequest(Guid RaceId, double ScoreWeight = 1.0, int SortOrder = 0);

// Race result snapshot from RaceService — used for programme calculations
public record RaceResultForCalculation(
    Guid RaceId, string RaceName,
    Guid ResultId, string RingNumber,
    Guid? UserId, string? UserFullName,
    double SpeedMperMin, double DistanceKm,
    DateTime? ArrivalTime, int ClubRank,
    Guid? PigeonId, string? PigeonName, string? PigeonSex, int? PigeonYearOfBirth);
