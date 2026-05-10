using PRC.Common;
using PRC.SubscriptionService.DTOs;

namespace PRC.SubscriptionService.Services;

public interface ISubscriptionService
{
    // Plans
    Task<Result<List<SubscriptionPlanDto>>> GetPlansAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<Result<SubscriptionPlanDto>> GetPlanAsync(Guid planId, CancellationToken ct = default);
    Task<Result<SubscriptionPlanDto>> CreatePlanAsync(CreateSubscriptionPlanRequest req, Guid createdBy, CancellationToken ct = default);
    Task<Result<SubscriptionPlanDto>> UpdatePlanAsync(Guid planId, UpdateSubscriptionPlanRequest req, Guid updatedBy, CancellationToken ct = default);
    Task<Result> DeletePlanAsync(Guid planId, Guid deletedBy, CancellationToken ct = default);

    // Federation subscriptions
    Task<Result<FederationSubscriptionDto>> GetActiveFederationSubscriptionAsync(Guid FederationId, CancellationToken ct = default);
    Task<Result<PagedResult<FederationSubscriptionDto>>> GetFederationSubscriptionsAsync(int page, int pageSize, string? search = null, string? billingCycle = null, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken ct = default);
    Task<Result<FederationSubscriptionDto>> CreateFederationSubscriptionAsync(CreateFederationSubscriptionRequest req, Guid createdBy, CancellationToken ct = default);

    // Club subscriptions
    Task<Result<ClubSubscriptionDto>> GetActiveClubSubscriptionAsync(Guid clubId, CancellationToken ct = default);
    Task<Result<ClubSubscriptionDto>> CreateClubSubscriptionAsync(CreateClubSubscriptionRequest req, Guid createdBy, CancellationToken ct = default);

    // Internal validation
    Task<SubscriptionValidationResult?> ValidateFederationAsync(Guid FederationId, CancellationToken ct = default);
    Task<SubscriptionValidationResult?> ValidateClubAsync(Guid clubId, CancellationToken ct = default);
}
