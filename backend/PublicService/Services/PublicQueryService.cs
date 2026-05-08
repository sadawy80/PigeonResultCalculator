using MassTransit;
using PRC.Common.Messages;

namespace PRC.PublicService.Services;

public class PublicQueryService : IPublicQueryService
{
    private readonly IRequestClient<GetPublicClubBySlugRequest>         _clubClient;
    private readonly IRequestClient<ListPublishedClubsForPublicRequest> _clubListClient;
    private readonly IRequestClient<GetPublishedRacesForPublicRequest>  _racesClient;
    private readonly IRequestClient<GetPublicFederationBySlugRequest>      _countryClient;
    private readonly IRequestClient<GetPublicSubscriptionPlansRequest>  _plansClient;
    private readonly IRequestClient<GetUserNamesRequest>                _namesClient;

    public PublicQueryService(
        IRequestClient<GetPublicClubBySlugRequest>         clubClient,
        IRequestClient<ListPublishedClubsForPublicRequest> clubListClient,
        IRequestClient<GetPublishedRacesForPublicRequest>  racesClient,
        IRequestClient<GetPublicFederationBySlugRequest>      countryClient,
        IRequestClient<GetPublicSubscriptionPlansRequest>  plansClient,
        IRequestClient<GetUserNamesRequest>                namesClient)
    {
        _clubClient     = clubClient;
        _clubListClient = clubListClient;
        _racesClient    = racesClient;
        _countryClient  = countryClient;
        _plansClient    = plansClient;
        _namesClient    = namesClient;
    }

    public async Task<PublicClubResult?> GetClubBySlugAsync(string slug, CancellationToken ct = default)
    {
        var resp = await _clubClient.GetResponse<PublicClubResult>(
            new GetPublicClubBySlugRequest(slug), ct);
        return resp.Message.Found ? resp.Message : null;
    }

    public async Task<PublishedRacesForPublicResult?> GetPublishedRacesAsync(Guid clubId, int take, CancellationToken ct = default)
    {
        var resp = await _racesClient.GetResponse<PublishedRacesForPublicResult>(
            new GetPublishedRacesForPublicRequest(clubId, take), ct);
        return resp.Message;
    }

    public async Task<IDictionary<Guid, string>> GetUserNamesAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var list = ids.Distinct().ToList();
        if (list.Count == 0) return new Dictionary<Guid, string>();

        var resp = await _namesClient.GetResponse<UserNamesResult>(
            new GetUserNamesRequest(list), ct);
        return resp.Message.Names as IDictionary<Guid, string> ?? new Dictionary<Guid, string>(resp.Message.Names);
    }

    public async Task<ListPublishedClubsForPublicResult?> ListPublishedClubsAsync(
        string? FederationCode, Guid? FederationId, int page, int pageSize, CancellationToken ct = default)
    {
        var resp = await _clubListClient.GetResponse<ListPublishedClubsForPublicResult>(
            new ListPublishedClubsForPublicRequest(FederationCode, FederationId, page, pageSize), ct);
        return resp.Message;
    }

    public async Task<PublicFederationResult?> GetFederationBySlugAsync(string slug, CancellationToken ct = default)
    {
        var resp = await _countryClient.GetResponse<PublicFederationResult>(
            new GetPublicFederationBySlugRequest(slug), ct);
        return resp.Message.Found ? resp.Message : null;
    }

    public async Task<PublicSubscriptionPlansResult?> GetPublicPlansAsync(CancellationToken ct = default)
    {
        var resp = await _plansClient.GetResponse<PublicSubscriptionPlansResult>(
            new GetPublicSubscriptionPlansRequest(), ct);
        return resp.Message;
    }
}
