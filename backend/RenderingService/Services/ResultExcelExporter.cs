using System.Text.Json;
using ClosedXML.Excel;
using PRC.RenderingService.Models;

namespace PRC.RenderingService.Services;

public interface IResultExcelExporter
{
    /// <summary>Builds an .xlsx workbook from the same JSON payload the PDF templates consume.</summary>
    byte[] Export(ResultType type, ResultRenderRequest req);
}

/// <summary>
/// Produces a tabular XLSX rendition of each result type. Excel can't reproduce
/// the visual chrome of the PDFs, so we focus on data correctness:
///  - Race: one row per result, in payload order
///  - Ace / Super Ace / Best Loft: one row per (fancier, race) pair, with the
///    fancier columns repeated so the workbook is filter/sort-friendly.
/// </summary>
public class ResultExcelExporter : IResultExcelExporter
{
    public byte[] Export(ResultType type, ResultRenderRequest req)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Results");

        var lang = string.IsNullOrWhiteSpace(req.Language) ? "en" : req.Language;
        var data = req.Data;

        WriteMetaBlock(ws, data, lang);
        var startRow = ws.LastRowUsed()!.RowNumber() + 2;

        switch (type)
        {
            case ResultType.Race:     WriteFlatRows(ws, data, lang, startRow); break;
            case ResultType.Ace:
            case ResultType.SuperAce: WriteNestedRows(ws, data, lang, startRow, includePigeon: true);  break;
            case ResultType.BestLoft: WriteNestedRows(ws, data, lang, startRow, includePigeon: false); break;
            default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    // ── meta block at the top ───────────────────────────────────────────────
    private static void WriteMetaBlock(IXLWorksheet ws, JsonElement data, string lang)
    {
        if (!data.TryGetProperty("meta", out var meta) || meta.ValueKind != JsonValueKind.Object)
            return;

        int row = 1;
        foreach (var prop in meta.EnumerateObject())
        {
            // Prefer the {key}_{lang} field; fall back to plain {key} for non-translated values.
            var name = prop.Name;
            if (name.EndsWith($"_{lang}", StringComparison.OrdinalIgnoreCase))
            {
                var key = name[..^(lang.Length + 1)];
                ws.Cell(row, 1).Value = ToTitle(key);
                ws.Cell(row, 2).Value = ToCellValue(prop.Value);
                ws.Cell(row, 1).Style.Font.Bold = true;
                row++;
            }
            else if (!name.Contains("_en") && !name.Contains("_ar") && !name.Contains("_fa")
                  && !name.Contains("_es") && !name.Contains("_de") && !name.Contains("_zh"))
            {
                // un-translated scalar (logo_url, coordinates, total_pigeons, etc.)
                if (prop.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array) continue;
                ws.Cell(row, 1).Value = ToTitle(name);
                ws.Cell(row, 2).Value = ToCellValue(prop.Value);
                ws.Cell(row, 1).Style.Font.Bold = true;
                row++;
            }
        }
    }

    // ── Race: flat rows ─────────────────────────────────────────────────────
    private static void WriteFlatRows(IXLWorksheet ws, JsonElement data, string lang, int startRow)
    {
        if (!data.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() == 0)
            return;

        var first   = rows[0];
        var columns = CollectColumns(first, lang);

        var headerRow = ws.Row(startRow);
        for (int i = 0; i < columns.Count; i++)
        {
            headerRow.Cell(i + 1).Value = ToTitle(columns[i].DisplayKey);
            headerRow.Cell(i + 1).Style.Font.Bold = true;
            headerRow.Cell(i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int r = startRow + 1;
        foreach (var row in rows.EnumerateArray())
        {
            for (int i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                if (row.TryGetProperty(col.SourceKey, out var v))
                    ws.Cell(r, i + 1).Value = ToCellValue(v);
            }
            r++;
        }
        ws.SheetView.FreezeRows(startRow);
    }

    // ── Ace / Super Ace / Best Loft: nested fancier→races, denormalised ────
    private static void WriteNestedRows(IXLWorksheet ws, JsonElement data, string lang, int startRow, bool includePigeon)
    {
        if (!data.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() == 0)
            return;

        // Header columns: Position | Fancier | [Pigeon] | N.Prizes | Race | Pos/Total | Coeffi/Points | Total
        var headers = new List<string> { "Position", "Fancier" };
        if (includePigeon) headers.Add("Pigeon");
        headers.AddRange(new[] { "No. Prizes", "Race", "Pos/Total", "Coeffi/Points", "Total" });

        var hdr = ws.Row(startRow);
        for (int i = 0; i < headers.Count; i++)
        {
            hdr.Cell(i + 1).Value = headers[i];
            hdr.Cell(i + 1).Style.Font.Bold = true;
            hdr.Cell(i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int r = startRow + 1;
        foreach (var fancier in rows.EnumerateArray())
        {
            var pos      = TryGet(fancier, "pos");
            var fName    = PickLang(fancier, "fancier", lang);
            var pigeon   = TryGet(fancier, "pigeon");
            var nPrizes  = TryGet(fancier, "n_prizes");
            var total    = TryGet(fancier, "total_coeffi") ?? TryGet(fancier, "total_points");

            if (!fancier.TryGetProperty("races", out var races) || races.ValueKind != JsonValueKind.Array
                || races.GetArrayLength() == 0)
            {
                // No sub-rows — still emit one row for the fancier.
                WriteFancierRow(ws, r, includePigeon, pos, fName, pigeon, nPrizes, null, null, null, total);
                r++;
                continue;
            }

            foreach (var race in races.EnumerateArray())
            {
                var raceName = PickLang(race, "name", lang);
                var posTotal = TryGet(race, "pos_total");
                var coeffi   = TryGet(race, "coeffi") ?? TryGet(race, "points");
                WriteFancierRow(ws, r, includePigeon, pos, fName, pigeon, nPrizes, raceName, posTotal, coeffi, total);
                r++;
            }
        }
        ws.SheetView.FreezeRows(startRow);
    }

    private static void WriteFancierRow(
        IXLWorksheet ws, int r, bool includePigeon,
        XLCellValue? pos, XLCellValue? fancier, XLCellValue? pigeon, XLCellValue? nPrizes,
        XLCellValue? race, XLCellValue? posTotal, XLCellValue? coeffi, XLCellValue? total)
    {
        int c = 1;
        if (pos     is { } p) ws.Cell(r, c).Value = p; c++;
        if (fancier is { } f) ws.Cell(r, c).Value = f; c++;
        if (includePigeon) { if (pigeon is { } pg) ws.Cell(r, c).Value = pg; c++; }
        if (nPrizes  is { } n) ws.Cell(r, c).Value = n; c++;
        if (race     is { } rc) ws.Cell(r, c).Value = rc; c++;
        if (posTotal is { } pt) ws.Cell(r, c).Value = pt; c++;
        if (coeffi   is { } co) ws.Cell(r, c).Value = co; c++;
        if (total    is { } t)  ws.Cell(r, c).Value = t;
    }

    // ── helpers ─────────────────────────────────────────────────────────────
    private record FlatColumn(string SourceKey, string DisplayKey);

    private static List<FlatColumn> CollectColumns(JsonElement sampleRow, string lang)
    {
        var cols = new List<FlatColumn>();
        foreach (var prop in sampleRow.EnumerateObject())
        {
            if (prop.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array) continue;

            if (prop.Name.EndsWith($"_{lang}", StringComparison.OrdinalIgnoreCase))
            {
                var stem = prop.Name[..^(lang.Length + 1)];
                cols.Add(new FlatColumn(prop.Name, stem));
            }
            else if (!HasLangSuffix(prop.Name))
            {
                cols.Add(new FlatColumn(prop.Name, prop.Name));
            }
        }
        return cols;
    }

    private static bool HasLangSuffix(string name) =>
        name.EndsWith("_en") || name.EndsWith("_ar") || name.EndsWith("_fa") ||
        name.EndsWith("_es") || name.EndsWith("_de") || name.EndsWith("_zh");

    private static XLCellValue? PickLang(JsonElement obj, string baseKey, string lang)
    {
        if (obj.TryGetProperty($"{baseKey}_{lang}", out var v)) return (XLCellValue?)ToCellValue(v);
        if (obj.TryGetProperty($"{baseKey}_en", out var en))    return (XLCellValue?)ToCellValue(en);
        if (obj.TryGetProperty(baseKey, out var plain))         return (XLCellValue?)ToCellValue(plain);
        return null;
    }

    private static XLCellValue? TryGet(JsonElement obj, string key) =>
        obj.TryGetProperty(key, out var v) ? (XLCellValue?)ToCellValue(v) : null;

    private static XLCellValue ToCellValue(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String  => el.GetString() ?? string.Empty,
        JsonValueKind.Number  => el.TryGetInt64(out var i) ? i : el.GetDouble(),
        JsonValueKind.True    => true,
        JsonValueKind.False   => false,
        JsonValueKind.Null    => string.Empty,
        _                     => el.ToString()
    };

    /// <summary>Turn "race_name" / "total_pigeons" into "Race Name" / "Total Pigeons".</summary>
    private static string ToTitle(string snake)
    {
        var parts = snake.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts.Select(p =>
            p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p[1..]));
    }
}
