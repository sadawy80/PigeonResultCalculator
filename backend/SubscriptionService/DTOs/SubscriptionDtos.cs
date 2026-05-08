using PRC.Common;

namespace PRC.SubscriptionService.DTOs;

public record SubscriptionPlanDto(
    Guid Id, string Name, string? Description,
    SubscriptionType Type, BillingCycle BillingCycle,
    decimal Price, string Currency,
    int MaxClubs, int MaxResultsPerClub,
    bool IsActive, bool IsHighlighted, int SortOrder,
    string? Features, DateTime CreatedAt);

public record FederationSubscriptionDto(
    Guid Id, Guid FederationId, string FederationName,
    Guid PlanId, string PlanName,
    SubscriptionStatus Status, BillingCycle BillingCycle,
    DateTime StartedAt, DateTime ExpiresAt,
    DateTime? RenewsAt, DateTime? CancelledAt,
    int CurrentClubCount, decimal AmountPaid,
    string? PaymentReference, string? Notes);

public record ClubSubscriptionDto(
    Guid Id, Guid ClubId, string ClubName,
    Guid PlanId, string PlanName,
    SubscriptionStatus Status,
    DateTime StartedAt, DateTime ExpiresAt,
    int ResultsUsedThisPeriod, decimal AmountPaid,
    string? PaymentReference, string? Notes);

public record SubscriptionValidationResult(
    bool IsActive, string PlanName,
    int MaxClubs, int MaxResultsPerClub,
    int CurrentClubCount, bool IsUnlimited);

// Requests
public record CreateSubscriptionPlanRequest(
    string Name, string? Description,
    SubscriptionType Type, BillingCycle BillingCycle,
    decimal Price, string Currency,
    int MaxClubs, int MaxResultsPerClub,
    bool IsHighlighted, int SortOrder, string? Features);

public record UpdateSubscriptionPlanRequest(
    string Name, string? Description,
    decimal Price, int MaxClubs, int MaxResultsPerClub,
    bool IsActive, bool IsHighlighted, int SortOrder, string? Features);

public record CreateFederationSubscriptionRequest(
    Guid FederationId, string FederationName,
    Guid PlanId, BillingCycle BillingCycle,
    decimal AmountPaid, string? PaymentReference, string? Notes);

public record CreateClubSubscriptionRequest(
    Guid ClubId, string ClubName,
    Guid PlanId,
    decimal AmountPaid, string? PaymentReference, string? Notes);
