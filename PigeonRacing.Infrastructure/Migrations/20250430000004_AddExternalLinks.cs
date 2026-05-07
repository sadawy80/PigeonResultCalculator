using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PigeonRacing.Infrastructure.Migrations
{
    public partial class AddExternalLinks : Migration
    {
        protected override void Up(MigrationBuilder mb)
        {
            mb.CreateTable("ExternalLinks", t => new
            {
                Id                    = t.Column<Guid>(nullable: false),
                UserId                = t.Column<Guid>(nullable: false),
                ClubId                = t.Column<Guid>(nullable: false),
                ExternalPlatformName  = t.Column<string>(maxLength: 100, nullable: false),
                ExternalUserId        = t.Column<string>(maxLength: 200, nullable: false),
                ExternalLoftId        = t.Column<string>(maxLength: 200, nullable: false),
                ExternalLoftName      = t.Column<string>(maxLength: 500, nullable: false, defaultValue: ""),
                CallbackUrl           = t.Column<string>(maxLength: 2000, nullable: false),
                LinkToken             = t.Column<string>(maxLength: 64, nullable: false),
                AccessToken           = t.Column<string>(maxLength: 64, nullable: true),
                AccessTokenExpiresAt  = t.Column<DateTime>(nullable: true),
                Status                = t.Column<int>(nullable: false, defaultValue: 1),
                RejectionReason       = t.Column<string>(maxLength: 1000, nullable: true),
                RevokedReason         = t.Column<string>(maxLength: 1000, nullable: true),
                RequestedAt           = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                ApprovedAt            = t.Column<DateTime>(nullable: true),
                RejectedAt            = t.Column<DateTime>(nullable: true),
                RevokedAt             = t.Column<DateTime>(nullable: true),
                LastDataAccessAt      = t.Column<DateTime>(nullable: true),
                ReviewedByUserId      = t.Column<Guid>(nullable: true),
                RequestMetadataJson   = t.Column<string>(nullable: true),
                // Base entity
                CreatedAt             = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt             = t.Column<DateTime>(nullable: true),
                IsDeleted             = t.Column<bool>(nullable: false, defaultValue: false),
                DeletedAt             = t.Column<DateTime>(nullable: true),
                CreatedBy             = t.Column<Guid>(nullable: true)
            }, constraints: t =>
            {
                t.PrimaryKey("PK_ExternalLinks", x => x.Id);
                t.ForeignKey("FK_ExternalLinks_AspNetUsers_UserId",
                    x => x.UserId, "AspNetUsers", "Id",
                    onDelete: ReferentialAction.Restrict);
                t.ForeignKey("FK_ExternalLinks_Clubs_ClubId",
                    x => x.ClubId, "Clubs", "Id",
                    onDelete: ReferentialAction.Restrict);
                t.ForeignKey("FK_ExternalLinks_AspNetUsers_ReviewedByUserId",
                    x => x.ReviewedByUserId, "AspNetUsers", "Id",
                    onDelete: ReferentialAction.SetNull);
            });

            mb.CreateIndex("IX_ExternalLinks_LinkToken",    "ExternalLinks", "LinkToken", unique: true);
            mb.CreateIndex("IX_ExternalLinks_AccessToken",  "ExternalLinks", "AccessToken");
            mb.CreateIndex("IX_ExternalLinks_UserId_Platform_Status", "ExternalLinks",
                new[] { "UserId", "ExternalPlatformName", "Status" });
            mb.CreateIndex("IX_ExternalLinks_ClubId_Status", "ExternalLinks",
                new[] { "ClubId", "Status" });
        }

        protected override void Down(MigrationBuilder mb)
        {
            mb.DropTable("ExternalLinks");
        }
    }
}
