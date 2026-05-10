using PRC.Common;
using PRC.IntegrationService.DTOs;
using PRC.IntegrationService.Models;

namespace PRC.IntegrationService.Services;

public interface IIntegrationService
{
    Task<Result<RequestExternalLinkResponse>> RequestLinkAsync(
        string externalPlatformName, string externalUserId, string externalLoftId,
        string externalLoftName, string callbackUrl, Guid clubId, Guid? prcUserId,
        string? metadataJson, CancellationToken ct);

    Task<Result<ExternalLinkDto>> ReviewLinkAsync(
        Guid linkId, bool approve, string? rejectionReason, Guid reviewerUserId, CancellationToken ct);

    Task<Result> RevokeLinkAsync(Guid linkId, string? reason, CancellationToken ct);

    Task<Result> RevokeByTokenAsync(string linkToken, string? reason, CancellationToken ct);

    Task<Result<List<ExternalLinkDto>>> GetClubLinksAsync(
        Guid clubId, ExternalLinkStatus? status, CancellationToken ct);

    Task<Result<List<ExternalLinkDto>>> GetMyLinksAsync(Guid userId, CancellationToken ct);

    Task<Result<List<ExternalLinkDto>>> GetAllLinksAsync(
        ExternalLinkStatus? status, int page, int pageSize, CancellationToken ct);

    // Data endpoints (token-authenticated — for PLM)
    Task<Result<(ExternalLink Link, string? Error)>> ValidateTokenAsync(
        string accessToken, CancellationToken ct);
}

public record RequestExternalLinkResponse(Guid LinkId, string LinkToken, string Status, string Message);
