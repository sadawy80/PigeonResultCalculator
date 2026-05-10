using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.RaceService.Models;
using Serilog;

namespace PRC.RaceService.Data;

public static class DemoSeeder
{
    // Must match ClubService DemoSeeder
    private static readonly Guid ClubId                 = new("b2b2b2b2-0000-0000-0000-000000000002");
    private static readonly Guid FederationId           = new("a1a1a1a1-0000-0000-0000-000000000001");
    private static readonly Guid ClubManagerId          = new("e5e5e5e5-0000-0000-0000-000000000005");
    private static readonly Guid FancierId              = new("f6f6f6f6-0000-0000-0000-000000000006");
    private static readonly Guid FancierMembershipId    = new("22000001-0000-0000-0000-000000000001");
    private static readonly Guid Prog2025Id             = new("cc000001-0000-0000-0000-000000000001");
    private static readonly Guid Prog2026Id             = new("cc000002-0000-0000-0000-000000000002");

    private static readonly Guid Pigeon1Id  = new("ee000001-0000-0000-0000-000000000001");
    private static readonly Guid Pigeon2Id  = new("ee000002-0000-0000-0000-000000000002");
    private static readonly Guid Pigeon3Id  = new("ee000003-0000-0000-0000-000000000003");

    private static readonly Guid Race1Id    = new("dd000001-0000-0000-0000-000000000001");
    private static readonly Guid Race2Id    = new("dd000002-0000-0000-0000-000000000002");
    private static readonly Guid Race3Id    = new("dd000003-0000-0000-0000-000000000003");
    private static readonly Guid Race4Id    = new("dd000004-0000-0000-0000-000000000004");
    private static readonly Guid Race5Id    = new("dd000005-0000-0000-0000-000000000005");
    private static readonly Guid Race6Id    = new("dd000006-0000-0000-0000-000000000006");
    private static readonly Guid Race7Id    = new("dd000007-0000-0000-0000-000000000007");
    private static readonly Guid Race8Id    = new("dd000008-0000-0000-0000-000000000008");
    private static readonly Guid Race9Id    = new("dd000009-0000-0000-0000-000000000009");
    private static readonly Guid Race10Id   = new("dd000010-0000-0000-0000-000000000010");

    // Result filler GUIDs (not registered users; used for leaderboard padding)
    private static readonly Guid OtherF1Id  = new("aa000001-0000-0000-0000-000000000001");
    private static readonly Guid OtherF2Id  = new("aa000002-0000-0000-0000-000000000002");
    private static readonly Guid OtherF3Id  = new("aa000003-0000-0000-0000-000000000003");
    private static readonly Guid OtherF4Id  = new("aa000004-0000-0000-0000-000000000004");

    // London loft coordinates (cached on Race for speed calc)
    private const double LoftLat = 51.5074;
    private const double LoftLon = -0.1278;

    public static async Task SeedAsync(RaceDbContext db)
    {
        if (!await db.Races.AnyAsync(r => r.Id == Race1Id))
        {
            SeedPigeons(db);
            SeedRaces2025(db);
            SeedRaces2026(db);
            await db.SaveChangesAsync();
            Log.Information("DemoSeeder (Race): seeded 3 pigeons and 10 demo races with results");
            return;
        }

        // Patch ProgrammeId/ProgrammeName on already-seeded races if missing
        var race2025Ids = new[] { Race1Id, Race2Id, Race3Id, Race4Id, Race5Id };
        var race2026Ids = new[] { Race6Id, Race7Id, Race8Id, Race9Id, Race10Id };
        var needsPatch = await db.Races
            .Where(r => race2025Ids.Contains(r.Id) || race2026Ids.Contains(r.Id))
            .Where(r => r.ProgrammeId == null)
            .AnyAsync();

        if (needsPatch)
        {
            await db.Races
                .Where(r => race2025Ids.Contains(r.Id))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.ProgrammeId,   Prog2025Id)
                    .SetProperty(r => r.ProgrammeName, "2025 Season Programme"));
            await db.Races
                .Where(r => race2026Ids.Contains(r.Id))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.ProgrammeId,   Prog2026Id)
                    .SetProperty(r => r.ProgrammeName, "2026 Season Programme"));
            Log.Information("DemoSeeder (Race): patched ProgrammeId on existing demo races");
        }
    }

    private static void SeedPigeons(RaceDbContext db)
    {
        db.Pigeons.AddRange(
            new Pigeon { Id = Pigeon1Id, RingNumber = "GB25A10001", Name = "Sky King",    Sex = "M", YearOfBirth = 2023, Color = "Blue Bar",  Strain = "Janssen", FederationId = FederationId },
            new Pigeon { Id = Pigeon2Id, RingNumber = "GB25A10002", Name = "Blue Streak", Sex = "F", YearOfBirth = 2022, Color = "Checker",   Strain = "Stichelbaut", FederationId = FederationId },
            new Pigeon { Id = Pigeon3Id, RingNumber = "GB25A10003", Name = "Pied Racer",  Sex = "M", YearOfBirth = 2024, Color = "Pied",      Strain = "Van Loon", FederationId = FederationId }
        );

        db.PigeonLinks.AddRange(
            new PigeonLink { MembershipId = FancierMembershipId, RingNumber = "GB25A10001", PigeonId = Pigeon1Id, IsVerified = true, LinkedByUserId = ClubManagerId },
            new PigeonLink { MembershipId = FancierMembershipId, RingNumber = "GB25A10002", PigeonId = Pigeon2Id, IsVerified = true, LinkedByUserId = ClubManagerId },
            new PigeonLink { MembershipId = FancierMembershipId, RingNumber = "GB25A10003", PigeonId = Pigeon3Id, IsVerified = true, LinkedByUserId = ClubManagerId }
        );
    }

    // ── 2025 races (all Published) ───────────────────────────────────────────────

    private static void SeedRaces2025(RaceDbContext db)
    {
        var cat1 = AddRace(db, Race1Id, "Bristol Sprint",      new DateTime(2025, 4, 12, 9,  0, 0, DateTimeKind.Utc), 51.4545, -2.5879, "Bristol",   190.0, RaceStatus.Published, Prog2025Id, "2025 Season Programme");
        var cat2 = AddRace(db, Race2Id, "Exeter Classic",      new DateTime(2025, 5,  3, 8,  0, 0, DateTimeKind.Utc), 50.7184, -3.5339, "Exeter",    270.0, RaceStatus.Published, Prog2025Id, "2025 Season Programme");
        var cat3 = AddRace(db, Race3Id, "York Open",           new DateTime(2025, 5, 17, 8, 30, 0, DateTimeKind.Utc), 53.9599, -1.0873, "York",      310.0, RaceStatus.Published, Prog2025Id, "2025 Season Programme");
        var cat4 = AddRace(db, Race4Id, "Newcastle Challenge", new DateTime(2025, 6,  7, 7,  0, 0, DateTimeKind.Utc), 54.9783, -1.6178, "Newcastle", 430.0, RaceStatus.Published, Prog2025Id, "2025 Season Programme");
        var cat5 = AddRace(db, Race5Id, "Edinburgh Classic",   new DateTime(2025, 6, 28, 6,  0, 0, DateTimeKind.Utc), 55.9533, -3.1883, "Edinburgh", 530.0, RaceStatus.Published, Prog2025Id, "2025 Season Programme");

        // Race 1 — Bristol 2025 (190 km, release 09:00)
        var r1 = new DateTime(2025, 4, 12, 9, 0, 0, DateTimeKind.Utc);
        AddResults(db, Race1Id, cat1.Id, 190.0, r1, new (Guid, string, string?, string?, int, Guid?, int, int)[]
        {
            (FancierId,  "GB25A10001", "Sky King",    "M", 2023, Pigeon1Id, 145, 1),
            (FancierId,  "GB25A10002", "Blue Streak", "F", 2022, Pigeon2Id, 148, 2),
            (FancierId,  "GB25A10003", "Pied Racer",  "M", 2024, Pigeon3Id, 151, 3),
            (OtherF1Id,  "GB21B20001", null,          null, 0,   null,      154, 4),
            (OtherF1Id,  "GB21B20002", null,          null, 0,   null,      158, 5),
            (OtherF2Id,  "GB22C30001", null,          null, 0,   null,      161, 6),
            (OtherF3Id,  "GB23D40001", null,          null, 0,   null,      165, 7),
            (OtherF4Id,  "GB24E50001", null,          null, 0,   null,      169, 8),
        });

        // Race 2 — Exeter 2025 (270 km, release 08:00)
        var r2 = new DateTime(2025, 5, 3, 8, 0, 0, DateTimeKind.Utc);
        AddResults(db, Race2Id, cat2.Id, 270.0, r2, new (Guid, string, string?, string?, int, Guid?, int, int)[]
        {
            (FancierId,  "GB25A10001", "Sky King",    "M", 2023, Pigeon1Id, 209, 1),
            (FancierId,  "GB25A10002", "Blue Streak", "F", 2022, Pigeon2Id, 214, 2),
            (FancierId,  "GB25A10003", "Pied Racer",  "M", 2024, Pigeon3Id, 219, 3),
            (OtherF1Id,  "GB21B20001", null,          null, 0,   null,      223, 4),
            (OtherF1Id,  "GB21B20002", null,          null, 0,   null,      229, 5),
            (OtherF2Id,  "GB22C30001", null,          null, 0,   null,      235, 6),
            (OtherF3Id,  "GB23D40001", null,          null, 0,   null,      241, 7),
            (OtherF4Id,  "GB24E50001", null,          null, 0,   null,      248, 8),
        });

        // Race 3 — York 2025 (310 km, release 08:30)
        var r3 = new DateTime(2025, 5, 17, 8, 30, 0, DateTimeKind.Utc);
        AddResults(db, Race3Id, cat3.Id, 310.0, r3, new (Guid, string, string?, string?, int, Guid?, int, int)[]
        {
            (FancierId,  "GB25A10001", "Sky King",    "M", 2023, Pigeon1Id, 244, 1),
            (FancierId,  "GB25A10002", "Blue Streak", "F", 2022, Pigeon2Id, 248, 2),
            (FancierId,  "GB25A10003", "Pied Racer",  "M", 2024, Pigeon3Id, 252, 3),
            (OtherF1Id,  "GB21B20001", null,          null, 0,   null,      256, 4),
            (OtherF1Id,  "GB21B20002", null,          null, 0,   null,      263, 5),
            (OtherF2Id,  "GB22C30001", null,          null, 0,   null,      270, 6),
            (OtherF3Id,  "GB23D40001", null,          null, 0,   null,      277, 7),
            (OtherF4Id,  "GB24E50001", null,          null, 0,   null,      284, 8),
        });

        // Race 4 — Newcastle 2025 (430 km, release 07:00)
        var r4 = new DateTime(2025, 6, 7, 7, 0, 0, DateTimeKind.Utc);
        AddResults(db, Race4Id, cat4.Id, 430.0, r4, new (Guid, string, string?, string?, int, Guid?, int, int)[]
        {
            (FancierId,  "GB25A10001", "Sky King",    "M", 2023, Pigeon1Id, 374, 1),
            (FancierId,  "GB25A10002", "Blue Streak", "F", 2022, Pigeon2Id, 380, 2),
            (FancierId,  "GB25A10003", "Pied Racer",  "M", 2024, Pigeon3Id, 387, 3),
            (OtherF1Id,  "GB21B20001", null,          null, 0,   null,      395, 4),
            (OtherF1Id,  "GB21B20002", null,          null, 0,   null,      406, 5),
            (OtherF2Id,  "GB22C30001", null,          null, 0,   null,      418, 6),
            (OtherF3Id,  "GB23D40001", null,          null, 0,   null,      430, 7),
            (OtherF4Id,  "GB24E50001", null,          null, 0,   null,      443, 8),
        });

        // Race 5 — Edinburgh 2025 (530 km, release 06:00)
        var r5 = new DateTime(2025, 6, 28, 6, 0, 0, DateTimeKind.Utc);
        AddResults(db, Race5Id, cat5.Id, 530.0, r5, new (Guid, string, string?, string?, int, Guid?, int, int)[]
        {
            (FancierId,  "GB25A10001", "Sky King",    "M", 2023, Pigeon1Id, 491, 1),
            (FancierId,  "GB25A10002", "Blue Streak", "F", 2022, Pigeon2Id, 500, 2),
            (FancierId,  "GB25A10003", "Pied Racer",  "M", 2024, Pigeon3Id, 510, 3),
            (OtherF1Id,  "GB21B20001", null,          null, 0,   null,      520, 4),
            (OtherF1Id,  "GB21B20002", null,          null, 0,   null,      530, 5),
            (OtherF2Id,  "GB22C30001", null,          null, 0,   null,      541, 6),
            (OtherF3Id,  "GB23D40001", null,          null, 0,   null,      552, 7),
            (OtherF4Id,  "GB24E50001", null,          null, 0,   null,      564, 8),
        });
    }

    // ── 2026 races (3 published + 2 scheduled) ───────────────────────────────────

    private static void SeedRaces2026(RaceDbContext db)
    {
        var cat6  = AddRace(db, Race6Id,  "Bristol Sprint",      new DateTime(2026, 3, 22, 9,  0, 0, DateTimeKind.Utc), 51.4545, -2.5879, "Bristol",   190.0, RaceStatus.Published,  Prog2026Id, "2026 Season Programme");
        var cat7  = AddRace(db, Race7Id,  "Exeter Classic",      new DateTime(2026, 4,  5, 8,  0, 0, DateTimeKind.Utc), 50.7184, -3.5339, "Exeter",    270.0, RaceStatus.Published,  Prog2026Id, "2026 Season Programme");
        var cat8  = AddRace(db, Race8Id,  "York Open",           new DateTime(2026, 4, 19, 8, 30, 0, DateTimeKind.Utc), 53.9599, -1.0873, "York",      310.0, RaceStatus.Published,  Prog2026Id, "2026 Season Programme");
        _         = AddRace(db, Race9Id,  "Newcastle Challenge", new DateTime(2026, 5, 17, 7,  0, 0, DateTimeKind.Utc), 54.9783, -1.6178, "Newcastle", 430.0, RaceStatus.Scheduled,  Prog2026Id, "2026 Season Programme");
        _         = AddRace(db, Race10Id, "Edinburgh Classic",   new DateTime(2026, 6,  7, 6,  0, 0, DateTimeKind.Utc), 55.9533, -3.1883, "Edinburgh", 530.0, RaceStatus.Scheduled,  Prog2026Id, "2026 Season Programme");

        // Race 6 — Bristol 2026 (190 km, release 09:00)
        var r6 = new DateTime(2026, 3, 22, 9, 0, 0, DateTimeKind.Utc);
        AddResults(db, Race6Id, cat6.Id, 190.0, r6, new (Guid, string, string?, string?, int, Guid?, int, int)[]
        {
            (FancierId,  "GB25A10001", "Sky King",    "M", 2023, Pigeon1Id, 144, 1),
            (FancierId,  "GB25A10002", "Blue Streak", "F", 2022, Pigeon2Id, 148, 2),
            (FancierId,  "GB25A10003", "Pied Racer",  "M", 2024, Pigeon3Id, 151, 3),
            (OtherF1Id,  "GB21B20001", null,          null, 0,   null,      154, 4),
            (OtherF1Id,  "GB21B20002", null,          null, 0,   null,      157, 5),
            (OtherF2Id,  "GB22C30001", null,          null, 0,   null,      161, 6),
            (OtherF3Id,  "GB23D40001", null,          null, 0,   null,      165, 7),
            (OtherF4Id,  "GB24E50001", null,          null, 0,   null,      169, 8),
        });

        // Race 7 — Exeter 2026 (270 km, release 08:00)
        var r7 = new DateTime(2026, 4, 5, 8, 0, 0, DateTimeKind.Utc);
        AddResults(db, Race7Id, cat7.Id, 270.0, r7, new (Guid, string, string?, string?, int, Guid?, int, int)[]
        {
            (FancierId,  "GB25A10001", "Sky King",    "M", 2023, Pigeon1Id, 208, 1),
            (FancierId,  "GB25A10002", "Blue Streak", "F", 2022, Pigeon2Id, 213, 2),
            (FancierId,  "GB25A10003", "Pied Racer",  "M", 2024, Pigeon3Id, 218, 3),
            (OtherF1Id,  "GB21B20001", null,          null, 0,   null,      222, 4),
            (OtherF1Id,  "GB21B20002", null,          null, 0,   null,      228, 5),
            (OtherF2Id,  "GB22C30001", null,          null, 0,   null,      234, 6),
            (OtherF3Id,  "GB23D40001", null,          null, 0,   null,      241, 7),
            (OtherF4Id,  "GB24E50001", null,          null, 0,   null,      248, 8),
        });

        // Race 8 — York 2026 (310 km, release 08:30) — P2 wins this one
        var r8 = new DateTime(2026, 4, 19, 8, 30, 0, DateTimeKind.Utc);
        AddResults(db, Race8Id, cat8.Id, 310.0, r8, new (Guid, string, string?, string?, int, Guid?, int, int)[]
        {
            (FancierId,  "GB25A10002", "Blue Streak", "F", 2022, Pigeon2Id, 243, 1),
            (FancierId,  "GB25A10001", "Sky King",    "M", 2023, Pigeon1Id, 245, 2),
            (FancierId,  "GB25A10003", "Pied Racer",  "M", 2024, Pigeon3Id, 250, 3),
            (OtherF1Id,  "GB21B20001", null,          null, 0,   null,      254, 4),
            (OtherF1Id,  "GB21B20002", null,          null, 0,   null,      261, 5),
            (OtherF2Id,  "GB22C30001", null,          null, 0,   null,      268, 6),
            (OtherF3Id,  "GB23D40001", null,          null, 0,   null,      275, 7),
            (OtherF4Id,  "GB24E50001", null,          null, 0,   null,      283, 8),
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static RaceCategory AddRace(RaceDbContext db, Guid id, string name,
        DateTime scheduledRelease, double relLat, double relLon, string relLocation,
        double distKm, RaceStatus status, Guid? programmeId = null, string? programmeName = null)
    {
        var isCompleted = status is RaceStatus.Published or RaceStatus.Completed;
        var race = new Race
        {
            Id                    = id,
            ClubId                = ClubId,
            FederationId          = FederationId,
            ProgrammeId           = programmeId,
            ProgrammeName         = programmeName,
            ClubName              = "Demo Racing Club",
            ClubLatitude          = LoftLat,
            ClubLongitude         = LoftLon,
            Name                  = name,
            Status                = status,
            ReleaseLocation       = relLocation,
            ReleaseLatitude       = relLat,
            ReleaseLongitude      = relLon,
            ScheduledReleaseTime  = scheduledRelease,
            ActualReleaseTime     = isCompleted ? scheduledRelease : null,
            NominatedDistanceKm   = distKm,
            TotalPigeonsEntered   = isCompleted ? 8 : null,
            CompletedAt           = isCompleted ? scheduledRelease.AddHours(10) : null,
            PublishedAt           = status == RaceStatus.Published ? scheduledRelease.AddDays(1) : null,
            WindSpeedKmh          = isCompleted ? 18.0 : null,
            WindDirection         = isCompleted ? WindDirection.SW : null,
            TemperatureCelsius    = isCompleted ? 16.0 : null,
        };
        db.Races.Add(race);

        var category = new RaceCategory { RaceId = id, Name = "Open", SortOrder = 1 };
        db.RaceCategories.Add(category);
        return category;
    }

    private static void AddResults(RaceDbContext db, Guid raceId, Guid categoryId, double distKm,
        DateTime release, (Guid userId, string ring, string? pigName, string? sex, int yob, Guid? pigeonId, int durationMins, int rank)[] entries)
    {
        foreach (var (userId, ring, pigName, sex, yob, pigeonId, durationMins, rank) in entries)
        {
            var speed    = distKm * 1000.0 / durationMins;
            var arrival  = release.AddMinutes(durationMins);
            db.RaceResults.Add(new RaceResult
            {
                RaceId            = raceId,
                CategoryId        = categoryId,
                UserId            = userId,
                RingNumber        = ring,
                PigeonName        = pigName,
                PigeonSex         = sex,
                PigeonYearOfBirth = yob > 0 ? yob : null,
                PigeonId          = pigeonId,
                ArrivalTime       = arrival,
                FlightDuration    = TimeSpan.FromMinutes(durationMins),
                DistanceKm        = distKm,
                SpeedMperMin      = Math.Round(speed, 2),
                ClubRank          = rank,
                CategoryRank      = rank,
                Status            = ResultStatus.Published,
                IngestionType     = DataIngestionType.Manual,
            });
        }
    }
}
