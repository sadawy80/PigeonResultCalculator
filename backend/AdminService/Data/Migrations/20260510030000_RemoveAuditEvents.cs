using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRC.AdminService.Data.Migrations
{
    public partial class RemoveAuditEvents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AuditEvents");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id                = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action            = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityType        = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityId          = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Severity          = table.Column<int>(type: "int", nullable: false),
                    Details           = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TriggeredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TriggeredByName   = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId     = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceName       = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress         = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country           = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt         = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt         = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted         = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt         = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy         = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy         = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });
        }
    }
}
