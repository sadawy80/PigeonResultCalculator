using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRC.BackupService.Migrations;

public partial class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Backups",
            columns: table => new
            {
                Id               = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DatabaseName     = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ObjectKey        = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                SizeBytes        = table.Column<long>(type: "bigint", nullable: false),
                CreatedAt        = table.Column<DateTime>(type: "datetime2", nullable: false),
                CompletedAt      = table.Column<DateTime>(type: "datetime2", nullable: true),
                Status           = table.Column<int>(type: "int", nullable: false),
                ErrorMessage     = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                UploadedToMinIO  = table.Column<bool>(type: "bit", nullable: false),
                UploadedToPCloud = table.Column<bool>(type: "bit", nullable: false),
                TriggeredBy      = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Backups", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Backups");
    }
}
