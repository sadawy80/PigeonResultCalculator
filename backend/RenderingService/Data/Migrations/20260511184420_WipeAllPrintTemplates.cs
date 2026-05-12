using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRC.RenderingService.Data.Migrations
{
    /// <inheritdoc />
    public partial class WipeAllPrintTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove the previous template library wholesale.
            // A new set of templates will be re-seeded later.
            migrationBuilder.Sql("DELETE FROM PrintTemplates;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: the deleted rows came from TemplateSeeder, not from a migration,
            // so there is nothing to restore on rollback.
        }
    }
}
