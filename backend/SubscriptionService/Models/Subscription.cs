using PRC.Common;

namespace PRC.SubscriptionService.Models;

public class SubscriptionPlan : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public SubscriptionType Type { get; set; }
    public BillingCycle BillingCycle { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public int MaxClubs { get; set; }
    public int MaxResultsPerClub { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsHighlighted { get; set; } = false;
    public int SortOrder { get; set; } = 0;
    public string? Description { get; set; }
    public string? Features { get; set; }

    public ICollection<FederationSubscription> FederationSubscriptions { get; set; } = new List<FederationSubscription>();
    public ICollection<ClubSubscription> ClubSubscriptions { get; set; } = new List<ClubSubscription>();
}

public class FederationSubscription : BaseEntity
{
    public Guid FederationId { get; set; }
    public string FederationName { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public BillingCycle BillingCycle { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RenewsAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public int CurrentClubCount { get; set; } = 0;
    public decimal AmountPaid { get; set; }
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }

    public SubscriptionPlan Plan { get; set; } = null!;
}

public class ClubSubscription : BaseEntity
{
    public Guid ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime StartedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int ResultsUsedThisPeriod { get; set; } = 0;
    public decimal AmountPaid { get; set; }
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }

    public SubscriptionPlan Plan { get; set; } = null!;
}
