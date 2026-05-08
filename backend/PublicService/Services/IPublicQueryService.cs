using PRC.Common.Messages;

namespace PRC.PublicService.Services;

public interface IPublicQueryService
{
    Task<PublicClubResult?> GetClubBySlugAsync(string slug, CancellationToken ct = default);
    Task<PublishedRacesForPublicResult?> GetPublishedRacesAsync(Guid clubId, int take, CancellationToken ct = default);
    Task<IDictionary<Guid, string>> GetUserNamesAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<ListPublishedClubsForPublicResult?> ListPublishedClubsAsync(string? FederationCode, Guid? FederationId, int page, int pageSize, CancellationToken ct = default);
    Task<PublicFederationResult?> GetFederationBySlugAsync(string slug, CancellationToken ct = default);
    Task<PublicSubscriptionPlansResult?> GetPublicPlansAsync(CancellationToken ct = default);
}
