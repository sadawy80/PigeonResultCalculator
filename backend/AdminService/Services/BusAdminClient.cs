using MassTransit;
using PRC.Common;
using PRC.Common.Messages;

namespace PRC.AdminService.Services;

public interface IBusAdminClient
{
    // Stats
    Task<IdentityStatsResult?> GetIdentityStatsAsync(CancellationToken ct = default);
    Task<ClubStatsResult?> GetClubStatsAsync(CancellationToken ct = default);
    Task<RaceStatsResult?> GetRaceStatsAsync(CancellationToken ct = default);
    Task<FederationStatsResult?> GetFederationStatsAsync(CancellationToken ct = default);
    Task<SubscriptionStatsResult?> GetSubscriptionStatsAsync(CancellationToken ct = default);

    // Users
    Task<GetUsersResult?> GetUsersAsync(string? search, UserRole? role, int page, int pageSize, CancellationToken ct = default);
    Task<ValidateAdminCredentialsResult?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default);
    Task<ToggleUserActiveResult?> ToggleUserActiveAsync(Guid userId, Guid requestingUserId, CancellationToken ct = default);
    Task<AssignRoleResult?> AssignRoleAsync(Guid userId, UserRole role, Guid? federationId, CancellationToken ct = default);
    Task<SetUserLimitsResult?> SetUserLimitsAsync(Guid userId, int? maxResults, int? maxClubs, CancellationToken ct = default);
    Task<DeleteUserResult?> DeleteUserAsync(Guid userId, Guid requestingUserId, CancellationToken ct = default);

    // Clubs
    Task<AllClubsResult?> GetAllClubsAsync(string? search, Guid? federationId, int page, int pageSize, CancellationToken ct = default);
    Task<SetClubSubscriptionExpiryResult?> SetClubSubscriptionExpiryAsync(Guid clubId, DateTime? expiresAt, CancellationToken ct = default);
    Task<ToggleClubActiveResult?> ToggleClubActiveAsync(Guid clubId, CancellationToken ct = default);
    Task<AdminCreateClubResult?> AdminCreateClubAsync(Guid? federationId, string name, string code, string? city, Guid createdBy, CancellationToken ct = default);
    Task<AdminAssignClubManagerResult?> AdminAssignClubManagerAsync(Guid clubId, Guid userId, string fullName, string email, bool force, CancellationToken ct = default);
    Task<AdminDeleteClubResult?> DeleteClubAsync(Guid clubId, Guid adminUserId, string adminName, CancellationToken ct = default);

    // Countries
    Task<FederationsResult?> GetFederationsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<CreateFederationResult?> CreateFederationAsync(CreateFederationRequest req, CancellationToken ct = default);
    Task<ToggleFederationActiveResult?> ToggleFederationActiveAsync(Guid federationId, CancellationToken ct = default);
    Task<AdminDeleteFederationResult?> DeleteFederationAsync(Guid federationId, Guid adminUserId, string adminName, CancellationToken ct = default);

    // Upgrade requests
    Task<GetUpgradeRequestsResult?> GetUpgradeRequestsAsync(Guid? federationId, UpgradeRequestStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<ReviewUpgradeRequestResult?> ReviewUpgradeRequestAsync(Guid requestId, bool approved, string? reason, Guid reviewedByUserId, bool isAdmin = false, CancellationToken ct = default);
    Task<RevokeUpgradeRequestResult?> RevokeUpgradeRequestAsync(Guid requestId, Guid revokedByUserId, CancellationToken ct = default);

    // Subscription plans
    Task<SubscriptionPlansResult?> GetSubscriptionPlansAsync(CancellationToken ct = default);
    Task<UpdateSubscriptionPlanBusResult?> UpdateSubscriptionPlanAsync(UpdateSubscriptionPlanBusRequest req, CancellationToken ct = default);
    Task<FederationSubscriptionsResult?> GetFederationSubscriptionsAsync(int page, int pageSize, string? search = null, string? billingCycle = null, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken ct = default);
    Task<CreateFederationSubscriptionResult?> CreateFederationSubscriptionAsync(CreateFederationSubscriptionRequest req, CancellationToken ct = default);
    Task<ActiveSubscriptionCountResult?> GetActiveSubscriptionCountAsync(CancellationToken ct = default);

    // Races
    Task<AdminRacesResult?> GetAdminRacesAsync(string? search, Guid? clubId, int? status, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct = default);
    Task<AdminDeleteRaceResult?> DeleteRaceAsync(Guid raceId, Guid adminUserId, string adminName, CancellationToken ct = default);

    // Programmes & results
    Task<AdminProgrammesResult?> GetAdminProgrammesAsync(string? search, Guid? clubId, int page, int pageSize, CancellationToken ct = default);
    Task<AdminDeleteProgrammeResult?> DeleteProgrammeAsync(Guid programmeId, Guid adminUserId, CancellationToken ct = default);
    Task<AdminAcePigeonResultsResult?> GetAdminAcePigeonResultsAsync(string? search, Guid? clubId, Guid? programmeId, int page, int pageSize, CancellationToken ct = default);
    Task<AdminSuperAceResultsResult?> GetAdminSuperAceResultsAsync(string? search, Guid? clubId, Guid? programmeId, int page, int pageSize, CancellationToken ct = default);
    Task<AdminBestLoftResultsResult?> GetAdminBestLoftResultsAsync(string? search, Guid? clubId, Guid? programmeId, int page, int pageSize, CancellationToken ct = default);
    Task<NotifyClubManagersResult?> NotifyClubManagersAsync(Guid clubId, string title, string message, string? entityType = null, string? entityId = null, CancellationToken ct = default);

    // Pigeons
    Task<AdminPigeonsResult?> GetAdminPigeonsAsync(string? search, Guid? federationId, Guid? clubId, int page, int pageSize, string? fancierSearch = null, CancellationToken ct = default);
    Task<AdminUpdatePigeonResult?> UpdatePigeonAsync(Guid pigeonId, string? name, string? sex, int? yearOfBirth, string? color, Guid updatedBy, CancellationToken ct = default);
    Task<AdminDeletePigeonResult?> DeletePigeonAsync(Guid pigeonId, Guid adminUserId, CancellationToken ct = default);

    // Fanciers
    Task<GetAdminFanciersResult?> GetFanciersAsync(string? search, Guid? clubId, Guid? federationId, bool? isLinked, int page, int pageSize, CancellationToken ct = default);
    Task<LinkFancierToUserResult?> LinkFancierAsync(Guid fancierId, Guid userId, string userName, string userEmail, CancellationToken ct = default);
    Task<UnlinkFancierResult?> UnlinkFancierAsync(Guid fancierId, CancellationToken ct = default);

    // External link requests
    Task<GetAdminExternalLinksResult?> GetAdminExternalLinksAsync(int? status, int page, int pageSize, CancellationToken ct = default);
    Task<AdminApproveLinkBusResult?> AdminApproveLinkAsync(Guid linkId, Guid adminUserId, CancellationToken ct = default);
    Task<AdminRejectLinkBusResult?> AdminRejectLinkAsync(Guid linkId, string? reason, Guid adminUserId, CancellationToken ct = default);
    Task<AdminRevokeLinkBusResult?> AdminRevokeLinkAsync(Guid linkId, CancellationToken ct = default);

    // Notifications
    Task<GetAdminNotificationsResult?> GetAdminNotificationsAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task<AdminSendNotificationBusResult?> AdminSendNotificationAsync(Guid clubId, string title, string message, Guid sentBy, CancellationToken ct = default);

    // Subscription plan CRUD
    Task<AdminCreateSubscriptionPlanBusResult?> AdminCreateSubscriptionPlanAsync(AdminCreateSubscriptionPlanBusRequest req, CancellationToken ct = default);
    Task<AdminDeleteSubscriptionPlanBusResult?> AdminDeleteSubscriptionPlanAsync(Guid planId, Guid deletedBy, CancellationToken ct = default);

    // Audit logs
    Task<GetAuditLogsResponse?> GetAuditLogsAsync(string? action, string? entityType, AuditSeverity? severity, int page, int pageSize, CancellationToken ct = default);
}

public class BusAdminClient : IBusAdminClient
{
    private readonly IRequestClient<GetIdentityStatsRequest>         _idStats;
    private readonly IRequestClient<GetClubStatsRequest>             _clubStats;
    private readonly IRequestClient<GetRaceStatsRequest>             _raceStats;
    private readonly IRequestClient<GetFederationStatsRequest>       _fedStats;
    private readonly IRequestClient<GetSubscriptionStatsRequest>     _subStats;
    private readonly IRequestClient<GetUsersRequest>                 _getUsers;
    private readonly IRequestClient<ValidateAdminCredentialsRequest> _validateCreds;
    private readonly IRequestClient<ToggleUserActiveRequest>         _toggleUser;
    private readonly IRequestClient<AssignRoleRequest>               _assignRole;
    private readonly IRequestClient<SetUserLimitsRequest>            _setLimits;
    private readonly IRequestClient<DeleteUserRequest>               _deleteUser;
    private readonly IRequestClient<GetAllClubsRequest>              _getClubs;
    private readonly IRequestClient<ToggleClubActiveRequest>         _toggleClub;
    private readonly IRequestClient<AdminCreateClubRequest>          _adminCreateClub;
    private readonly IRequestClient<AdminAssignClubManagerRequest>   _adminAssignClubManager;
    private readonly IRequestClient<AdminDeleteClubRequest>          _deleteClub;
    private readonly IRequestClient<SetClubSubscriptionExpiryRequest> _setClubExpiry;
    private readonly IRequestClient<GetFederationsRequest>             _getFederations;
    private readonly IRequestClient<CreateFederationRequest>            _createFederation;
    private readonly IRequestClient<ToggleFederationActiveRequest>      _toggleFederation;
    private readonly IRequestClient<AdminDeleteFederationRequest>        _deleteFederation;
    private readonly IRequestClient<GetUpgradeRequestsRequest>    _getUpgradeRequests;
    private readonly IRequestClient<ReviewUpgradeRequestRequest>   _reviewUpgradeRequest;
    private readonly IRequestClient<RevokeUpgradeRequestRequest>   _revokeUpgradeRequest;
    private readonly IRequestClient<GetSubscriptionPlansRequest>          _getPlans;
    private readonly IRequestClient<UpdateSubscriptionPlanBusRequest>     _updatePlan;
    private readonly IRequestClient<GetFederationSubscriptionsRequest>    _getFederationSubs;
    private readonly IRequestClient<CreateFederationSubscriptionRequest>  _createSub;
    private readonly IRequestClient<GetActiveSubscriptionCountRequest>    _activeSubCount;
    private readonly IRequestClient<GetAdminRacesRequest>               _getAdminRaces;
    private readonly IRequestClient<AdminDeleteRaceRequest>             _deleteRace;
    private readonly IRequestClient<GetAdminProgrammesRequest>          _getAdminProgrammes;
    private readonly IRequestClient<GetAdminAcePigeonResultsRequest>    _getAceResults;
    private readonly IRequestClient<GetAdminSuperAceResultsRequest>     _getSuperAceResults;
    private readonly IRequestClient<GetAdminBestLoftResultsRequest>     _getBestLoftResults;
    private readonly IRequestClient<NotifyClubManagersRequest>          _notifyManagers;
    private readonly IRequestClient<GetAdminPigeonsRequest>             _getAdminPigeons;
    private readonly IRequestClient<AdminUpdatePigeonRequest>           _updatePigeon;
    private readonly IRequestClient<AdminDeletePigeonRequest>           _deletePigeon;
    private readonly IRequestClient<GetAdminFanciersRequest>            _getFanciers;
    private readonly IRequestClient<LinkFancierToUserRequest>           _linkFancier;
    private readonly IRequestClient<UnlinkFancierRequest>               _unlinkFancier;
    private readonly IRequestClient<AdminDeleteProgrammeRequest>        _deleteProgramme;
    private readonly IRequestClient<GetAdminExternalLinksRequest>       _getAdminLinks;
    private readonly IRequestClient<AdminApproveLinkBusRequest>         _approveLink;
    private readonly IRequestClient<AdminRejectLinkBusRequest>          _rejectLink;
    private readonly IRequestClient<AdminRevokeLinkBusRequest>          _revokeLink;
    private readonly IRequestClient<GetAdminNotificationsRequest>       _getAdminNotifications;
    private readonly IRequestClient<AdminSendNotificationBusRequest>    _sendNotification;
    private readonly IRequestClient<AdminCreateSubscriptionPlanBusRequest> _createPlan;
    private readonly IRequestClient<AdminDeleteSubscriptionPlanBusRequest> _deletePlan;
    private readonly IRequestClient<GetAuditLogsRequest>                  _getAuditLogs;
    private readonly ILogger<BusAdminClient> _log;

    public BusAdminClient(
        IRequestClient<GetIdentityStatsRequest> idStats,
        IRequestClient<GetClubStatsRequest> clubStats,
        IRequestClient<GetRaceStatsRequest> raceStats,
        IRequestClient<GetFederationStatsRequest> fedStats,
        IRequestClient<GetSubscriptionStatsRequest> subStats,
        IRequestClient<GetUsersRequest> getUsers,
        IRequestClient<ValidateAdminCredentialsRequest> validateCreds,
        IRequestClient<ToggleUserActiveRequest> toggleUser,
        IRequestClient<AssignRoleRequest> assignRole,
        IRequestClient<SetUserLimitsRequest> setLimits,
        IRequestClient<DeleteUserRequest> deleteUser,
        IRequestClient<GetAllClubsRequest> getClubs,
        IRequestClient<ToggleClubActiveRequest> toggleClub,
        IRequestClient<AdminCreateClubRequest> adminCreateClub,
        IRequestClient<AdminAssignClubManagerRequest> adminAssignClubManager,
        IRequestClient<AdminDeleteClubRequest> deleteClub,
        IRequestClient<SetClubSubscriptionExpiryRequest> setClubExpiry,
        IRequestClient<GetFederationsRequest> getFederations,
        IRequestClient<CreateFederationRequest> createFederation,
        IRequestClient<ToggleFederationActiveRequest> toggleFederation,
        IRequestClient<AdminDeleteFederationRequest> deleteFederation,
        IRequestClient<GetUpgradeRequestsRequest> getUpgradeRequests,
        IRequestClient<ReviewUpgradeRequestRequest> reviewUpgradeRequest,
        IRequestClient<RevokeUpgradeRequestRequest> revokeUpgradeRequest,
        IRequestClient<GetSubscriptionPlansRequest> getPlans,
        IRequestClient<UpdateSubscriptionPlanBusRequest> updatePlan,
        IRequestClient<GetFederationSubscriptionsRequest> getFederationSubs,
        IRequestClient<CreateFederationSubscriptionRequest> createSub,
        IRequestClient<GetActiveSubscriptionCountRequest> activeSubCount,
        IRequestClient<GetAdminRacesRequest> getAdminRaces,
        IRequestClient<AdminDeleteRaceRequest> deleteRace,
        IRequestClient<GetAdminProgrammesRequest> getAdminProgrammes,
        IRequestClient<GetAdminAcePigeonResultsRequest> getAceResults,
        IRequestClient<GetAdminSuperAceResultsRequest> getSuperAceResults,
        IRequestClient<GetAdminBestLoftResultsRequest> getBestLoftResults,
        IRequestClient<NotifyClubManagersRequest> notifyManagers,
        IRequestClient<GetAdminPigeonsRequest> getAdminPigeons,
        IRequestClient<AdminUpdatePigeonRequest> updatePigeon,
        IRequestClient<AdminDeletePigeonRequest> deletePigeon,
        IRequestClient<GetAdminFanciersRequest> getFanciers,
        IRequestClient<LinkFancierToUserRequest> linkFancier,
        IRequestClient<UnlinkFancierRequest> unlinkFancier,
        IRequestClient<AdminDeleteProgrammeRequest> deleteProgramme,
        IRequestClient<GetAdminExternalLinksRequest> getAdminLinks,
        IRequestClient<AdminApproveLinkBusRequest> approveLink,
        IRequestClient<AdminRejectLinkBusRequest> rejectLink,
        IRequestClient<AdminRevokeLinkBusRequest> revokeLink,
        IRequestClient<GetAdminNotificationsRequest> getAdminNotifications,
        IRequestClient<AdminSendNotificationBusRequest> sendNotification,
        IRequestClient<AdminCreateSubscriptionPlanBusRequest> createPlan,
        IRequestClient<AdminDeleteSubscriptionPlanBusRequest> deletePlan,
        IRequestClient<GetAuditLogsRequest> getAuditLogs,
        ILogger<BusAdminClient> log)
    {
        _idStats        = idStats;
        _clubStats      = clubStats;
        _raceStats      = raceStats;
        _fedStats       = fedStats;
        _subStats       = subStats;
        _getUsers       = getUsers;
        _validateCreds  = validateCreds;
        _toggleUser     = toggleUser;
        _assignRole     = assignRole;
        _setLimits      = setLimits;
        _deleteUser     = deleteUser;
        _getClubs              = getClubs;
        _toggleClub            = toggleClub;
        _adminCreateClub       = adminCreateClub;
        _adminAssignClubManager = adminAssignClubManager;
        _deleteClub             = deleteClub;
        _setClubExpiry          = setClubExpiry;
        _getUpgradeRequests   = getUpgradeRequests;
        _reviewUpgradeRequest = reviewUpgradeRequest;
        _revokeUpgradeRequest = revokeUpgradeRequest;
        _getFederations       = getFederations;
        _createFederation  = createFederation;
        _toggleFederation  = toggleFederation;
        _deleteFederation  = deleteFederation;
        _getPlans          = getPlans;
        _updatePlan        = updatePlan;
        _getFederationSubs = getFederationSubs;
        _createSub         = createSub;
        _activeSubCount     = activeSubCount;
        _getAdminRaces      = getAdminRaces;
        _deleteRace         = deleteRace;
        _getAdminProgrammes = getAdminProgrammes;
        _getAceResults      = getAceResults;
        _getSuperAceResults = getSuperAceResults;
        _getBestLoftResults = getBestLoftResults;
        _notifyManagers  = notifyManagers;
        _getAdminPigeons = getAdminPigeons;
        _updatePigeon    = updatePigeon;
        _deletePigeon    = deletePigeon;
        _getFanciers     = getFanciers;
        _linkFancier     = linkFancier;
        _unlinkFancier   = unlinkFancier;
        _deleteProgramme       = deleteProgramme;
        _getAdminLinks         = getAdminLinks;
        _approveLink           = approveLink;
        _rejectLink            = rejectLink;
        _revokeLink            = revokeLink;
        _getAdminNotifications = getAdminNotifications;
        _sendNotification      = sendNotification;
        _createPlan            = createPlan;
        _deletePlan            = deletePlan;
        _getAuditLogs          = getAuditLogs;
        _log                   = log;
    }

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private async Task<TResponse?> Ask<TRequest, TResponse>(
        IRequestClient<TRequest> client, TRequest request,
        CancellationToken ct = default)
        where TRequest  : class
        where TResponse : class
    {
        try
        {
            var resp = await client.GetResponse<TResponse>(request, ct, Timeout);
            return resp.Message;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Bus request {Request} timed out or failed", typeof(TRequest).Name);
            return null;
        }
    }

    public Task<IdentityStatsResult?> GetIdentityStatsAsync(CancellationToken ct = default)
        => Ask<GetIdentityStatsRequest, IdentityStatsResult>(_idStats, new(), ct);

    public Task<ClubStatsResult?> GetClubStatsAsync(CancellationToken ct = default)
        => Ask<GetClubStatsRequest, ClubStatsResult>(_clubStats, new(), ct);

    public Task<RaceStatsResult?> GetRaceStatsAsync(CancellationToken ct = default)
        => Ask<GetRaceStatsRequest, RaceStatsResult>(_raceStats, new(), ct);

    public Task<FederationStatsResult?> GetFederationStatsAsync(CancellationToken ct = default)
        => Ask<GetFederationStatsRequest, FederationStatsResult>(_fedStats, new(), ct);

    public Task<SubscriptionStatsResult?> GetSubscriptionStatsAsync(CancellationToken ct = default)
        => Ask<GetSubscriptionStatsRequest, SubscriptionStatsResult>(_subStats, new(), ct);

    public Task<GetUsersResult?> GetUsersAsync(string? search, UserRole? role, int page, int pageSize, CancellationToken ct = default)
        => Ask<GetUsersRequest, GetUsersResult>(_getUsers, new(search, role, page, pageSize), ct);

    public Task<ValidateAdminCredentialsResult?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default)
        => Ask<ValidateAdminCredentialsRequest, ValidateAdminCredentialsResult>(_validateCreds, new(email, password), ct);

    public Task<ToggleUserActiveResult?> ToggleUserActiveAsync(Guid userId, Guid requestingUserId, CancellationToken ct = default)
        => Ask<ToggleUserActiveRequest, ToggleUserActiveResult>(_toggleUser, new(userId, requestingUserId), ct);

    public Task<AssignRoleResult?> AssignRoleAsync(Guid userId, UserRole role, Guid? federationId, CancellationToken ct = default)
        => Ask<AssignRoleRequest, AssignRoleResult>(_assignRole, new(userId, role, federationId), ct);

    public Task<SetUserLimitsResult?> SetUserLimitsAsync(Guid userId, int? maxResults, int? maxClubs, CancellationToken ct = default)
        => Ask<SetUserLimitsRequest, SetUserLimitsResult>(_setLimits, new(userId, maxResults, maxClubs), ct);

    public Task<DeleteUserResult?> DeleteUserAsync(Guid userId, Guid requestingUserId, CancellationToken ct = default)
        => Ask<DeleteUserRequest, DeleteUserResult>(_deleteUser, new(userId, requestingUserId), ct);

    public Task<AllClubsResult?> GetAllClubsAsync(string? search, Guid? federationId, int page, int pageSize, CancellationToken ct = default)
        => Ask<GetAllClubsRequest, AllClubsResult>(_getClubs, new(search, federationId, page, pageSize), ct);

    public Task<SetClubSubscriptionExpiryResult?> SetClubSubscriptionExpiryAsync(Guid clubId, DateTime? expiresAt, CancellationToken ct = default)
        => Ask<SetClubSubscriptionExpiryRequest, SetClubSubscriptionExpiryResult>(_setClubExpiry, new(clubId, expiresAt), ct);

    public Task<ToggleClubActiveResult?> ToggleClubActiveAsync(Guid clubId, CancellationToken ct = default)
        => Ask<ToggleClubActiveRequest, ToggleClubActiveResult>(_toggleClub, new(clubId), ct);

    public Task<FederationsResult?> GetFederationsAsync(int page, int pageSize, CancellationToken ct = default)
        => Ask<GetFederationsRequest, FederationsResult>(_getFederations, new(page, pageSize), ct);

    public Task<CreateFederationResult?> CreateFederationAsync(CreateFederationRequest req, CancellationToken ct = default)
        => Ask<CreateFederationRequest, CreateFederationResult>(_createFederation, req, ct);

    public Task<ToggleFederationActiveResult?> ToggleFederationActiveAsync(Guid federationId, CancellationToken ct = default)
        => Ask<ToggleFederationActiveRequest, ToggleFederationActiveResult>(_toggleFederation, new(federationId), ct);

    public Task<AdminDeleteFederationResult?> DeleteFederationAsync(Guid federationId, Guid adminUserId, string adminName, CancellationToken ct = default)
        => Ask<AdminDeleteFederationRequest, AdminDeleteFederationResult>(_deleteFederation, new(federationId, adminUserId, adminName), ct);

    public Task<GetUpgradeRequestsResult?> GetUpgradeRequestsAsync(
        Guid? federationId, UpgradeRequestStatus? status, int page, int pageSize, CancellationToken ct = default)
        => Ask<GetUpgradeRequestsRequest, GetUpgradeRequestsResult>(
            _getUpgradeRequests, new(federationId, status, page, pageSize), ct);

    public Task<ReviewUpgradeRequestResult?> ReviewUpgradeRequestAsync(
        Guid requestId, bool approved, string? reason, Guid reviewedByUserId, bool isAdmin = false, CancellationToken ct = default)
        => Ask<ReviewUpgradeRequestRequest, ReviewUpgradeRequestResult>(
            _reviewUpgradeRequest, new(requestId, approved, reason, reviewedByUserId, isAdmin), ct);

    public Task<RevokeUpgradeRequestResult?> RevokeUpgradeRequestAsync(
        Guid requestId, Guid revokedByUserId, CancellationToken ct = default)
        => Ask<RevokeUpgradeRequestRequest, RevokeUpgradeRequestResult>(
            _revokeUpgradeRequest, new(requestId, revokedByUserId, IsAdmin: true), ct);

    public Task<SubscriptionPlansResult?> GetSubscriptionPlansAsync(CancellationToken ct = default)
        => Ask<GetSubscriptionPlansRequest, SubscriptionPlansResult>(_getPlans, new(), ct);

    public Task<UpdateSubscriptionPlanBusResult?> UpdateSubscriptionPlanAsync(UpdateSubscriptionPlanBusRequest req, CancellationToken ct = default)
        => Ask<UpdateSubscriptionPlanBusRequest, UpdateSubscriptionPlanBusResult>(_updatePlan, req, ct);

    public Task<FederationSubscriptionsResult?> GetFederationSubscriptionsAsync(int page, int pageSize, string? search = null, string? billingCycle = null, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken ct = default)
        => Ask<GetFederationSubscriptionsRequest, FederationSubscriptionsResult>(_getFederationSubs, new(page, pageSize, search, billingCycle, dateFrom, dateTo), ct);

    public Task<CreateFederationSubscriptionResult?> CreateFederationSubscriptionAsync(CreateFederationSubscriptionRequest req, CancellationToken ct = default)
        => Ask<CreateFederationSubscriptionRequest, CreateFederationSubscriptionResult>(_createSub, req, ct);

    public Task<ActiveSubscriptionCountResult?> GetActiveSubscriptionCountAsync(CancellationToken ct = default)
        => Ask<GetActiveSubscriptionCountRequest, ActiveSubscriptionCountResult>(_activeSubCount, new(), ct);

    public Task<AdminRacesResult?> GetAdminRacesAsync(string? search, Guid? clubId, int? status, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct = default)
        => Ask<GetAdminRacesRequest, AdminRacesResult>(_getAdminRaces,
               new(search, clubId, status, dateFrom, dateTo, page, pageSize), ct);

    public Task<AdminDeleteRaceResult?> DeleteRaceAsync(Guid raceId, Guid adminUserId, string adminName, CancellationToken ct = default)
        => Ask<AdminDeleteRaceRequest, AdminDeleteRaceResult>(_deleteRace, new(raceId, adminUserId, adminName), ct);

    public Task<AdminProgrammesResult?> GetAdminProgrammesAsync(string? search, Guid? clubId, int page, int pageSize, CancellationToken ct = default)
        => Ask<GetAdminProgrammesRequest, AdminProgrammesResult>(_getAdminProgrammes, new(search, clubId, page, pageSize), ct);

    public Task<AdminAcePigeonResultsResult?> GetAdminAcePigeonResultsAsync(string? search, Guid? clubId, Guid? programmeId, int page, int pageSize, CancellationToken ct = default)
        => Ask<GetAdminAcePigeonResultsRequest, AdminAcePigeonResultsResult>(_getAceResults, new(search, clubId, programmeId, page, pageSize), ct);

    public Task<AdminSuperAceResultsResult?> GetAdminSuperAceResultsAsync(string? search, Guid? clubId, Guid? programmeId, int page, int pageSize, CancellationToken ct = default)
        => Ask<GetAdminSuperAceResultsRequest, AdminSuperAceResultsResult>(_getSuperAceResults, new(search, clubId, programmeId, page, pageSize), ct);

    public Task<AdminBestLoftResultsResult?> GetAdminBestLoftResultsAsync(string? search, Guid? clubId, Guid? programmeId, int page, int pageSize, CancellationToken ct = default)
        => Ask<GetAdminBestLoftResultsRequest, AdminBestLoftResultsResult>(_getBestLoftResults, new(search, clubId, programmeId, page, pageSize), ct);

    public Task<NotifyClubManagersResult?> NotifyClubManagersAsync(Guid clubId, string title, string message, string? entityType = null, string? entityId = null, CancellationToken ct = default)
        => Ask<NotifyClubManagersRequest, NotifyClubManagersResult>(_notifyManagers, new(clubId, title, message, entityType, entityId), ct);

    public Task<AdminPigeonsResult?> GetAdminPigeonsAsync(string? search, Guid? federationId, Guid? clubId, int page, int pageSize, string? fancierSearch = null, CancellationToken ct = default)
        => Ask<GetAdminPigeonsRequest, AdminPigeonsResult>(_getAdminPigeons, new(search, federationId, clubId, page, pageSize, fancierSearch), ct);

    public Task<AdminUpdatePigeonResult?> UpdatePigeonAsync(Guid pigeonId, string? name, string? sex, int? yearOfBirth, string? color, Guid updatedBy, CancellationToken ct = default)
        => Ask<AdminUpdatePigeonRequest, AdminUpdatePigeonResult>(_updatePigeon, new(pigeonId, name, sex, yearOfBirth, color, updatedBy), ct);

    public Task<AdminDeletePigeonResult?> DeletePigeonAsync(Guid pigeonId, Guid adminUserId, CancellationToken ct = default)
        => Ask<AdminDeletePigeonRequest, AdminDeletePigeonResult>(_deletePigeon, new(pigeonId, adminUserId), ct);

    public Task<GetAdminFanciersResult?> GetFanciersAsync(string? search, Guid? clubId, Guid? federationId, bool? isLinked, int page, int pageSize, CancellationToken ct = default)
        => Ask<GetAdminFanciersRequest, GetAdminFanciersResult>(_getFanciers, new(search, clubId, federationId, isLinked, page, pageSize), ct);

    public Task<LinkFancierToUserResult?> LinkFancierAsync(Guid fancierId, Guid userId, string userName, string userEmail, CancellationToken ct = default)
        => Ask<LinkFancierToUserRequest, LinkFancierToUserResult>(_linkFancier, new(fancierId, userId, userName, userEmail), ct);

    public Task<UnlinkFancierResult?> UnlinkFancierAsync(Guid fancierId, CancellationToken ct = default)
        => Ask<UnlinkFancierRequest, UnlinkFancierResult>(_unlinkFancier, new(fancierId), ct);

    public Task<AdminDeleteProgrammeResult?> DeleteProgrammeAsync(Guid programmeId, Guid adminUserId, CancellationToken ct = default)
        => Ask<AdminDeleteProgrammeRequest, AdminDeleteProgrammeResult>(_deleteProgramme, new(programmeId, adminUserId), ct);

    public Task<GetAdminExternalLinksResult?> GetAdminExternalLinksAsync(int? status, int page, int pageSize, CancellationToken ct = default)
        => Ask<GetAdminExternalLinksRequest, GetAdminExternalLinksResult>(_getAdminLinks, new(status, page, pageSize), ct);

    public Task<AdminApproveLinkBusResult?> AdminApproveLinkAsync(Guid linkId, Guid adminUserId, CancellationToken ct = default)
        => Ask<AdminApproveLinkBusRequest, AdminApproveLinkBusResult>(_approveLink, new(linkId, adminUserId), ct);

    public Task<AdminRejectLinkBusResult?> AdminRejectLinkAsync(Guid linkId, string? reason, Guid adminUserId, CancellationToken ct = default)
        => Ask<AdminRejectLinkBusRequest, AdminRejectLinkBusResult>(_rejectLink, new(linkId, reason, adminUserId), ct);

    public Task<AdminRevokeLinkBusResult?> AdminRevokeLinkAsync(Guid linkId, CancellationToken ct = default)
        => Ask<AdminRevokeLinkBusRequest, AdminRevokeLinkBusResult>(_revokeLink, new(linkId), ct);

    public Task<GetAdminNotificationsResult?> GetAdminNotificationsAsync(string? search, int page, int pageSize, CancellationToken ct = default)
        => Ask<GetAdminNotificationsRequest, GetAdminNotificationsResult>(_getAdminNotifications, new(search, page, pageSize), ct);

    public Task<AdminSendNotificationBusResult?> AdminSendNotificationAsync(Guid clubId, string title, string message, Guid sentBy, CancellationToken ct = default)
        => Ask<AdminSendNotificationBusRequest, AdminSendNotificationBusResult>(_sendNotification, new(clubId, title, message, sentBy), ct);

    public Task<AdminCreateSubscriptionPlanBusResult?> AdminCreateSubscriptionPlanAsync(AdminCreateSubscriptionPlanBusRequest req, CancellationToken ct = default)
        => Ask<AdminCreateSubscriptionPlanBusRequest, AdminCreateSubscriptionPlanBusResult>(_createPlan, req, ct);

    public Task<AdminDeleteSubscriptionPlanBusResult?> AdminDeleteSubscriptionPlanAsync(Guid planId, Guid deletedBy, CancellationToken ct = default)
        => Ask<AdminDeleteSubscriptionPlanBusRequest, AdminDeleteSubscriptionPlanBusResult>(_deletePlan, new(planId, deletedBy), ct);

    public Task<AdminCreateClubResult?> AdminCreateClubAsync(Guid? federationId, string name, string code, string? city, Guid createdBy, CancellationToken ct = default)
        => Ask<AdminCreateClubRequest, AdminCreateClubResult>(_adminCreateClub, new(federationId, name, code, city, createdBy), ct);

    public Task<AdminAssignClubManagerResult?> AdminAssignClubManagerAsync(Guid clubId, Guid userId, string fullName, string email, bool force, CancellationToken ct = default)
        => Ask<AdminAssignClubManagerRequest, AdminAssignClubManagerResult>(_adminAssignClubManager, new(clubId, userId, fullName, email, force), ct);

    public Task<AdminDeleteClubResult?> DeleteClubAsync(Guid clubId, Guid adminUserId, string adminName, CancellationToken ct = default)
        => Ask<AdminDeleteClubRequest, AdminDeleteClubResult>(_deleteClub, new(clubId, adminUserId, adminName), ct);

    public Task<GetAuditLogsResponse?> GetAuditLogsAsync(string? action, string? entityType, AuditSeverity? severity, int page, int pageSize, CancellationToken ct = default)
        => Ask<GetAuditLogsRequest, GetAuditLogsResponse>(_getAuditLogs, new(action, entityType, severity, page, pageSize), ct);
}
