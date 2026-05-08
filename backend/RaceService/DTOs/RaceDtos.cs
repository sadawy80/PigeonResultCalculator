using PRC.Common;

namespace PRC.RaceService.DTOs;

public record RaceDto(
    Guid Id, Guid ClubId, string ClubName, string Name, string? Description,
    RaceStatus Status, string ReleaseLocation,
    double ReleaseLongitude, double ReleaseLatitude,
    DateTime? ScheduledReleaseTime, DateTime? ActualReleaseTime,
    double? WindSpeedKmh, string? WindDirection, double? TemperatureCelsius,
    int? TotalPigeonsEntered, bool IsLiveTracking,
    DateTime? PublishedAt, DateTime CreatedAt,
    List<RaceCategoryDto> Categories);

public record RaceCategoryDto(Guid Id, string Name, string? Description, int SortOrder);

public record RaceSummaryDto(
    Guid Id, string Name, RaceStatus Status,
    DateTime? ScheduledReleaseTime, DateTime? ActualReleaseTime,
    int? TotalPigeonsEntered, string ClubName, Guid ClubId);

// Cross-service endpoints
public record RaceSnapshotDto(Guid Id, string Name, DateTime? ActualReleaseTime);
public record RaceExistsDto(bool Exists);

// Requests
public record CreateRaceRequest(
    Guid ClubId, string Name, string? Description,
    string ReleaseLocation, double ReleaseLongitude, double ReleaseLatitude,
    DateTime? ScheduledReleaseTime,
    double? WindSpeedKmh, WindDirection? WindDirection, double? TemperatureCelsius,
    List<CreateRaceCategoryRequest> Categories,
    Guid? FederationId = null);

public record CreateRaceCategoryRequest(string Name, string? Description, int SortOrder);

public record UpdateRaceRequest(
    string Name, string? Description,
    string ReleaseLocation, double ReleaseLongitude, double ReleaseLatitude,
    DateTime? ScheduledReleaseTime,
    double? WindSpeedKmh, WindDirection? WindDirection, double? TemperatureCelsius);

public record StartRaceRequest(DateTime ActualReleaseTime);

public record PigeonDto(
    Guid Id, string RingNumber, string? Name, string? Sex,
    int? YearOfBirth, string? Color, string? Strain, string? PhotoUrl);

public record PigeonIdDto(Guid? Id);
public record PigeonExistsDto(bool Exists);
