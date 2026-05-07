using PigeonRacing.Domain.Entities;
using PigeonRacing.Domain.Enums;
using PigeonRacing.Infrastructure.Persistence;

namespace PigeonRacing.Infrastructure.Templates;

/// <summary>
/// Seeds all 160 system templates into the database.
/// Called once on first startup; idempotent (checks by IsSystem + Category + SortOrder).
/// </summary>
public static class TemplateSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Only seed if no system templates exist
        if (db.PrintTemplates.Any(t => t.IsSystem)) return;

        var templates = BuildAll();
        db.PrintTemplates.AddRange(templates);
        await db.SaveChangesAsync();
    }

    private static List<PrintTemplate> BuildAll()
    {
        var list = new List<PrintTemplate>();
        int sortOrder = 1;

        // ── Race Results (50) ────────────────────────────────────────────────

        // Manually coded core templates (1-10)
        var rrCore = new[]
        {
            ("RR-01","Classic Navy Table",    TemplateStyle.Classic,    TemplatePaperSize.A4Portrait,  TemplateColourScheme.Navy,  "#1E3A5F","#C9A84C", TemplateLibrary.RR01),
            ("RR-02","Gold & Black Landscape",TemplateStyle.Elegant,    TemplatePaperSize.A4Landscape, TemplateColourScheme.Gold,  "#1a1a1a","#C9A84C", TemplateLibrary.RR02),
            ("RR-03","Green Federation",      TemplateStyle.Classic,    TemplatePaperSize.A4Portrait,  TemplateColourScheme.Light, "#2D6A4F","#F4A261", TemplateLibrary.RR03),
            ("RR-04","Crimson Compact",       TemplateStyle.Sporty,     TemplatePaperSize.A4Portrait,  TemplateColourScheme.Crimson,"#C1121F","#2B2D42",TemplateLibrary.RR04),
            ("RR-05","Ivory & Gold Classic",  TemplateStyle.Elegant,    TemplatePaperSize.A4Portrait,  TemplateColourScheme.Gold,  "#1E3A5F","#C9A84C", TemplateLibrary.RR05),
            ("RR-06","Dark Mode Landscape",   TemplateStyle.Dark,       TemplatePaperSize.A4Landscape, TemplateColourScheme.Dark,  "#0D1B2A","#1E90FF", TemplateLibrary.RR06),
            ("RR-07","Minimal Whitespace",    TemplateStyle.Minimal,    TemplatePaperSize.A4Portrait,  TemplateColourScheme.Monochrome,"#111111","#CCCCCC",TemplateLibrary.RR07),
            ("RR-08","Sporty Bold Stripes",   TemplateStyle.Sporty,     TemplatePaperSize.A4Landscape, TemplateColourScheme.Crimson,"#E63946","#1a1a1a",TemplateLibrary.RR08),
            ("RR-09","Royal Purple",          TemplateStyle.Elegant,    TemplatePaperSize.A4Portrait,  TemplateColourScheme.Light, "#4A0E8F","#D4A017", TemplateLibrary.RR09),
            ("RR-10","Teal & White Modern",   TemplateStyle.Modern,     TemplatePaperSize.A4Portrait,  TemplateColourScheme.Light, "#0077B6","#00B4D8", TemplateLibrary.RR10),
        };

        foreach (var (id, name, style, paper, scheme, primary, accent, html) in rrCore)
        {
            list.Add(MakeTemplate(id, name, TemplateCategory.RaceResults, style, paper, scheme, primary, accent, html, sortOrder++));
        }

        // Extra colour variants (11-25)
        foreach (var (id, name, style, html) in TemplateLibrary.RaceResultTemplatesExtra)
        {
            list.Add(MakeTemplate(id, name, TemplateCategory.RaceResults, ParseStyle(style),
                TemplatePaperSize.A4Portrait, TemplateColourScheme.Light, "#333","#666", html, sortOrder++));
        }

        // Additional variants (26-50)
        foreach (var (id, name, style, html) in TemplateLibrary.RaceResultTemplates26To50)
        {
            list.Add(MakeTemplate(id, name, TemplateCategory.RaceResults, ParseStyle(style),
                id.Contains("Wide") || id.Contains("Landscape") ? TemplatePaperSize.A4Landscape : TemplatePaperSize.A4Portrait,
                TemplateColourScheme.Light, "#333", "#666", html, sortOrder++));
        }

        sortOrder = 100; // reset for next category

        // ── Best Loft (20) ───────────────────────────────────────────────────
        foreach (var (id, name, style, html) in TemplateLibrary.BestLoftTemplates)
        {
            list.Add(MakeTemplate(id, name, TemplateCategory.BestLoft, ParseStyle(style),
                id.Contains("Landscape") || id.Contains("Wide") ? TemplatePaperSize.A4Landscape : TemplatePaperSize.A4Portrait,
                TemplateColourScheme.Light, "#333", "#666", html, sortOrder++));
        }

        sortOrder = 200;

        // ── Ace Pigeon (20) ──────────────────────────────────────────────────
        foreach (var (id, name, style, html) in TemplateLibrary.AcePigeonTemplates)
        {
            list.Add(MakeTemplate(id, name, TemplateCategory.AcePigeon, ParseStyle(style),
                id.Contains("Landscape") || id.Contains("Wide") ? TemplatePaperSize.A4Landscape : TemplatePaperSize.A4Portrait,
                TemplateColourScheme.Light, "#333", "#666", html, sortOrder++));
        }

        sortOrder = 300;

        // ── Super Ace (20) ───────────────────────────────────────────────────
        foreach (var (id, name, style, html) in TemplateLibrary.SuperAceTemplates)
        {
            list.Add(MakeTemplate(id, name, TemplateCategory.SuperAcePigeon, ParseStyle(style),
                id.Contains("Landscape") || id.Contains("Wide") ? TemplatePaperSize.A4Landscape : TemplatePaperSize.A4Portrait,
                TemplateColourScheme.Light, "#333", "#666", html, sortOrder++));
        }

        sortOrder = 400;

        // ── Certificates (50) ────────────────────────────────────────────────
        foreach (var (id, name, style, html) in TemplateLibrary.CertificateTemplates)
        {
            list.Add(MakeTemplate(id, name, TemplateCategory.Certificate, ParseStyle(style),
                id.Contains("Landscape") ? TemplatePaperSize.A4Landscape : TemplatePaperSize.A4Portrait,
                TemplateColourScheme.Light, "#1E3A5F", "#C9A84C", html, sortOrder++));
        }

        return list;
    }

    private static PrintTemplate MakeTemplate(
        string id, string name, TemplateCategory category,
        TemplateStyle style, TemplatePaperSize paper, TemplateColourScheme scheme,
        string primary, string accent, string html, int sortOrder) => new()
    {
        Id = DeterministicGuid(id),
        Name = name,
        Description = $"{category} template — {style} style",
        Category = category,
        Style = style,
        PaperSize = paper,
        ColourScheme = scheme,
        PrimaryColour = primary,
        SecondaryColour = accent,
        HtmlTemplate = html,
        ThumbnailUrl = $"/assets/templates/thumbs/{id.ToLower().Replace("-","_")}.jpg",
        IsActive = true,
        IsSystem = true,
        SortOrder = sortOrder,
        MaxRows = 0,
        IsMultiPage = false,
        VariableSchemaJson = BuildVariableSchema(category)
    };

    private static Guid DeterministicGuid(string seed)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(
            System.Text.Encoding.UTF8.GetBytes("pigeon-racing:" + seed));
        bytes[6] = (byte)((bytes[6] & 0x0f) | 0x30); // version 3
        bytes[8] = (byte)((bytes[8] & 0x3f) | 0x80); // variant
        return new Guid(bytes);
    }

    private static TemplateStyle ParseStyle(string s) => s switch
    {
        "Classic"   => TemplateStyle.Classic,
        "Modern"    => TemplateStyle.Modern,
        "Elegant"   => TemplateStyle.Elegant,
        "Minimal"   => TemplateStyle.Minimal,
        "Sporty"    => TemplateStyle.Sporty,
        "Heritage"  => TemplateStyle.Heritage,
        "Corporate" => TemplateStyle.Corporate,
        "Vibrant"   => TemplateStyle.Vibrant,
        "Dark"      => TemplateStyle.Dark,
        "Branded"   => TemplateStyle.Branded,
        _           => TemplateStyle.Classic
    };

    private static string BuildVariableSchema(TemplateCategory cat)
    {
        var common = @"{
            ""club"": {""name"":"""",""logoUrl"":"""",""primaryColour"":"""",""secondaryColour"":""""},
            ""season"": """",
            ""printDate"": """"
        }";

        return cat switch
        {
            TemplateCategory.RaceResults => @"{
                ""race"": {""name"":"""",""releaseLocation"":"""",""date"":"""",""releaseTime"":"""",""distance"":"""",""totalEntries"":"""",""wind"":"""",""temperature"":""""},
                ""results"": [{""rank"":0,""ringNumber"":"""",""pigeonName"":"""",""pigeonSex"":"""",""pigeonYear"":0,""fancierName"":"""",""arrivalTime"":"""",""distanceKm"":"""",""velocityMperMin"":"""",""velocityKmH"":"""",""categoryName"":""""}],
                ""club"": {""name"":"""",""logoUrl"":"""",""primaryColour"":"""",""secondaryColour"":""""},
                ""season"":"""", ""printDate"":""""
            }",
            TemplateCategory.BestLoft => @"{
                ""programme"": {""name"":"""",""year"":0,""scoringMethod"":"""",""raceCount"":0},
                ""results"": [{""loftRank"":0,""fancierName"":"""",""racesEntered"":0,""pigeonsEntered"":0,""bestSingleVelocityMperMin"":"""",""averageVelocityMperMin"":"""",""totalScore"":"""",""averageScore"":""""}],
                ""totalLofts"": 0,
                ""club"": {""name"":"""",""logoUrl"":"""",""primaryColour"":"""",""secondaryColour"":""""},
                ""season"":"""", ""printDate"":""""
            }",
            TemplateCategory.AcePigeon => @"{
                ""programme"": {""name"":"""",""year"":0,""scoringMethod"":"""",""raceCount"":0,""acePigeonMinRaces"":0},
                ""results"": [{""aceRank"":0,""ringNumber"":"""",""pigeonName"":"""",""pigeonSex"":"""",""pigeonYearOfBirth"":0,""fancierName"":"""",""racesEntered"":0,""racesInProgramme"":0,""participationRate"":"""",""bestVelocityMperMin"":"""",""averageVelocityMperMin"":"""",""totalScore"":""""}],
                ""totalPigeons"": 0,
                ""club"": {""name"":"""",""logoUrl"":"""",""primaryColour"":"""",""secondaryColour"":""""},
                ""season"":"""", ""printDate"":""""
            }",
            TemplateCategory.SuperAcePigeon => @"{
                ""programme"": {""name"":"""",""year"":0,""scoringMethod"":"""",""raceCount"":0,""superAceQualification"":""""},
                ""results"": [{""superAceRank"":0,""ringNumber"":"""",""pigeonName"":"""",""pigeonSex"":"""",""pigeonYearOfBirth"":0,""fancierName"":"""",""racesEntered"":0,""racesInProgramme"":0,""participationRate"":"""",""bestVelocityMperMin"":"""",""averageVelocityMperMin"":"""",""bestClubRank"":0,""totalScore"":""""}],
                ""totalQualifiers"": 0,
                ""club"": {""name"":"""",""logoUrl"":"""",""primaryColour"":"""",""secondaryColour"":""""},
                ""season"":"""", ""printDate"":""""
            }",
            TemplateCategory.Certificate => @"{
                ""certificate"": {""recipientName"":"""",""rank"":"""",""achievement"":"""",""ringNumber"":"""",""pigeonName"":"""",""pigeonSex"":"""",""velocityMperMin"":"""",""distanceKm"":"""",""arrivalTime"":"""",""raceName"":"""",""aceRank"":0,""superAceRank"":0,""loftRank"":0,""totalScore"":"""",""racesEntered"":0,""racesInProgramme"":0,""pigeonsEntered"":0,""bestVelocityMperMin"":""""},
                ""race"": {""name"":"""",""date"":"""",""releaseLocation"":""""},
                ""programme"": {""name"":"""",""year"":0,""superAceQualification"":""""},
                ""club"": {""name"":"""",""logoUrl"":"""",""primaryColour"":"""",""secondaryColour"":""""},
                ""season"":"""", ""printDate"":""""
            }",
            _ => common
        };
    }
}
