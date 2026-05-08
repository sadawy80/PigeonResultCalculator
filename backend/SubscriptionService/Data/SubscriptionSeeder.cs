using System.Text.Json;
using PRC.Common;
using PRC.SubscriptionService.Models;

namespace PRC.SubscriptionService.Data;

public static class SubscriptionSeeder
{
    public static async Task SeedAsync(SubscriptionDbContext db)
    {
        if (db.SubscriptionPlans.Any()) return;

        var plans = new List<SubscriptionPlan>();
        int sort = 1;

        plans.AddRange(BuildTier("Starter", "For small clubs and new federations getting started",
            sort++, false, SubscriptionType.Federation,
            monthly:  (29m,   3,  300,  ["Up to 3 clubs", "300 results/month", "Standard print templates", "Email support"]),
            seasonal: (149m,  5,  500,  ["Up to 5 clubs", "500 results/season (6 months)", "Standard print templates", "Email support"]),
            annual:   (279m,  8, 1500,  ["Up to 8 clubs", "1,500 results/year", "Standard print templates", "Priority email support"])));

        plans.AddRange(BuildTier("Standard", "For growing national federations",
            sort++, false, SubscriptionType.Federation,
            monthly:  (79m,   10,  1000,  ["Up to 10 clubs", "1,000 results/month", "All print templates", "ETS file import", "Priority support"]),
            seasonal: (399m,  20,  5000,  ["Up to 20 clubs", "5,000 results/season (6 months)", "All print templates", "ETS file import", "Priority support"]),
            annual:   (749m,  30, 15000,  ["Up to 30 clubs", "15,000 results/year", "All print templates", "ETS file import", "Dedicated support"])));

        plans.AddRange(BuildTier("Professional", "For active multi-region federations",
            sort++, true, SubscriptionType.Federation,
            monthly:  (149m,  30, 0, ["Up to 30 clubs", "Unlimited results", "IoT live tracking", "Custom branding", "Dedicated support"]),
            seasonal: (749m,  50, 0, ["Up to 50 clubs", "Unlimited results", "IoT live tracking", "Custom branding", "Dedicated support"]),
            annual:   (1399m, 75, 0, ["Up to 75 clubs", "Unlimited results", "IoT live tracking", "Custom branding", "SLA guarantee"])));

        plans.AddRange(BuildTier("Enterprise", "For national bodies at full scale",
            sort++, false, SubscriptionType.Federation,
            monthly:  (299m,  0, 0, ["Unlimited clubs", "Unlimited results", "National broadcast page", "Race timing device API", "SLA guarantee"]),
            seasonal: (1499m, 0, 0, ["Unlimited clubs", "Unlimited results", "National broadcast page", "Race timing device API", "SLA guarantee"]),
            annual:   (2799m, 0, 0, ["Unlimited clubs", "Unlimited results", "National broadcast page", "Race timing device API", "SLA guarantee"])));

        db.SubscriptionPlans.AddRange(plans);
        await db.SaveChangesAsync();
    }

    private static IEnumerable<SubscriptionPlan> BuildTier(
        string name, string description, int sortOrder, bool isHighlighted,
        SubscriptionType type,
        (decimal price, int maxClubs, int maxResults, string[] features) monthly,
        (decimal price, int maxClubs, int maxResults, string[] features) seasonal,
        (decimal price, int maxClubs, int maxResults, string[] features) annual)
    {
        yield return Make(name, description, sortOrder, isHighlighted, type, BillingCycle.Monthly,  monthly.price,  monthly.maxClubs,  monthly.maxResults,  monthly.features);
        yield return Make(name, description, sortOrder, isHighlighted, type, BillingCycle.Seasonal, seasonal.price, seasonal.maxClubs, seasonal.maxResults, seasonal.features);
        yield return Make(name, description, sortOrder, isHighlighted, type, BillingCycle.Annual,   annual.price,   annual.maxClubs,   annual.maxResults,   annual.features);
    }

    private static SubscriptionPlan Make(
        string name, string description, int sortOrder, bool isHighlighted,
        SubscriptionType type, BillingCycle cycle,
        decimal price, int maxClubs, int maxResults, string[] features) =>
        new()
        {
            Name              = name,
            Description       = description,
            Type              = type,
            BillingCycle      = cycle,
            Price             = price,
            Currency          = "EUR",
            MaxClubs          = maxClubs,
            MaxResultsPerClub = maxResults,
            IsActive          = true,
            IsHighlighted     = isHighlighted,
            SortOrder         = sortOrder,
            Features          = JsonSerializer.Serialize(features),
            CreatedAt         = DateTime.UtcNow,
        };
}
