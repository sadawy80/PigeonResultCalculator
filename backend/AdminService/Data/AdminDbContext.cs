using Microsoft.EntityFrameworkCore;
using PRC.AdminService.Models;  // AdminNotification

namespace PRC.AdminService.Data;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    public DbSet<AdminNotification> AdminNotifications => Set<AdminNotification>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

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
