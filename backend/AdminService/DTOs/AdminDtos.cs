using PRC.Common;

namespace PRC.AdminService.DTOs;

// ── Stats ─────────────────────────────────────────────────────────────────────

public record PlatformStatsDto(
    int TotalFederations, int TotalClubs, int TotalUsers,
    int TotalRaces, int PublishedRaces, int RacesThisMonth,
    int ActiveSubscriptions, int TotalResults);

// ── Federation (proxied from FederationService) ───────────────────────────────

public record AdminFederationDto(
    Guid Id, string Name, string Code, string Slug,
    bool IsActive, int ClubCount, DateTime CreatedAt);

public record AdminFederationSummaryDto(
    Guid Id, string Name, string Code, bool IsActive);

// ── User (proxied from IdentityService) ───────────────────────────────────────

public record AdminUserDto(
    Guid Id, string FirstName, string LastName, string? Email,
    UserRole Role, bool IsActive,
    Guid? FederationId, DateTime? LastLoginAt, DateTime CreatedAt,
    int? MaxResultsOverride, int? MaxClubsOverride);

// ── Club (proxied from ClubService) ───────────────────────────────────────────

public record AdminClubDto(
    Guid Id, string Name, string Code, string? City,
    bool IsActive, Guid FederationId, string? FederationName,
    DateTime CreatedAt);

// ── Audit Events ───────────────────────────────────────────────────────────────

public record AuditEventDto(
    Guid Id, string Action, string EntityType, Guid? EntityId,
    string Severity, string? Details,
    Guid? TriggeredByUserId, string? TriggeredByName,
    string? CorrelationId, string ServiceName,
    string? IpAddress, DateTime CreatedAt);

// ── HTTP request bodies (controller [FromBody] params) ────────────────────────

public record CreateFederationBody(
    string Name,
    string Code,
    string? Slug,
    string? FlagUrl,
    string? DefaultLanguage,
    string? DefaultTimezone,
    string? DefaultDistanceUnit);

public record AssignRoleBody(UserRole Role, Guid? FederationId);

public record SetUserLimitsBody(int? MaxResults, int? MaxClubs);

public record AdminLoginRequest(string Email, string Password);

public record ImpersonateRequest(Guid TargetUserId, string Reason);

public record RejectUpgradeBody(string? Reason);
