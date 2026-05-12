# Result Tables Backend Integration — Production

Four multi-page result table templates for full federation rankings.

## Files in this package

| File | Purpose | Designs |
|---|---|---|
| `race_results_prod.html` | Single-race result, multi-page | 20 (T1-T20) |
| `ace_result_prod.html` | Season Ace ranking, multi-page | 13 (A1-A10, AR-A1-AR-A3) |
| `superace_result_prod.html` | Super Ace ranking, multi-page | 13 (SA1-SA10, AR-SA1-AR-SA3) |
| `bestloft_result_prod.html` | Best Loft ranking, multi-page | 13 (L1-L10, AR-L1-AR-L3) |
| `RACE_RESULTS_PROD_SPEC.md` | (in prompt3_race_results_backend.md from earlier delivery) |
| `ACE_RESULT_PROD_SPEC.md` | Ace JSON contract |
| `SUPERACE_RESULT_PROD_SPEC.md` | Super Ace JSON contract |
| `BESTLOFT_RESULT_PROD_SPEC.md` | Best Loft JSON contract |

Total: **59 design variants** across the 4 templates.

## Architecture overview

Each template renders a **multi-page A4 portrait table**. The data is paginated client-side based on `schema.rows_per_page` — the backend sends ALL rows in one payload, the template chunks them onto pages.

| Template | Schema | Default labels |
|---|---|---|
| Race results | Flat rows (one row per result) | Pos/Fancier/Pigeon/Arrival/Duration/Distance/Speed/Bask/Points (or Coeffi via `use_coeffi`) |
| Ace result | Nested: 1 fancier → N race sub-rows | Pos/Fancier/Pigeon/N.Prizes/Race+Pos+Coeffi/Total Coeffi |
| Super Ace result | Same as Ace | Same as Ace |
| Best Loft result | Same as Ace **but no Pigeon column** | Pos/Fancier/N.Prizes/Race+Basketed+Points/Total Points |

## Window globals per template

Each result template uses **distinct** window globals (unlike certs which share `window.CERT_*`):

| Template | Globals |
|---|---|
| `race_results_prod.html` | `window.RACE_DATA`, `window.RACE_DESIGN`, `window.RACE_LANG`, `window.RACE_AUTOPRINT` |
| `ace_result_prod.html` | `window.ACE_DATA`, `window.ACE_DESIGN`, `window.ACE_LANG`, `window.ACE_AUTOPRINT` |
| `superace_result_prod.html` | `window.SUPERACE_DATA`, `window.SUPERACE_DESIGN`, `window.SUPERACE_LANG`, `window.SUPERACE_AUTOPRINT` |
| `bestloft_result_prod.html` | `window.BESTLOFT_DATA`, `window.BESTLOFT_DESIGN`, `window.BESTLOFT_LANG`, `window.BESTLOFT_AUTOPRINT` |

This is intentional — results templates evolved independently and have distinct schemas. Certs share globals because they share schema.

## postMessage types

Same independence:
- `{type:'race_data', payload:...}` for race results
- `{type:'ace_data', payload:...}` for ace
- `{type:'superace_data', payload:...}` for super ace
- `{type:'bestloft_data', payload:...}` for best loft

## Architecture — one renderer per type

```csharp
public enum ResultType { Race, Ace, SuperAce, BestLoft }

public class ResultRenderer : IResultRenderer {
    private readonly IBrowser _browser;
    private readonly SemaphoreSlim _semaphore = new(4);

    public async Task<byte[]> RenderToPdfAsync(
        ResultType resultType,
        string designId,
        string lang,
        object data,
        CancellationToken ct)
    {
        var templateName = resultType switch {
            ResultType.Race     => "race_results.html",
            ResultType.Ace      => "ace_result.html",
            ResultType.SuperAce => "superace_result.html",
            ResultType.BestLoft => "bestloft_result.html",
            _ => throw new ArgumentException()
        };

        var (designVar, langVar, dataVar) = resultType switch {
            ResultType.Race     => ("RACE_DESIGN", "RACE_LANG", "RACE_DATA"),
            ResultType.Ace      => ("ACE_DESIGN", "ACE_LANG", "ACE_DATA"),
            ResultType.SuperAce => ("SUPERACE_DESIGN", "SUPERACE_LANG", "SUPERACE_DATA"),
            ResultType.BestLoft => ("BESTLOFT_DESIGN", "BESTLOFT_LANG", "BESTLOFT_DATA"),
            _ => throw new ArgumentException()
        };

        await _semaphore.WaitAsync(ct);
        try {
            await using var page = await _browser.NewPageAsync();
            await page.SetViewportAsync(new ViewPortOptions {
                Width = 794, Height = 1123, DeviceScaleFactor = 2
            });

            var templatePath = Path.Combine(_env.WebRootPath, "templates", templateName);
            var html = await File.ReadAllTextAsync(templatePath, ct);
            var dataJson = JsonSerializer.Serialize(data, _jsonOpts);
            var injection = $@"<script>
  window.{designVar} = '{designId}';
  window.{langVar} = '{lang}';
  window.{dataVar} = {dataJson};
</script>";
            html = html.Replace("</head>", injection + "\n</head>");

            await page.SetContentAsync(html, new NavigationOptions {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                Timeout = 60000  // longer timeout — large result sets take more time
            });

            await page.WaitForExpressionAsync(
                "document.body.getAttribute('data-render-status') === 'complete'",
                new WaitForFunctionOptions { Timeout = 30000 }  // also longer
            );

            return await page.PdfDataAsync(new PdfOptions {
                Format = PaperFormat.A4,
                Landscape = false,
                PrintBackground = true,
                PreferCSSPageSize = true,
                MarginOptions = new MarginOptions { Top = "0", Right = "0", Bottom = "0", Left = "0" }
            });
        } finally {
            _semaphore.Release();
        }
    }
}
```

**Note the longer timeouts** — large result sets (337 rows × 9 pages) take more time than single-page certs.

## Points⇄Coeffi toggles

- **Race results:** `schema.use_coeffi: true` flips Points/Total Points → Coeffi./Total Coeffi.
- **Ace + Super Ace:** `schema.use_points: true` flips Coeffi./Total Coeffi. → Points/Total Points
- **Best Loft:** `schema.use_coeffi: true` flips Points/Total Points → Coeffi./Total Coeffi.

Defaults reflect typical usage: race uses Points by default, Ace/Super Ace use Coeffi (matches Iraqi convention), Best Loft uses Points.

## Rowspan structure (Ace/Super Ace/Best Loft)

The Ace family templates use **HTML `rowspan`** to merge cells per fancier:

```html
<tr class="fancier-row">
  <td rowspan="4">1</td>           <!-- position -->
  <td rowspan="4">Fancier 0</td>
  <td rowspan="4">EG-1230-22</td>  <!-- not in best loft -->
  <td rowspan="4">4</td>           <!-- n_prizes -->
  <td>Result 1</td><td>1/100</td><td>0.010</td>   <!-- sub-row -->
  <td rowspan="4">100.000</td>     <!-- total -->
</tr>
<tr class="sub-row">  <!-- additional sub-rows for races 2, 3, 4 -->
  <td>Result 2</td><td>15/200</td><td>0.030</td>
</tr>
<!-- ... etc -->
```

This is preferred over nested-table layouts because:
- Better PDF rendering by Puppeteer (no nested-table edge cases)
- Better screen-reader accessibility
- Compatible with table-extraction tools

## Pagination

The templates handle pagination automatically. `schema.rows_per_page` (default 9 for Ace family, 35 for Race results) controls how many fanciers/results per page. The backend sends ALL rows; the template chunks them.

**Watch out for:** very large results (337+ rows) generate 10+ pages, which uses more memory. Test once with your largest realistic dataset before deploying.

## Caching

Same SHA-256-keyed PDF cache as other renderers. Hash `(ResultType + DesignId + Language + canonical JSON of Data)`.

## Deployment requirements

Same as other backends:
1. Bundle Google Fonts locally
2. Bundle qrcodejs locally
3. Install fonts in Docker:
   ```dockerfile
   RUN apt-get update && apt-get install -y \
     fonts-noto fonts-noto-cjk fonts-noto-color-emoji \
     fontconfig && fc-cache -f
   ```
   **`fonts-noto-color-emoji` is critical here** — podium row 🥇🥈🥉 medal emojis depend on it.

## Concurrency note

Result rendering takes longer than cert rendering (~2-4 seconds for a 50-row result vs ~600ms for a single cert). Adjust `SemaphoreSlim` cap accordingly. On a 4-core box, 2-3 concurrent result renders is a safer ceiling than 4.

## What the templates do NOT do

- Sort or filter rows (send them pre-sorted)
- Compute totals (send `total_coeffi` / `total_points` per row)
- Translate (just picks `_<lang>` keys)
- Format numbers (send pre-formatted strings; for Arabic-Indic digits like ٠-٩, send them that way)

## Known limits

- A4 portrait only (landscape would need a different template)
- EN + AR only for Ace/Super Ace/Best Loft. Race results supports 6 languages (EN, AR, Farsi, Spanish, German, Chinese) — that's the only multi-language result template.
- Best Loft has NO pigeon column (loft-level ranking). The other three have it.
