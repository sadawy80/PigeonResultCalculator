using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRC.AuditService.Data.Migrations;

[Migration("20260510020000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id              = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Action          = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                EntityType      = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                EntityId        = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Severity        = table.Column<int>(type: "int", nullable: false),
                Details         = table.Column<string>(type: "nvarchar(max)", nullable: true),
                TriggeredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                TriggeredByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                CorrelationId   = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                ServiceName     = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                IpAddress       = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                Country         = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                CreatedAt       = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.Id);
            });

        migrationBuilder.CreateIndex(name: "IX_AuditLogs_Action",            table: "AuditLogs", column: "Action");
        migrationBuilder.CreateIndex(name: "IX_AuditLogs_EntityType",        table: "AuditLogs", column: "EntityType");
        migrationBuilder.CreateIndex(name: "IX_AuditLogs_Severity",          table: "AuditLogs", column: "Severity");
        migrationBuilder.CreateIndex(name: "IX_AuditLogs_CreatedAt",         table: "AuditLogs", column: "CreatedAt");
        migrationBuilder.CreateIndex(name: "IX_AuditLogs_TriggeredByUserId", table: "AuditLogs", column: "TriggeredByUserId");
        migrationBuilder.CreateIndex(name: "IX_AuditLogs_ServiceName",       table: "AuditLogs", column: "ServiceName");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AuditLogs");
    }
}
