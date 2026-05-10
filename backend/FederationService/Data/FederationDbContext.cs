using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.FederationService.Models;
using PRC.Common.Tenancy;

namespace PRC.FederationService.Data;

public class FederationDbContext : DbContext
{
    private readonly Guid? _tenantId;

    public FederationDbContext(DbContextOptions<FederationDbContext> opts, ITenantContext tenant) : base(opts)
        => _tenantId = tenant.TenantId;

    public FederationDbContext(DbContextOptions<FederationDbContext> opts) : base(opts) { }

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
            e.HasQueryFilter(x => !x.IsDeleted && (_tenantId == null || x.Id == _tenantId));
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
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<FederationResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted && (_tenantId == null || x.FederationId == _tenantId));
            e.HasOne(x => x.Federation)
             .WithMany(x => x.FederationResults)
             .HasForeignKey(x => x.FederationId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FederationResultRace>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasOne(x => x.FederationResult)
             .WithMany(x => x.IncludedRaces)
             .HasForeignKey(x => x.FederationResultId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<FederationResultEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted);
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

        builder.AddOutboxMessageEntity();
        builder.AddOutboxStateEntity();
        builder.AddInboxStateEntity();
    }
}
