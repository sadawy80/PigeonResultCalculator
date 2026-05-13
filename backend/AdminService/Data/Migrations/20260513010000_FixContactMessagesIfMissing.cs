using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRC.AdminService.Data.Migrations
{
    /// <summary>
    /// Recovery migration. The original AddContactMessages migration shipped
    /// with an empty Up() body, so installs created between then and now have
    /// a recorded migration but no table — the table-rebuild was added in the
    /// same commit, but EF will not re-run an already-applied migration. This
    /// follow-up creates the table if and only if it is missing, so existing
    /// deployments self-heal on the next migrate without rolling back history.
    /// On fresh installs this is a no-op.
    /// </summary>
    public partial class FixContactMessagesIfMissing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[ContactMessages]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ContactMessages] (
        [Id]                UNIQUEIDENTIFIER NOT NULL,
        [SenderRole]        NVARCHAR(40)     NOT NULL,
        [UserId]            UNIQUEIDENTIFIER NULL,
        [SenderName]        NVARCHAR(200)    NOT NULL,
        [SenderEmail]       NVARCHAR(200)    NOT NULL,
        [SenderPhone]       NVARCHAR(50)     NULL,
        [Subject]           NVARCHAR(300)    NOT NULL,
        [Body]              NVARCHAR(MAX)    NOT NULL,
        [Status]            NVARCHAR(20)     NOT NULL,
        [AssignedAdminId]   UNIQUEIDENTIFIER NULL,
        [AdminReply]        NVARCHAR(MAX)    NULL,
        [RepliedAt]         DATETIME2        NULL,
        [RepliedByAdminId]  UNIQUEIDENTIFIER NULL,
        [CreatedAt]         DATETIME2        NOT NULL,
        [UpdatedAt]         DATETIME2        NULL,
        [IsDeleted]         BIT              NOT NULL,
        CONSTRAINT [PK_ContactMessages] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_ContactMessages_CreatedAt]   ON [dbo].[ContactMessages] ([CreatedAt]);
    CREATE INDEX [IX_ContactMessages_SenderEmail] ON [dbo].[ContactMessages] ([SenderEmail]);
    CREATE INDEX [IX_ContactMessages_Status]      ON [dbo].[ContactMessages] ([Status]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op — the table belongs to the prior migration in the timeline.
        }
    }
}
