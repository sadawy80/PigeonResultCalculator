using Microsoft.EntityFrameworkCore;
using PRC.IntegrationService.Models;

namespace PRC.IntegrationService.Data;

public class IntegrationDbContext : DbContext
{
    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options) : base(options) { }

    public DbSet<ExternalLink> ExternalLinks => Set<ExternalLink>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<ExternalLink>(e =>
        {
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasIndex(x => x.LinkToken).IsUnique();
            e.HasIndex(x => x.AccessToken);
            e.HasIndex(x => new { x.ClubId, x.Status });
            e.HasIndex(x => new { x.ExternalPlatformName, x.ExternalLoftId, x.ClubId });
        });
    }
}
