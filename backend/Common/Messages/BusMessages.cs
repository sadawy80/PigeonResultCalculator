namespace PRC.Common.Messages;

// ─────────────────────────────────────────────────────────────────────────────
// Events (publish / subscribe — fire-and-forget)
// ─────────────────────────────────────────────────────────────────────────────

/// Published by RaceService when a race's results are finalised and published.
/// FederationService subscribes to build its local race-snapshot cache.
public record RaceResultsPublished(
    Guid RaceId,
    Guid ClubId,
    string ClubName,
    string RaceName,
    RaceStatus RaceStatus,
    IReadOnlyList<RaceResultItem> Results,
    DateTime OccurredAt,
    Guid? FederationId = null);

/// Published by IdentityService when a user is assigned the FederationManager role.
/// FederationService subscribes to cache the federation manager email for notifications.
public record FederationManagerAssigned(
    Guid FederationId,
    string ManagerEmail,
    string ManagerName,
    DateTime OccurredAt);

public record RaceResultItem(
    Guid ResultId,
    string RingNumber,
    Guid? UserId,
    string? UserFullName,
    double SpeedMperMin,
    double DistanceKm,
    DateTime? ArrivalTime);

/// Published by SubscriptionService on plan lifecycle changes.
public record SubscriptionActivated(
    Guid SubscriptionId,
    Guid EntityId,
    string EntityName,
    SubscriptionType SubscriptionType,
    string PlanName,
    string BillingCycle,
    DateTime ExpiresAt,
    DateTime OccurredAt);

public record SubscriptionExpiredEvent(
    Guid SubscriptionId,
    Guid EntityId,
    string EntityName,
    SubscriptionType SubscriptionType,
    DateTime OccurredAt);

public record SubscriptionCancelledEvent(
    Guid SubscriptionId,
    Guid EntityId,
    string EntityName,
    SubscriptionType SubscriptionType,
    string Reason,
    DateTime OccurredAt);

// ─────────────────────────────────────────────────────────────────────────────
// Admin — Stats (Request / Response per service)
// ─────────────────────────────────────────────────────────────────────────────

public record GetIdentityStatsRequest;
public record IdentityStatsResult(int TotalUsers, int PendingUsers, int ActiveUsers);

public record GetClubStatsRequest;
public record ClubStatsResult(int TotalClubs, int TotalActiveClubs, int TotalMembers);

public record GetRaceStatsRequest;
public record RaceStatsResult(int TotalRaces, int PublishedRaces, int RacesThisMonth, int TotalResults);

public record GetFederationStatsRequest;
public record FederationStatsResult(int TotalFederations, int ActiveFederations);

public record GetSubscriptionStatsRequest;
public record SubscriptionStatsResult(int ActiveFederationSubscriptions, int ActiveClubSubscriptions);

// ─────────────────────────────────────────────────────────────────────────────
// Admin — User management (IdentityService consumers)
// ─────────────────────────────────────────────────────────────────────────────

public record GetUsersRequest(string? Search, UserRole? Role, int Page, int PageSize);
public record GetUsersResult(IReadOnlyList<AdminUserItem> Users, int TotalCount);

public record AdminUserItem(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    bool IsActive,
    Guid? FederationId,
    DateTime? LastLoginAt);

public record ValidateAdminCredentialsRequest(string Email, string Password);
public record ValidateAdminCredentialsResult(
    bool IsValid,
    Guid UserId,
    string FullName,
    UserRole? Role,
    bool IsActive);

public record ToggleUserActiveRequest(Guid UserId, Guid RequestingUserId);
public record ToggleUserActiveResult(Guid Id, bool IsActive, string? Error);

public record AssignRoleRequest(Guid UserId, UserRole Role, Guid? FederationId);
public record AssignRoleResult(Guid Id, UserRole Role, Guid? FederationId, string? Error);

public record SetUserLimitsRequest(Guid UserId, int? MaxResults, int? MaxClubs);
public record SetUserLimitsResult(Guid Id, int? MaxResultsOverride, int? MaxClubsOverride, string? Error);

// ─────────────────────────────────────────────────────────────────────────────
// Admin — Club management (ClubService consumers)
// ─────────────────────────────────────────────────────────────────────────────

public record GetAllClubsRequest(string? Search, Guid? FederationId, int Page, int PageSize);
public record AllClubsResult(IReadOnlyList<AdminClubItem> Clubs, int TotalCount);

public record AdminClubItem(
    Guid Id,
    string Name,
    string? Code,
    string? City,
    Guid? FederationId,
    string? FederationName,
    bool IsActive,
    DateTime CreatedAt);

public record ToggleClubActiveRequest(Guid ClubId);
public record ToggleClubActiveResult(Guid Id, bool IsActive, string? Error);

// ─────────────────────────────────────────────────────────────────────────────
// Admin — Federation management (FederationService consumers)
// ─────────────────────────────────────────────────────────────────────────────

public record GetFederationsRequest(int Page, int PageSize);
public record FederationsResult(IReadOnlyList<AdminFederationItem> Federations, int TotalCount);

public record AdminFederationItem(
    Guid Id,
    string Name,
    string Code,
    string? Slug,
    string? FlagUrl,
    bool IsActive);

public record CreateFederationRequest(
    string Name,
    string Code,
    string? Slug,
    string? FlagUrl,
    string? DefaultLanguage,
    string? DefaultTimezone,
    string? DefaultDistanceUnit,
    Guid CreatedBy);

public record CreateFederationResult(bool Success, Guid? Id, string? Error);

public record ToggleFederationActiveRequest(Guid FederationId);
public record ToggleFederationActiveResult(Guid Id, bool IsActive, string? Error);

// ─────────────────────────────────────────────────────────────────────────────
// Admin — Subscription management (SubscriptionService consumers)
// ─────────────────────────────────────────────────────────────────────────────

public record GetSubscriptionPlansRequest;
public record SubscriptionPlansResult(IReadOnlyList<SubscriptionPlanItem> Plans);

public record SubscriptionPlanItem(
    Guid Id,
    string Name,
    string Type,
    string BillingCycle,
    decimal Price,
    string Currency,
    int? MaxClubs,
    int? MaxResultsPerClub,
    bool IsActive,
    bool IsHighlighted,
    int SortOrder);

public record GetFederationSubscriptionsRequest(int Page, int PageSize);
public record FederationSubscriptionsResult(IReadOnlyList<FederationSubscriptionItem> Items, int TotalCount);

public record FederationSubscriptionItem(
    Guid Id,
    Guid FederationId,
    string FederationName,
    string PlanName,
    string BillingCycle,
    SubscriptionStatus Status,
    DateTime? ExpiresAt,
    DateTime CreatedAt);

public record CreateFederationSubscriptionRequest(
    Guid FederationId,
    string FederationName,
    Guid PlanId,
    string BillingCycle,
    decimal AmountPaid,
    string? PaymentReference,
    string? Notes,
    Guid CreatedBy);

public record CreateFederationSubscriptionResult(bool Success, Guid? Id, string? Error);

public record GetActiveSubscriptionCountRequest;
public record ActiveSubscriptionCountResult(int FederationSubscriptions, int ClubSubscriptions);

// ─────────────────────────────────────────────────────────────────────────────
// Rendering — cross-service data requests
// ─────────────────────────────────────────────────────────────────────────────

public record GetRaceForRenderRequest(Guid RaceId);
public record RaceForRenderResult(
    bool Found,
    Guid RaceId,
    string RaceName,
    Guid ClubId,
    string? ReleaseLocation,
    DateTime? ActualReleaseTime,
    double? NominatedDistanceKm,
    int TotalPigeonsEntered,
    string? WindDescription,
    string? TemperatureDescription,
    IReadOnlyList<RaceResultRenderItem> Results);

public record RaceResultRenderItem(
    Guid ResultId,
    int? ClubRank,
    int? CategoryRank,
    string RingNumber,
    string? PigeonName,
    string? PigeonSex,
    int? PigeonYearOfBirth,
    Guid? UserId,
    DateTime ArrivalTime,
    double DistanceKm,
    double SpeedMperMin,
    string CategoryName);

public record GetRaceResultForRenderRequest(Guid RaceResultId);
public record RaceResultForRenderResult(
    bool Found,
    Guid ResultId,
    int? ClubRank,
    string RingNumber,
    string? PigeonName,
    string? PigeonSex,
    double SpeedMperMin,
    double DistanceKm,
    DateTime ArrivalTime,
    Guid? UserId,
    Guid RaceId,
    string RaceName,
    DateTime? RaceDate,
    string? ReleaseLocation,
    Guid ClubId);

public record GetClubBrandingRequest(Guid ClubId);
public record ClubBrandingResult(bool Found, string Name, string? LogoUrl, string PrimaryColor, string SecondaryColor);

public record GetUserNamesRequest(IReadOnlyList<Guid> UserIds);
public record UserNamesResult(IReadOnlyDictionary<Guid, string> Names);

public record GetUserEmailsRequest(IReadOnlyList<Guid> UserIds);
public record UserEmailsResult(IReadOnlyDictionary<Guid, string> Emails);

public record GetProgrammeForRenderRequest(Guid ProgrammeId);
public record ProgrammeForRenderResult(
    bool Found,
    Guid ProgrammeId,
    string Name,
    int Year,
    string ScoringMethod,
    int AcePigeonMinRaces,
    string SuperAceQualification,
    int RaceCount,
    Guid ClubId,
    string ClubName,
    string? ClubLogoUrl,
    string ClubPrimaryColor,
    string ClubSecondaryColor,
    IReadOnlyList<BestLoftRenderItem> BestLoftResults,
    IReadOnlyList<AcePigeonRenderItem> AcePigeonResults,
    IReadOnlyList<SuperAceRenderItem> SuperAceResults);

public record BestLoftRenderItem(
    int LoftRank, string FancierName, int RacesEntered, int PigeonsEntered,
    double BestSingleSpeedMperMin, double AverageSpeedMperMin, double TotalScore, double AverageScore);

public record AcePigeonRenderItem(
    int AceRank, string RingNumber, string? PigeonName, string? PigeonSex, int? PigeonYearOfBirth,
    string FancierName, int RacesEntered, int RacesInProgramme, double ParticipationRate,
    double BestSpeedMperMin, double AverageSpeedMperMin, double TotalScore, double AverageScore, int BestClubRank);

public record SuperAceRenderItem(
    int SuperAceRank, string RingNumber, string? PigeonName, string? PigeonSex, int? PigeonYearOfBirth,
    string FancierName, int RacesEntered, int RacesInProgramme, double ParticipationRate,
    double BestSpeedMperMin, double AverageSpeedMperMin, double TotalScore, double AverageScore, int BestClubRank);

// ─────────────────────────────────────────────────────────────────────────────
// Integration — fancier data queries (called by IntegrationService)
// ─────────────────────────────────────────────────────────────────────────────

public record GetFancierRaceResultsRequest(Guid UserId, Guid ClubId, int Page, int PageSize);
public record FancierRaceResultsResponse(
    bool Found,
    int TotalCount,
    IReadOnlyList<FancierRaceResultItem> Items);
public record FancierRaceResultItem(
    string RingNumber,
    string? PigeonName,
    string? PigeonSex,
    int? PigeonYearOfBirth,
    string RaceName,
    string ReleaseLocation,
    DateTime RaceDate,
    double DistanceKm,
    double SpeedMperMin,
    int? ClubRank,
    int? CategoryRank,
    string? CategoryName);

public record GetFancierProgrammeResultsRequest(Guid UserId, Guid ClubId);
public record FancierProgrammeResultsResponse(
    IReadOnlyList<FancierAcePigeonItem> AcePigeonResults,
    IReadOnlyList<FancierSuperAceItem> SuperAceResults,
    IReadOnlyList<FancierBestLoftItem> BestLoftResults);
public record FancierAcePigeonItem(
    string RingNumber, string? PigeonName, string? PigeonSex, int? PigeonYearOfBirth,
    string ProgrammeName, int ProgrammeYear, int AceRank,
    double TotalScore, double AverageScore, int RacesEntered, int RacesInProgramme,
    double ParticipationRate, double BestSpeedMperMin, double AverageSpeedMperMin, int BestClubRank);
public record FancierSuperAceItem(
    string RingNumber, string? PigeonName, string? PigeonSex, int? PigeonYearOfBirth,
    string ProgrammeName, int ProgrammeYear, int SuperAceRank,
    double TotalScore, double AverageScore, int RacesEntered, int RacesInProgramme,
    double ParticipationRate, double BestSpeedMperMin, double AverageSpeedMperMin, int BestClubRank);
public record FancierBestLoftItem(
    string ProgrammeName, int ProgrammeYear, int LoftRank,
    double TotalScore, double AverageScore, int RacesEntered, int PigeonsEntered,
    double BestSingleSpeedMperMin, double AverageSpeedMperMin);

// ─────────────────────────────────────────────────────────────────────────────
// ClubService — race data queries (for programme calculation)
// ─────────────────────────────────────────────────────────────────────────────

public record GetRaceSnapshotRequest(Guid RaceId);
public record RaceSnapshotResult(bool Found, Guid RaceId, Guid ClubId, string Name,
    DateTime? ActualReleaseTime, int ResultCount);

public record GetPublishedResultsForProgrammeRequest(IReadOnlyList<Guid> RaceIds);
public record PublishedResultsForProgrammeResult(IReadOnlyList<ProgrammeRaceResultItem> Items);
public record ProgrammeRaceResultItem(
    Guid RaceId, string RaceName,
    Guid ResultId, string RingNumber,
    Guid? UserId, double SpeedMperMin, double DistanceKm,
    DateTime? ArrivalTime, int ClubRank,
    Guid? PigeonId, string? PigeonName, string? PigeonSex, int? PigeonYearOfBirth);

public record GetPigeonLookupRequest(string RingNumber);
public record PigeonLookupResult(bool Found, Guid? PigeonId, string RingNumber);

// ─────────────────────────────────────────────────────────────────────────────
// Integration — notification events
// ─────────────────────────────────────────────────────────────────────────────

public record ExternalLinkRequested(
    Guid LinkId,
    Guid ClubId,
    string ExternalPlatformName,
    string ExternalLoftName);

public record MemberInvited(
    Guid InvitationId,
    string Email,
    string ClubName,
    string InviterName,
    string AcceptLink);

// ─────────────────────────────────────────────────────────────────────────────
// PublicService — club page data (ClubService consumers)
// ─────────────────────────────────────────────────────────────────────────────

public record GetPublicClubBySlugRequest(string Slug);
public record PublicClubResult(
    bool Found,
    Guid ClubId, string Name, string? Code, string? Description,
    string? City, string? LogoUrl, string? PrimaryColor, string? SecondaryColor,
    int MemberCount, string? FederationName, int Theme, string? AnnouncementsJson);

public record ListPublishedClubsForPublicRequest(
    string? FederationCode, Guid? FederationId, int Page, int PageSize);
public record ListPublishedClubsForPublicResult(
    int Total, IReadOnlyList<PublicClubListItem> Items);
public record PublicClubListItem(
    string Slug, string Name, string? City,
    string? FederationCode, string? FederationName, int Theme, string? LogoUrl);

// ─────────────────────────────────────────────────────────────────────────────
// PublicService — race data (RaceService consumers)
// ─────────────────────────────────────────────────────────────────────────────

public record GetPublishedRacesForPublicRequest(Guid ClubId, int Take);
public record PublishedRacesForPublicResult(
    IReadOnlyList<PublicRaceItem> Published,
    IReadOnlyList<PublicLiveRaceItem> Live);
public record PublicRaceItem(
    Guid Id, string Name, string? Description, int Status,
    string? ReleaseLocation, DateTime? ActualReleaseTime, DateTime? ScheduledReleaseTime,
    DateTime? PublishedAt, int TotalPigeonsEntered,
    double? WindSpeedKmh, string? WindDirection, double? TemperatureCelsius,
    IReadOnlyList<PublicRaceCategoryItem> Categories,
    IReadOnlyList<PublicRaceResultItem> TopResults);
public record PublicRaceCategoryItem(Guid Id, string Name, int SortOrder);
public record PublicRaceResultItem(
    Guid Id, string RingNumber, string? PigeonName,
    Guid? FancierId, double VelocityMperMin, double DistanceKm,
    int ClubRank, int? CategoryRank, string? CategoryName, DateTime? ArrivalTime);
public record PublicLiveRaceItem(Guid Id, string Name, int TotalPigeonsEntered);

// ─────────────────────────────────────────────────────────────────────────────
// PublicService — federation page data (FederationService consumers)
// ─────────────────────────────────────────────────────────────────────────────

public record GetPublicFederationBySlugRequest(string Slug);
public record PublicFederationResult(
    bool Found,
    Guid FederationId, string Name, string? Code, string? FederationSlug,
    int Theme, string? AnnouncementsJson,
    IReadOnlyList<PublicFederationResultSummary> RecentResults);
public record PublicFederationResultSummary(
    Guid Id, string Name, string? Description, DateTime? PublishedAt,
    int TotalEntriesCount, int TotalClubsCount,
    IReadOnlyList<PublicFederationResultEntry> TopEntries);
public record PublicFederationResultEntry(
    int NationalRank, string RingNumber, double VelocityMperMin,
    string? FancierName, string ClubName);

// ─────────────────────────────────────────────────────────────────────────────
// PublicService — subscription plans (SubscriptionService consumers)
// ─────────────────────────────────────────────────────────────────────────────

public record GetPublicSubscriptionPlansRequest;
public record PublicSubscriptionPlansResult(IReadOnlyList<PublicPlanGroup> Plans);
public record PublicPlanGroup(
    string Name, string? Description, bool IsHighlighted, string? Currency, int SortOrder,
    PublicPlanCycle? Monthly, PublicPlanCycle? Seasonal, PublicPlanCycle? Annual);
public record PublicPlanCycle(decimal Price, int MaxClubs, int MaxResults, string? Features);

// ─────────────────────────────────────────────────────────────────────────────
// Email delivery (IdentityService → NotificationService, fire-and-forget)
// ─────────────────────────────────────────────────────────────────────────────

public record SendEmailEvent(string To, string Subject, string HtmlBody);

// ─────────────────────────────────────────────────────────────────────────────
// Subscription limit enforcement (RaceService → SubscriptionService)
// ─────────────────────────────────────────────────────────────────────────────

public record CheckResultLimitRequest(Guid ClubId, int NewResultsCount);
public record CheckResultLimitResult(bool Allowed, string? Error, int CurrentUsed, int MaxAllowed);

public record IncrementResultUsageRequest(Guid ClubId, int Count);
public record IncrementResultUsageResult(bool Success, string? Error);

// ─────────────────────────────────────────────────────────────────────────────
// Federation subscription limits (IdentityService → SubscriptionService, for manager approval)
// ─────────────────────────────────────────────────────────────────────────────

public record GetFederationSubscriptionLimitsRequest(Guid FederationId);
public record GetFederationSubscriptionLimitsResult(
    bool HasActiveSubscription,
    int MaxClubs,
    int CurrentClubCount,
    bool IsUnlimited);

// ─────────────────────────────────────────────────────────────────────────────
// Active club count (IdentityService → ClubService, for manager approval)
// ─────────────────────────────────────────────────────────────────────────────

public record GetActiveClubCountForFederationRequest(Guid FederationId);
public record GetActiveClubCountForFederationResult(int ActiveClubCount);

// ─────────────────────────────────────────────────────────────────────────────
// Subscription lifecycle notification emails (SubscriptionService → NotificationService)
// ContactEmail/ContactName are optional; NotificationService only sends if present.
// ─────────────────────────────────────────────────────────────────────────────

public record SubscriptionConfirmedEmail(
    string To, string ContactName, string EntityName,
    string SubscriptionType, string PlanName, string BillingCycle, DateTime ExpiresAt);

public record SubscriptionExpiredEmail(
    string To, string ContactName, string EntityName,
    string SubscriptionType, string PlanName);

public record SubscriptionCancelledEmail(
    string To, string ContactName, string EntityName,
    string SubscriptionType, string Reason);

// ─────────────────────────────────────────────────────────────────────────────
// Role Upgrade Requests (IdentityService consumers, called by AdminService)
// ─────────────────────────────────────────────────────────────────────────────

public record GetUpgradeRequestsRequest(
    Guid? FederationId, UpgradeRequestStatus? Status, int Page, int PageSize);

public record GetUpgradeRequestsResult(
    IReadOnlyList<UpgradeRequestItem> Items, int TotalCount);

public record UpgradeRequestItem(
    Guid Id,
    Guid UserId,
    string UserFullName,
    string UserEmail,
    UserRole RequestedRole,
    Guid? FederationId,
    string? FederationName,
    UpgradeRequestStatus Status,
    string? Notes,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? ReviewedAt);

public record ReviewUpgradeRequestRequest(
    Guid RequestId, bool Approved, string? RejectionReason, Guid ReviewedByUserId);

public record ReviewUpgradeRequestResult(bool Success, string? Error);

// Published when a user submits a role upgrade request (for notifications)
public record UpgradeRequestSubmitted(
    Guid RequestId, Guid UserId, string UserFullName, string UserEmail,
    UserRole RequestedRole, Guid? FederationId, DateTime OccurredAt);

// Public federation list (for upgrade request form dropdown)
public record GetActiveFederationsForPublicRequest;
public record ActiveFederationsForPublicResult(IReadOnlyList<PublicFederationListItem> Federations);
public record PublicFederationListItem(Guid Id, string Name, string Code);
