using Microsoft.EntityFrameworkCore;
using PRC.FederationService.Models;

namespace PRC.FederationService.Data;

public class FederationDbContext : DbContext
{
    public FederationDbContext(DbContextOptions<FederationDbContext> options) : base(options) { }

    public DbSet<Federation> Federations => Set<Federation>();
    public DbSet<FederationPage> FederationPages => Set<FederationPage>();
    public DbSet<FederationResult> FederationResults => Set<FederationResult>();
    public DbSet<FederationResultRace> FederationResultRaces => Set<FederationResultRace>();
    public DbSet<FederationResultEntry> FederationResultEntries => Set<FederationResultEntry>();
    public DbSet<RaceSnapshotCache> RaceSnapshotCaches => Set<RaceSnapshotCache>();
    public DbSet<RaceResultSnapshotCache> RaceResultSnapshotCaches => Set<RaceResultSnapshotCache>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Federation>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Code).HasMaxLength(2).IsRequired();
            e.HasOne(x => x.FederationPage)
             .WithOne(x => x.Federation)
             .HasForeignKey<FederationPage>(x => x.FederationId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<FederationPage>(e =>
        {
            e.HasKey(x => x.Id);
        });

        builder.Entity<FederationResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasOne(x => x.Federation)
             .WithMany(x => x.FederationResults)
             .HasForeignKey(x => x.FederationId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FederationResultRace>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.FederationResult)
             .WithMany(x => x.IncludedRaces)
             .HasForeignKey(x => x.FederationResultId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<FederationResultEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.FederationResult)
             .WithMany(x => x.Entries)
             .HasForeignKey(x => x.FederationResultId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RaceSnapshotCache>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.RaceId).IsUnique();
        });

        builder.Entity<RaceResultSnapshotCache>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ResultId).IsUnique();
            e.HasOne(x => x.RaceSnapshot)
             .WithMany(x => x.Results)
             .HasForeignKey(x => x.RaceSnapshotCacheId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
