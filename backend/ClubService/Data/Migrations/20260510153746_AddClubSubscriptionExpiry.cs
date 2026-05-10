using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRC.ClubService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClubSubscriptionExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionExpiresAt",
                table: "Clubs",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubscriptionExpiresAt",
                table: "Clubs");
        }
    }
}
