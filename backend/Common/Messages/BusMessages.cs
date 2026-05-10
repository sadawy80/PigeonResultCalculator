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
public record IdentityStatsResult(int TotalUsers, int PendingUsers, int ActiveUsers,
    int TotalFanciers = 0, int UsersThisYear = 0, int FanciersThisYear = 0);

public record GetClubStatsRequest;
public record ClubStatsResult(int TotalClubs, int TotalActiveClubs, int TotalMembers,
    int TotalAceResults = 0, int TotalSuperAceResults = 0, int TotalBestLoftResults = 0,
    int TotalProgrammes = 0, int ProgrammesThisYear = 0,
    int ClubsThisYear = 0, int AceResultsThisYear = 0,
    int SuperAceResultsThisYear = 0, int BestLoftResultsThisYear = 0);

public record GetRaceStatsRequest;
public record RaceStatsResult(int TotalRaces, int PublishedRaces, int RacesThisMonth, int TotalResults,
    int TotalPigeons = 0, int RacesThisYear = 0, int ResultsThisYear = 0, int PigeonsThisYear = 0);

public record GetFederationStatsRequest;
public record FederationStatsResult(int TotalFederations, int ActiveFederations, int FederationsThisYear = 0);

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

public record DeleteUserRequest(Guid UserId, Guid RequestingUserId);
public record DeleteUserResult(bool Success, string? Error);

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
    DateTime CreatedAt,
    DateTime? SubscriptionExpiresAt = null);

public record SetClubSubscriptionExpiryRequest(Guid ClubId, DateTime? ExpiresAt);
public record SetClubSubscriptionExpiryResult(bool Success, string? Error);

public record ToggleClubActiveRequest(Guid ClubId);
public record ToggleClubActiveResult(Guid Id, bool IsActive, string? Error);

public record AdminCreateClubRequest(Guid? FederationId, string Name, string Code, string? City, Guid CreatedBy);
public record AdminCreateClubResult(bool Success, Guid? ClubId, string? Error);

public record AdminAssignClubManagerRequest(Guid ClubId, Guid UserId, string UserFullName, string UserEmail, bool Force);
public record AdminAssignClubManagerResult(bool Success, bool HasConflict, Guid? ConflictClubId, string? ConflictClubName, Guid? FederationId, string? Error);

public record AdminDeleteClubRequest(Guid ClubId, Guid AdminUserId, string AdminName);
public record AdminDeleteClubResult(bool Success, string? Error, string? ClubName);

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
    bool IsActive,
    string? ManagerEmail);

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

public record AdminDeleteFederationRequest(Guid FederationId, Guid AdminUserId, string AdminName);
public record AdminDeleteFederationResult(bool Success, string? Error, string? FederationName);

// ─────────────────────────────────────────────────────────────────────────────
// Admin — Subscription management (SubscriptionService consumers)
// ─────────────────────────────────────────────────────────────────────────────

public record GetSubscriptionPlansRequest;
public record SubscriptionPlansResult(IReadOnlyList<SubscriptionPlanItem> Plans);

public record SubscriptionPlanItem(
    Guid Id,
    string Name,
    string? Description,
    string Type,
    string BillingCycle,
    decimal Price,
    string Currency,
    int MaxClubs,
    int MaxResultsPerClub,
    bool IsActive,
    bool IsHighlighted,
    int SortOrder,
    string? Features);

public record UpdateSubscriptionPlanBusRequest(
    Guid PlanId,
    string Name,
    string? Description,
    decimal Price,
    int MaxClubs,
    int MaxResultsPerClub,
    bool IsActive,
    bool IsHighlighted,
    int SortOrder,
    string? Features,
    Guid UpdatedBy);

public record UpdateSubscriptionPlanBusResult(bool Success, string? Error, SubscriptionPlanItem? Plan);

public record GetFederationSubscriptionsRequest(
    int Page, int PageSize,
    string? Search = null,
    string? BillingCycle = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null);
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
public record ActiveSubscriptionCountResult(int FederationSubscriptions, int ClubSubscriptions,
    int FederationSubsThisYear = 0, int ClubSubsThisYear = 0);

// ─────────────────────────────────────────────────────────────────────────────
// Admin — Race & Result management (RaceService consumers)
// ─────────────────────────────────────────────────────────────────────────────

public record GetAdminRacesRequest(
    string? Search, Guid? ClubId, int? Status,
    DateTime? DateFrom, DateTime? DateTo, int Page, int PageSize);

public record AdminRacesResult(IReadOnlyList<AdminRaceItem> Items, int TotalCount);

public record AdminRaceItem(
    Guid Id, string Name, Guid ClubId, string ClubName, Guid? FederationId,
    int Status, DateTime? ScheduledAt, DateTime? PublishedAt,
    int ResultCount, DateTime CreatedAt);

public record AdminDeleteRaceRequest(Guid RaceId, Guid AdminUserId, string AdminName);
public record AdminDeleteRaceResult(bool Success, string? Error, string? RaceName, Guid? ClubId);

// ─────────────────────────────────────────────────────────────────────────────
// Admin — Programme & aggregate results (ClubService consumers)
// ─────────────────────────────────────────────────────────────────────────────

public record GetAdminProgrammesRequest(string? Search, Guid? ClubId, int Page, int PageSize);
public record AdminProgrammesResult(IReadOnlyList<AdminProgrammeItem> Items, int TotalCount);
public record AdminProgrammeItem(
    Guid Id, string Name, int Year, Guid ClubId, string ClubName,
    int AceCount, int SuperAceCount, int BestLoftCount, int Status, DateTime CreatedAt);

public record GetAdminAcePigeonResultsRequest(string? Search, Guid? ClubId, Guid? ProgrammeId, int Page, int PageSize);
public record AdminAcePigeonResultsResult(IReadOnlyList<AdminAcePigeonItem> Items, int TotalCount);

public record GetAdminSuperAceResultsRequest(string? Search, Guid? ClubId, Guid? ProgrammeId, int Page, int PageSize);
public record AdminSuperAceResultsResult(IReadOnlyList<AdminAcePigeonItem> Items, int TotalCount);

public record GetAdminBestLoftResultsRequest(string? Search, Guid? ClubId, Guid? ProgrammeId, int Page, int PageSize);
public record AdminBestLoftResultsResult(IReadOnlyList<AdminBestLoftItem> Items, int TotalCount);

public record AdminAcePigeonItem(
    Guid Id, Guid ProgrammeId, string ProgrammeName, int ProgrammeYear,
    Guid ClubId, string ClubName,
    string FancierName, string RingNumber, string? PigeonName,
    int Rank, double TotalScore, int RacesEntered);

public record AdminBestLoftItem(
    Guid Id, Guid ProgrammeId, string ProgrammeName, int ProgrammeYear,
    Guid ClubId, string ClubName,
    string FancierName, int Rank, double TotalScore, int RacesEntered);

public record NotifyClubManagersRequest(
    Guid ClubId, string Title, string Message,
    string? EntityType = null, string? EntityId = null);
public record NotifyClubManagersResult(bool Success, int NotifiedCount);

// ─────────────────────────────────────────────────────────────────────────────
// Admin — Pigeon management (RaceService consumers)
// ─────────────────────────────────────────────────────────────────────────────

public record GetAdminPigeonsRequest(string? Search, Guid? FederationId, Guid? ClubId, int Page, int PageSize, string? FancierSearch = null);
public record AdminPigeonsResult(IReadOnlyList<AdminPigeonItem> Items, int TotalCount);
public record AdminPigeonItem(
    Guid Id, string RingNumber, string? Name, string? Sex, int? YearOfBirth,
    string? Color, Guid? FederationId, DateTime CreatedAt, string? FancierName);

public record AdminUpdatePigeonRequest(Guid PigeonId, string? Name, string? Sex, int? YearOfBirth, string? Color, Guid UpdatedBy);
public record AdminUpdatePigeonResult(bool Success, string? Error);

public record AdminDeletePigeonRequest(Guid PigeonId, Guid AdminUserId);
public record AdminDeletePigeonResult(bool Success, string? Error, string? RingNumber);

// ── Admin — Fancier management (RaceService consumers) ───────────────────────
public record GetAdminFanciersRequest(
    string? Search, Guid? ClubId, Guid? FederationId, bool? IsLinked, int Page, int PageSize);
public record GetAdminFanciersResult(IReadOnlyList<AdminFancierItem> Items, int TotalCount);
public record AdminFancierItem(
    Guid Id, string Name,
    Guid ClubId, string ClubName,
    Guid? FederationId, string? FederationName, string? Country,
    bool IsLinked, Guid? LinkedUserId, string? LinkedUserName, string? LinkedUserEmail,
    DateTime? LinkedAt, DateTime CreatedAt);
public record LinkFancierToUserRequest(Guid FancierId, Guid UserId, string UserName, string UserEmail);
public record LinkFancierToUserResult(bool Success, string? Error);
public record UnlinkFancierRequest(Guid FancierId);
public record UnlinkFancierResult(bool Success, string? Error);

// ─────────────────────────────────────────────────────────────────────────────
// Admin — Programme management (ClubService consumers)
// ─────────────────────────────────────────────────────────────────────────────

public record AdminDeleteProgrammeRequest(Guid ProgrammeId, Guid AdminUserId);
public record AdminDeleteProgrammeResult(bool Success, string? Error, string? ProgrammeName);

// ─────────────────────────────────────────────────────────────────────────────
// Admin — Notifications (ClubService consumers)
// ─────────────────────────────────────────────────────────────────────────────

public record GetAdminNotificationsRequest(string? Search, int Page, int PageSize);
public record GetAdminNotificationsResult(IReadOnlyList<AdminNotificationItem> Items, int TotalCount);
public record AdminNotificationItem(
    Guid Id, Guid UserId, string Title, string? Body,
    string Type, string Status, string Channel, DateTime CreatedAt, DateTime? ReadAt);
public record AdminSendNotificationBusRequest(Guid ClubId, string Title, string Message, Guid SentBy);
public record AdminSendNotificationBusResult(bool Success, int SentCount, string? Error);

// ─────────────────────────────────────────────────────────────────────────────
// Admin — External Link management (IntegrationService consumers)
// ─────────────────────────────────────────────────────────────────────────────

public record GetAdminExternalLinksRequest(int? Status, int Page, int PageSize);
public record GetAdminExternalLinksResult(IReadOnlyList<AdminExternalLinkItem> Items, int TotalCount);
public record AdminExternalLinkItem(
    Guid Id, string ExternalLoftName, string ExternalLoftId,
    string ExternalPlatformName, Guid UserId, Guid ClubId,
    int Status, string StatusLabel,
    string? RejectionReason, string? RevokedReason,
    DateTime RequestedAt, DateTime? LastDataAccessAt);
public record AdminApproveLinkBusRequest(Guid LinkId, Guid AdminUserId);
public record AdminApproveLinkBusResult(bool Success, string? Error);
public record AdminRejectLinkBusRequest(Guid LinkId, string? Reason, Guid AdminUserId);
public record AdminRejectLinkBusResult(bool Success, string? Error);
public record AdminRevokeLinkBusRequest(Guid LinkId);
public record AdminRevokeLinkBusResult(bool Success, string? Error);

// ─────────────────────────────────────────────────────────────────────────────
// Admin — Subscription plan create / delete (SubscriptionService consumers)
// ─────────────────────────────────────────────────────────────────────────────

public record AdminCreateSubscriptionPlanBusRequest(
    string Name, string? Description,
    string Type, string BillingCycle,
    decimal Price, string Currency,
    int MaxClubs, int MaxResultsPerClub,
    bool IsHighlighted, int SortOrder, string? Features, Guid CreatedBy);
public record AdminCreateSubscriptionPlanBusResult(bool Success, SubscriptionPlanItem? Plan, string? Error);
public record AdminDeleteSubscriptionPlanBusRequest(Guid PlanId, Guid DeletedBy);
public record AdminDeleteSubscriptionPlanBusResult(bool Success, string? Error);

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
    Guid? FancierId, double SpeedMperMin, double DistanceKm,
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
    int NationalRank, string RingNumber, double SpeedMperMin,
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
    Guid RequestId, bool Approved, string? RejectionReason, Guid ReviewedByUserId, bool IsAdmin = false);

public record ReviewUpgradeRequestResult(bool Success, string? Error);

public record RevokeUpgradeRequestRequest(Guid RequestId, Guid RevokedByUserId, bool IsAdmin);
public record RevokeUpgradeRequestResult(bool Success, string? Error);

// Published when a user submits a role upgrade request (for notifications)
public record UpgradeRequestSubmitted(
    Guid RequestId, Guid UserId, string UserFullName, string UserEmail,
    UserRole RequestedRole, Guid? FederationId, DateTime OccurredAt);

// Public federation list (for upgrade request form dropdown)
public record GetActiveFederationsForPublicRequest;
public record ActiveFederationsForPublicResult(IReadOnlyList<PublicFederationListItem> Federations);
public record PublicFederationListItem(Guid Id, string Name, string Code);

// Creates an in-app notification row for a specific user (consumed by ClubService)
public record CreateInAppNotification(
    Guid UserId,
    NotificationType Type,
    string Title,
    string Body,
    string? ActionUrl = null);
