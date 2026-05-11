using Microsoft.EntityFrameworkCore;
using PRC.AdminService.Models;  // AdminNotification

namespace PRC.AdminService.Data;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    public DbSet<AdminNotification> AdminNotifications => Set<AdminNotification>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

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

        mb.Entity<ContactMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SenderName).HasMaxLength(200);
            e.Property(x => x.SenderEmail).HasMaxLength(200);
            e.Property(x => x.SenderPhone).HasMaxLength(50);
            e.Property(x => x.Subject).HasMaxLength(300);
            e.Property(x => x.SenderRole).HasMaxLength(40);
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.SenderEmail);
            e.HasQueryFilter(x => !x.IsDeleted);
        });
    }
}
