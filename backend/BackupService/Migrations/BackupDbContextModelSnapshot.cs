using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PRC.BackupService.Data;

#nullable disable

[assembly: DbContext(typeof(BackupDbContext))]

namespace PRC.BackupService.Migrations;

[DbContext(typeof(BackupDbContext))]
partial class BackupDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "9.0.4")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("PRC.BackupService.Models.BackupEntry", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uniqueidentifier");
            b.Property<string>("DatabaseName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<string>("ObjectKey").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<long>("SizeBytes").HasColumnType("bigint");
            b.Property<DateTime>("CreatedAt").HasColumnType("datetime2");
            b.Property<DateTime?>("CompletedAt").HasColumnType("datetime2");
            b.Property<int>("Status").HasColumnType("int");
            b.Property<string>("ErrorMessage").HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            b.Property<bool>("UploadedToMinIO").HasColumnType("bit");
            b.Property<bool>("UploadedToPCloud").HasColumnType("bit");
            b.Property<string>("TriggeredBy").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            b.HasKey("Id");
            b.ToTable("Backups");
        });
#pragma warning restore 612, 618
    }
}
