using Microsoft.AspNetCore.Identity;
using PRC.Common;
using PRC.IdentityService.Models;
using Serilog;

namespace PRC.IdentityService.Data;

public static class AdminSeeder
{
    // Fixed GUIDs — shared across all service seeders so FK references stay consistent
    public static readonly Guid FederationId  = new("a1a1a1a1-0000-0000-0000-000000000001");
    public static readonly Guid ClubId        = new("b2b2b2b2-0000-0000-0000-000000000002");
    public static readonly Guid SuperAdminId  = new("c3c3c3c3-0000-0000-0000-000000000003");
    public static readonly Guid FedManagerId  = new("d4d4d4d4-0000-0000-0000-000000000004");
    public static readonly Guid ClubManagerId = new("e5e5e5e5-0000-0000-0000-000000000005");
    public static readonly Guid FancierId     = new("f6f6f6f6-0000-0000-0000-000000000006");

    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db          = services.GetRequiredService<IdentityDbContext>();

        // Ensure all Identity roles exist
        foreach (var role in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role) { NormalizedName = role.ToUpperInvariant() });
        }

        await CreateUser(userManager, db, new ApplicationUser
        {
            Id        = SuperAdminId,
            UserName  = config["AdminSeed:Email"]     ?? "admin@prc.local",
            Email     = config["AdminSeed:Email"]     ?? "admin@prc.local",
            FirstName = config["AdminSeed:FirstName"] ?? "Platform",
            LastName  = config["AdminSeed:LastName"]  ?? "Admin",
            Role      = UserRole.SuperAdmin,
            IsActive  = true,
            EmailConfirmed = true,
        }, config["AdminSeed:Password"] ?? "Admin@1234", UserRole.SuperAdmin);

        await CreateUser(userManager, db, new ApplicationUser
        {
            Id           = FedManagerId,
            UserName     = "fedmanager@prc.local",
            Email        = "fedmanager@prc.local",
            FirstName    = "Federation",
            LastName     = "Manager",
            Role         = UserRole.FederationManager,
            FederationId = FederationId,
            IsActive     = true,
            EmailConfirmed = true,
        }, "Manager@1234", UserRole.FederationManager);

        await CreateUser(userManager, db, new ApplicationUser
        {
            Id        = ClubManagerId,
            UserName  = "clubmanager@prc.local",
            Email     = "clubmanager@prc.local",
            FirstName = "Club",
            LastName  = "Manager",
            Role      = UserRole.ClubManager,
            IsActive  = true,
            EmailConfirmed = true,
        }, "Manager@1234", UserRole.ClubManager,
        membership: new ClubMembership { UserId = ClubManagerId, ClubId = ClubId });

        await CreateUser(userManager, db, new ApplicationUser
        {
            Id        = FancierId,
            UserName  = "fancier@prc.local",
            Email     = "fancier@prc.local",
            FirstName = "Demo",
            LastName  = "Fancier",
            Role      = UserRole.Fancier,
            IsActive  = true,
            EmailConfirmed = true,
        }, "Fancier@1234", UserRole.Fancier,
        membership: new ClubMembership { UserId = FancierId, ClubId = ClubId });
    }

    private static async Task CreateUser(
        UserManager<ApplicationUser> userManager,
        IdentityDbContext db,
        ApplicationUser user,
        string password,
        UserRole role,
        ClubMembership? membership = null)
    {
        if (await userManager.FindByEmailAsync(user.Email!) != null)
            return;

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            Log.Error("AdminSeeder: failed to create {Email} — {Errors}",
                user.Email, string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, role.ToString());

        if (membership != null)
        {
            db.ClubMemberships.Add(membership);
            await db.SaveChangesAsync();
        }

        Log.Information("AdminSeeder: {Role} seeded → {Email}", role, user.Email);
    }
}
