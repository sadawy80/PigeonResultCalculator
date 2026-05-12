using System.Text.Json;
using System.Text.Json.Nodes;
using MassTransit;
using PRC.Common.Messages;
using PRC.RenderingService.Models;

namespace PRC.RenderingService.Services;

public interface IPrintOrchestrator
{
    // ── Certificates ────────────────────────────────────────────────────────
    Task<byte[]> RenderRaceCertAsync(Guid raceResultId, string designId, string lang, CancellationToken ct);
    Task<byte[]> RenderAceCertAsync(Guid programmeId, string ringNumber, string designId, string lang, CancellationToken ct);
    Task<byte[]> RenderSuperAceCertAsync(Guid programmeId, string ringNumber, string designId, string lang, CancellationToken ct);
    Task<byte[]> RenderBestLoftCertAsync(Guid programmeId, Guid fancierUserId, string designId, string lang, CancellationToken ct);

    // ── Result tables (PDF) ────────────────────────────────────────────────
    Task<byte[]> RenderRaceResultsPdfAsync(Guid raceId, string designId, string lang, CancellationToken ct);
    Task<byte[]> RenderAceResultsPdfAsync(Guid programmeId, string designId, string lang, CancellationToken ct);
    Task<byte[]> RenderSuperAceResultsPdfAsync(Guid programmeId, string designId, string lang, CancellationToken ct);
    Task<byte[]> RenderBestLoftResultsPdfAsync(Guid programmeId, string designId, string lang, CancellationToken ct);

    // ── Result tables (Excel) ──────────────────────────────────────────────
    Task<byte[]> RenderRaceResultsExcelAsync(Guid raceId, string lang, CancellationToken ct);
    Task<byte[]> RenderAceResultsExcelAsync(Guid programmeId, string lang, CancellationToken ct);
    Task<byte[]> RenderSuperAceResultsExcelAsync(Guid programmeId, string lang, CancellationToken ct);
    Task<byte[]> RenderBestLoftResultsExcelAsync(Guid programmeId, string lang, CancellationToken ct);
}

/// <summary>
/// Bridges domain entities (raceId, programmeId, raceResultId) into the JSON
/// payload shape that the bundled templates expect, then delegates to the
/// file-based <see cref="ICertRenderer"/> / <see cref="IResultRenderer"/> /
/// <see cref="IResultExcelExporter"/>. All data fetching happens here via the
/// existing MassTransit request clients so the front-end only needs entity IDs.
/// </summary>
public class PrintOrchestrator : IPrintOrchestrator
{
    private static readonly string[] AllLangs = ["en", "ar", "fa", "es", "de", "zh"];

    private readonly IRequestClient<GetRaceForRenderRequest> _raceClient;
    private readonly IRequestClient<GetRaceResultForRenderRequest> _resultClient;
    private readonly IRequestClient<GetClubBrandingRequest> _brandingClient;
    private readonly IRequestClient<GetUserNamesRequest> _userNamesClient;
    private readonly IRequestClient<GetProgrammeForRenderRequest> _programmeClient;

    private readonly ICertRenderer _certRenderer;
    private readonly IResultRenderer _resultRenderer;
    private readonly IResultExcelExporter _excel;

    public PrintOrchestrator(
        IRequestClient<GetRaceForRenderRequest> raceClient,
        IRequestClient<GetRaceResultForRenderRequest> resultClient,
        IRequestClient<GetClubBrandingRequest> brandingClient,
        IRequestClient<GetUserNamesRequest> userNamesClient,
        IRequestClient<GetProgrammeForRenderRequest> programmeClient,
        ICertRenderer certRenderer,
        IResultRenderer resultRenderer,
        IResultExcelExporter excel)
    {
        _raceClient       = raceClient;
        _resultClient     = resultClient;
        _brandingClient   = brandingClient;
        _userNamesClient  = userNamesClient;
        _programmeClient  = programmeClient;
        _certRenderer     = certRenderer;
        _resultRenderer   = resultRenderer;
        _excel            = excel;
    }

    // ═══ CERTIFICATES ═══════════════════════════════════════════════════════

    public async Task<byte[]> RenderRaceCertAsync(Guid raceResultId, string designId, string lang, CancellationToken ct)
    {
        var rr = await Ask<GetRaceResultForRenderRequest, RaceResultForRenderResult>(
            _resultClient, new GetRaceResultForRenderRequest(raceResultId), ct)
            ?? throw new InvalidOperationException("Race result not found.");
        if (!rr.Found) throw new InvalidOperationException("Race result not found.");

        var branding = await Ask<GetClubBrandingRequest, ClubBrandingResult>(
            _brandingClient, new GetClubBrandingRequest(rr.ClubId), ct);
        var fancier = await ResolveUserName(rr.UserId, ct);

        var data = new JsonObject
        {
            ["meta"] = MetaTranslated(
                eyebrow:  "Certificate of Race Performance",
                title:    "Race Result",
                subtitle: rr.RaceName ?? "",
                logoUrl:  branding?.LogoUrl,
                qrContent: $"verify/race/{raceResultId}"),
            ["loft"] = LoftBlock(branding, fancier),
            ["bird"] = BirdHeroBlock(
                name:     rr.PigeonName ?? rr.RingNumber,
                ring:     rr.RingNumber,
                citation: $"For an outstanding performance in {rr.RaceName} on {rr.RaceDate?.ToString("dd MMM yyyy") ?? ""}."),
            ["stats"] = new JsonObject
            {
                ["position"] = OrdinalRank(rr.ClubRank ?? 0),
                ["velocity"] = $"{Math.Round(rr.SpeedMperMin, 0)} mpm",
                ["distance"] = $"{Math.Round(rr.DistanceKm, 1)} km",
                ["time"]     = rr.ArrivalTime.ToString("HH:mm:ss")
            },
            ["meta_row"] = SpreadTranslated(new()
            {
                ["date"]       = rr.RaceDate?.ToString("dd MMM yyyy") ?? "",
                ["birds"]      = "",
                ["federation"] = ""
            }, scalarKeys: ["date", "birds"]),
            ["sig_left"]  = SigBlock("Race Secretary"),
            ["sig_right"] = SigBlock("Federation President"),
            ["labels"]    = new JsonObject(),
            ["schema"]    = new JsonObject { ["show_loft"] = true, ["show_qr"] = true }
        };

        return await _certRenderer.RenderAsync(CertType.Race,
            new CertRenderRequest { DesignId = designId, Language = lang, Data = ToElement(data) }, ct);
    }

    public Task<byte[]> RenderAceCertAsync(Guid programmeId, string ringNumber, string designId, string lang, CancellationToken ct)
        => RenderAceLikeCertAsync(programmeId, ringNumber, designId, lang, CertType.Ace, "Certificate of Ace Pigeon Performance", "Ace Pigeon", ct);

    public Task<byte[]> RenderSuperAceCertAsync(Guid programmeId, string ringNumber, string designId, string lang, CancellationToken ct)
        => RenderAceLikeCertAsync(programmeId, ringNumber, designId, lang, CertType.SuperAce, "Certificate of Super Ace Distinction", "Super Ace Pigeon", ct);

    private async Task<byte[]> RenderAceLikeCertAsync(
        Guid programmeId, string ringNumber, string designId, string lang,
        CertType certType, string eyebrow, string title, CancellationToken ct)
    {
        var prog = await Ask<GetProgrammeForRenderRequest, ProgrammeForRenderResult>(
            _programmeClient, new GetProgrammeForRenderRequest(programmeId), ct)
            ?? throw new InvalidOperationException("Programme not found.");
        if (!prog.Found) throw new InvalidOperationException("Programme not found.");

        var entry = certType == CertType.Ace
            ? (object?)prog.AcePigeonResults.FirstOrDefault(r => r.RingNumber == ringNumber)
            : prog.SuperAceResults.FirstOrDefault(r => r.RingNumber == ringNumber);
        if (entry is null) throw new InvalidOperationException("Pigeon not found in this programme's results.");

        var (rank, points, races, racesInProg, fancierName, pigeonName, bestSpeed) = certType == CertType.Ace
            ? ExtractAce((AcePigeonRenderItem)entry)
            : ExtractSuperAce((SuperAceRenderItem)entry);

        var data = new JsonObject
        {
            ["meta"] = MetaTranslated(eyebrow, title, $"— {prog.Name} {prog.Year} —", prog.ClubLogoUrl, $"verify/{certType.ToString().ToLowerInvariant()}/{programmeId}/{ringNumber}"),
            ["loft"] = LoftBlock(brandingForProg(prog), fancierName),
            ["bird"] = BirdHeroBlock(pigeonName, ringNumber, $"For exceptional performance in the {prog.Year} {prog.Name} programme."),
            ["stats"] = certType == CertType.Ace
                ? new JsonObject
                {
                    ["rank"] = OrdinalRank(rank),
                    ["points"] = points,
                    ["races"] = races.ToString(),
                    ["avg_velocity"] = $"{Math.Round(bestSpeed, 0)} mpm"
                }
                : new JsonObject
                {
                    ["national_rank"] = OrdinalRank(rank),
                    ["total_points"]  = points,
                    ["races"]         = races.ToString(),
                    ["categories"]    = racesInProg.ToString()
                },
            ["meta_row"] = SpreadTranslated(new()
            {
                ["category"]   = prog.Name,
                ["distances"]  = prog.SuperAceQualification ?? "",
                ["season"]     = prog.Year.ToString(),
                ["federation"] = ""
            }, scalarKeys: []),
            ["sig_left"]  = SigBlock("Programme Director"),
            ["sig_right"] = SigBlock("Federation President"),
            ["labels"]    = new JsonObject(),
            ["schema"]    = new JsonObject { ["show_loft"] = true, ["show_qr"] = true }
        };

        return await _certRenderer.RenderAsync(certType,
            new CertRenderRequest { DesignId = designId, Language = lang, Data = ToElement(data) }, ct);
    }

    public async Task<byte[]> RenderBestLoftCertAsync(Guid programmeId, Guid fancierUserId, string designId, string lang, CancellationToken ct)
    {
        var prog = await Ask<GetProgrammeForRenderRequest, ProgrammeForRenderResult>(
            _programmeClient, new GetProgrammeForRenderRequest(programmeId), ct)
            ?? throw new InvalidOperationException("Programme not found.");
        if (!prog.Found) throw new InvalidOperationException("Programme not found.");

        // Match best-loft entry by fancier name (the message DTO does not currently
        // carry user IDs for loft rows; we resolve via user name lookup).
        var nm = await Ask<GetUserNamesRequest, UserNamesResult>(
            _userNamesClient, new GetUserNamesRequest([fancierUserId]), ct);
        var name = nm?.Names.GetValueOrDefault(fancierUserId) ?? "";
        var entry = prog.BestLoftResults.FirstOrDefault(r => r.FancierName == name)
            ?? throw new InvalidOperationException("Loft not found in this programme's best-loft ranking.");

        var data = new JsonObject
        {
            ["meta"] = new JsonObject(SpreadLang(new()
            {
                ["eyebrow"]    = "Best Loft Championship",
                ["title"]      = "Best Loft",
                ["subtitle"]   = $"— {prog.Year} Season —",
                ["qr_label"]   = "Verify",
                ["awarded_to"] = "Awarded to",
                ["citation"]   = $"For outstanding consistent performance in the {prog.Year} {prog.Name} programme."
            }).Concat(new[]
            {
                new KeyValuePair<string, JsonNode?>("logo_url",   prog.ClubLogoUrl ?? ""),
                new KeyValuePair<string, JsonNode?>("qr_content", $"verify/best-loft/{programmeId}/{fancierUserId}")
            })),
            ["loft"] = new JsonObject(SpreadLang(new()
            {
                ["name"]  = entry.FancierName + " Loft",
                ["owner"] = entry.FancierName
            }).Concat(new[]
            {
                new KeyValuePair<string, JsonNode?>("established", ""),
                new KeyValuePair<string, JsonNode?>("phone",       ""),
                new KeyValuePair<string, JsonNode?>("email",       "")
            })),
            ["federation"] = new JsonObject(SpreadLang(new()
            {
                ["name"]   = prog.ClubName ?? "",
                ["region"] = "",
                ["season"] = $"{prog.Year} Racing Season"
            }).Concat(new[]
            {
                new KeyValuePair<string, JsonNode?>("phone", ""),
                new KeyValuePair<string, JsonNode?>("email", "")
            })),
            ["stats"] = new JsonObject
            {
                ["championship_rank"] = OrdinalRank(entry.LoftRank),
                ["total_points"]      = Math.Round(entry.TotalScore, 2).ToString("F2"),
                ["top_finishes"]      = entry.RacesEntered.ToString(),
                ["birds_entered"]     = entry.PigeonsEntered.ToString()
            },
            ["meta_row"] = SpreadTranslated(new()
            {
                ["races_participated"] = entry.RacesEntered.ToString(),
                ["national_wins"]      = "",
                ["notes"]              = ""
            }, scalarKeys: []),
            ["sig_left"]  = SigBlock("Programme Director"),
            ["sig_right"] = SigBlock("Federation President"),
            ["labels"]    = new JsonObject(),
            ["schema"]    = new JsonObject { ["show_federation"] = true, ["show_qr"] = true }
        };

        return await _certRenderer.RenderAsync(CertType.BestLoft,
            new CertRenderRequest { DesignId = designId, Language = lang, Data = ToElement(data) }, ct);
    }

    // ═══ RESULT TABLES (PDF + EXCEL) ════════════════════════════════════════

    public async Task<byte[]> RenderRaceResultsPdfAsync(Guid raceId, string designId, string lang, CancellationToken ct)
        => await _resultRenderer.RenderAsync(ResultType.Race,
            new ResultRenderRequest { DesignId = designId, Language = lang, Data = await BuildRaceResultsAsync(raceId, ct) }, ct);

    public async Task<byte[]> RenderAceResultsPdfAsync(Guid programmeId, string designId, string lang, CancellationToken ct)
        => await _resultRenderer.RenderAsync(ResultType.Ace,
            new ResultRenderRequest { DesignId = designId, Language = lang, Data = await BuildAcePigeonResultsAsync(programmeId, "Ace Pigeon Result", ct) }, ct);

    public async Task<byte[]> RenderSuperAceResultsPdfAsync(Guid programmeId, string designId, string lang, CancellationToken ct)
        => await _resultRenderer.RenderAsync(ResultType.SuperAce,
            new ResultRenderRequest { DesignId = designId, Language = lang, Data = await BuildSuperAceResultsAsync(programmeId, ct) }, ct);

    public async Task<byte[]> RenderBestLoftResultsPdfAsync(Guid programmeId, string designId, string lang, CancellationToken ct)
        => await _resultRenderer.RenderAsync(ResultType.BestLoft,
            new ResultRenderRequest { DesignId = designId, Language = lang, Data = await BuildBestLoftResultsAsync(programmeId, ct) }, ct);

    public async Task<byte[]> RenderRaceResultsExcelAsync(Guid raceId, string lang, CancellationToken ct)
        => _excel.Export(ResultType.Race, new ResultRenderRequest { DesignId = "T1", Language = lang, Data = await BuildRaceResultsAsync(raceId, ct) });

    public async Task<byte[]> RenderAceResultsExcelAsync(Guid programmeId, string lang, CancellationToken ct)
        => _excel.Export(ResultType.Ace, new ResultRenderRequest { DesignId = "A1", Language = lang, Data = await BuildAcePigeonResultsAsync(programmeId, "Ace Pigeon Result", ct) });

    public async Task<byte[]> RenderSuperAceResultsExcelAsync(Guid programmeId, string lang, CancellationToken ct)
        => _excel.Export(ResultType.SuperAce, new ResultRenderRequest { DesignId = "SA1", Language = lang, Data = await BuildSuperAceResultsAsync(programmeId, ct) });

    public async Task<byte[]> RenderBestLoftResultsExcelAsync(Guid programmeId, string lang, CancellationToken ct)
        => _excel.Export(ResultType.BestLoft, new ResultRenderRequest { DesignId = "L1", Language = lang, Data = await BuildBestLoftResultsAsync(programmeId, ct) });

    // ── data builders ───────────────────────────────────────────────────────

    private async Task<JsonElement> BuildRaceResultsAsync(Guid raceId, CancellationToken ct)
    {
        var race = await Ask<GetRaceForRenderRequest, RaceForRenderResult>(_raceClient, new GetRaceForRenderRequest(raceId), ct)
            ?? throw new InvalidOperationException("Race not found.");
        if (!race.Found) throw new InvalidOperationException("Race not found.");

        var userIds = race.Results.Where(r => r.UserId.HasValue).Select(r => r.UserId!.Value).Distinct().ToList();
        var names = userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await Ask<GetUserNamesRequest, UserNamesResult>(_userNamesClient, new GetUserNamesRequest(userIds), ct))?.Names
              ?? new Dictionary<Guid, string>();

        var branding = await Ask<GetClubBrandingRequest, ClubBrandingResult>(_brandingClient, new GetClubBrandingRequest(race.ClubId), ct);

        var meta = new JsonObject(SpreadLang(new()
        {
            ["org"]         = branding?.Name ?? "",
            ["race_name"]   = race.RaceName,
            ["remarks"]     = race.WindDescription ?? "",
            ["footer_left"] = "Official Results",
            ["footer_center"] = "Calculated by PigeonResultCalculator.com",
            ["footer_right"]  = race.ActualReleaseTime?.Year.ToString() ?? ""
        }).Concat(new[]
        {
            new KeyValuePair<string, JsonNode?>("coordinates",    ""),
            new KeyValuePair<string, JsonNode?>("liberation",     race.ActualReleaseTime?.ToString("dd/MM/yyyy HH:mm:ss") ?? ""),
            new KeyValuePair<string, JsonNode?>("total_pigeons",  race.TotalPigeonsEntered),
            new KeyValuePair<string, JsonNode?>("total_fanciers", userIds.Count),
            new KeyValuePair<string, JsonNode?>("prizes",         race.Results.Count),
            new KeyValuePair<string, JsonNode?>("logo_url",       branding?.LogoUrl ?? ""),
            new KeyValuePair<string, JsonNode?>("qr_content",     $"verify/race/{raceId}")
        }));

        var rows = new JsonArray();
        foreach (var r in race.Results)
        {
            var fancier = r.UserId.HasValue && names.TryGetValue(r.UserId.Value, out var n) ? n : "";
            var row = new JsonObject
            {
                ["pos"]       = r.ClubRank ?? 0,
                ["pigeon"]    = r.RingNumber,
                ["arrival"]   = r.ArrivalTime.ToString("HH:mm:ss"),
                ["duration"]  = "",
                ["distance"]  = Math.Round(r.DistanceKm, 3).ToString("F3"),
                ["speed"]     = Math.Round(r.SpeedMperMin, 4).ToString("F4"),
                ["bask"]      = race.TotalPigeonsEntered,
                ["points"]    = ""
            };
            foreach (var l in AllLangs) row[$"fancier_{l}"] = fancier;
            rows.Add(row);
        }

        var data = new JsonObject
        {
            ["meta"]   = meta,
            ["schema"] = new JsonObject
            {
                ["show_nom"] = false, ["use_coeffi"] = false,
                ["show_podium"] = true, ["show_qr"] = true, ["rows_per_page"] = 35
            },
            ["rows"]   = rows
        };
        return ToElement(data);
    }

    private async Task<JsonElement> BuildAcePigeonResultsAsync(Guid programmeId, string eyebrow, CancellationToken ct)
    {
        var prog = await Ask<GetProgrammeForRenderRequest, ProgrammeForRenderResult>(_programmeClient, new GetProgrammeForRenderRequest(programmeId), ct)
            ?? throw new InvalidOperationException("Programme not found.");
        if (!prog.Found) throw new InvalidOperationException("Programme not found.");

        var rows = new JsonArray();
        foreach (var (entry, index) in prog.AcePigeonResults.Select((e, i) => (e, i)))
        {
            var fancierRow = new JsonObject
            {
                ["pos"]      = entry.AceRank,
                ["pigeon"]   = entry.RingNumber,
                ["n_prizes"] = entry.RacesEntered,
                ["total_coeffi"] = Math.Round(entry.TotalScore, 3).ToString("F3"),
                ["races"]    = new JsonArray()  // sub-rows not available from current message; left empty
            };
            foreach (var l in AllLangs) fancierRow[$"fancier_{l}"] = entry.FancierName;
            rows.Add(fancierRow);
        }

        var data = new JsonObject
        {
            ["meta"]   = AceLikeMeta(eyebrow, prog),
            ["schema"] = new JsonObject { ["show_qr"] = true, ["show_podium"] = true, ["rows_per_page"] = 9 },
            ["labels"] = new JsonObject(),
            ["rows"]   = rows
        };
        return ToElement(data);
    }

    private async Task<JsonElement> BuildSuperAceResultsAsync(Guid programmeId, CancellationToken ct)
    {
        var prog = await Ask<GetProgrammeForRenderRequest, ProgrammeForRenderResult>(_programmeClient, new GetProgrammeForRenderRequest(programmeId), ct)
            ?? throw new InvalidOperationException("Programme not found.");
        if (!prog.Found) throw new InvalidOperationException("Programme not found.");

        var rows = new JsonArray();
        foreach (var entry in prog.SuperAceResults)
        {
            var row = new JsonObject
            {
                ["pos"]      = entry.SuperAceRank,
                ["pigeon"]   = entry.RingNumber,
                ["n_prizes"] = entry.RacesEntered,
                ["total_coeffi"] = Math.Round(entry.TotalScore, 3).ToString("F3"),
                ["races"]    = new JsonArray()
            };
            foreach (var l in AllLangs) row[$"fancier_{l}"] = entry.FancierName;
            rows.Add(row);
        }

        return ToElement(new JsonObject
        {
            ["meta"]   = AceLikeMeta("Super Ace Pigeon Result", prog),
            ["schema"] = new JsonObject { ["show_qr"] = true, ["show_podium"] = true, ["rows_per_page"] = 9 },
            ["labels"] = new JsonObject(),
            ["rows"]   = rows
        });
    }

    private async Task<JsonElement> BuildBestLoftResultsAsync(Guid programmeId, CancellationToken ct)
    {
        var prog = await Ask<GetProgrammeForRenderRequest, ProgrammeForRenderResult>(_programmeClient, new GetProgrammeForRenderRequest(programmeId), ct)
            ?? throw new InvalidOperationException("Programme not found.");
        if (!prog.Found) throw new InvalidOperationException("Programme not found.");

        var rows = new JsonArray();
        foreach (var entry in prog.BestLoftResults)
        {
            var row = new JsonObject
            {
                ["pos"]          = entry.LoftRank,
                ["n_prizes"]     = entry.RacesEntered,
                ["total_points"] = Math.Round(entry.TotalScore, 3).ToString("F3"),
                ["races"]        = new JsonArray()
            };
            foreach (var l in AllLangs) row[$"fancier_{l}"] = entry.FancierName;
            rows.Add(row);
        }

        return ToElement(new JsonObject
        {
            ["meta"]   = AceLikeMeta("Best Loft Result", prog),
            ["schema"] = new JsonObject { ["show_qr"] = true, ["show_podium"] = true, ["rows_per_page"] = 9 },
            ["labels"] = new JsonObject(),
            ["rows"]   = rows
        });
    }

    private static JsonObject AceLikeMeta(string eyebrow, ProgrammeForRenderResult prog) =>
        new(SpreadLang(new()
            {
                ["org"]            = prog.ClubName ?? "",
                ["eyebrow"]        = eyebrow,
                ["result_name"]    = $"{prog.Name} {prog.Year}",
                ["remarks"]        = "",
                ["footer_left"]    = "Official Result",
                ["footer_center"]  = "Calculated by PigeonResultCalculator.com",
                ["footer_right"]   = $"{prog.Year} Season"
            }).Concat(new[]
            {
                new KeyValuePair<string, JsonNode?>("logo_url",   prog.ClubLogoUrl ?? ""),
                new KeyValuePair<string, JsonNode?>("qr_content", "")
            }));

    // ── shared block builders ───────────────────────────────────────────────

    private static JsonObject MetaTranslated(string eyebrow, string title, string subtitle, string? logoUrl, string qrContent)
        => new(SpreadLang(new()
            {
                ["eyebrow"]  = eyebrow,
                ["title"]    = title,
                ["subtitle"] = subtitle,
                ["qr_label"] = "Verify"
            }).Concat(new[]
            {
                new KeyValuePair<string, JsonNode?>("logo_url",   logoUrl ?? ""),
                new KeyValuePair<string, JsonNode?>("qr_content", qrContent)
            }));

    private static JsonObject LoftBlock(ClubBrandingResult? branding, string fancierName)
        => new(SpreadLang(new()
            {
                ["name"]    = branding?.Name ?? "",
                ["owner"]   = fancierName,
                ["address"] = ""
            }).Concat(new[]
            {
                new KeyValuePair<string, JsonNode?>("phone", ""),
                new KeyValuePair<string, JsonNode?>("email", "")
            }));

    private static JsonObject BirdHeroBlock(string name, string ring, string citation)
        => new(SpreadLang(new()
            {
                ["awarded_to"] = "Awarded to",
                ["name"]       = name,
                ["citation"]   = citation
            }).Concat(new[]
            {
                new KeyValuePair<string, JsonNode?>("ring", ring)
            }));

    private static JsonObject SigBlock(string title)
        => new(SpreadLang(new() { ["name"] = "", ["title"] = title }));

    /// <summary>
    /// Expands a dictionary of base keys (e.g. "name") into per-language entries
    /// ("name_en", "name_ar", ...) all populated with the same source value, plus
    /// the supplied scalar keys passed through verbatim.
    /// </summary>
    private static JsonObject SpreadTranslated(Dictionary<string, string> translatable, IReadOnlyList<string> scalarKeys)
    {
        var obj = new JsonObject();
        foreach (var (k, v) in translatable)
        {
            if (scalarKeys.Contains(k)) { obj[k] = v; continue; }
            foreach (var l in AllLangs) obj[$"{k}_{l}"] = v;
        }
        return obj;
    }

    private static IEnumerable<KeyValuePair<string, JsonNode?>> SpreadLang(Dictionary<string, string> translatable) =>
        translatable.SelectMany(kv => AllLangs.Select(l =>
            new KeyValuePair<string, JsonNode?>($"{kv.Key}_{l}", kv.Value)));

    // ── helpers ─────────────────────────────────────────────────────────────

    private async Task<string> ResolveUserName(Guid? userId, CancellationToken ct)
    {
        if (!userId.HasValue) return "";
        var nm = await Ask<GetUserNamesRequest, UserNamesResult>(_userNamesClient, new GetUserNamesRequest([userId.Value]), ct);
        return nm?.Names.GetValueOrDefault(userId.Value) ?? "";
    }

    private static (int Rank, string Points, int Races, int RacesInProg, string Fancier, string Pigeon, double BestSpeed) ExtractAce(AcePigeonRenderItem r) =>
        (r.AceRank, Math.Round(r.TotalScore, 2).ToString("F2"), r.RacesEntered, r.RacesInProgramme,
         r.FancierName, r.PigeonName ?? r.RingNumber, r.BestSpeedMperMin);

    private static (int Rank, string Points, int Races, int RacesInProg, string Fancier, string Pigeon, double BestSpeed) ExtractSuperAce(SuperAceRenderItem r) =>
        (r.SuperAceRank, Math.Round(r.TotalScore, 2).ToString("F2"), r.RacesEntered, r.RacesInProgramme,
         r.FancierName, r.PigeonName ?? r.RingNumber, r.BestSpeedMperMin);

    private static ClubBrandingResult? brandingForProg(ProgrammeForRenderResult prog) =>
        new ClubBrandingResult(true, prog.ClubName ?? "", prog.ClubLogoUrl, prog.ClubPrimaryColor ?? "", prog.ClubSecondaryColor ?? "");

    private static string OrdinalRank(int n) => n switch
    {
        1 => "1st", 2 => "2nd", 3 => "3rd", _ => $"{n}th"
    };

    private static JsonElement ToElement(JsonNode node) =>
        JsonDocument.Parse(node.ToJsonString()).RootElement.Clone();

    private static async Task<T?> Ask<TRequest, T>(IRequestClient<TRequest> client, TRequest request, CancellationToken ct)
        where TRequest : class where T : class
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(20));
            var response = await client.GetResponse<T>(request, cts.Token);
            return response.Message;
        }
        catch
        {
            return null;
        }
    }
}
