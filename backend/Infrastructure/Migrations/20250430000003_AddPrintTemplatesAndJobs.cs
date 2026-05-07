using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PigeonRacing.Infrastructure.Migrations
{
    public partial class AddPrintTemplatesAndJobs : Migration
    {
        protected override void Up(MigrationBuilder mb)
        {
            mb.CreateTable("PrintTemplates", t => new
            {
                Id              = t.Column<Guid>(nullable: false),
                Name            = t.Column<string>(maxLength: 200, nullable: false),
                Description     = t.Column<string>(maxLength: 500, nullable: false, defaultValue: ""),
                Category        = t.Column<int>(nullable: false),
                Style           = t.Column<int>(nullable: false, defaultValue: 1),
                PaperSize       = t.Column<int>(nullable: false, defaultValue: 1),
                ColourScheme    = t.Column<int>(nullable: false, defaultValue: 1),
                PrimaryColour   = t.Column<string>(maxLength: 20, nullable: false, defaultValue: "#1E3A5F"),
                SecondaryColour = t.Column<string>(maxLength: 20, nullable: false, defaultValue: "#C9A84C"),
                ThumbnailUrl    = t.Column<string>(maxLength: 500, nullable: false, defaultValue: ""),
                HtmlTemplate    = t.Column<string>(nullable: false),
                VariableSchemaJson = t.Column<string>(nullable: false, defaultValue: "{}"),
                MaxRows         = t.Column<int>(nullable: false, defaultValue: 0),
                IsMultiPage     = t.Column<bool>(nullable: false, defaultValue: false),
                SortOrder       = t.Column<int>(nullable: false, defaultValue: 0),
                IsActive        = t.Column<bool>(nullable: false, defaultValue: true),
                IsSystem        = t.Column<bool>(nullable: false, defaultValue: true),
                ClubId          = t.Column<Guid>(nullable: true),
                CreatedAt       = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt       = t.Column<DateTime>(nullable: true),
                IsDeleted       = t.Column<bool>(nullable: false, defaultValue: false),
                CreatedBy       = t.Column<Guid>(nullable: true)
            }, constraints: t =>
            {
                t.PrimaryKey("PK_PrintTemplates", x => x.Id);
                t.ForeignKey("FK_PrintTemplates_Clubs_ClubId",
                    x => x.ClubId, "Clubs", "Id", onDelete: ReferentialAction.Cascade);
            });

            mb.CreateIndex("IX_PrintTemplates_Category_IsActive_SortOrder", "PrintTemplates",
                new[] { "Category", "IsActive", "SortOrder" });
            mb.CreateIndex("IX_PrintTemplates_IsSystem_Category", "PrintTemplates",
                new[] { "IsSystem", "Category" });

            mb.CreateTable("PrintJobs", t => new
            {
                Id                  = t.Column<Guid>(nullable: false),
                TemplateId          = t.Column<Guid>(nullable: false),
                ClubId              = t.Column<Guid>(nullable: false),
                Category            = t.Column<int>(nullable: false),
                Status              = t.Column<int>(nullable: false, defaultValue: 1),
                DataPayloadJson     = t.Column<string>(nullable: false, defaultValue: "{}"),
                PdfUrl              = t.Column<string>(maxLength: 1000, nullable: true),
                FileSizeBytes       = t.Column<long>(nullable: true),
                CompletedAt         = t.Column<DateTime>(nullable: true),
                ErrorMessage        = t.Column<string>(maxLength: 2000, nullable: true),
                GeneratedByUserId   = t.Column<Guid>(nullable: false),
                RaceId              = t.Column<Guid>(nullable: true),
                ProgrammeId         = t.Column<Guid>(nullable: true),
                RaceResultId        = t.Column<Guid>(nullable: true),
                CreatedAt           = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt           = t.Column<DateTime>(nullable: true),
                IsDeleted           = t.Column<bool>(nullable: false, defaultValue: false)
            }, constraints: t =>
            {
                t.PrimaryKey("PK_PrintJobs", x => x.Id);
                t.ForeignKey("FK_PrintJobs_PrintTemplates_TemplateId",
                    x => x.TemplateId, "PrintTemplates", "Id", onDelete: ReferentialAction.Restrict);
                t.ForeignKey("FK_PrintJobs_Clubs_ClubId",
                    x => x.ClubId, "Clubs", "Id", onDelete: ReferentialAction.Restrict);
                t.ForeignKey("FK_PrintJobs_AspNetUsers_GeneratedByUserId",
                    x => x.GeneratedByUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
            });

            mb.CreateIndex("IX_PrintJobs_ClubId_CreatedAt", "PrintJobs", new[] { "ClubId", "CreatedAt" });
            mb.CreateIndex("IX_PrintJobs_TemplateId", "PrintJobs", "TemplateId");
        }

        protected override void Down(MigrationBuilder mb)
        {
            mb.DropTable("PrintJobs");
            mb.DropTable("PrintTemplates");
        }
    }
}
