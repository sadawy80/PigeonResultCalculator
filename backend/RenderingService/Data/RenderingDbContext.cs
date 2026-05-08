using Microsoft.EntityFrameworkCore;
using PRC.RenderingService.Models;

namespace PRC.RenderingService.Data;

public class RenderingDbContext : DbContext
{
    public RenderingDbContext(DbContextOptions<RenderingDbContext> options) : base(options) { }

    public DbSet<PrintTemplate> PrintTemplates => Set<PrintTemplate>();
    public DbSet<PrintJob> PrintJobs => Set<PrintJob>();
    public DbSet<PageTemplate> PageTemplates => Set<PageTemplate>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<PrintTemplate>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<PrintJob>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<PageTemplate>().HasQueryFilter(e => !e.IsDeleted);

        b.Entity<PrintTemplate>().HasIndex(t => t.Category);
        b.Entity<PrintTemplate>().HasIndex(t => t.IsSystem);
        b.Entity<PrintJob>().HasIndex(j => j.ClubId);
        b.Entity<PrintJob>().HasIndex(j => j.Status);
        b.Entity<PageTemplate>().HasIndex(t => t.Category);
        b.Entity<PageTemplate>().Property(t => t.Category).HasMaxLength(50);
    }
}
