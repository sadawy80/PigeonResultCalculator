using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRC.ClubService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFederationProgrammeSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubProgrammes_Clubs_ClubId",
                table: "ClubProgrammes");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClubId",
                table: "ClubProgrammes",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "FederationId",
                table: "ClubProgrammes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FederationName",
                table: "ClubProgrammes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClubProgrammes_Clubs_ClubId",
                table: "ClubProgrammes",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubProgrammes_Clubs_ClubId",
                table: "ClubProgrammes");

            migrationBuilder.DropColumn(
                name: "FederationId",
                table: "ClubProgrammes");

            migrationBuilder.DropColumn(
                name: "FederationName",
                table: "ClubProgrammes");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClubId",
                table: "ClubProgrammes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClubProgrammes_Clubs_ClubId",
                table: "ClubProgrammes",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
