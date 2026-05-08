using PRC.Common;
using PRC.RenderingService.Models;
using Microsoft.EntityFrameworkCore;

namespace PRC.RenderingService.Data;

public static class TemplateSeeder
{
    public static async Task SeedAsync(RenderingDbContext db)
    {
        if (await db.PrintTemplates.AnyAsync(t => t.IsSystem)) return;

        var templates = BuildAll();
        db.PrintTemplates.AddRange(templates);
        await db.SaveChangesAsync();
    }

    private static List<PrintTemplate> BuildAll()
    {
        var list = new List<PrintTemplate>();
        int sortOrder = 1;

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
            list.Add(MakeTemplate(id, name, TemplateCategory.RaceResults, style, paper, scheme, primary, accent, html, sortOrder++));

        foreach (var (id, name, style, html) in TemplateLibrary.RaceResultTemplatesExtra)
            list.Add(MakeTemplate(id, name, TemplateCategory.RaceResults, ParseStyle(style),
                TemplatePaperSize.A4Portrait, TemplateColourScheme.Light, "#333","#666", html, sortOrder++));

        foreach (var (id, name, style, html) in TemplateLibrary.RaceResultTemplates26To50)
            list.Add(MakeTemplate(id, name, TemplateCategory.RaceResults, ParseStyle(style),
                id.Contains("Wide") || id.Contains("Landscape") ? TemplatePaperSize.A4Landscape : TemplatePaperSize.A4Portrait,
                TemplateColourScheme.Light, "#333", "#666", html, sortOrder++));

        sortOrder = 100;
        foreach (var (id, name, style, html) in TemplateLibrary.BestLoftTemplates)
            list.Add(MakeTemplate(id, name, TemplateCategory.BestLoft, ParseStyle(style),
                id.Contains("Landscape") || id.Contains("Wide") ? TemplatePaperSize.A4Landscape : TemplatePaperSize.A4Portrait,
                TemplateColourScheme.Light, "#333", "#666", html, sortOrder++));

        sortOrder = 200;
        foreach (var (id, name, style, html) in TemplateLibrary.AcePigeonTemplates)
            list.Add(MakeTemplate(id, name, TemplateCategory.AcePigeon, ParseStyle(style),
                id.Contains("Landscape") || id.Contains("Wide") ? TemplatePaperSize.A4Landscape : TemplatePaperSize.A4Portrait,
                TemplateColourScheme.Light, "#333", "#666", html, sortOrder++));

        sortOrder = 300;
        foreach (var (id, name, style, html) in TemplateLibrary.SuperAceTemplates)
            list.Add(MakeTemplate(id, name, TemplateCategory.SuperAcePigeon, ParseStyle(style),
                id.Contains("Landscape") || id.Contains("Wide") ? TemplatePaperSize.A4Landscape : TemplatePaperSize.A4Portrait,
                TemplateColourScheme.Light, "#333", "#666", html, sortOrder++));

        sortOrder = 400;
        foreach (var (id, name, style, html) in TemplateLibrary.CertificateTemplates)
            list.Add(MakeTemplate(id, name, TemplateCategory.Certificate, ParseStyle(style),
                id.Contains("Landscape") ? TemplatePaperSize.A4Landscape : TemplatePaperSize.A4Portrait,
                TemplateColourScheme.Light, "#333", "#666", html, sortOrder++));

        return list;
    }

    private static PrintTemplate MakeTemplate(
        string code, string name, TemplateCategory category,
        TemplateStyle style, TemplatePaperSize paper, TemplateColourScheme scheme,
        string primary, string accent, string html, int sortOrder) => new()
    {
        Id             = Guid.NewGuid(),
        Name           = name,
        Description    = $"{code} — {name}",
        Category       = category,
        Style          = style,
        PaperSize      = paper,
        ColourScheme   = scheme,
        PrimaryColour  = primary,
        SecondaryColour= accent,
        HtmlTemplate   = html,
        SortOrder      = sortOrder,
        IsSystem       = true,
        IsActive       = true
    };

    private static TemplateStyle ParseStyle(string s) => s switch
    {
        "Classic"   => TemplateStyle.Classic,
        "Elegant"   => TemplateStyle.Elegant,
        "Modern"    => TemplateStyle.Modern,
        "Minimal"   => TemplateStyle.Minimal,
        "Sporty"    => TemplateStyle.Sporty,
        "Dark"      => TemplateStyle.Dark,
        "Heritage"  => TemplateStyle.Heritage,
        "Corporate" => TemplateStyle.Corporate,
        "Branded"   => TemplateStyle.Branded,
        "Vibrant"   => TemplateStyle.Vibrant,
        _           => TemplateStyle.Classic
    };
}
