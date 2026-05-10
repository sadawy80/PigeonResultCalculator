using Microsoft.EntityFrameworkCore;
using PRC.AuditService.Models;

namespace PRC.AuditService.Data;

public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(256).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(256).IsRequired();
            e.Property(x => x.ServiceName).HasMaxLength(256).IsRequired();
            e.Property(x => x.TriggeredByName).HasMaxLength(256);
            e.Property(x => x.CorrelationId).HasMaxLength(128);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.Country).HasMaxLength(128);
            e.HasIndex(x => x.Action);
            e.HasIndex(x => x.EntityType);
            e.HasIndex(x => x.Severity);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.TriggeredByUserId);
            e.HasIndex(x => x.ServiceName);
        });
    }
}
