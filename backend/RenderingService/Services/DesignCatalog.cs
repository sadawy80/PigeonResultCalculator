namespace PRC.RenderingService.Services;

public record DesignInfo(string Id, string Name, bool Arabic);

public record DesignCatalogue(IReadOnlyList<DesignInfo> Portrait, IReadOnlyList<DesignInfo> Landscape);

/// <summary>
/// Hardcoded list of design IDs supported by each bundled template. The designs
/// themselves are CSS classes inside the template HTML; this catalogue tells the
/// front-end which IDs are valid so users can pick one. Names are taken from the
/// spec docs (RACE_CERT_PROD_SPEC.md etc.).
/// </summary>
public static class DesignCatalog
{
    // ── Certificates ────────────────────────────────────────────────────────

    private static readonly DesignInfo[] RaceCertPortrait =
    [
        new("R1",  "Classic Cream Diploma",   false),
        new("R2",  "Royal Trophy Gold",       false),
        new("R3",  "Elite Black & Gold",      false),
        new("R4",  "Editorial Italic",        false),
        new("R5",  "Minimal Swiss",           false),
        new("R6",  "Aviation Cockpit",        false),
        new("R7",  "Art Deco Gold",           false),
        new("R8",  "Vintage Trophy",          false),
        new("R9",  "Premium Pastel",          false),
        new("R10", "Carbon Premium",          false),
        new("AR-R1", "Kaaba Gold",            true),
        new("AR-R2", "Ivory Calligraphic",    true),
        new("AR-R3", "Modern Kufi Mihrab",    true),
    ];

    private static readonly DesignInfo[] RaceCertLandscape =
        SuffixLandscape(RaceCertPortrait);

    private static readonly DesignInfo[] AceCertPortrait =
    [
        new("A1",  "Bronze Medal Classic",    false),
        new("A2",  "Silver Royal",            false),
        new("A3",  "Gold Crown",              false),
        new("A4",  "Editorial Modern",        false),
        new("A5",  "Sage Minimal",            false),
        new("A6",  "Vintage Achievement",     false),
        new("A7",  "Burgundy Elite",          false),
        new("A8",  "Carbon Elite",            false),
        new("A9",  "Couture Ivory",           false),
        new("A10", "Cosmic Violet",           false),
        new("AR-A1", "Kaaba Gold",            true),
        new("AR-A2", "Ivory Calligraphic",    true),
        new("AR-A3", "Modern Kufi Mihrab",    true),
    ];

    private static readonly DesignInfo[] AceCertLandscape =
        SuffixLandscape(AceCertPortrait);

    private static readonly DesignInfo[] SuperAceCertPortrait =
    [
        new("S1",  "Imperial Gold",           false),
        new("S2",  "Maison Noir Premium",     false),
        new("S3",  "Burgundy Regalia",        false),
        new("S4",  "Emerald Throne",          false),
        new("S5",  "Platinum Edge",           false),
        new("S6",  "Couture Ivory",           false),
        new("S7",  "Carbon Premium",          false),
        new("S8",  "Art Deco Master",         false),
        new("S9",  "Atelier Cream",           false),
        new("S10", "Cosmic Luxe",             false),
        new("AR-S1", "Kaaba Gold",            true),
        new("AR-S2", "Ivory Calligraphic",    true),
        new("AR-S3", "Modern Kufi Mihrab",    true),
    ];

    private static readonly DesignInfo[] SuperAceCertLandscape =
        SuffixLandscape(SuperAceCertPortrait);

    private static readonly DesignInfo[] BestLoftCertPortrait =
    [
        new("L1",  "Cream Classical",         false),
        new("L2",  "Royal Navy & Gold",       false),
        new("L3",  "Elite Black & Gold",      false),
        new("L4",  "Editorial Green",         false),
        new("L5",  "Trophy Green",            false),
        new("L6",  "Burgundy Royale",         false),
        new("L7",  "Champion Federation",     false),
        new("L8",  "Carbon Premium",          false),
        new("L9",  "Ivory Italiana",          false),
        new("L10", "Cosmic Champion",         false),
        new("AR-L1", "Kaaba Gold",            true),
        new("AR-L2", "Ivory Calligraphic",    true),
        new("AR-L3", "Modern Kufi Mihrab",    true),
    ];

    private static readonly DesignInfo[] BestLoftCertLandscape =
        SuffixLandscape(BestLoftCertPortrait);

    // ── Result tables (portrait only) ───────────────────────────────────────

    private static readonly DesignInfo[] RaceResultDesigns =
        Enumerable.Range(1, 20).Select(i => new DesignInfo($"T{i}", $"Race Results T{i}", false)).ToArray();

    private static readonly DesignInfo[] AceResultDesigns =
        Enumerable.Range(1, 10).Select(i => new DesignInfo($"A{i}", $"Ace Result A{i}", false))
            .Concat(Enumerable.Range(1, 3).Select(i => new DesignInfo($"AR-A{i}", $"Ace Arabic AR-A{i}", true)))
            .ToArray();

    private static readonly DesignInfo[] SuperAceResultDesigns =
        Enumerable.Range(1, 10).Select(i => new DesignInfo($"SA{i}", $"Super Ace SA{i}", false))
            .Concat(Enumerable.Range(1, 3).Select(i => new DesignInfo($"AR-SA{i}", $"Super Ace Arabic AR-SA{i}", true)))
            .ToArray();

    private static readonly DesignInfo[] BestLoftResultDesigns =
        Enumerable.Range(1, 10).Select(i => new DesignInfo($"L{i}", $"Best Loft L{i}", false))
            .Concat(Enumerable.Range(1, 3).Select(i => new DesignInfo($"AR-L{i}", $"Best Loft Arabic AR-L{i}", true)))
            .ToArray();

    // ── Public lookups ──────────────────────────────────────────────────────

    public static DesignCatalogue GetCert(string certType) => certType.ToLowerInvariant() switch
    {
        "race"      => new(RaceCertPortrait,     RaceCertLandscape),
        "ace"       => new(AceCertPortrait,      AceCertLandscape),
        "super-ace" => new(SuperAceCertPortrait, SuperAceCertLandscape),
        "superace"  => new(SuperAceCertPortrait, SuperAceCertLandscape),
        "best-loft" => new(BestLoftCertPortrait, BestLoftCertLandscape),
        "bestloft"  => new(BestLoftCertPortrait, BestLoftCertLandscape),
        _ => throw new ArgumentException($"Unknown cert type '{certType}'", nameof(certType))
    };

    public static IReadOnlyList<DesignInfo> GetResult(string resultType) => resultType.ToLowerInvariant() switch
    {
        "race"      => RaceResultDesigns,
        "ace"       => AceResultDesigns,
        "super-ace" => SuperAceResultDesigns,
        "superace"  => SuperAceResultDesigns,
        "best-loft" => BestLoftResultDesigns,
        "bestloft"  => BestLoftResultDesigns,
        _ => throw new ArgumentException($"Unknown result type '{resultType}'", nameof(resultType))
    };

    private static DesignInfo[] SuffixLandscape(DesignInfo[] portrait) =>
        portrait.Select(d => d with { Id = d.Id + "L" }).ToArray();
}
