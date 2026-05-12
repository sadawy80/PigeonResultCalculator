# Race Certificate Production Template — Data Contract

Covers **both** `race_cert_portrait_prod.html` and `race_cert_landscape_prod.html`. The two files share the same JSON schema; only the rendering geometry differs.

---

## Files

| File | Format | Designs |
|---|---|---|
| `race_cert_portrait_prod.html` | A4 portrait | R1, R2, R3, R4, R5, R6, R7, R8, R9, R10, AR-R1, AR-R2, AR-R3 |
| `race_cert_landscape_prod.html` | A4 landscape | R1L, R2L, R3L, R4L, R5L, R6L, R7L, R8L, R9L, R10L, AR-R1L, AR-R2L, AR-R3L |

The two files are independent — backend picks one based on the user's orientation preference, then picks a design within it.

---

## Data sources (priority order)

Both files follow the pedigree pattern:

1. `?data_url=<absolute-url>` — fetched as JSON
2. `?data=<base64-json>` — inline
3. `window.CERT_DATA = {...}` — preferred for headless rendering
4. `window.postMessage({type:'cert_data', payload:{...}})` — for iframe embedding

## URL params and window globals

- `?design=R1..R10` (portrait) or `R1L..R10L` (landscape) or `AR-R1..AR-R3` / `AR-R1L..AR-R3L`
- `?lang=en|ar` — Arabic designs (`AR-*`) auto-force `lang=ar` regardless of param
- `?print=1` — auto window.print() on render complete
- `window.CERT_DESIGN`, `window.CERT_LANG`, `window.CERT_AUTOPRINT` — alternatives

## Headless render contract

- `body[data-render-status="rendering"]` initially
- `body[data-render-status="complete"]` once QR drawn and fonts loaded
- `body[data-render-status="error"]` with `body[data-error-message]` on failure

---

## JSON schema — full example

```json
{
  "meta": {
    "eyebrow_en": "Certificate of Race Performance",
    "eyebrow_ar": "شهادة أداء السباق",
    "title_en": "Race Result",
    "title_ar": "نتيجة السباق",
    "subtitle_en": "— Quiévrain National · April 2024 —",
    "subtitle_ar": "— سباق الرياض الوطني · أبريل ٢٠٢٤ —",
    "logo_url": "https://cdn.example.com/loft-logos/janssen.png",
    "qr_content": "https://verify.example.com/race/abc123",
    "qr_label_en": "Verify",
    "qr_label_ar": "تحقق"
  },

  "loft": {
    "name_en": "Janssen Loft",
    "name_ar": "حمامية يانسن",
    "owner_en": "A. Janssen",
    "owner_ar": "أ. يانسن",
    "address_en": "Arendonk, Belgium",
    "address_ar": "أرندونك، بلجيكا",
    "phone": "+32 14 555 0142",
    "email": "info@janssenloft.be"
  },

  "bird": {
    "awarded_to_en": "Awarded to",
    "awarded_to_ar": "تُمنح إلى",
    "name_en": "Storm Rider",
    "name_ar": "عاصفة الجبال",
    "ring": "BE-2024-6018421",
    "citation_en": "For outstanding performance in the Quiévrain National race, demonstrating exceptional speed against a field of 4,328 competing birds.",
    "citation_ar": "لإنجازه المتميز في سباق كيفران الوطني، مع إظهار سرعة استثنائية ضد ٤٬٣٢٨ حمامة منافسة."
  },

  "stats": {
    "position": "1st",
    "velocity": "1428 mpm",
    "distance": "287 km",
    "time": "03:21:14"
  },

  "meta_row": {
    "date": "14 April 2024",
    "birds": 4328,
    "federation_en": "KBDB Belgium",
    "federation_ar": "الاتحاد البلجيكي للحمام"
  },

  "sig_left": {
    "name_en": "J. Vermeer",
    "name_ar": "ج. الورديني",
    "title_en": "Race Director",
    "title_ar": "مدير السباق"
  },

  "sig_right": {
    "name_en": "M. De Vries",
    "name_ar": "م. الفيصل",
    "title_en": "Federation President",
    "title_ar": "رئيس الاتحاد"
  },

  "labels": {
    "position_en": "Position", "position_ar": "المركز",
    "velocity_en": "Velocity", "velocity_ar": "السرعة",
    "distance_en": "Distance", "distance_ar": "المسافة",
    "time_en": "Time", "time_ar": "الزمن",
    "date_en": "Date:", "date_ar": "التاريخ:",
    "birds_en": "Birds:", "birds_ar": "عدد الحمام:",
    "federation_en": "Fed:", "federation_ar": "الاتحاد:"
  },

  "schema": {
    "show_loft": true,
    "show_qr": true
  }
}
```

---

## Field reference

### `meta` — header chrome + QR

| Key | Type | Notes |
|---|---|---|
| `eyebrow_<lang>` | string | Small uppercase line above the title |
| `title_<lang>` | string | "Race Result" / "نتيجة السباق". Required. |
| `subtitle_<lang>` | string | Italic line below title (date range, event name) |
| `logo_url` | string (URL) | Absolute HTTPS URL or `data:` URI |
| `qr_content` | string | URL or text encoded into QR. If empty, QR omitted. |
| `qr_label_<lang>` | string | Label under QR (default "Verify" / "تحقق") |

### `loft` — issuing loft strip (above body)

| Key | Type | Notes |
|---|---|---|
| `name_<lang>` | string | Loft name (bold) |
| `owner_<lang>` | string | Owner name |
| `address_<lang>` | string | Location |
| `phone` | string | LTR even on RTL pages |
| `email` | string | LTR even on RTL pages |

Toggle off via `schema.show_loft = false`.

### `bird` — the bird being recognized (hero block)

| Key | Type | Notes |
|---|---|---|
| `awarded_to_<lang>` | string | Defaults to "Awarded to" / "تُمنح إلى" |
| `name_<lang>` | string | Bird's name. Hero text — overflow-protected. |
| `ring` | string | Ring number. Always LTR. |
| `citation_<lang>` | string | The "for outstanding..." paragraph. ~150 chars max for clean layout. |

### `stats` — 4 stat boxes (race performance)

| Key | Type | Notes |
|---|---|---|
| `position` | string | "1st", "1st National", etc. |
| `velocity` | string | "1428 mpm" |
| `distance` | string | "287 km" |
| `time` | string | "03:21:14" |

Stats are **not** translated per language — they're numerics. If you want Arabic-Indic digits (٠-٩), send them that way.

### `meta_row` — three meta items below stats

| Key | Type | Notes |
|---|---|---|
| `date` | string | "14 April 2024" |
| `birds` | string/number | "4,328" |
| `federation_<lang>` | string | "KBDB Belgium" / "الاتحاد البلجيكي" |

### `sig_left` and `sig_right` — signature blocks

| Key | Type | Notes |
|---|---|---|
| `name_<lang>` | string | "J. Vermeer" — rendered in italic display font |
| `title_<lang>` | string | "Race Director" — small caps |

### `labels` — stat box labels and meta row labels

The 4 stat boxes have labels (Position / Velocity / Distance / Time). Meta row has 3 labels (Date / Birds / Fed). All translatable via `labels`:

| Key | Default EN | Default AR |
|---|---|---|
| `position` | Position | المركز |
| `velocity` | Velocity | السرعة |
| `distance` | Distance | المسافة |
| `time` | Time | الزمن |
| `date` | Date: | التاريخ: |
| `birds` | Birds: | عدد الحمام: |
| `federation` | Fed: | الاتحاد: |

Backend can override any of these by sending `labels.{key}_{lang}`. If a key is missing, the built-in default is used.

### `schema` — render flags

| Key | Default | Notes |
|---|---|---|
| `show_loft` | `true` | Show the loft strip between header and body |
| `show_qr` | `true` | Show the QR code in the footer |

---

## Backend integration example

```csharp
public async Task<byte[]> RenderRaceCertAsync(
    string designId,         // "R1".."R10", "AR-R1".."AR-R3", "R1L".."R10L", "AR-R1L".."AR-R3L"
    string lang,             // "en" or "ar"
    RaceCertData data,
    CancellationToken ct)
{
    // Pick orientation based on design ID suffix
    var isLandscape = designId.EndsWith("L");
    var templatePath = Path.Combine(_env.WebRootPath, "templates",
        isLandscape ? "race_cert_landscape.html" : "race_cert_portrait.html");

    var html = await File.ReadAllTextAsync(templatePath, ct);
    var dataJson = JsonSerializer.Serialize(data, _jsonOpts);
    var injection = $$"""
      <script>
        window.CERT_DESIGN = '{{designId}}';
        window.CERT_LANG = '{{lang}}';
        window.CERT_DATA = {{dataJson}};
      </script>
      """;
    html = html.Replace("</head>", $"{injection}\n</head>");

    await using var page = await _browser.NewPageAsync();
    await page.SetViewportAsync(new ViewPortOptions {
        Width = isLandscape ? 1123 : 794,
        Height = isLandscape ? 794 : 1123,
        DeviceScaleFactor = 2
    });
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
}
```

The `isLandscape` flag is derived from the design ID suffix (`L`). The backend doesn't need a separate `orientation` field — the design ID encodes it.

---

## Design IDs reference

### Portrait (10 EN + 3 AR)
- **R1** — Classic Cream Diploma
- **R2** — Royal Trophy Gold (with 🏆 emoji — see note below)
- **R3** — Elite Black & Gold
- **R4** — Editorial Italic
- **R5** — Minimal Swiss
- **R6** — Aviation Cockpit
- **R7** — Art Deco Gold
- **R8** — Vintage Trophy
- **R9** — Premium Pastel
- **R10** — Carbon Premium
- **AR-R1** — Kaaba Gold (Gulf Formal)
- **AR-R2** — Ivory Calligraphic (Levantine)
- **AR-R3** — Modern Kufi Mihrab

### Landscape (10 EN + 3 AR)
- **R1L–R10L** — Same families as portrait, landscape-native (some side-by-side, some centered, some stacked horizontal)
- **AR-R1L** — Kaaba Gold (centered)
- **AR-R2L** — Ivory Calligraphic (side-by-side hero+stats)
- **AR-R3L** — Modern Kufi Mihrab (stacked horizontal)

---

## Production hardening already applied

Compared to the original dev files (`certificates_race.html`, etc.), these prod files address:

- ✅ **P0.2** — No html2canvas. PNG/JPG export is backend's job via Puppeteer screenshots.
- ✅ **P0.3** — All user data through `escapeHtml()`; no raw `innerHTML` with data.
- ✅ **P0.4** — `data-render-status` attribute lifecycle. Backend can `WaitFor` it. Waits on `document.fonts.ready`.
- ✅ **P1.1** — Standardized `@media print` with `print-color-adjust: exact` and proper `@page { size: A4 portrait/landscape; margin: 0 }`.
- ✅ **P1.2** — Arabic font stacks include multi-tier fallback (`Aref Ruqaa, Amiri, Noto Naskh Arabic, serif`).
- ✅ **P1.3** — Bird name has `overflow:hidden; text-overflow:ellipsis; white-space:nowrap`.
- ✅ **P1.4** — Stat values have same overflow protection for large numbers.
- ✅ **P1.5** — `<bdi>` wraps every text node with potential mixed RTL/LTR (names, addresses, ring numbers).
- ✅ **P1.6** — Adjusted muted colors for R3, R6, R10 (raised contrast ratios).
- ✅ **R3L bug fix** — Removed `background-clip:text` gradient on bird name (was printing invisible in some print engines). Gold color now applied directly.

Still outstanding at deployment time:

- ⚠️ **P0.1** — Replace Google Fonts CDN `<link>` with local `/fonts/all.css` before production deploy.
- ⚠️ **P2.1** — Bundle qrcodejs locally instead of CDN.
- ⚠️ Emoji handling — **R2 design uses 🏆 emoji.** On headless Chromium Linux servers without `fonts-noto-color-emoji` installed, this renders as a tofu box (□). Either install the emoji font in the Docker image, or replace the emoji with an SVG trophy icon in R2's CSS `::after` content.

---

## Things the template does NOT do

- Format dates or numbers (send pre-formatted strings)
- Localize numerals (Arabic-Indic vs Western — backend's choice)
- Translate (just picks the right `_<lang>` key)
- Manage logos/QR assets (just renders the URL you give)
- Auth (anyone with the URL+data sees the cert — gate at the backend)

---

## Known limits

- **One bird per certificate.** Multi-bird recognition needs a different template.
- **EN + AR only.** Adding Farsi/Spanish/etc. requires adding font stacks and labels.
- **Stat layout is fixed at 4 boxes.** A 3-box or 6-box layout requires CSS modification.
