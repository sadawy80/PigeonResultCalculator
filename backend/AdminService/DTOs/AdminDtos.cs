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
    bool IsActive, Guid? FederationId, string? FederationName,
    DateTime CreatedAt);

// ── Audit Events ───────────────────────────────────────────────────────────────

public record AuditEventDto(
    Guid Id, string Action, string EntityType, Guid? EntityId,
    string Severity, string? Details,
    Guid? TriggeredByUserId, string? TriggeredByName,
    string? CorrelationId, string ServiceName,
    string? IpAddress, string? Country, DateTime CreatedAt);

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

public record UpdatePigeonBody(string? Name, string? Sex, int? YearOfBirth, string? Color);

public record AssignManagerBody(string Email);
public record CreateClubAdminBody(Guid? FederationId, string Name, string Code, string? City);
public record AssignClubManagerBody(string Email, bool? Force);
public record SetClubExpiryBody(DateTime? ExpiresAt);

public record LinkFancierBody(Guid UserId, string UserName, string UserEmail);

public record UpdatePlanBody(
    string Name,
    string? Description,
    decimal Price,
    int MaxClubs,
    int MaxResultsPerClub,
    bool IsActive,
    bool IsHighlighted,
    int SortOrder,
    string? Features);

public record CreateSubscriptionPlanBody(
    string Name,
    string? Description,
    string Type,
    string BillingCycle,
    decimal Price,
    string? Currency,
    int MaxClubs,
    int MaxResultsPerClub,
    bool IsHighlighted,
    int SortOrder,
    string? Features);

public record SendNotificationBody(Guid ClubId, string Title, string Message);

public record RejectLinkBody(string? Reason);
