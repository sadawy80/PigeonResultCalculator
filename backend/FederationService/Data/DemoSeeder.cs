using Microsoft.EntityFrameworkCore;
using PRC.FederationService.Models;
using Serilog;

namespace PRC.FederationService.Data;

public static class DemoSeeder
{
    // Must match AdminSeeder fixed GUIDs in IdentityService
    private static readonly Guid FederationId = new("a1a1a1a1-0000-0000-0000-000000000001");
    private static readonly Guid FedManagerId = new("d4d4d4d4-0000-0000-0000-000000000004");

    public static async Task SeedAsync(FederationDbContext db)
    {
        if (await db.Federations.AnyAsync(f => f.Id == FederationId))
            return;

        db.Federations.Add(new Federation
        {
            Id              = FederationId,
            Name            = "United Kingdom",
            Code            = "GB",
            Slug            = "united-kingdom",
            FlagUrl         = "",
            DefaultLanguage = "en",
            DefaultTimezone = "Europe/London",
            IsActive        = true,
            ManagerEmail    = "fedmanager@prc.local",
            ManagerName     = "Federation Manager",
        });

        await db.SaveChangesAsync();
        Log.Information("DemoSeeder (Federation): seeded demo federation");
    }
}
