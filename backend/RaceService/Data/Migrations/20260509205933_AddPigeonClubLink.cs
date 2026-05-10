using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRC.RaceService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPigeonClubLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FancierId",
                table: "RaceResults",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FancierName",
                table: "RaceResults",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClubId",
                table: "Pigeons",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClubName",
                table: "Pigeons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Fanciers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClubId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClubName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FederationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FederationName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LinkedUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkedUserEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fanciers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fanciers_ClubId",
                table: "Fanciers",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Fanciers_FederationId",
                table: "Fanciers",
                column: "FederationId");

            migrationBuilder.CreateIndex(
                name: "IX_Fanciers_Name_ClubId",
                table: "Fanciers",
                columns: new[] { "Name", "ClubId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fanciers");

            migrationBuilder.DropColumn(
                name: "FancierId",
                table: "RaceResults");

            migrationBuilder.DropColumn(
                name: "FancierName",
                table: "RaceResults");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "Pigeons");

            migrationBuilder.DropColumn(
                name: "ClubName",
                table: "Pigeons");
        }
    }
}
