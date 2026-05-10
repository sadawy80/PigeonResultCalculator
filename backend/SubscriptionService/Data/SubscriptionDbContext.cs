using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.SubscriptionService.Models;
using PRC.Common.Tenancy;

namespace PRC.SubscriptionService.Data;

public class SubscriptionDbContext : DbContext
{
    private readonly Guid? _tenantId;

    public SubscriptionDbContext(DbContextOptions<SubscriptionDbContext> opts, ITenantContext tenant) : base(opts)
        => _tenantId = tenant.TenantId;

    public SubscriptionDbContext(DbContextOptions<SubscriptionDbContext> opts) : base(opts) { }

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<FederationSubscription> FederationSubscriptions => Set<FederationSubscription>();
    public DbSet<ClubSubscription> ClubSubscriptions => Set<ClubSubscription>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<SubscriptionPlan>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.HasIndex(x => new { x.Type, x.BillingCycle });
        });

        mb.Entity<FederationSubscription>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted && (_tenantId == null || x.FederationId == _tenantId));
            e.HasIndex(x => x.FederationId);
            e.HasIndex(x => x.Status);
            e.Property(x => x.AmountPaid).HasPrecision(18, 2);
            e.HasOne(x => x.Plan)
             .WithMany(p => p.FederationSubscriptions)
             .HasForeignKey(x => x.PlanId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<ClubSubscription>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasIndex(x => x.ClubId);
            e.HasIndex(x => x.Status);
            e.Property(x => x.AmountPaid).HasPrecision(18, 2);
            e.HasOne(x => x.Plan)
             .WithMany(p => p.ClubSubscriptions)
             .HasForeignKey(x => x.PlanId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        mb.AddOutboxMessageEntity();
        mb.AddOutboxStateEntity();
        mb.AddInboxStateEntity();
    }
}
