using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRC.RaceService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRaceProgrammeLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProgrammeId",
                table: "Races",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProgrammeName",
                table: "Races",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProgrammeId",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "ProgrammeName",
                table: "Races");
        }
    }
}
