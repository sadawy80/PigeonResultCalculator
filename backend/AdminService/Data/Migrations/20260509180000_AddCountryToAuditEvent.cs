using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRC.AdminService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryToAuditEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "AuditEvents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                table: "AuditEvents");
        }
    }
}
