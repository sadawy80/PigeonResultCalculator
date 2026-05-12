namespace PRC.RenderingService.Data;

/// <summary>
/// Template seeding intentionally does nothing — the previous library was removed
/// and a new set of templates will be re-introduced later. The method is kept so
/// startup callers compile; restore seeding here when the new templates land.
/// </summary>
public static class TemplateSeeder
{
    public static Task SeedAsync(RenderingDbContext db) => Task.CompletedTask;
}
