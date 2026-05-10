using Microsoft.EntityFrameworkCore;
using PRC.BackupService.Models;

namespace PRC.BackupService.Data;

public class BackupDbContext(DbContextOptions<BackupDbContext> options) : DbContext(options)
{
    public DbSet<BackupEntry> Backups => Set<BackupEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BackupEntry>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.DatabaseName).HasMaxLength(100).IsRequired();
            e.Property(b => b.ObjectKey).HasMaxLength(500).IsRequired();
            e.Property(b => b.ErrorMessage).HasMaxLength(2000);
            e.Property(b => b.TriggeredBy).HasMaxLength(200).IsRequired();
        });
    }
}
