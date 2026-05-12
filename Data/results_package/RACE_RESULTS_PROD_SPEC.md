# Backend Addendum — Race Results Templates

Append this to the certificates backend prompt (or create a separate Race Results service). This covers the 20 race-result table designs, the JSON data contract, URL parameter handling, and headless-Chromium PDF generation.

## What this delivers

- An endpoint that accepts a race result + design choice + language and returns a PDF
- 20 design variants (T1–T20) × 6 languages (en/ar/fa/es/de/zh) = 120 visual combinations from a single template
- A4 portrait, multi-page with proper page breaks, embedded QR codes
- Headless render via PuppeteerSharp using `race_results_prod.html`

## The template files

The user provides two HTML files:

- **`race_results_prod.html`** — production template. No UI panel. Reads data from URL params, `window.RACE_DATA`, or `postMessage`. This is what the backend renders.
- **`race_results_dev.html`** — development/QC template. Has the design picker and sample data. Used by humans to preview designs; the backend does NOT use this file.

Place `race_results_prod.html` in `wwwroot/templates/race_results.html` (or wherever your static asset path is). The backend opens this URL in headless Chromium with data injected, then captures the PDF.

## JSON data contract — the source of truth

The template expects a single JSON object with three top-level keys: `meta`, `schema`, `rows`. Every translatable field uses a `<key>_<lang>` suffix pattern. The full translation contract means the backend MUST provide all 6 language versions for every translatable field — the template picks one and renders it.

```json
{
  "meta": {
    "org_en": "Saudi Pigeon Federation",
    "org_ar": "الاتحاد السعودي للحمام",
    "org_fa": "فدراسیون کبوتر عربستان سعودی",
    "org_es": "Federación Saudí de Palomas",
    "org_de": "Saudischer Taubenverband",
    "org_zh": "沙特鸽子联合会",

    "race_name_en": "Matrouh 400 — 2025",
    "race_name_ar": "مطروح ٤٠٠ — ٢٠٢٥",
    "race_name_fa": "مترح ۴۰۰ — ۲۰۲۵",
    "race_name_es": "Matrouh 400 — 2025",
    "race_name_de": "Matrouh 400 — 2025",
    "race_name_zh": "马特鲁 400 — 2025",

    "coordinates": "312146.8 271523.9",
    "liberation": "04/05/2025 07:00:00",
    "total_pigeons": 2247,
    "total_fanciers": 53,
    "prizes": 337,

    "remarks_en": "South wind, light",
    "remarks_ar": "رياح جنوبية خفيفة",
    "remarks_fa": "باد جنوبی، ملایم",
    "remarks_es": "Viento sur, ligero",
    "remarks_de": "Südwind, leicht",
    "remarks_zh": "南风,轻",

    "logo_url": "https://cdn.example.com/loft-logos/saudi-fed.png",
    "qr_content": "https://verify.example.com/race/abc123",

    "footer_left_en": "Official Results",
    "footer_left_ar": "النتائج الرسمية",
    "footer_center_en": "Calculated by PigeonResultCalculator.com",
    "footer_center_ar": "محسوبة بواسطة PigeonResultCalculator.com",
    "footer_right_en": "2025 Season",
    "footer_right_ar": "موسم ٢٠٢٥"
  },

  "schema": {
    "show_nom": false,
    "use_coeffi": false,
    "show_podium": true,
    "show_qr": true,
    "rows_per_page": 35
  },

  "rows": [
    {
      "pos": 1,
      "nom": 73,
      "fancier_en": "Mahmoud Elgazar",
      "fancier_ar": "محمود الجزار",
      "fancier_fa": "محمود الجزار",
      "fancier_es": "Mahmoud Elgazar",
      "fancier_de": "Mahmoud Elgazar",
      "fancier_zh": "马哈茂德·埃尔加扎尔",
      "pigeon": "EG-23-12AA",
      "arrival": "04/05/2025 13:05:40",
      "duration": "06:05:40.200",
      "distance": "401,501",
      "speed": "1,098.040",
      "bask": 73,
      "points": "100.00"
    }
  ]
}
```

### Field reference

**`meta` (object)** — header/footer chrome data:

- `org_<lang>` — federation/club name shown in header (required, per language)
- `race_name_<lang>` — race title shown under org and in page-num bar (required, per language)
- `coordinates` — string, displayed as-is (LTR even in RTL pages because it's coordinates)
- `liberation` — datetime string, displayed as-is
- `total_pigeons`, `total_fanciers`, `prizes` — integers
- `remarks_<lang>` — string per language
- `logo_url` — absolute HTTPS URL or data: URI; can also be language-specific (`logo_url_<lang>`) if different per language
- `qr_content` — string encoded into QR (URL, hash, or any text). If empty/missing, QR is not rendered.
- `footer_left_<lang>`, `footer_center_<lang>`, `footer_right_<lang>` — optional

**`schema` (object)** — controls table column/layout behavior:

- `show_nom` — boolean. Iraqi format has a "Nom." column (pigeon count per fancier); Egyptian format does not. Default: false.
- `use_coeffi` — boolean. Iraqi format uses "Coeffi." in the last column; Egyptian uses "Points". Default: false.
- `show_podium` — boolean. Apply gold/silver/bronze tint + medal emoji to rows where pos is 1/2/3. Default: true.
- `show_qr` — boolean. Render the QR code in the header. Default: true.
- `rows_per_page` — integer 20–50. Default: 35. The template will paginate `rows` into chunks of this size and render each on its own A4 page.

**`rows` (array)** — one entry per result:

- `pos` — integer, position (1, 2, 3, ...). Used for podium detection.
- `nom` — integer, basket count for the fancier (Iraqi schema). Only displayed if `schema.show_nom` is true.
- `fancier_<lang>` — string per language. Mixed RTL/LTR text in one row is handled correctly by `<bdi>` in the template.
- `pigeon` — ring number string. Always displayed LTR regardless of page language.
- `arrival` — datetime string. Always LTR.
- `duration` — duration string. Always LTR.
- `distance` — number or string. Always LTR.
- `speed` — number or string. Always LTR.
- `bask` — integer.
- `points` (or `coeffi`) — final-column value. Template reads `points` first, then `coeffi` as fallback when `schema.use_coeffi` is true.

The template's `langField(record, baseKey, lang)` helper looks up `<baseKey>_<lang>` first, then falls back to `<baseKey>` (unsuffixed) if the language-specific version is missing. This means the backend can omit translations and the template won't break — but the "full translation" contract you chose means the backend should provide every language for every translatable field.

## URL parameters

The production template reads three parameters from the query string:

- `design` — one of T1, T2, ..., T20. Default: T1.
- `lang` — one of en, ar, fa, es, de, zh. Default: en.
- `print` — set to `1` to auto-trigger `window.print()` once the render completes. Useful for browser-based PDF generation; not needed when using headless Chromium's `page.PdfDataAsync`.

Data is loaded via one of four methods, checked in this order:

1. **`?data_url=<absolute-url>`** — template fetches JSON from this URL via `fetch()`. URL must be CORS-accessible or same-origin.
2. **`?data=<base64>`** — base64-encoded JSON payload. Use this for small results (browser URL length limits apply, typically ~2KB safe).
3. **`window.RACE_DATA = {...}`** — global variable. Set this before the template's `<script>` runs. This is the recommended method for headless rendering.
4. **`postMessage`** — `parent.postMessage({type:'race_data', payload:{...}}, '*')`. Useful when the template is embedded in an iframe.

After render completes, the template sets `document.body.setAttribute('data-render-status', 'complete')`. The backend should wait for this attribute before capturing the PDF. If render fails, it sets `data-render-status="error"`.

## ASP.NET Core service implementation

### `RaceResultRenderRequest` model

```csharp
public class RaceResultRenderRequest {
    [Required] public string DesignId { get; set; } = "T1";
    [Required] public string Language { get; set; } = "en";
    [Required] public RaceResultData Data { get; set; } = null!;
}

public class RaceResultData {
    public Dictionary<string, object>? Meta { get; set; }
    public Dictionary<string, object>? Schema { get; set; }
    public List<Dictionary<string, object>>? Rows { get; set; }
}
```

Use `Dictionary<string, object>` rather than strongly-typed properties for `meta` and `rows` because the language-suffix pattern means the keys vary (`org_en`, `org_ar`, `fancier_en`, `fancier_ar`, etc.) and adding a new language shouldn't require model changes.

### Controller endpoint

```csharp
[ApiController]
[Route("api/race-results")]
public class RaceResultsController : ControllerBase {
    private readonly IRaceResultRenderer _renderer;

    public RaceResultsController(IRaceResultRenderer renderer) {
        _renderer = renderer;
    }

    [HttpPost("render")]
    public async Task<IActionResult> Render([FromBody] RaceResultRenderRequest req, CancellationToken ct) {
        if (!VALID_DESIGNS.Contains(req.DesignId))
            return BadRequest($"Invalid design '{req.DesignId}'. Must be T1..T20.");
        if (!VALID_LANGS.Contains(req.Language))
            return BadRequest($"Invalid language '{req.Language}'. Must be en|ar|fa|es|de|zh.");
        if (req.Data?.Rows == null || req.Data.Rows.Count == 0)
            return BadRequest("Data must contain at least one row.");

        var pdf = await _renderer.RenderToPdfAsync(req.DesignId, req.Language, req.Data, ct);
        return File(pdf, "application/pdf", $"race-results-{req.DesignId}-{req.Language}.pdf");
    }

    private static readonly HashSet<string> VALID_DESIGNS = new(Enumerable.Range(1,20).Select(i => $"T{i}"));
    private static readonly HashSet<string> VALID_LANGS = new(new[] { "en","ar","fa","es","de","zh" });
}
```

### Renderer service

```csharp
public interface IRaceResultRenderer {
    Task<byte[]> RenderToPdfAsync(string designId, string lang, RaceResultData data, CancellationToken ct);
}

public class RaceResultRenderer : IRaceResultRenderer {
    private readonly IBrowser _browser;
    private readonly SemaphoreSlim _semaphore;
    private readonly IWebHostEnvironment _env;

    public RaceResultRenderer(IBrowser browser, IWebHostEnvironment env) {
        _browser = browser;
        _env = env;
        _semaphore = new SemaphoreSlim(4); // max 4 concurrent renders; tune for CPU
    }

    public async Task<byte[]> RenderToPdfAsync(string designId, string lang, RaceResultData data, CancellationToken ct) {
        await _semaphore.WaitAsync(ct);
        try {
            await using var page = await _browser.NewPageAsync();

            // A4 portrait viewport — must match @page size in template CSS
            await page.SetViewportAsync(new ViewPortOptions {
                Width = 794, Height = 1123, DeviceScaleFactor = 2
            });

            // Build the data-injection HTML by reading the template and inserting window.RACE_DATA
            var templatePath = Path.Combine(_env.WebRootPath, "templates", "race_results.html");
            var templateHtml = await File.ReadAllTextAsync(templatePath, ct);

            // Inject window.RACE_DATA before the template's <script> tag runs.
            // The template checks window.RACE_DATA as data source #3.
            var dataJson = JsonSerializer.Serialize(data, new JsonSerializerOptions {
                PropertyNamingPolicy = null,  // preserve snake_case keys like "org_en"
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping  // preserve Arabic/Chinese chars
            });
            var injection = $"<script>window.RACE_DATA = {dataJson};</script>";
            // Insert before <script src="...qrcode..."> or before any <script> tag
            templateHtml = templateHtml.Replace("</head>", $"{injection}\n</head>");

            // Also inject URL params via search-string trick — append ?design=...&lang=...
            // But since we're setting page content directly, we use a synthetic URL with the params
            // and rely on the template's getParam() reading from window.location
            var url = $"http://localhost/?design={designId}&lang={lang}";
            await page.SetContentAsync(templateHtml, new NavigationOptions {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                Timeout = 30000
            });

            // PROBLEM: SetContentAsync ignores the URL. To make getParam() work, evaluate
            // a script that overrides window.location's search before init() runs.
            // Simpler approach: pass design and lang via window globals too, and have the
            // template fall back to those. But the template as-given reads URL params, so
            // we must override window.location.search before the script executes.
            //
            // The cleanest fix is to inject design+lang into a hidden meta or window var
            // and have the template read both URL AND those. Since the template ships
            // as-is, use this two-step approach:

            // Step 1: navigate to a file:// or http:// URL with the query params set
            // Step 2: inject window.RACE_DATA after page loads but before init() runs

            // --- Better implementation: serve the template via a file URL with query string ---
            // This requires writing a temp HTML or using a local HTTP server. For simplicity:
            await page.EvaluateExpressionAsync("document.fonts.ready");

            // Wait for render to complete (template sets data-render-status="complete")
            await page.WaitForExpressionAsync(
                "document.body.getAttribute('data-render-status') === 'complete'",
                new WaitForFunctionOptions { Timeout = 20000 }
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

### The URL-vs-SetContent issue — recommended fix

The template's `getParam()` reads from `window.location.search`. When using `SetContentAsync`, the URL is `about:blank` and params are lost. Two clean solutions:

**Option A (preferred): Inject design and lang as globals, modify template to check them**

Add this to the start of the template's `getConfig()`:

```javascript
function getConfig(){
  // Prefer injected globals if backend set them (headless render path)
  if (window.RACE_DESIGN && window.RACE_LANG) {
    return {
      design: VALID_DESIGNS.includes(window.RACE_DESIGN) ? window.RACE_DESIGN : 'T1',
      lang: VALID_LANGS.includes(window.RACE_LANG) ? window.RACE_LANG : 'en',
      autoPrint: false
    };
  }
  // Otherwise fall back to URL params (browser preview path)
  let design = getParam('design') || 'T1';
  if (!VALID_DESIGNS.includes(design)) design = 'T1';
  let lang = getParam('lang') || 'en';
  if (!VALID_LANGS.includes(lang)) lang = 'en';
  const autoPrint = getParam('print') === '1';
  return { design, lang, autoPrint };
}
```

Then backend injection becomes:

```csharp
var injection = $@"<script>
  window.RACE_DESIGN = '{designId}';
  window.RACE_LANG = '{lang}';
  window.RACE_DATA = {dataJson};
</script>";
templateHtml = templateHtml.Replace("</head>", $"{injection}\n</head>");
```

**Option B: Serve the template via a real URL with query params**

Run a local Kestrel route that returns the static template, then navigate to `https://localhost:5001/_internal/race-results-template?design=T7&lang=ar`. Inject the data after navigation via `page.EvaluateExpressionAsync($"window.RACE_DATA = {dataJson};")` before triggering re-render.

This requires the template to expose a re-render function. Option A is cleaner — recommend that.

### Font loading on headless Chromium

The template uses Google Fonts via `<link href="https://fonts.googleapis.com/...">`. In production, **do not depend on remote font fetching** — it's slow and fails in air-gapped deployments. Bundle the fonts into your Docker image and rewrite the template's `<link>` to point at local `/fonts/...` paths.

The user's `download-fonts.sh` script (from the certificates backend prompt) covers most of these but **needs additional fonts for race results**:

```bash
# Add to download-fonts.sh:
# - Noto Sans SC (Chinese sans)
# - Noto Serif SC (Chinese serif)
# - Vazirmatn (Farsi sans)
# - Source Sans 3
# - Manrope
# - Aref Ruqaa, Cairo, Tajawal, Reem Kufi (Arabic; may already be present from certificate fonts)
```

After fonts are local, replace the template's `<link href="https://fonts.googleapis.com/...">` with `<link href="/fonts/all.css">` where `/fonts/all.css` is a CSS file with `@font-face` declarations pointing at local woff2 files.

### Concurrency

A single PuppeteerSharp `IBrowser` instance handles all renders. Use a `SemaphoreSlim` to cap concurrent renders. On a 4-core ARM Linux instance, 4 concurrent renders is a safe ceiling — race results pages with 337 rows take ~600–900ms each, so this gives ~5 PDFs/sec sustained throughput.

### Caching

Hash the request body (designId + lang + canonical JSON of `data`) with SHA-256, key the cache by that hash, store rendered PDFs in `wwwroot/storage/race-results-pdfs/{hash}.pdf`. Same pattern as the certificate renderer.

## Testing

Add a CLI command to render all 20 designs in all 6 languages from a sample dataset:

```bash
dotnet run -- render-race-result-test
```

This should produce 120 PDFs (`race-T1-en.pdf`, `race-T1-ar.pdf`, ..., `race-T20-zh.pdf`). Visual inspection of a few catches font-loading issues, RTL bugs, and podium-row formatting problems before they hit production.

## What stays the same

- Database connection, PostgreSQL, ARM Linux Dockerfile — unchanged
- `IBrowser` singleton lifecycle and Chromium launch flags — unchanged from the certificate renderer
- Authentication/authorization on the API endpoint — apply the same policy as the certificate render endpoint

## What's different from certificates

- **Output is multi-page** (certificates are single-page). The template handles pagination internally; backend just provides all rows.
- **No "design × type" matrix needed** — race results have one type. The 20 designs are visual variants, not different cert types.
- **Larger payloads** — 337 rows of JSON ≈ 100–150 KB. Use the `window.RACE_DATA` injection path, not the URL `?data=` param (too long for URL).
- **No `orientation` column needed** — race results are always A4 portrait. If landscape becomes needed later, follow the same pattern from `backend_landscape_addendum.md`.
