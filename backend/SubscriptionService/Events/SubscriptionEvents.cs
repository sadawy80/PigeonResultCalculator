namespace PRC.SubscriptionService.Events;

public record SubscriptionCreated(
    Guid SubscriptionId,
    Guid FederationId,
    string FederationName,
    string PlanName,
    string BillingCycle,
    DateTime ExpiresAt,
    DateTime OccurredAt);

public record SubscriptionExpired(
    Guid SubscriptionId,
    Guid FederationId,
    string FederationName,
    string PlanName,
    DateTime ExpiredAt,
    DateTime OccurredAt);

public record SubscriptionCancelled(
    Guid SubscriptionId,
    Guid FederationId,
    string FederationName,
    DateTime CancelledAt,
    DateTime OccurredAt);
