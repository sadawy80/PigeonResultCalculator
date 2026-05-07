using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PigeonRacing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Identity tables ────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: t => new
                {
                    Id = t.Column<Guid>(nullable: false),
                    Name = t.Column<string>(maxLength: 256, nullable: true),
                    NormalizedName = t.Column<string>(maxLength: 256, nullable: true),
                    ConcurrencyStamp = t.Column<string>(nullable: true)
                },
                constraints: t => t.PrimaryKey("PK_AspNetRoles", x => x.Id));

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: t => new
                {
                    Id             = t.Column<Guid>(nullable: false),
                    FirstName      = t.Column<string>(maxLength: 100, nullable: false),
                    LastName       = t.Column<string>(maxLength: 100, nullable: false),
                    Role           = t.Column<int>(nullable: false),
                    CountryId      = t.Column<Guid>(nullable: true),
                    IsActive       = t.Column<bool>(nullable: false, defaultValue: true),
                    LastLoginAt    = t.Column<DateTime>(nullable: true),
                    ProfileImageUrl= t.Column<string>(maxLength: 500, nullable: true),
                    CreatedAt      = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt      = t.Column<DateTime>(nullable: true),
                    IsDeleted      = t.Column<bool>(nullable: false, defaultValue: false),
                    UserName       = t.Column<string>(maxLength: 256, nullable: true),
                    NormalizedUserName = t.Column<string>(maxLength: 256, nullable: true),
                    Email          = t.Column<string>(maxLength: 256, nullable: true),
                    NormalizedEmail= t.Column<string>(maxLength: 256, nullable: true),
                    EmailConfirmed = t.Column<bool>(nullable: false),
                    PasswordHash   = t.Column<string>(nullable: true),
                    SecurityStamp  = t.Column<string>(nullable: true),
                    ConcurrencyStamp = t.Column<string>(nullable: true),
                    PhoneNumber    = t.Column<string>(nullable: true),
                    PhoneNumberConfirmed = t.Column<bool>(nullable: false),
                    TwoFactorEnabled = t.Column<bool>(nullable: false),
                    LockoutEnd     = t.Column<DateTimeOffset>(nullable: true),
                    LockoutEnabled = t.Column<bool>(nullable: false),
                    AccessFailedCount = t.Column<int>(nullable: false)
                },
                constraints: t => t.PrimaryKey("PK_AspNetUsers", x => x.Id));

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: t => new
                {
                    Id         = t.Column<Guid>(nullable: false),
                    UserId     = t.Column<Guid>(nullable: false),
                    Token      = t.Column<string>(maxLength: 500, nullable: false),
                    ExpiresAt  = t.Column<DateTime>(nullable: false),
                    IsRevoked  = t.Column<bool>(nullable: false, defaultValue: false),
                    RevokedReason    = t.Column<string>(maxLength: 200, nullable: true),
                    ReplacedByToken  = t.Column<string>(maxLength: 500, nullable: true),
                    CreatedAt  = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt  = t.Column<DateTime>(nullable: true),
                    IsDeleted  = t.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    t.ForeignKey("FK_RefreshTokens_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            // ── Countries ──────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Countries",
                columns: t => new
                {
                    Id          = t.Column<Guid>(nullable: false),
                    Name        = t.Column<string>(maxLength: 100, nullable: false),
                    Code        = t.Column<string>(maxLength: 5, nullable: false),
                    Slug        = t.Column<string>(maxLength: 100, nullable: false),
                    IsActive    = t.Column<bool>(nullable: false, defaultValue: true),
                    CreatedAt   = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt   = t.Column<DateTime>(nullable: true),
                    IsDeleted   = t.Column<bool>(nullable: false, defaultValue: false),
                    CreatedBy   = t.Column<Guid>(nullable: true),
                    DeletedAt   = t.Column<DateTime>(nullable: true)
                },
                constraints: t => t.PrimaryKey("PK_Countries", x => x.Id));

            migrationBuilder.CreateIndex("IX_Countries_Code", "Countries", "Code", unique: true);
            migrationBuilder.CreateIndex("IX_Countries_Slug", "Countries", "Slug", unique: true);

            // ── Subscription Plans ─────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: t => new
                {
                    Id          = t.Column<Guid>(nullable: false),
                    Name        = t.Column<string>(maxLength: 100, nullable: false),
                    Type        = t.Column<int>(nullable: false),
                    PricePerMonth = t.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PricePerYear  = t.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxClubs    = t.Column<int>(nullable: false),
                    MaxMembersPerClub = t.Column<int>(nullable: false),
                    MaxRacesPerMonth  = t.Column<int>(nullable: false),
                    HasLiveTracking   = t.Column<bool>(nullable: false),
                    HasAdvancedReports= t.Column<bool>(nullable: false),
                    HasApiAccess      = t.Column<bool>(nullable: false),
                    HasCustomDomain   = t.Column<bool>(nullable: false),
                    IsActive    = t.Column<bool>(nullable: false, defaultValue: true),
                    SortOrder   = t.Column<int>(nullable: false),
                    CreatedAt   = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt   = t.Column<DateTime>(nullable: true),
                    IsDeleted   = t.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: t => t.PrimaryKey("PK_SubscriptionPlans", x => x.Id));

            // ── Clubs ──────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Clubs",
                columns: t => new
                {
                    Id           = t.Column<Guid>(nullable: false),
                    CountryId    = t.Column<Guid>(nullable: false),
                    Name         = t.Column<string>(maxLength: 200, nullable: false),
                    Code         = t.Column<string>(maxLength: 20, nullable: false),
                    Description  = t.Column<string>(maxLength: 1000, nullable: true),
                    City         = t.Column<string>(maxLength: 100, nullable: true),
                    Address      = t.Column<string>(maxLength: 300, nullable: true),
                    PostalCode   = t.Column<string>(maxLength: 20, nullable: true),
                    Latitude     = t.Column<double>(nullable: true),
                    Longitude    = t.Column<double>(nullable: true),
                    ContactEmail = t.Column<string>(maxLength: 200, nullable: true),
                    ContactPhone = t.Column<string>(maxLength: 50, nullable: true),
                    LogoUrl      = t.Column<string>(maxLength: 500, nullable: true),
                    PrimaryColor = t.Column<string>(maxLength: 7, nullable: true),
                    SecondaryColor = t.Column<string>(maxLength: 7, nullable: true),
                    IsActive     = t.Column<bool>(nullable: false, defaultValue: true),
                    CreatedAt    = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt    = t.Column<DateTime>(nullable: true),
                    IsDeleted    = t.Column<bool>(nullable: false, defaultValue: false),
                    DeletedAt    = t.Column<DateTime>(nullable: true),
                    CreatedBy    = t.Column<Guid>(nullable: true)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_Clubs", x => x.Id);
                    t.ForeignKey("FK_Clubs_Countries_CountryId", x => x.CountryId, "Countries", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex("IX_Clubs_CountryId", "Clubs", "CountryId");
            migrationBuilder.CreateIndex("IX_Clubs_Code_CountryId", "Clubs", new[] { "Code", "CountryId" }, unique: true);

            // ── Club Pages ─────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ClubPages",
                columns: t => new
                {
                    Id                  = t.Column<Guid>(nullable: false),
                    ClubId              = t.Column<Guid>(nullable: false),
                    CustomDomain        = t.Column<string>(maxLength: 200, nullable: true),
                    Slug                = t.Column<string>(maxLength: 100, nullable: false),
                    IsPublished         = t.Column<bool>(nullable: false, defaultValue: false),
                    Theme               = t.Column<int>(nullable: false, defaultValue: 1),
                    HeaderHtml          = t.Column<string>(nullable: true),
                    FooterHtml          = t.Column<string>(nullable: true),
                    CustomCss           = t.Column<string>(nullable: true),
                    AnnouncementsJson   = t.Column<string>(nullable: true),
                    LayoutConfig        = t.Column<string>(nullable: true),
                    CertificateTemplateId = t.Column<string>(maxLength: 50, nullable: true),
                    ResultsTemplateId   = t.Column<string>(maxLength: 50, nullable: true),
                    CreatedAt           = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt           = t.Column<DateTime>(nullable: true),
                    IsDeleted           = t.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_ClubPages", x => x.Id);
                    t.ForeignKey("FK_ClubPages_Clubs_ClubId", x => x.ClubId, "Clubs", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_ClubPages_Slug", "ClubPages", "Slug", unique: true);

            // ── Club Memberships ───────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ClubMemberships",
                columns: t => new
                {
                    Id       = t.Column<Guid>(nullable: false),
                    ClubId   = t.Column<Guid>(nullable: false),
                    UserId   = t.Column<Guid>(nullable: false),
                    IsActive = t.Column<bool>(nullable: false, defaultValue: true),
                    JoinedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedAt = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = t.Column<DateTime>(nullable: true),
                    IsDeleted = t.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_ClubMemberships", x => x.Id);
                    t.ForeignKey("FK_ClubMemberships_Clubs_ClubId", x => x.ClubId, "Clubs", "Id", onDelete: ReferentialAction.Cascade);
                    t.ForeignKey("FK_ClubMemberships_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex("IX_ClubMemberships_ClubId_UserId", "ClubMemberships", new[] { "ClubId", "UserId" }, unique: true);

            // ── Pigeons ────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Pigeons",
                columns: t => new
                {
                    Id           = t.Column<Guid>(nullable: false),
                    RingNumber   = t.Column<string>(maxLength: 50, nullable: false),
                    Name         = t.Column<string>(maxLength: 100, nullable: true),
                    Sex          = t.Column<string>(maxLength: 1, nullable: true),
                    YearOfBirth  = t.Column<int>(nullable: true),
                    Color        = t.Column<string>(maxLength: 50, nullable: true),
                    Strain       = t.Column<string>(maxLength: 100, nullable: true),
                    CreatedAt    = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt    = t.Column<DateTime>(nullable: true),
                    IsDeleted    = t.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: t => t.PrimaryKey("PK_Pigeons", x => x.Id));

            migrationBuilder.CreateIndex("IX_Pigeons_RingNumber", "Pigeons", "RingNumber", unique: true);

            // ── Pigeon Links ───────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "PigeonLinks",
                columns: t => new
                {
                    Id            = t.Column<Guid>(nullable: false),
                    MembershipId  = t.Column<Guid>(nullable: false),
                    PigeonId      = t.Column<Guid>(nullable: true),
                    RingNumber    = t.Column<string>(maxLength: 50, nullable: false),
                    IsVerified    = t.Column<bool>(nullable: false, defaultValue: false),
                    LinkedByUserId = t.Column<Guid>(nullable: false),
                    CreatedAt     = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt     = t.Column<DateTime>(nullable: true),
                    IsDeleted     = t.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_PigeonLinks", x => x.Id);
                    t.ForeignKey("FK_PigeonLinks_ClubMemberships_MembershipId", x => x.MembershipId, "ClubMemberships", "Id", onDelete: ReferentialAction.Cascade);
                    t.ForeignKey("FK_PigeonLinks_Pigeons_PigeonId", x => x.PigeonId, "Pigeons", "Id", onDelete: ReferentialAction.SetNull);
                });

            // ── Races ──────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Races",
                columns: t => new
                {
                    Id                   = t.Column<Guid>(nullable: false),
                    ClubId               = t.Column<Guid>(nullable: false),
                    Name                 = t.Column<string>(maxLength: 200, nullable: false),
                    Description          = t.Column<string>(maxLength: 1000, nullable: true),
                    Status               = t.Column<int>(nullable: false, defaultValue: 1),
                    ReleaseLocation      = t.Column<string>(maxLength: 200, nullable: false),
                    ReleaseLongitude     = t.Column<double>(nullable: false),
                    ReleaseLatitude      = t.Column<double>(nullable: false),
                    ScheduledReleaseTime = t.Column<DateTime>(nullable: true),
                    ActualReleaseTime    = t.Column<DateTime>(nullable: true),
                    CompletedAt          = t.Column<DateTime>(nullable: true),
                    PublishedAt          = t.Column<DateTime>(nullable: true),
                    WindSpeedKmh         = t.Column<double>(nullable: true),
                    WindDirection        = t.Column<int>(nullable: true),
                    TemperatureCelsius   = t.Column<double>(nullable: true),
                    TotalPigeonsEntered  = t.Column<int>(nullable: true),
                    IsLiveTracking       = t.Column<bool>(nullable: false, defaultValue: false),
                    CreatedAt            = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt            = t.Column<DateTime>(nullable: true),
                    IsDeleted            = t.Column<bool>(nullable: false, defaultValue: false),
                    DeletedAt            = t.Column<DateTime>(nullable: true),
                    CreatedBy            = t.Column<Guid>(nullable: true)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_Races", x => x.Id);
                    t.ForeignKey("FK_Races_Clubs_ClubId", x => x.ClubId, "Clubs", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex("IX_Races_ClubId_Status", "Races", new[] { "ClubId", "Status" });

            // ── Race Categories ────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "RaceCategories",
                columns: t => new
                {
                    Id          = t.Column<Guid>(nullable: false),
                    RaceId      = t.Column<Guid>(nullable: false),
                    Name        = t.Column<string>(maxLength: 100, nullable: false),
                    Description = t.Column<string>(maxLength: 500, nullable: true),
                    SortOrder   = t.Column<int>(nullable: false),
                    CreatedAt   = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt   = t.Column<DateTime>(nullable: true),
                    IsDeleted   = t.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_RaceCategories", x => x.Id);
                    t.ForeignKey("FK_RaceCategories_Races_RaceId", x => x.RaceId, "Races", "Id", onDelete: ReferentialAction.Cascade);
                });

            // ── Race Results ───────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "RaceResults",
                columns: t => new
                {
                    Id                  = t.Column<Guid>(nullable: false),
                    RaceId              = t.Column<Guid>(nullable: false),
                    CategoryId          = t.Column<Guid>(nullable: true),
                    UserId              = t.Column<Guid>(nullable: true),
                    RingNumber          = t.Column<string>(maxLength: 50, nullable: false),
                    PigeonName          = t.Column<string>(maxLength: 100, nullable: true),
                    PigeonSex           = t.Column<string>(maxLength: 1, nullable: true),
                    PigeonYearOfBirth   = t.Column<int>(nullable: true),
                    ArrivalTime         = t.Column<DateTime>(nullable: false),
                    FlightDurationTicks = t.Column<long>(nullable: true),
                    DistanceKm          = t.Column<double>(nullable: false),
                    VelocityMperMin     = t.Column<double>(nullable: false),
                    ClubRank            = t.Column<int>(nullable: true),
                    CategoryRank        = t.Column<int>(nullable: true),
                    Status              = t.Column<int>(nullable: false, defaultValue: 1),
                    IsDuplicate         = t.Column<bool>(nullable: false, defaultValue: false),
                    IsLateArrival       = t.Column<bool>(nullable: false, defaultValue: false),
                    HasInvalidTimestamp = t.Column<bool>(nullable: false, defaultValue: false),
                    ValidationNotes     = t.Column<string>(maxLength: 500, nullable: true),
                    IngestionType       = t.Column<int>(nullable: false),
                    CreatedAt           = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt           = t.Column<DateTime>(nullable: true),
                    IsDeleted           = t.Column<bool>(nullable: false, defaultValue: false),
                    DeletedAt           = t.Column<DateTime>(nullable: true),
                    CreatedBy           = t.Column<Guid>(nullable: true)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_RaceResults", x => x.Id);
                    t.ForeignKey("FK_RaceResults_Races_RaceId", x => x.RaceId, "Races", "Id", onDelete: ReferentialAction.Cascade);
                    t.ForeignKey("FK_RaceResults_RaceCategories_CategoryId", x => x.CategoryId, "RaceCategories", "Id", onDelete: ReferentialAction.SetNull);
                    t.ForeignKey("FK_RaceResults_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex("IX_RaceResults_RaceId_RingNumber", "RaceResults", new[] { "RaceId", "RingNumber" });
            migrationBuilder.CreateIndex("IX_RaceResults_UserId", "RaceResults", "UserId");
            migrationBuilder.CreateIndex("IX_RaceResults_ClubRank", "RaceResults", "ClubRank");

            // ── Data Ingestion Logs ────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "DataIngestionLogs",
                columns: t => new
                {
                    Id               = t.Column<Guid>(nullable: false),
                    RaceId           = t.Column<Guid>(nullable: false),
                    IngestionType    = t.Column<int>(nullable: false),
                    FileName         = t.Column<string>(maxLength: 300, nullable: true),
                    TotalRowsRead    = t.Column<int>(nullable: false),
                    SuccessfulRows   = t.Column<int>(nullable: false),
                    FailedRows       = t.Column<int>(nullable: false),
                    DuplicateRows    = t.Column<int>(nullable: false),
                    ErrorSummary     = t.Column<string>(nullable: true),
                    RawFileUrl       = t.Column<string>(maxLength: 500, nullable: true),
                    IsSuccess        = t.Column<bool>(nullable: false),
                    ProcessedAt      = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ProcessedByUserId = t.Column<Guid>(nullable: false),
                    CreatedAt        = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt        = t.Column<DateTime>(nullable: true),
                    IsDeleted        = t.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_DataIngestionLogs", x => x.Id);
                    t.ForeignKey("FK_DataIngestionLogs_Races_RaceId", x => x.RaceId, "Races", "Id", onDelete: ReferentialAction.Cascade);
                });

            // ── Country Results ────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "CountryResults",
                columns: t => new
                {
                    Id                = t.Column<Guid>(nullable: false),
                    CountryId         = t.Column<Guid>(nullable: false),
                    Name              = t.Column<string>(maxLength: 200, nullable: false),
                    Description       = t.Column<string>(maxLength: 1000, nullable: true),
                    Status            = t.Column<int>(nullable: false, defaultValue: 1),
                    TotalEntriesCount = t.Column<int>(nullable: false),
                    TotalClubsCount   = t.Column<int>(nullable: false),
                    PublishedAt       = t.Column<DateTime>(nullable: true),
                    PublishedByUserId = t.Column<Guid>(nullable: true),
                    CreatedAt         = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt         = t.Column<DateTime>(nullable: true),
                    IsDeleted         = t.Column<bool>(nullable: false, defaultValue: false),
                    CreatedBy         = t.Column<Guid>(nullable: true)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_CountryResults", x => x.Id);
                    t.ForeignKey("FK_CountryResults_Countries_CountryId", x => x.CountryId, "Countries", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CountryResultRaces",
                columns: t => new
                {
                    Id             = t.Column<Guid>(nullable: false),
                    CountryResultId = t.Column<Guid>(nullable: false),
                    RaceId         = t.Column<Guid>(nullable: false),
                    ClubId         = t.Column<Guid>(nullable: false),
                    CreatedAt      = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt      = t.Column<DateTime>(nullable: true),
                    IsDeleted      = t.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_CountryResultRaces", x => x.Id);
                    t.ForeignKey("FK_CountryResultRaces_CountryResults_CountryResultId", x => x.CountryResultId, "CountryResults", "Id", onDelete: ReferentialAction.Cascade);
                    t.ForeignKey("FK_CountryResultRaces_Races_RaceId", x => x.RaceId, "Races", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CountryResultEntries",
                columns: t => new
                {
                    Id                    = t.Column<Guid>(nullable: false),
                    CountryResultId       = t.Column<Guid>(nullable: false),
                    RaceResultId          = t.Column<Guid>(nullable: false),
                    ClubId                = t.Column<Guid>(nullable: false),
                    RingNumber            = t.Column<string>(maxLength: 50, nullable: false),
                    UserId                = t.Column<Guid>(nullable: true),
                    VelocityMperMin       = t.Column<double>(nullable: false),
                    DistanceKm            = t.Column<double>(nullable: false),
                    NationalRank          = t.Column<int>(nullable: false),
                    NationalCategoryRank  = t.Column<int>(nullable: true),
                    CreatedAt             = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt             = t.Column<DateTime>(nullable: true),
                    IsDeleted             = t.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_CountryResultEntries", x => x.Id);
                    t.ForeignKey("FK_CountryResultEntries_CountryResults_CountryResultId", x => x.CountryResultId, "CountryResults", "Id", onDelete: ReferentialAction.Cascade);
                    t.ForeignKey("FK_CountryResultEntries_Clubs_ClubId", x => x.ClubId, "Clubs", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_CountryResultEntries_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex("IX_CountryResultEntries_NationalRank", "CountryResultEntries", new[] { "CountryResultId", "NationalRank" });

            // ── Invitations ────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: t => new
                {
                    Id               = t.Column<Guid>(nullable: false),
                    ClubId           = t.Column<Guid>(nullable: false),
                    Email            = t.Column<string>(maxLength: 256, nullable: false),
                    Token            = t.Column<string>(maxLength: 100, nullable: false),
                    Status           = t.Column<int>(nullable: false, defaultValue: 1),
                    InvitedByUserId  = t.Column<Guid>(nullable: false),
                    AcceptedByUserId = t.Column<Guid>(nullable: true),
                    ExpiresAt        = t.Column<DateTime>(nullable: false),
                    AcceptedAt       = t.Column<DateTime>(nullable: true),
                    CreatedAt        = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt        = t.Column<DateTime>(nullable: true),
                    IsDeleted        = t.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_Invitations", x => x.Id);
                    t.ForeignKey("FK_Invitations_Clubs_ClubId", x => x.ClubId, "Clubs", "Id", onDelete: ReferentialAction.Cascade);
                    t.ForeignKey("FK_Invitations_AspNetUsers_InvitedByUserId", x => x.InvitedByUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex("IX_Invitations_Token", "Invitations", "Token", unique: true);

            // ── Notifications ──────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: t => new
                {
                    Id          = t.Column<Guid>(nullable: false),
                    UserId      = t.Column<Guid>(nullable: false),
                    Type        = t.Column<int>(nullable: false),
                    Channel     = t.Column<int>(nullable: false),
                    Status      = t.Column<int>(nullable: false, defaultValue: 1),
                    Title       = t.Column<string>(maxLength: 200, nullable: false),
                    Body        = t.Column<string>(maxLength: 2000, nullable: false),
                    ActionUrl   = t.Column<string>(maxLength: 500, nullable: true),
                    ReadAt      = t.Column<DateTime>(nullable: true),
                    CreatedAt   = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt   = t.Column<DateTime>(nullable: true),
                    IsDeleted   = t.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_Notifications", x => x.Id);
                    t.ForeignKey("FK_Notifications_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_Notifications_UserId_Status", "Notifications", new[] { "UserId", "Status" });

            // ── Country Subscriptions ──────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "CountrySubscriptions",
                columns: t => new
                {
                    Id            = t.Column<Guid>(nullable: false),
                    CountryId     = t.Column<Guid>(nullable: false),
                    PlanId        = t.Column<Guid>(nullable: false),
                    Status        = t.Column<int>(nullable: false),
                    BillingCycle  = t.Column<int>(nullable: false),
                    StartedAt     = t.Column<DateTime>(nullable: false),
                    ExpiresAt     = t.Column<DateTime>(nullable: false),
                    RenewsAt      = t.Column<DateTime>(nullable: true),
                    CancelledAt   = t.Column<DateTime>(nullable: true),
                    CreatedAt     = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt     = t.Column<DateTime>(nullable: true),
                    IsDeleted     = t.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: t =>
                {
                    t.PrimaryKey("PK_CountrySubscriptions", x => x.Id);
                    t.ForeignKey("FK_CountrySubscriptions_Countries_CountryId", x => x.CountryId, "Countries", "Id", onDelete: ReferentialAction.Restrict);
                    t.ForeignKey("FK_CountrySubscriptions_SubscriptionPlans_PlanId", x => x.PlanId, "SubscriptionPlans", "Id", onDelete: ReferentialAction.Restrict);
                });

            // ── Domain Events ──────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "DomainEvents",
                columns: t => new
                {
                    Id              = t.Column<Guid>(nullable: false),
                    EventType       = t.Column<string>(maxLength: 100, nullable: false),
                    AggregateId     = t.Column<string>(maxLength: 50, nullable: false),
                    AggregateType   = t.Column<string>(maxLength: 100, nullable: false),
                    Payload         = t.Column<string>(nullable: false),
                    TriggeredByUserId = t.Column<Guid>(nullable: true),
                    CorrelationId   = t.Column<string>(maxLength: 50, nullable: true),
                    IsProcessed     = t.Column<bool>(nullable: false, defaultValue: false),
                    RetryCount      = t.Column<int>(nullable: false, defaultValue: 0),
                    ProcessedAt     = t.Column<DateTime>(nullable: true),
                    ProcessingError = t.Column<string>(nullable: true),
                    CreatedAt       = t.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt       = t.Column<DateTime>(nullable: true),
                    IsDeleted       = t.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: t => t.PrimaryKey("PK_DomainEvents", x => x.Id));

            migrationBuilder.CreateIndex("IX_DomainEvents_EventType", "DomainEvents", "EventType");
            migrationBuilder.CreateIndex("IX_DomainEvents_AggregateId", "DomainEvents", "AggregateId");

            // ── Seed built-in subscription plans ──────────────────────────
            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "Id","Name","Type","PricePerMonth","PricePerYear","MaxClubs","MaxMembersPerClub","MaxRacesPerMonth","HasLiveTracking","HasAdvancedReports","HasApiAccess","HasCustomDomain","IsActive","SortOrder","CreatedAt","UpdatedAt","IsDeleted" },
                values: new object[,]
                {
                    { Guid.NewGuid(), "Starter",      1,  29m,  290m,   5,   50,  10, false, false, false, false, true, 1, DateTime.UtcNow, null, false },
                    { Guid.NewGuid(), "Standard",     2,  79m,  790m,  20,  200,  30, true,  false, false, false, true, 2, DateTime.UtcNow, null, false },
                    { Guid.NewGuid(), "Professional", 3, 149m, 1490m,  50,  500, 100, true,  true,  false, true,  true, 3, DateTime.UtcNow, null, false },
                    { Guid.NewGuid(), "Enterprise",   4, 299m, 2990m, 999, 9999, 999, true,  true,  true,  true,  true, 4, DateTime.UtcNow, null, false },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("DomainEvents");
            migrationBuilder.DropTable("CountrySubscriptions");
            migrationBuilder.DropTable("Notifications");
            migrationBuilder.DropTable("Invitations");
            migrationBuilder.DropTable("CountryResultEntries");
            migrationBuilder.DropTable("CountryResultRaces");
            migrationBuilder.DropTable("CountryResults");
            migrationBuilder.DropTable("DataIngestionLogs");
            migrationBuilder.DropTable("RaceResults");
            migrationBuilder.DropTable("RaceCategories");
            migrationBuilder.DropTable("Races");
            migrationBuilder.DropTable("PigeonLinks");
            migrationBuilder.DropTable("Pigeons");
            migrationBuilder.DropTable("ClubMemberships");
            migrationBuilder.DropTable("ClubPages");
            migrationBuilder.DropTable("Clubs");
            migrationBuilder.DropTable("SubscriptionPlans");
            migrationBuilder.DropTable("Countries");
            migrationBuilder.DropTable("RefreshTokens");
            migrationBuilder.DropTable("AspNetRoles");
            migrationBuilder.DropTable("AspNetUsers");
        }
    }
}
