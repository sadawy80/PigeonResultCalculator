using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.RaceService.Models;

namespace PRC.RaceService.Data;

public class RaceDbContext : DbContext
{
    public RaceDbContext(DbContextOptions<RaceDbContext> options) : base(options) { }

    public DbSet<Race> Races => Set<Race>();
    public DbSet<RaceCategory> RaceCategories => Set<RaceCategory>();
    public DbSet<RaceResult> RaceResults => Set<RaceResult>();
    public DbSet<DataIngestionLog> DataIngestionLogs => Set<DataIngestionLog>();
    public DbSet<Pigeon> Pigeons => Set<Pigeon>();
    public DbSet<PigeonLink> PigeonLinks => Set<PigeonLink>();
    public DbSet<Fancier> Fanciers => Set<Fancier>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Soft-delete global filters
        b.Entity<Race>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<RaceResult>().HasQueryFilter(e => !e.IsDeleted);
        b.Entity<Pigeon>().HasQueryFilter(e => !e.IsDeleted);

        b.Entity<Race>().HasIndex(r => r.ClubId);
        b.Entity<Race>().HasIndex(r => r.Status);

        b.Entity<RaceResult>().HasIndex(r => r.RaceId);
        b.Entity<RaceResult>().HasIndex(r => r.RingNumber);
        b.Entity<RaceResult>().HasIndex(r => r.UserId);

        b.Entity<RaceCategory>()
            .HasOne(c => c.Race).WithMany(r => r.Categories)
            .HasForeignKey(c => c.RaceId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<RaceResult>()
            .HasOne(r => r.Race).WithMany(r => r.Results)
            .HasForeignKey(r => r.RaceId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<RaceResult>()
            .HasOne(r => r.Category).WithMany(c => c.Results)
            .HasForeignKey(r => r.CategoryId).OnDelete(DeleteBehavior.SetNull);

        b.Entity<DataIngestionLog>()
            .HasOne(l => l.Race).WithMany(r => r.IngestionLogs)
            .HasForeignKey(l => l.RaceId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Fancier>().HasIndex(f => new { f.Name, f.ClubId }).IsUnique();
        b.Entity<Fancier>().HasIndex(f => f.ClubId);
        b.Entity<Fancier>().HasIndex(f => f.FederationId);

        b.Entity<Pigeon>().HasIndex(p => p.RingNumber).IsUnique();

        b.Entity<PigeonLink>().HasIndex(p => new { p.MembershipId, p.RingNumber }).IsUnique();
        b.Entity<PigeonLink>()
            .HasOne(l => l.Pigeon).WithMany(p => p.Links)
            .HasForeignKey(l => l.PigeonId).OnDelete(DeleteBehavior.SetNull);

        b.AddOutboxMessageEntity();
        b.AddOutboxStateEntity();
        b.AddInboxStateEntity();
    }
}
