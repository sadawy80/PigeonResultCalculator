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

    // Clubs
    Task<AllClubsResult?> GetAllClubsAsync(string? search, Guid? federationId, int page, int pageSize, CancellationToken ct = default);
    Task<ToggleClubActiveResult?> ToggleClubActiveAsync(Guid clubId, CancellationToken ct = default);

    // Countries
    Task<FederationsResult?> GetFederationsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<CreateFederationResult?> CreateFederationAsync(CreateFederationRequest req, CancellationToken ct = default);
    Task<ToggleFederationActiveResult?> ToggleFederationActiveAsync(Guid federationId, CancellationToken ct = default);

    // Upgrade requests
    Task<GetUpgradeRequestsResult?> GetUpgradeRequestsAsync(Guid? federationId, UpgradeRequestStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<ReviewUpgradeRequestResult?> ReviewUpgradeRequestAsync(Guid requestId, bool approved, string? reason, Guid reviewedByUserId, CancellationToken ct = default);

    // Subscription plans
    Task<SubscriptionPlansResult?> GetSubscriptionPlansAsync(CancellationToken ct = default);
    Task<FederationSubscriptionsResult?> GetFederationSubscriptionsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<CreateFederationSubscriptionResult?> CreateFederationSubscriptionAsync(CreateFederationSubscriptionRequest req, CancellationToken ct = default);
    Task<ActiveSubscriptionCountResult?> GetActiveSubscriptionCountAsync(CancellationToken ct = default);
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
    private readonly IRequestClient<GetAllClubsRequest>              _getClubs;
    private readonly IRequestClient<ToggleClubActiveRequest>         _toggleClub;
    private readonly IRequestClient<GetFederationsRequest>             _getFederations;
    private readonly IRequestClient<CreateFederationRequest>            _createFederation;
    private readonly IRequestClient<ToggleFederationActiveRequest>      _toggleFederation;
    private readonly IRequestClient<GetUpgradeRequestsRequest>    _getUpgradeRequests;
    private readonly IRequestClient<ReviewUpgradeRequestRequest>   _reviewUpgradeRequest;
    private readonly IRequestClient<GetSubscriptionPlansRequest>   _getPlans;
    private readonly IRequestClient<GetFederationSubscriptionsRequest>  _getFederationSubs;
    private readonly IRequestClient<CreateFederationSubscriptionRequest> _createSub;
    private readonly IRequestClient<GetActiveSubscriptionCountRequest> _activeSubCount;
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
        IRequestClient<GetAllClubsRequest> getClubs,
        IRequestClient<ToggleClubActiveRequest> toggleClub,
        IRequestClient<GetFederationsRequest> getCountries,
        IRequestClient<CreateFederationRequest> createCountry,
        IRequestClient<ToggleFederationActiveRequest> toggleCountry,
        IRequestClient<GetUpgradeRequestsRequest> getUpgradeRequests,
        IRequestClient<ReviewUpgradeRequestRequest> reviewUpgradeRequest,
        IRequestClient<GetSubscriptionPlansRequest> getPlans,
        IRequestClient<GetFederationSubscriptionsRequest> getCountrySubs,
        IRequestClient<CreateFederationSubscriptionRequest> createSub,
        IRequestClient<GetActiveSubscriptionCountRequest> activeSubCount,
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
        _getClubs       = getClubs;
        _toggleClub     = toggleClub;
        _getUpgradeRequests   = getUpgradeRequests;
        _reviewUpgradeRequest = reviewUpgradeRequest;
        _getFederations       = getCountries;
        _createFederation  = createCountry;
        _toggleFederation  = toggleCountry;
        _getPlans       = getPlans;
        _getFederationSubs = getCountrySubs;
        _createSub      = createSub;
        _activeSubCount = activeSubCount;
        _log            = log;
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

    public Task<AllClubsResult?> GetAllClubsAsync(string? search, Guid? federationId, int page, int pageSize, CancellationToken ct = default)
        => Ask<GetAllClubsRequest, AllClubsResult>(_getClubs, new(search, federationId, page, pageSize), ct);

    public Task<ToggleClubActiveResult?> ToggleClubActiveAsync(Guid clubId, CancellationToken ct = default)
        => Ask<ToggleClubActiveRequest, ToggleClubActiveResult>(_toggleClub, new(clubId), ct);

    public Task<FederationsResult?> GetFederationsAsync(int page, int pageSize, CancellationToken ct = default)
        => Ask<GetFederationsRequest, FederationsResult>(_getFederations, new(page, pageSize), ct);

    public Task<CreateFederationResult?> CreateFederationAsync(CreateFederationRequest req, CancellationToken ct = default)
        => Ask<CreateFederationRequest, CreateFederationResult>(_createFederation, req, ct);

    public Task<ToggleFederationActiveResult?> ToggleFederationActiveAsync(Guid federationId, CancellationToken ct = default)
        => Ask<ToggleFederationActiveRequest, ToggleFederationActiveResult>(_toggleFederation, new(federationId), ct);

    public Task<GetUpgradeRequestsResult?> GetUpgradeRequestsAsync(
        Guid? federationId, UpgradeRequestStatus? status, int page, int pageSize, CancellationToken ct = default)
        => Ask<GetUpgradeRequestsRequest, GetUpgradeRequestsResult>(
            _getUpgradeRequests, new(federationId, status, page, pageSize), ct);

    public Task<ReviewUpgradeRequestResult?> ReviewUpgradeRequestAsync(
        Guid requestId, bool approved, string? reason, Guid reviewedByUserId, CancellationToken ct = default)
        => Ask<ReviewUpgradeRequestRequest, ReviewUpgradeRequestResult>(
            _reviewUpgradeRequest, new(requestId, approved, reason, reviewedByUserId), ct);

    public Task<SubscriptionPlansResult?> GetSubscriptionPlansAsync(CancellationToken ct = default)
        => Ask<GetSubscriptionPlansRequest, SubscriptionPlansResult>(_getPlans, new(), ct);

    public Task<FederationSubscriptionsResult?> GetFederationSubscriptionsAsync(int page, int pageSize, CancellationToken ct = default)
        => Ask<GetFederationSubscriptionsRequest, FederationSubscriptionsResult>(_getFederationSubs, new(page, pageSize), ct);

    public Task<CreateFederationSubscriptionResult?> CreateFederationSubscriptionAsync(CreateFederationSubscriptionRequest req, CancellationToken ct = default)
        => Ask<CreateFederationSubscriptionRequest, CreateFederationSubscriptionResult>(_createSub, req, ct);

    public Task<ActiveSubscriptionCountResult?> GetActiveSubscriptionCountAsync(CancellationToken ct = default)
        => Ask<GetActiveSubscriptionCountRequest, ActiveSubscriptionCountResult>(_activeSubCount, new(), ct);
}
