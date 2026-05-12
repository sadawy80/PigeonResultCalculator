# Certificates Backend Integration — Production

Four certificate types × 2 orientations = 8 HTML template files. Each renders a single-page A4 award certificate via headless Chromium.

## Files in this package

| File | Purpose |
|---|---|
| `race_cert_portrait_prod.html` | Race result certificate, A4 portrait, 13 designs |
| `race_cert_landscape_prod.html` | Race result certificate, A4 landscape, 13 designs |
| `ace_cert_portrait_prod.html` | Ace pigeon certificate, A4 portrait, 13 designs |
| `ace_cert_landscape_prod.html` | Ace pigeon certificate, A4 landscape, 13 designs |
| `superace_cert_portrait_prod.html` | Super Ace certificate, A4 portrait, 13 designs |
| `superace_cert_landscape_prod.html` | Super Ace certificate, A4 landscape, 13 designs |
| `bestloft_cert_portrait_prod.html` | Best Loft certificate, A4 portrait, 13 designs |
| `bestloft_cert_landscape_prod.html` | Best Loft certificate, A4 landscape, 13 designs |
| `RACE_CERT_PROD_SPEC.md` | Race cert JSON contract |
| `ACE_CERT_PROD_SPEC.md` | Ace cert JSON contract |
| `SUPERACE_CERT_PROD_SPEC.md` | Super Ace cert JSON contract |
| `BESTLOFT_CERT_PROD_SPEC.md` | Best Loft cert JSON contract (different schema) |

Total: **104 cert designs** (13 per file × 8 files) — but functionally 52 unique designs each in portrait + landscape variants.

## Schema overview

**Race / Ace / Super Ace** share the same JSON shape:
- `meta` — header chrome + QR + signature labels
- `loft` — issuing loft strip (above body)
- `bird` — bird being recognized (hero)
- `stats` — 4 stat boxes (different keys per cert type)
- `meta_row` — 3 meta items (different keys per cert type)
- `sig_left`, `sig_right` — signature blocks
- `labels` — overridable column/label translations
- `schema` — `show_loft`, `show_qr` toggles

**Best Loft has a different schema** — loft is the hero (not bird), federation strip replaces loft strip:
- `meta` — header chrome + `awarded_to` + `citation` (citation moved here from bird)
- `loft` — the hero (replaces bird)
- `federation` — strip above body (replaces loft strip)
- Same stats/meta_row/signatures/labels structure but with cert-specific keys
- `schema.show_federation` instead of `schema.show_loft`

See individual spec docs for full per-type JSON examples.

## Stat keys per cert type

| Cert | Stat 1 | Stat 2 | Stat 3 | Stat 4 |
|---|---|---|---|---|
| **Race** | `position` | `velocity` | `distance` | `time` |
| **Ace** | `rank` | `points` | `races` | `avg_velocity` |
| **Super Ace** | `national_rank` | `total_points` | `races` | `categories` |
| **Best Loft** | `championship_rank` | `total_points` | `top_finishes` | `birds_entered` |

## Meta row keys per cert type

| Cert | Item 1 | Item 2 | Item 3 |
|---|---|---|---|
| **Race** | `date` | `birds` | `federation` |
| **Ace** | `category` | `season` | `federation` |
| **Super Ace** | `distances` | `season` | `federation` |
| **Best Loft** | `races_participated` | `national_wins` | `notes` |

## Architecture — unified renderer

A single `CertRenderer` service handles all 4 cert types. Cert type determines which template file to load and which data shape to expect.

```csharp
public enum CertType { Race, Ace, SuperAce, BestLoft }

public class CertRenderer : ICertRenderer {
    private readonly IBrowser _browser;
    private readonly SemaphoreSlim _semaphore = new(4);
    private readonly IWebHostEnvironment _env;

    public async Task<byte[]> RenderToPdfAsync(
        CertType certType,
        string designId,   // "R1".."R10", "A1".."A10", "S1".."S10", "L1".."L10", "AR-*", and L-suffixed for landscape
        string lang,
        object data,        // RaceCertData | AceCertData | SuperAceCertData | BestLoftCertData
        CancellationToken ct)
    {
        var isLandscape = designId.EndsWith("L") && !designId.EndsWith("RL"); // R10L still ends with L
        // Safer: check the design ID against the valid set for each type
        isLandscape = IsLandscapeDesign(certType, designId);

        var templateName = GetTemplateName(certType, isLandscape);
        var templatePath = Path.Combine(_env.WebRootPath, "templates", templateName);

        await _semaphore.WaitAsync(ct);
        try {
            await using var page = await _browser.NewPageAsync();
            await page.SetViewportAsync(new ViewPortOptions {
                Width = isLandscape ? 1123 : 794,
                Height = isLandscape ? 794 : 1123,
                DeviceScaleFactor = 2
            });

            var html = await File.ReadAllTextAsync(templatePath, ct);
            var dataJson = JsonSerializer.Serialize(data, _jsonOpts);
            var injection = $@"<script>
  window.CERT_DESIGN = '{designId}';
  window.CERT_LANG = '{lang}';
  window.CERT_DATA = {dataJson};
</script>";
            html = html.Replace("</head>", injection + "\n</head>");

            await page.SetContentAsync(html, new NavigationOptions {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                Timeout = 30000
            });

            await page.WaitForExpressionAsync(
                "document.body.getAttribute('data-render-status') === 'complete'",
                new WaitForFunctionOptions { Timeout = 20000 }
            );

            return await page.PdfDataAsync(new PdfOptions {
                Format = PaperFormat.A4,
                Landscape = isLandscape,
                PrintBackground = true,
                PreferCSSPageSize = true,
                MarginOptions = new MarginOptions { Top = "0", Right = "0", Bottom = "0", Left = "0" }
            });
        } finally {
            _semaphore.Release();
        }
    }

    private string GetTemplateName(CertType type, bool landscape) {
        var suffix = landscape ? "landscape" : "portrait";
        return type switch {
            CertType.Race     => $"race_cert_{suffix}.html",
            CertType.Ace      => $"ace_cert_{suffix}.html",
            CertType.SuperAce => $"superace_cert_{suffix}.html",
            CertType.BestLoft => $"bestloft_cert_{suffix}.html",
            _ => throw new ArgumentException($"Unknown cert type: {type}")
        };
    }

    private bool IsLandscapeDesign(CertType type, string designId) {
        // Landscape design IDs all end with 'L' AFTER the number
        // R1..R10 are portrait, R1L..R10L are landscape
        // AR-R1 is portrait, AR-R1L is landscape
        return Regex.IsMatch(designId, @"\d+L$|L$") && designId.EndsWith("L");
        // Simpler: check VALID_DESIGNS sets per cert+orientation
    }
}
```

## Controller endpoint

```csharp
[ApiController]
[Route("api/certificates")]
public class CertificatesController : ControllerBase {
    private readonly ICertRenderer _renderer;

    [HttpPost("render-race")]
    public async Task<IActionResult> RenderRace([FromBody] RaceCertRenderRequest req, CancellationToken ct) {
        var pdf = await _renderer.RenderToPdfAsync(CertType.Race, req.DesignId, req.Language, req.Data, ct);
        return File(pdf, "application/pdf", $"race-cert-{req.DesignId}-{req.Language}.pdf");
    }

    [HttpPost("render-ace")]
    public async Task<IActionResult> RenderAce([FromBody] AceCertRenderRequest req, CancellationToken ct) { /* same */ }

    [HttpPost("render-superace")]
    public async Task<IActionResult> RenderSuperAce([FromBody] SuperAceCertRenderRequest req, CancellationToken ct) { /* same */ }

    [HttpPost("render-bestloft")]
    public async Task<IActionResult> RenderBestLoft([FromBody] BestLoftCertRenderRequest req, CancellationToken ct) { /* same */ }
}
```

Separate endpoints because Best Loft has a different data shape. The other three could share a request type if you use polymorphism, but separate endpoints are clearer.

## All cert templates use `window.CERT_DATA`

All 8 cert templates read from the **same** global names: `window.CERT_DATA`, `window.CERT_DESIGN`, `window.CERT_LANG`. The cert type is implicit in which template file the backend loads. This keeps backend code uniform — only the template path and data shape change.

## Deployment requirements

Same as pedigree backend:

1. **Bundle Google Fonts locally** — replace CDN `<link>` with `/fonts/all.css` in every template
2. **Bundle qrcodejs locally** — replace CDN `<script>` with local copy
3. **Install required fonts in Docker:**
   ```dockerfile
   RUN apt-get update && apt-get install -y \
     fonts-noto fonts-noto-cjk fonts-noto-color-emoji \
     fontconfig && fc-cache -f
   ```
   The `fonts-noto-color-emoji` package is **required** because:
   - Race cert R2 design uses the 🏆 emoji in its title
   - Without it, the emoji renders as a tofu box

## download-fonts.sh

Run this script before deployment to fetch all required Google Fonts locally. Save the output as `wwwroot/fonts/` and the generated `all.css` as `wwwroot/fonts/all.css`. Update all 8 template `<link>` tags to point to `/fonts/all.css`.

```bash
#!/bin/bash
mkdir -p wwwroot/fonts
cd wwwroot/fonts

FONTS=(
  "Amiri:400,700"
  "Aref+Ruqaa:400,700"
  "Cairo:400,600,700,900"
  "Cinzel:400,600,800"
  "Cormorant+Garamond:400,600,700"
  "DM+Sans:400,500,700"
  "Fraunces:opsz,wght@9..144,300;9..144,600;9..144,800"
  "Inter+Tight:400,500,600,700,800"
  "JetBrains+Mono:400,500,700"
  "Marcellus:400"
  "Noto+Naskh+Arabic:400,500,700"
  "Oswald:400,700"
  "Playfair+Display:400,700,900"
  "Reem+Kufi:400,500,700"
  "Scheherazade+New:400,500,700"
  "Syne:500,700,800"
  "Tajawal:400,500,700,900"
  # Add others from each cert design family as needed
)

# Use google-webfonts-helper or curl to download. See https://gwfh.mranftl.com/
# Pseudo-script:
for font in "${FONTS[@]}"; do
  # curl/wget the woff2 files
  # append @font-face to all.css
  echo "TODO: download $font"
done
```

## Caching

Hash `(CertType + DesignId + Language + canonical JSON of Data)` with SHA-256, cache PDFs in `wwwroot/storage/cert-pdfs/{hash}.pdf`. Same as pedigree renderer.

## Static QC notes (from earlier audit)

These designs were flagged in static analysis and warrant a human visual check after deployment:

- **R3, S2L, S10L, L3L, L10L** — used `background-clip:text` gradients on hero names. R3L was already fixed in the prod template (gold color applied directly). The others may need similar treatment depending on print-engine behavior.
- **R2** — uses 🏆 emoji. Requires `fonts-noto-color-emoji` on Linux Docker hosts.
- **AR-S2L, AR-S3L** — had Python-applied structural edits in earlier sessions; visually verify against your real data.
- **AR-L1L, AR-L2L, AR-L3L** — Best Loft Arabic landscape designs had the most structural changes (loft-as-hero, federation strip). Verify all fields render in both EN and AR modes.

## What the templates do NOT do

- Format dates or numbers (send pre-formatted strings)
- Translate (just picks `_<lang>` keys)
- Manage assets (URLs only)
- Authenticate (gate at backend)
- Multi-page output (one cert per page; multi-bird recognition would need a new template)

## Known limits

- A4 only (portrait or landscape)
- EN + AR only
- One bird/loft per certificate
- Stat layout is fixed at 4 boxes (3-box or 6-box would require CSS modification)
