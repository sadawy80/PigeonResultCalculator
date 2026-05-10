using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.ClubService.Models;
using PRC.Common.Tenancy;

namespace PRC.ClubService.Data;

public class ClubDbContext : DbContext
{
    private readonly Guid? _tenantId;

    public ClubDbContext(DbContextOptions<ClubDbContext> opts, ITenantContext tenant) : base(opts)
        => _tenantId = tenant.TenantId;

    public ClubDbContext(DbContextOptions<ClubDbContext> opts) : base(opts) { }

    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<ClubMembership> ClubMemberships => Set<ClubMembership>();
    public DbSet<ClubPage> ClubPages => Set<ClubPage>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PigeonLink> PigeonLinks => Set<PigeonLink>();
    public DbSet<ClubProgramme> ClubProgrammes => Set<ClubProgramme>();
    public DbSet<ProgrammeRace> ProgrammeRaces => Set<ProgrammeRace>();
    public DbSet<BestLoftResult> BestLoftResults => Set<BestLoftResult>();
    public DbSet<AcePigeonResult> AcePigeonResults => Set<AcePigeonResult>();
    public DbSet<SuperAcePigeonResult> SuperAcePigeonResults => Set<SuperAcePigeonResult>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Club>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted && (_tenantId == null || x.FederationId == _tenantId));
            e.HasIndex(x => new { x.FederationId, x.Code });
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.HasOne(x => x.ClubPage)
             .WithOne(x => x.Club)
             .HasForeignKey<ClubPage>(x => x.ClubId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ClubMembership>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasIndex(x => new { x.ClubId, x.UserId }).IsUnique();
            e.HasOne(x => x.Club)
             .WithMany(x => x.Memberships)
             .HasForeignKey(x => x.ClubId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ClubPage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<Invitation>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasIndex(x => x.Token).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.HasOne(x => x.Club)
             .WithMany(x => x.Invitations)
             .HasForeignKey(x => x.ClubId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Notification>(e =>
        {
            e.HasKey(x => x.Id);
        });

        builder.Entity<PigeonLink>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasIndex(x => new { x.MembershipId, x.RingNumber }).IsUnique();
            e.HasOne(x => x.Membership)
             .WithMany(x => x.PigeonLinks)
             .HasForeignKey(x => x.MembershipId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ClubProgramme>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted && (_tenantId == null || x.FederationId == _tenantId));
            e.HasOne(x => x.Club)
             .WithMany(x => x.Programmes)
             .HasForeignKey(x => x.ClubId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ProgrammeRace>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasIndex(x => new { x.ProgrammeId, x.RaceId }).IsUnique();
            e.HasOne(x => x.Programme)
             .WithMany(x => x.ProgrammeRaces)
             .HasForeignKey(x => x.ProgrammeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BestLoftResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasOne(x => x.Programme)
             .WithMany(x => x.BestLoftResults)
             .HasForeignKey(x => x.ProgrammeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AcePigeonResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasOne(x => x.Programme)
             .WithMany(x => x.AcePigeonResults)
             .HasForeignKey(x => x.ProgrammeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SuperAcePigeonResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasOne(x => x.Programme)
             .WithMany(x => x.SuperAcePigeonResults)
             .HasForeignKey(x => x.ProgrammeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.AddOutboxMessageEntity();
        builder.AddOutboxStateEntity();
        builder.AddInboxStateEntity();
    }
}
