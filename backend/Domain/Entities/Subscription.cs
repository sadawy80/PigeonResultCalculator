using PigeonRacing.Domain.Common;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.Domain.Entities;

public class SubscriptionPlan : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public SubscriptionType Type { get; set; }
    public BillingCycle BillingCycle { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public int MaxClubs { get; set; }              // for country plans
    public int MaxResultsPerClub { get; set; }     // per billing period
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public string? Features { get; set; }          // JSON array of feature strings

    // Navigation
    public ICollection<CountrySubscription> CountrySubscriptions { get; set; } = new List<CountrySubscription>();
    public ICollection<ClubSubscription> ClubSubscriptions { get; set; } = new List<ClubSubscription>();
}

public class CountrySubscription : BaseEntity
{
    public Guid CountryId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime StartsAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int CurrentClubCount { get; set; } = 0;
    public decimal AmountPaid { get; set; }
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Country Country { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;
}

public class ClubSubscription : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime StartsAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int ResultsUsedThisPeriod { get; set; } = 0;
    public decimal AmountPaid { get; set; }
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Club Club { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;
}
