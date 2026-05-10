using Microsoft.EntityFrameworkCore;
using PRC.AdminService.Models;

namespace PRC.AdminService.Data;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    public DbSet<AuditEvent>        AuditEvents        => Set<AuditEvent>();
    public DbSet<AdminNotification> AdminNotifications => Set<AdminNotification>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<AuditEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Action);
            e.HasIndex(x => x.EntityType);
            e.HasIndex(x => x.Severity);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.TriggeredByUserId);
        });

        mb.Entity<AdminNotification>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.IsRead);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.Type);
            e.HasIndex(x => x.IsDeleted);
            e.HasQueryFilter(x => !x.IsDeleted);
        });
    }
}
