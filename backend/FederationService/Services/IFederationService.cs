using PRC.Common;
using PRC.FederationService.DTOs;

namespace PRC.FederationService.Services;

public interface IFederationService
{
    Task<Result<FederationResultDto>> CreateFederationResultAsync(CreateFederationResultRequest req, Guid createdBy, CancellationToken ct);
    Task<Result<FederationResultDto>> PublishFederationResultAsync(Guid federationResultId, Guid publishedBy, CancellationToken ct);
    Task<Result<FederationResultDto>> GetFederationResultAsync(Guid federationResultId, CancellationToken ct);
    Task<Result<PagedResult<FederationResultDto>>> GetFederationResultsAsync(Guid federationId, PagedQuery paged, CancellationToken ct);
    Task<Result<object>> GetFederationPageAsync(Guid federationId, CancellationToken ct);
    Task<Result> UpdateFederationPageAsync(Guid federationId, UpdateFederationPageRequest req, CancellationToken ct);
    Task<Result<IEnumerable<FederationDto>>> GetAllFederationsAsync(CancellationToken ct);
}
