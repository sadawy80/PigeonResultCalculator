using Microsoft.EntityFrameworkCore;
using PRC.AdminService.Models;

namespace PRC.AdminService.Data;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // AuditEvent: immutable audit trail — no soft-delete query filter
        mb.Entity<AuditEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Action);
            e.HasIndex(x => x.EntityType);
            e.HasIndex(x => x.Severity);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.TriggeredByUserId);
        });
    }
}
