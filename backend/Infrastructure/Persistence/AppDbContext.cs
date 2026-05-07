using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PigeonRacing.Domain.Entities;

namespace PigeonRacing.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Core
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<ClubMembership> ClubMemberships => Set<ClubMembership>();

    // Racing
    public DbSet<Race> Races => Set<Race>();
    public DbSet<RaceCategory> RaceCategories => Set<RaceCategory>();
    public DbSet<RaceResult> RaceResults => Set<RaceResult>();
    public DbSet<DataIngestionLog> DataIngestionLogs => Set<DataIngestionLog>();

    // Pigeons
    public DbSet<Pigeon> Pigeons => Set<Pigeon>();
    public DbSet<PigeonLink> PigeonLinks => Set<PigeonLink>();

    // Country results
    public DbSet<CountryResult> CountryResults => Set<CountryResult>();
    public DbSet<CountryResultRace> CountryResultRaces => Set<CountryResultRace>();
    public DbSet<CountryResultEntry> CountryResultEntries => Set<CountryResultEntry>();

    // Subscriptions
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<CountrySubscription> CountrySubscriptions => Set<CountrySubscription>();
    public DbSet<ClubSubscription> ClubSubscriptions => Set<ClubSubscription>();

    // Supporting
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Pages
    public DbSet<ClubPage> ClubPages => Set<ClubPage>();
    public DbSet<CountryPage> CountryPages => Set<CountryPage>();
    public DbSet<PageTemplate> PageTemplates => Set<PageTemplate>();

    // Events
    public DbSet<DomainEvent> DomainEvents => Set<DomainEvent>();

    // Print templates & jobs
    public DbSet<PrintTemplate> PrintTemplates => Set<PrintTemplate>();
    public DbSet<PrintJob> PrintJobs => Set<PrintJob>();

    // External platform integrations
    public DbSet<ExternalLink> ExternalLinks => Set<ExternalLink>();
    public DbSet<ProgrammeRace> ProgrammeRaces => Set<ProgrammeRace>();
    public DbSet<BestLoftResult> BestLoftResults => Set<BestLoftResult>();
    public DbSet<AcePigeonResult> AcePigeonResults => Set<AcePigeonResult>();
    public DbSet<SuperAcePigeonResult> SuperAcePigeonResults => Set<SuperAcePigeonResult>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── Rename Identity tables ───────────────────────────────────────────
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        // ── Global soft-delete filters ───────────────────────────────────────
        builder.Entity<Country>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Club>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Race>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<RaceResult>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Pigeon>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<CountryResult>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<SubscriptionPlan>().HasQueryFilter(e => !e.IsDeleted);

        // ── Country ──────────────────────────────────────────────────────────
        builder.Entity<Country>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Code).HasMaxLength(2).IsRequired();
            e.HasOne(x => x.CountryPage)
             .WithOne(x => x.Country)
             .HasForeignKey<CountryPage>(x => x.CountryId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Club ─────────────────────────────────────────────────────────────
        builder.Entity<Club>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CountryId, x.Code }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.HasOne(x => x.Country)
             .WithMany(x => x.Clubs)
             .HasForeignKey(x => x.CountryId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Subscription)
             .WithOne(x => x.Club)
             .HasForeignKey<ClubSubscription>(x => x.ClubId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ClubMembership ───────────────────────────────────────────────────
        builder.Entity<ClubMembership>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ClubId, x.UserId }).IsUnique();
            e.HasOne(x => x.Club)
             .WithMany(x => x.Memberships)
             .HasForeignKey(x => x.ClubId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User)
             .WithMany(x => x.ClubMemberships)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ApplicationUser ──────────────────────────────────────────────────
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Country)
             .WithMany(x => x.Managers)
             .HasForeignKey(x => x.CountryId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Race ─────────────────────────────────────────────────────────────
        builder.Entity<Race>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ReleaseLocation).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Club)
             .WithMany(x => x.Races)
             .HasForeignKey(x => x.ClubId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── RaceCategory ─────────────────────────────────────────────────────
        builder.Entity<RaceCategory>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Race)
             .WithMany(x => x.Categories)
             .HasForeignKey(x => x.RaceId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── RaceResult ───────────────────────────────────────────────────────
        builder.Entity<RaceResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.RaceId, x.RingNumber });
            e.Property(x => x.RingNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.VelocityMperMin).HasPrecision(10, 4);
            e.Property(x => x.DistanceKm).HasPrecision(10, 4);
            // TimeSpan is not natively supported by SQL Server — store as ticks (long)
            e.Property(x => x.FlightDuration)
             .HasConversion(
                 v => v.HasValue ? (long?)v.Value.Ticks : null,
                 v => v.HasValue ? (TimeSpan?)TimeSpan.FromTicks(v.Value) : null)
             .HasColumnName("FlightDurationTicks");
            // VelocityKmH is a computed property — not mapped to a column
            e.Ignore(x => x.VelocityKmH);
            e.HasOne(x => x.Race)
             .WithMany(x => x.Results)
             .HasForeignKey(x => x.RaceId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category)
             .WithMany(x => x.Results)
             .HasForeignKey(x => x.CategoryId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── DataIngestionLog ─────────────────────────────────────────────────
        builder.Entity<DataIngestionLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Race)
             .WithMany(x => x.IngestionLogs)
             .HasForeignKey(x => x.RaceId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Pigeon ───────────────────────────────────────────────────────────
        builder.Entity<Pigeon>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.RingNumber, x.CountryId }).IsUnique();
            e.Property(x => x.RingNumber).HasMaxLength(50).IsRequired();
        });

        // ── PigeonLink ───────────────────────────────────────────────────────
        builder.Entity<PigeonLink>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MembershipId, x.RingNumber }).IsUnique();
            e.HasOne(x => x.Membership)
             .WithMany(x => x.PigeonLinks)
             .HasForeignKey(x => x.MembershipId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Pigeon)
             .WithMany(x => x.Links)
             .HasForeignKey(x => x.PigeonId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── CountryResult ────────────────────────────────────────────────────
        builder.Entity<CountryResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Country)
             .WithMany(x => x.CountryResults)
             .HasForeignKey(x => x.CountryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CountryResultRace>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CountryResultId, x.RaceId }).IsUnique();
            e.HasOne(x => x.CountryResult)
             .WithMany(x => x.IncludedRaces)
             .HasForeignKey(x => x.CountryResultId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Race)
             .WithMany(x => x.CountryResultRaces)
             .HasForeignKey(x => x.RaceId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CountryResultEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.VelocityMperMin).HasPrecision(10, 4);
            e.Property(x => x.DistanceKm).HasPrecision(10, 4);
            e.HasOne(x => x.CountryResult)
             .WithMany(x => x.Entries)
             .HasForeignKey(x => x.CountryResultId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.RaceResult)
             .WithMany(x => x.CountryResultEntries)
             .HasForeignKey(x => x.RaceResultId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Subscriptions ────────────────────────────────────────────────────
        builder.Entity<SubscriptionPlan>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Price).HasPrecision(10, 2);
        });

        builder.Entity<CountrySubscription>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AmountPaid).HasPrecision(10, 2);
            e.HasOne(x => x.Country)
             .WithMany(x => x.Subscriptions)
             .HasForeignKey(x => x.CountryId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Plan)
             .WithMany(x => x.CountrySubscriptions)
             .HasForeignKey(x => x.PlanId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ClubSubscription>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AmountPaid).HasPrecision(10, 2);
            e.HasOne(x => x.Plan)
             .WithMany(x => x.ClubSubscriptions)
             .HasForeignKey(x => x.PlanId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Invitation ───────────────────────────────────────────────────────
        builder.Entity<Invitation>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Token).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.HasOne(x => x.Club)
             .WithMany()
             .HasForeignKey(x => x.ClubId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Notification ─────────────────────────────────────────────────────
        builder.Entity<Notification>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.User)
             .WithMany(x => x.Notifications)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── RefreshToken ─────────────────────────────────────────────────────
        builder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Token).IsUnique();
            e.HasOne(x => x.User)
             .WithMany(x => x.RefreshTokens)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ClubPage ─────────────────────────────────────────────────────────
        builder.Entity<ClubPage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasOne(x => x.Club)
             .WithMany(x => x.ClubPages)
             .HasForeignKey(x => x.ClubId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── DomainEvent ──────────────────────────────────────────────────────
        builder.Entity<DomainEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.EventType);
            e.HasIndex(x => x.AggregateId);
            e.HasIndex(x => new { x.IsProcessed, x.CreatedAt });
        });

        // ── ClubProgramme ────────────────────────────────────────────────────
        builder.Entity<ClubProgramme>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.ClubId, x.Year });
            e.HasOne(x => x.Club)
             .WithMany()
             .HasForeignKey(x => x.ClubId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ProgrammeRace ────────────────────────────────────────────────────
        builder.Entity<ProgrammeRace>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProgrammeId, x.RaceId }).IsUnique();
            e.Property(x => x.ScoreWeight).HasPrecision(5, 2);
            e.HasOne(x => x.Programme)
             .WithMany(x => x.ProgrammeRaces)
             .HasForeignKey(x => x.ProgrammeId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Race)
             .WithMany()
             .HasForeignKey(x => x.RaceId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── BestLoftResult ───────────────────────────────────────────────────
        builder.Entity<BestLoftResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProgrammeId, x.LoftRank });
            e.HasIndex(x => x.UserId);
            e.Property(x => x.TotalScore).HasPrecision(12, 4);
            e.Property(x => x.AverageScore).HasPrecision(12, 4);
            e.Property(x => x.BestSingleVelocityMperMin).HasPrecision(10, 4);
            e.Property(x => x.AverageVelocityMperMin).HasPrecision(10, 4);
            e.HasOne(x => x.Programme)
             .WithMany(x => x.BestLoftResults)
             .HasForeignKey(x => x.ProgrammeId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── AcePigeonResult ──────────────────────────────────────────────────
        builder.Entity<AcePigeonResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProgrammeId, x.AceRank });
            e.HasIndex(x => new { x.ProgrammeId, x.RingNumber });
            e.Property(x => x.RingNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.TotalScore).HasPrecision(12, 4);
            e.Property(x => x.AverageScore).HasPrecision(12, 4);
            e.Property(x => x.BestVelocityMperMin).HasPrecision(10, 4);
            e.Property(x => x.AverageVelocityMperMin).HasPrecision(10, 4);
            e.Property(x => x.ParticipationRate).HasPrecision(5, 2);
            e.HasOne(x => x.Programme)
             .WithMany(x => x.AcePigeonResults)
             .HasForeignKey(x => x.ProgrammeId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Pigeon)
             .WithMany()
             .HasForeignKey(x => x.PigeonId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── ExternalLink ─────────────────────────────────────────────────────
        builder.Entity<ExternalLink>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ExternalPlatformName).HasMaxLength(100).IsRequired();
            e.Property(x => x.ExternalUserId).HasMaxLength(200).IsRequired();
            e.Property(x => x.ExternalLoftId).HasMaxLength(200).IsRequired();
            e.Property(x => x.ExternalLoftName).HasMaxLength(500);
            e.Property(x => x.CallbackUrl).HasMaxLength(2000).IsRequired();
            e.Property(x => x.LinkToken).HasMaxLength(64).IsRequired();
            e.Property(x => x.AccessToken).HasMaxLength(64);
            e.HasIndex(x => x.LinkToken).IsUnique();
            e.HasIndex(x => x.AccessToken);
            e.HasIndex(x => new { x.UserId, x.ExternalPlatformName, x.Status });
            e.HasIndex(x => new { x.ClubId, x.Status });
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Club)
             .WithMany()
             .HasForeignKey(x => x.ClubId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReviewedBy)
             .WithMany()
             .HasForeignKey(x => x.ReviewedByUserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<SuperAcePigeonResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProgrammeId, x.SuperAceRank });
            e.HasIndex(x => new { x.ProgrammeId, x.RingNumber });
            e.Property(x => x.RingNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.TotalScore).HasPrecision(12, 4);
            e.Property(x => x.AverageScore).HasPrecision(12, 4);
            e.Property(x => x.BestVelocityMperMin).HasPrecision(10, 4);
            e.Property(x => x.AverageVelocityMperMin).HasPrecision(10, 4);
            e.Property(x => x.ParticipationRate).HasPrecision(5, 2);
            e.HasOne(x => x.Programme)
             .WithMany(x => x.SuperAcePigeonResults)
             .HasForeignKey(x => x.ProgrammeId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Pigeon)
             .WithMany()
             .HasForeignKey(x => x.PigeonId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.AcePigeonResult)
             .WithMany()
             .HasForeignKey(x => x.AcePigeonResultId)
             .OnDelete(DeleteBehavior.SetNull);
        });
        // ── PrintTemplate ────────────────────────────────────────────────────
        builder.Entity<PrintTemplate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.HtmlTemplate).IsRequired();
            e.HasIndex(x => new { x.Category, x.IsActive, x.SortOrder });
            e.HasIndex(x => new { x.IsSystem, x.Category });
            e.HasOne(x => x.Club)
             .WithMany()
             .HasForeignKey(x => x.ClubId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PrintJob ─────────────────────────────────────────────────────────
        builder.Entity<PrintJob>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ClubId, x.CreatedAt });
            e.HasIndex(x => x.TemplateId);
            e.HasOne(x => x.Template)
             .WithMany()
             .HasForeignKey(x => x.TemplateId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Club)
             .WithMany()
             .HasForeignKey(x => x.ClubId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.GeneratedByUser)
             .WithMany()
             .HasForeignKey(x => x.GeneratedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
