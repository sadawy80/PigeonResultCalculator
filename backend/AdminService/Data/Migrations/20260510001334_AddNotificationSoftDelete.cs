using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRC.AdminService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AdminNotifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AdminNotifications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotifications_IsDeleted",
                table: "AdminNotifications",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdminNotifications_IsDeleted",
                table: "AdminNotifications");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AdminNotifications");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AdminNotifications");
        }
    }
}
