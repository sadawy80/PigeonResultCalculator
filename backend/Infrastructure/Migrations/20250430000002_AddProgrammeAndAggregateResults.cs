using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PigeonRacing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProgrammeAndAggregateResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── ClubProgramme ──────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ClubProgrammes",
                columns: t => new
                {
                    Id                         = t.Column<Guid>(nullable: false),
                    ClubId                     = t.Column<Guid>(nullable: false),
                    Name                       = t.Column<string>(maxLength: 200, nullable: false),
                    Description                = t.Column<string>(maxLength: 1000, nullable: true),
                    Year                       = t.Column<int>(nullable: false),
                    StartDate                  = t.Column<DateTime>(nullable: true),
                    EndDate                    = t.Column<DateTime>(nullable: true),
                    Status                     = t.Column<int>(nullable: false, defaultValue: 1),

                    // Scoring config
                    ScoringMethod              = t.Column<int>(nullable: false, defaultValue: 1),
                    PointsForFirst             = t.Column<int>(nullable: false, defaultValue: 10),
                    MaxPointPositions          = t.Column<int>(nullable: false, defaultValue: 0),

                    // Best Loft config
                    BestLoftPigeonsPerRace     = t.Column<int>(nullable: false, defaultValue: 0),
                    BestLoftMinRaces           = t.Column<int>(nullable: false, defaultValue: 1),

                    // Ace Pigeon config
                    AcePigeonMinRaces          = t.Column<int>(nullable: false, defaultValue: 3),

                    // Super Ace config
                    SuperAceQualification      = t.Column<int>(nullable: false, defaultValue: 1),
                    SuperAceMinRaceCount       = t.Column<int>(nullable: false, defaultValue: 0),
                    SuperAceMinRacePercentage  = t.Column<double>(nullable: false, defaultValue: 100.0),

                    // Publication
                    PublishedAt                = t.Column<DateTime>(nullable: true),
                    PublishedByUserId          = t.Column<Guid>(nullable: true),

                    // Audit
                    CreatedAt                  = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt                  = t.Column<DateTime>(nullable: true),
                    IsDeleted                  = t.Column<bool>(nullable: false, defaultValue: false),
                    DeletedAt                  = t.Column<DateTime>(nullable: true),
                    CreatedBy                  = t.Column<Guid>(nullable: true)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_ClubProgrammes", x => x.Id);
                    t.ForeignKey("FK_ClubProgrammes_Clubs_ClubId",
                        x => x.ClubId, "Clubs", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_ClubProgrammes_AspNetUsers_PublishedByUserId",
                        x => x.PublishedByUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex("IX_ClubProgrammes_ClubId_Year", "ClubProgrammes",
                new[] { "ClubId", "Year" });

            // ── ProgrammeRace ──────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ProgrammeRaces",
                columns: t => new
                {
                    Id          = t.Column<Guid>(nullable: false),
                    ProgrammeId = t.Column<Guid>(nullable: false),
                    RaceId      = t.Column<Guid>(nullable: false),
                    ScoreWeight = t.Column<double>(nullable: false, defaultValue: 1.0),
                    SortOrder   = t.Column<int>(nullable: false, defaultValue: 0),
                    CreatedAt   = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt   = t.Column<DateTime>(nullable: true),
                    IsDeleted   = t.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_ProgrammeRaces", x => x.Id);
                    t.ForeignKey("FK_ProgrammeRaces_ClubProgrammes_ProgrammeId",
                        x => x.ProgrammeId, "ClubProgrammes", "Id", onDelete: ReferentialAction.Cascade);
                    t.ForeignKey("FK_ProgrammeRaces_Races_RaceId",
                        x => x.RaceId, "Races", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex("IX_ProgrammeRaces_ProgrammeId_RaceId", "ProgrammeRaces",
                new[] { "ProgrammeId", "RaceId" }, unique: true);

            // ── BestLoftResult ─────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "BestLoftResults",
                columns: t => new
                {
                    Id                      = t.Column<Guid>(nullable: false),
                    ProgrammeId             = t.Column<Guid>(nullable: false),
                    UserId                  = t.Column<Guid>(nullable: true),
                    FancierName             = t.Column<string>(maxLength: 200, nullable: false),
                    LoftRank                = t.Column<int>(nullable: false),
                    TotalScore              = t.Column<decimal>(type: "decimal(12,4)", nullable: false),
                    AverageScore            = t.Column<decimal>(type: "decimal(12,4)", nullable: false),
                    RacesEntered            = t.Column<int>(nullable: false),
                    PigeonsEntered          = t.Column<int>(nullable: false),
                    BestSingleVelocityMperMin = t.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    AverageVelocityMperMin  = t.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    RaceBreakdownJson       = t.Column<string>(nullable: true),
                    CreatedAt               = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt               = t.Column<DateTime>(nullable: true),
                    IsDeleted               = t.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_BestLoftResults", x => x.Id);
                    t.ForeignKey("FK_BestLoftResults_ClubProgrammes_ProgrammeId",
                        x => x.ProgrammeId, "ClubProgrammes", "Id", onDelete: ReferentialAction.Cascade);
                    t.ForeignKey("FK_BestLoftResults_AspNetUsers_UserId",
                        x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex("IX_BestLoftResults_ProgrammeId_LoftRank", "BestLoftResults",
                new[] { "ProgrammeId", "LoftRank" });

            // ── AcePigeonResult ────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "AcePigeonResults",
                columns: t => new
                {
                    Id                      = t.Column<Guid>(nullable: false),
                    ProgrammeId             = t.Column<Guid>(nullable: false),
                    UserId                  = t.Column<Guid>(nullable: true),
                    PigeonId                = t.Column<Guid>(nullable: true),
                    RingNumber              = t.Column<string>(maxLength: 50, nullable: false),
                    PigeonName              = t.Column<string>(maxLength: 100, nullable: true),
                    PigeonSex               = t.Column<string>(maxLength: 1, nullable: true),
                    PigeonYearOfBirth       = t.Column<int>(nullable: true),
                    FancierName             = t.Column<string>(maxLength: 200, nullable: false),
                    AceRank                 = t.Column<int>(nullable: false),
                    TotalScore              = t.Column<decimal>(type: "decimal(12,4)", nullable: false),
                    AverageScore            = t.Column<decimal>(type: "decimal(12,4)", nullable: false),
                    RacesEntered            = t.Column<int>(nullable: false),
                    RacesInProgramme        = t.Column<int>(nullable: false),
                    ParticipationRate       = t.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    BestVelocityMperMin     = t.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    AverageVelocityMperMin  = t.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    BestClubRank            = t.Column<int>(nullable: false),
                    RaceBreakdownJson       = t.Column<string>(nullable: true),
                    CreatedAt               = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt               = t.Column<DateTime>(nullable: true),
                    IsDeleted               = t.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_AcePigeonResults", x => x.Id);
                    t.ForeignKey("FK_AcePigeonResults_ClubProgrammes_ProgrammeId",
                        x => x.ProgrammeId, "ClubProgrammes", "Id", onDelete: ReferentialAction.Cascade);
                    t.ForeignKey("FK_AcePigeonResults_AspNetUsers_UserId",
                        x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.SetNull);
                    t.ForeignKey("FK_AcePigeonResults_Pigeons_PigeonId",
                        x => x.PigeonId, "Pigeons", "Id", onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex("IX_AcePigeonResults_ProgrammeId_AceRank", "AcePigeonResults",
                new[] { "ProgrammeId", "AceRank" });
            migrationBuilder.CreateIndex("IX_AcePigeonResults_ProgrammeId_RingNumber", "AcePigeonResults",
                new[] { "ProgrammeId", "RingNumber" });

            // ── SuperAcePigeonResult ───────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "SuperAcePigeonResults",
                columns: t => new
                {
                    Id                      = t.Column<Guid>(nullable: false),
                    ProgrammeId             = t.Column<Guid>(nullable: false),
                    UserId                  = t.Column<Guid>(nullable: true),
                    PigeonId                = t.Column<Guid>(nullable: true),
                    RingNumber              = t.Column<string>(maxLength: 50, nullable: false),
                    PigeonName              = t.Column<string>(maxLength: 100, nullable: true),
                    PigeonSex               = t.Column<string>(maxLength: 1, nullable: true),
                    PigeonYearOfBirth       = t.Column<int>(nullable: true),
                    FancierName             = t.Column<string>(maxLength: 200, nullable: false),
                    SuperAceRank            = t.Column<int>(nullable: false),
                    TotalScore              = t.Column<decimal>(type: "decimal(12,4)", nullable: false),
                    AverageScore            = t.Column<decimal>(type: "decimal(12,4)", nullable: false),
                    RacesEntered            = t.Column<int>(nullable: false),
                    RacesInProgramme        = t.Column<int>(nullable: false),
                    ParticipationRate       = t.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    BestVelocityMperMin     = t.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    AverageVelocityMperMin  = t.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    BestClubRank            = t.Column<int>(nullable: false),
                    AcePigeonResultId       = t.Column<Guid>(nullable: true),
                    RaceBreakdownJson       = t.Column<string>(nullable: true),
                    CreatedAt               = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt               = t.Column<DateTime>(nullable: true),
                    IsDeleted               = t.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_SuperAcePigeonResults", x => x.Id);
                    t.ForeignKey("FK_SuperAcePigeonResults_ClubProgrammes_ProgrammeId",
                        x => x.ProgrammeId, "ClubProgrammes", "Id", onDelete: ReferentialAction.Cascade);
                    t.ForeignKey("FK_SuperAcePigeonResults_AspNetUsers_UserId",
                        x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.SetNull);
                    t.ForeignKey("FK_SuperAcePigeonResults_Pigeons_PigeonId",
                        x => x.PigeonId, "Pigeons", "Id", onDelete: ReferentialAction.SetNull);
                    t.ForeignKey("FK_SuperAcePigeonResults_AcePigeonResults_AcePigeonResultId",
                        x => x.AcePigeonResultId, "AcePigeonResults", "Id", onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex("IX_SuperAcePigeonResults_ProgrammeId_SuperAceRank", "SuperAcePigeonResults",
                new[] { "ProgrammeId", "SuperAceRank" });
            migrationBuilder.CreateIndex("IX_SuperAcePigeonResults_ProgrammeId_RingNumber", "SuperAcePigeonResults",
                new[] { "ProgrammeId", "RingNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("SuperAcePigeonResults");
            migrationBuilder.DropTable("AcePigeonResults");
            migrationBuilder.DropTable("BestLoftResults");
            migrationBuilder.DropTable("ProgrammeRaces");
            migrationBuilder.DropTable("ClubProgrammes");
        }
    }
}
