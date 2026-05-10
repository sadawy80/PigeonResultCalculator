using Microsoft.EntityFrameworkCore;
using PRC.ClubService.Models;
using PRC.Common;
using Serilog;

namespace PRC.ClubService.Data;

public static class DemoSeeder
{
    // Must match AdminSeeder fixed GUIDs in IdentityService
    private static readonly Guid FederationId           = new("a1a1a1a1-0000-0000-0000-000000000001");
    private static readonly Guid ClubId                 = new("b2b2b2b2-0000-0000-0000-000000000002");
    private static readonly Guid ClubManagerId          = new("e5e5e5e5-0000-0000-0000-000000000005");
    private static readonly Guid FancierId              = new("f6f6f6f6-0000-0000-0000-000000000006");

    // Fixed membership IDs — cross-referenced by RaceService DemoSeeder
    public static readonly Guid ClubManagerMembershipId = new("11000001-0000-0000-0000-000000000001");
    public static readonly Guid FancierMembershipId     = new("22000001-0000-0000-0000-000000000001");

    // Fixed programme IDs — cross-referenced by RaceService DemoSeeder
    public static readonly Guid Prog2025Id              = new("cc000001-0000-0000-0000-000000000001");
    public static readonly Guid Prog2026Id              = new("cc000002-0000-0000-0000-000000000002");

    // Race IDs must match RaceService DemoSeeder
    private static readonly Guid Race1Id  = new("dd000001-0000-0000-0000-000000000001");
    private static readonly Guid Race2Id  = new("dd000002-0000-0000-0000-000000000002");
    private static readonly Guid Race3Id  = new("dd000003-0000-0000-0000-000000000003");
    private static readonly Guid Race4Id  = new("dd000004-0000-0000-0000-000000000004");
    private static readonly Guid Race5Id  = new("dd000005-0000-0000-0000-000000000005");
    private static readonly Guid Race6Id  = new("dd000006-0000-0000-0000-000000000006");
    private static readonly Guid Race7Id  = new("dd000007-0000-0000-0000-000000000007");
    private static readonly Guid Race8Id  = new("dd000008-0000-0000-0000-000000000008");

    // Pigeon IDs must match RaceService DemoSeeder
    private static readonly Guid Pigeon1Id = new("ee000001-0000-0000-0000-000000000001");
    private static readonly Guid Pigeon2Id = new("ee000002-0000-0000-0000-000000000002");
    private static readonly Guid Pigeon3Id = new("ee000003-0000-0000-0000-000000000003");

    // Other fancier GUIDs (padding results, not registered users)
    private static readonly Guid OtherF1Id = new("aa000001-0000-0000-0000-000000000001");
    private static readonly Guid OtherF2Id = new("aa000002-0000-0000-0000-000000000002");

    // Fixed notification IDs
    private static readonly Guid Notif1Id = new("ff000001-0000-0000-0000-000000000001");
    private static readonly Guid Notif2Id = new("ff000002-0000-0000-0000-000000000002");
    private static readonly Guid Notif3Id = new("ff000003-0000-0000-0000-000000000003");
    private static readonly Guid Notif4Id = new("ff000004-0000-0000-0000-000000000004");

    public static async Task SeedAsync(ClubDbContext db)
    {
        if (!await db.Clubs.AnyAsync(c => c.Id == ClubId))
        {
            db.Clubs.Add(new Club
            {
                Id                   = ClubId,
                FederationId         = FederationId,
                FederationName       = "United Kingdom",
                Name                 = "Demo Racing Club",
                Code                 = "DRC",
                City                 = "London",
                Latitude             = 51.5074,
                Longitude            = -0.1278,
                ContactEmail         = "clubmanager@prc.local",
                IsActive             = true,
                SubscriptionExpiresAt = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            });

            db.ClubPages.Add(new ClubPage
            {
                ClubId            = ClubId,
                Slug              = "demo-racing-club",
                IsPublished       = true,
                Theme             = SiteTheme.Skyline,
                AnnouncementsJson = """[{"title":"Welcome to Demo Racing Club","body":"Season 2026 is underway — check the programme for upcoming races.","date":"2026-03-01"}]""",
            });

            db.ClubMemberships.AddRange(
                new ClubMembership
                {
                    Id           = ClubManagerMembershipId,
                    ClubId       = ClubId,
                    UserId       = ClubManagerId,
                    UserFullName = "Club Manager",
                    UserEmail    = "clubmanager@prc.local",
                    UserRole     = UserRole.ClubManager,
                    IsActive     = true,
                },
                new ClubMembership
                {
                    Id           = FancierMembershipId,
                    ClubId       = ClubId,
                    UserId       = FancierId,
                    UserFullName = "Demo Fancier",
                    UserEmail    = "fancier@prc.local",
                    UserRole     = UserRole.Fancier,
                    IsActive     = true,
                }
            );

            db.PigeonLinks.AddRange(
                new PigeonLink { MembershipId = FancierMembershipId, RingNumber = "GB25A10001", PigeonId = Pigeon1Id, IsVerified = true, LinkedByUserId = ClubManagerId },
                new PigeonLink { MembershipId = FancierMembershipId, RingNumber = "GB25A10002", PigeonId = Pigeon2Id, IsVerified = true, LinkedByUserId = ClubManagerId },
                new PigeonLink { MembershipId = FancierMembershipId, RingNumber = "GB25A10003", PigeonId = Pigeon3Id, IsVerified = true, LinkedByUserId = ClubManagerId }
            );

            await db.SaveChangesAsync();
            Log.Information("DemoSeeder (Club): seeded club, page, memberships, pigeon links");
        }

        if (!await db.ClubProgrammes.AnyAsync(p => p.Id == Prog2025Id))
        {
            SeedProgramme2025(db);
            await db.SaveChangesAsync();
            Log.Information("DemoSeeder (Club): seeded 2025 programme with results");
        }

        if (!await db.ClubProgrammes.AnyAsync(p => p.Id == Prog2026Id))
        {
            SeedProgramme2026(db);
            await db.SaveChangesAsync();
            Log.Information("DemoSeeder (Club): seeded 2026 programme with results");
        }

        if (!await db.Notifications.AnyAsync(n => n.Id == Notif1Id))
        {
            var now = DateTime.UtcNow;
            db.Notifications.AddRange(
                new Notification { Id = Notif1Id, UserId = FancierId,     Type = NotificationType.RaceResult,       Channel = NotificationChannel.InApp, Status = NotificationStatus.Sent, Title = "Race results published", Body = "Results for Bristol Sprint 2026 have been published. View your standings.", ActionUrl = "/dashboard", SentAt = now.AddDays(-5) },
                new Notification { Id = Notif2Id, UserId = FancierId,     Type = NotificationType.RaceAnnouncement, Channel = NotificationChannel.InApp, Status = NotificationStatus.Read, Title = "Upcoming race: Newcastle Challenge", Body = "Newcastle Challenge is scheduled for 17 May 2026. Enter your pigeons now.", ActionUrl = "/dashboard", SentAt = now.AddDays(-12), ReadAt = now.AddDays(-10) },
                new Notification { Id = Notif3Id, UserId = ClubManagerId, Type = NotificationType.ClubUpdate,       Channel = NotificationChannel.InApp, Status = NotificationStatus.Sent, Title = "New member joined", Body = "Demo Fancier has joined Demo Racing Club.", ActionUrl = "/club/members", SentAt = now.AddDays(-3) },
                new Notification { Id = Notif4Id, UserId = ClubManagerId, Type = NotificationType.SystemUpdate,     Channel = NotificationChannel.InApp, Status = NotificationStatus.Read, Title = "Subscription active", Body = "Your club subscription is active until 31 Dec 2026.", ActionUrl = "/club/settings", SentAt = now.AddDays(-20), ReadAt = now.AddDays(-19) }
            );
            await db.SaveChangesAsync();
            Log.Information("DemoSeeder (Club): seeded demo notifications");
        }
    }

    private static void SeedProgramme2025(ClubDbContext db)
    {
        db.ClubProgrammes.Add(new ClubProgramme
        {
            Id                    = Prog2025Id,
            ClubId                = null,
            FederationId          = FederationId,
            FederationName        = "United Kingdom",
            Name                  = "2025 Season Programme",
            Description           = "Annual racing programme for the 2025 season.",
            Year                  = 2025,
            StartDate             = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate               = new DateTime(2025, 7, 31, 0, 0, 0, DateTimeKind.Utc),
            Status                = ProgrammeStatus.Published,
            ScoringMethod         = ScoringMethod.AverageSpeed,
            BestLoftPigeonsPerRace = 3,
            BestLoftMinRaces      = 3,
            AcePigeonMinRaces     = 3,
            SuperAceQualification = SuperAceQualification.MinimumRaceCount,
            SuperAceMinRaceCount  = 4,
            PublishedAt           = new DateTime(2025, 8, 15, 0, 0, 0, DateTimeKind.Utc),
            PublishedByUserId     = ClubManagerId,
        });

        db.ProgrammeRaces.AddRange(
            PR(Prog2025Id, Race1Id, "Bristol Sprint",      new DateTime(2025, 4, 12, 9,  0, 0, DateTimeKind.Utc), 8, 1),
            PR(Prog2025Id, Race2Id, "Exeter Classic",      new DateTime(2025, 5,  3, 8,  0, 0, DateTimeKind.Utc), 8, 2),
            PR(Prog2025Id, Race3Id, "York Open",           new DateTime(2025, 5, 17, 8, 30, 0, DateTimeKind.Utc), 8, 3),
            PR(Prog2025Id, Race4Id, "Newcastle Challenge", new DateTime(2025, 6,  7, 7,  0, 0, DateTimeKind.Utc), 8, 4),
            PR(Prog2025Id, Race5Id, "Edinburgh Classic",   new DateTime(2025, 6, 28, 6,  0, 0, DateTimeKind.Utc), 8, 5)
        );

        db.BestLoftResults.AddRange(
            BLR(Prog2025Id, FancierId, "Demo Fancier",  1, 5986.6, 1197.3, 5, 15, 1310.3),
            BLR(Prog2025Id, OtherF1Id, "A. Thompson",  2, 5691.9, 1138.4, 5, 10, 1233.8),
            BLR(Prog2025Id, OtherF2Id, "B. Hargreaves", 3, 5485.5, 1097.1, 5,  5, 1180.1)
        );

        // Ace Pigeon 2025
        db.AcePigeonResults.AddRange(
            Ace(Prog2025Id, FancierId, Pigeon1Id, "GB25A10001", "Sky King",    "M", 2023, "Demo Fancier", 1, 6102.2, 1220.4, 5, 5, 100.0, 1310.3, 1),
            Ace(Prog2025Id, FancierId, Pigeon2Id, "GB25A10002", "Blue Streak", "F", 2022, "Demo Fancier", 2, 5987.1, 1197.4, 5, 5, 100.0, 1283.8, 2),
            Ace(Prog2025Id, FancierId, Pigeon3Id, "GB25A10003", "Pied Racer",  "M", 2024, "Demo Fancier", 3, 5870.9, 1174.2, 5, 5, 100.0, 1258.3, 3)
        );

        // Super Ace 2025 (all 5 races entered, minimum 4 met)
        db.SuperAcePigeonResults.AddRange(
            SAce(Prog2025Id, FancierId, Pigeon1Id, "GB25A10001", "Sky King",    "M", 2023, "Demo Fancier", 1, 6102.2, 1220.4, 5, 5, 100.0, 1310.3, 1),
            SAce(Prog2025Id, FancierId, Pigeon2Id, "GB25A10002", "Blue Streak", "F", 2022, "Demo Fancier", 2, 5987.1, 1197.4, 5, 5, 100.0, 1283.8, 2),
            SAce(Prog2025Id, FancierId, Pigeon3Id, "GB25A10003", "Pied Racer",  "M", 2024, "Demo Fancier", 3, 5870.9, 1174.2, 5, 5, 100.0, 1258.3, 3)
        );
    }

    private static void SeedProgramme2026(ClubDbContext db)
    {
        db.ClubProgrammes.Add(new ClubProgramme
        {
            Id                    = Prog2026Id,
            ClubId                = null,
            FederationId          = FederationId,
            FederationName        = "United Kingdom",
            Name                  = "2026 Season Programme",
            Description           = "Annual racing programme for the 2026 season.",
            Year                  = 2026,
            StartDate             = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate               = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc),
            Status                = ProgrammeStatus.Active,
            ScoringMethod         = ScoringMethod.AverageSpeed,
            BestLoftPigeonsPerRace = 3,
            BestLoftMinRaces      = 3,
            AcePigeonMinRaces     = 3,
            SuperAceQualification = SuperAceQualification.MinimumRaceCount,
            SuperAceMinRaceCount  = 4,
        });

        // 3 completed races; 2 future races (Race9/Race10) added to programme later as season progresses
        db.ProgrammeRaces.AddRange(
            PR(Prog2026Id, Race6Id, "Bristol Sprint", new DateTime(2026, 3, 22, 9,  0, 0, DateTimeKind.Utc), 8, 1),
            PR(Prog2026Id, Race7Id, "Exeter Classic", new DateTime(2026, 4,  5, 8,  0, 0, DateTimeKind.Utc), 8, 2),
            PR(Prog2026Id, Race8Id, "York Open",      new DateTime(2026, 4, 19, 8, 30, 0, DateTimeKind.Utc), 8, 3)
        );

        db.BestLoftResults.Add(
            BLR(Prog2026Id, FancierId, "Demo Fancier", 1, 3815.8, 1271.9, 3, 9, 1319.4)
        );

        // Ace Pigeon 2026 (3 races, min 3 met)
        db.AcePigeonResults.AddRange(
            Ace(Prog2026Id, FancierId, Pigeon1Id, "GB25A10001", "Sky King",    "M", 2023, "Demo Fancier", 1, 3882.8, 1294.3, 3, 5, 60.0, 1319.4, 1),
            Ace(Prog2026Id, FancierId, Pigeon2Id, "GB25A10002", "Blue Streak", "F", 2022, "Demo Fancier", 2, 3827.5, 1275.8, 3, 5, 60.0, 1283.8, 1),
            Ace(Prog2026Id, FancierId, Pigeon3Id, "GB25A10003", "Pied Racer",  "M", 2024, "Demo Fancier", 3, 3736.8, 1245.6, 3, 5, 60.0, 1258.3, 3)
        );
        // No SuperAce for 2026 yet — needs 4+ races completed
    }

    // ── Factories ────────────────────────────────────────────────────────────────

    private static ProgrammeRace PR(Guid progId, Guid raceId, string name, DateTime release, int entries, int sort) =>
        new() { ProgrammeId = progId, RaceId = raceId, RaceName = name, ActualReleaseTime = release, TotalEntries = entries, SortOrder = sort };

    private static BestLoftResult BLR(Guid progId, Guid? userId, string name, int rank,
        double total, double avg, int races, int pigeons, double bestSpeed) =>
        new()
        {
            ProgrammeId           = progId,
            UserId                = userId,
            FancierName           = name,
            LoftRank              = rank,
            TotalScore            = total,
            AverageScore          = avg,
            RacesEntered          = races,
            PigeonsEntered        = pigeons,
            BestSingleSpeedMperMin = bestSpeed,
            AverageSpeedMperMin   = avg,
        };

    private static AcePigeonResult Ace(Guid progId, Guid? userId, Guid? pigeonId,
        string ring, string? name, string? sex, int yob, string fancier,
        int rank, double total, double avg, int racesDone, int racesInProg,
        double participation, double bestSpeed, int bestClubRank) =>
        new()
        {
            ProgrammeId       = progId,
            UserId            = userId,
            PigeonId          = pigeonId,
            RingNumber        = ring,
            PigeonName        = name,
            PigeonSex         = sex,
            PigeonYearOfBirth = yob,
            FancierName       = fancier,
            AceRank           = rank,
            TotalScore        = total,
            AverageScore      = avg,
            RacesEntered      = racesDone,
            RacesInProgramme  = racesInProg,
            ParticipationRate = participation,
            BestSpeedMperMin  = bestSpeed,
            AverageSpeedMperMin = avg,
            BestClubRank      = bestClubRank,
        };

    private static SuperAcePigeonResult SAce(Guid progId, Guid? userId, Guid? pigeonId,
        string ring, string? name, string? sex, int yob, string fancier,
        int rank, double total, double avg, int racesDone, int racesInProg,
        double participation, double bestSpeed, int bestClubRank) =>
        new()
        {
            ProgrammeId       = progId,
            UserId            = userId,
            PigeonId          = pigeonId,
            RingNumber        = ring,
            PigeonName        = name,
            PigeonSex         = sex,
            PigeonYearOfBirth = yob,
            FancierName       = fancier,
            SuperAceRank      = rank,
            TotalScore        = total,
            AverageScore      = avg,
            RacesEntered      = racesDone,
            RacesInProgramme  = racesInProg,
            ParticipationRate = participation,
            BestSpeedMperMin  = bestSpeed,
            AverageSpeedMperMin = avg,
            BestClubRank      = bestClubRank,
        };
}
