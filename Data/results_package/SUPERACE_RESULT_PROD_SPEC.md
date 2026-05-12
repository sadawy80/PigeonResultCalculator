# Super Ace Pigeon Result — Production Spec

`superace_result_prod.html` — A4 portrait, multi-page table, 13 designs (10 EN + 3 AR), EN+AR languages. Renders an Super Ace Pigeon ranking (top-tier seasonal champion) across many races, with sub-rows per race per fancier.

## Files

- **`superace_result_prod.html`** — the template
- This spec document

## Data sources (priority order)

1. `?data_url=<absolute-url>` — fetched as JSON
2. `?data=<base64-json>` — inline
3. `window.SUPERACE_DATA = {...}` — **preferred for headless rendering**
4. `window.postMessage({type:'superace_data', payload:{...}})` — iframe embed

## URL params and window globals

- `?design=SA1..SA10` or `AR-SA1..AR-SA3` (default `A1`)
- `?lang=en|ar` (Arabic designs `AR-*` auto-force `lang=ar`)
- `?print=1` — auto-`window.print()` on render complete
- `window.SUPERACE_DESIGN`, `window.SUPERACE_LANG`, `window.SUPERACE_AUTOPRINT` — alternatives

## Headless render contract

- `body[data-render-status="rendering"]` initially
- `body[data-render-status="complete"]` once QR + fonts ready
- `body[data-render-status="error"]` + `body[data-error-message]` on failure
- Backend should `WaitFor("body[data-render-status='complete']")` before capturing PDF

## JSON schema

```json
{
  "meta": {
    "org_en": "English Club Name",
    "org_ar": "اسم النادي",
    "eyebrow_en": "Super Ace Pigeon Result",
    "eyebrow_ar": "نتيجة سوبر أفضل طائر",
    "result_name_en": "Ace Result 2024 Season",
    "result_name_ar": "نتيجة الآس ٢٠٢٤",
    "remarks_en": "Spring season totals",
    "remarks_ar": "إجماليات موسم الربيع",
    "logo_url": "https://cdn.example.com/club-logo.png",
    "qr_content": "https://verify.example.com/ace/2024",
    "footer_left_en": "Official Result",
    "footer_left_ar": "النتيجة الرسمية",
    "footer_center_en": "Calculated by PigeonResultCalculator.com",
    "footer_center_ar": "محسوبة بواسطة PigeonResultCalculator.com",
    "footer_right_en": "2024 Season",
    "footer_right_ar": "موسم ٢٠٢٤"
  },

  "schema": {
    "show_qr": true,
    "show_podium": true,
    "rows_per_page": 9
  },

  "labels": {
    "col_pos_en": "Pos.", "col_pos_ar": "المركز",
    "col_fancier_en": "Fancier Name", "col_fancier_ar": "أسم المتسابق",
    "col_pigeon_en": "Pigeon", "col_pigeon_ar": "رقم الطائر",
    "col_nprize_en": "No. Prizes", "col_nprize_ar": "عدد النتائج",
    "col_race_en": "Race Name", "col_race_ar": "السباق",
    "col_postotal_en": "Position / Total", "col_postotal_ar": "المركز / المجموع",
    "col_coeffi_en": "Coeffi.", "col_coeffi_ar": "المعامل",
    "col_totalcoeffi_en": "Total Coeffi.", "col_totalcoeffi_ar": "مجموع المعامل",
    "page_en": "Page", "page_ar": "صفحة",
    "of_en": "of", "of_ar": "من"
  },

  "rows": [
    {
      "pos": 1,
      "fancier_en": "Fancier 0",
      "fancier_ar": "الهاوي 0",
      "pigeon": "EG-1230-22",
      "n_prizes": 4,
      "total_coeffi": "100.000",
      "races": [
        { "name_en": "Result 1", "name_ar": "نتيجة 1", "pos_total": "1/100", "coeffi": "0.010" },
        { "name_en": "Result 2", "name_ar": "نتيجة 2", "pos_total": "15/200", "coeffi": "0.030" },
        { "name_en": "Result 3", "name_ar": "نتيجة 3", "pos_total": "5/200", "coeffi": "0.850" },
        { "name_en": "Result 4", "name_ar": "نتيجة 4", "pos_total": "5/200", "coeffi": "0.450" }
      ]
    }
  ]
}
```

## Field reference

### `meta` — header/footer chrome

| Key | Type | Notes |
|---|---|---|
| `org_<lang>` | string | Club name in header (large) |
| `eyebrow_<lang>` | string | "Super Ace Pigeon Result" line above org name |
| `result_name_<lang>` | string | Specific result title (e.g. "2024 Season Ace Result") |
| `remarks_<lang>` | string | Notes/comments line above table |
| `logo_url` | string | Absolute HTTPS or `data:` URI |
| `qr_content` | string | Encoded into QR. If empty AND `show_qr` is true, QR slot reserves space but stays blank. If `show_qr: false`, QR is removed. |
| `footer_*_<lang>` | string | Three footer slots (left/center/right) |

### `schema` — render flags

| Key | Default | Notes |
|---|---|---|
| `show_qr` | `true` | Set `false` to hide QR entirely |
| `show_podium` | `true` | Gold/silver/bronze tint + medal emoji on top-3 rows |
| `use_points` | `false` | Shortcut to flip last column from "Coeffi."/"Total Coeffi." to "Points"/"Total Points" (EN+AR). For finer control, override individual `labels`. |
| `rows_per_page` | `9` | How many **fanciers** per page. Each fancier occupies N table rows where N = `races.length`. With 4 races per fancier, 9 fanciers ≈ 36 rendered rows which fits an A4 page. |

### `labels` — all column headers, page text

Every column header and page indicator is overridable per language. If a label is missing, the built-in default is used.

### `rows[]` — one entry per fancier

| Key | Type | Notes |
|---|---|---|
| `pos` | int | Final position. Used for podium (1/2/3). |
| `fancier_<lang>` | string | Fancier name |
| `pigeon` | string | Ring number. Always LTR. |
| `n_prizes` | int | Total race count for this fancier. If omitted, defaults to `races.length`. |
| `total_coeffi` | string/number | Final aggregated coefficient. Alias `total_points` also accepted. |
| `races[]` | array | Sub-rows. Each has `name_<lang>`, `pos_total`, `coeffi` |

### `races[]` — sub-rows nested inside each fancier row

| Key | Notes |
|---|---|
| `name_<lang>` | Race name shown in sub-row |
| `pos_total` | E.g. "1/100" — position over total competitors. Always LTR. |
| `coeffi` | Coefficient for this race. Always LTR. Alias `points` also accepted. |

## Design IDs

**English (10):** SA1, SA2, SA3, SA4, SA5, SA6, SA7, SA8, SA9, SA10
**Arabic (3):** AR-SA1, AR-SA2, AR-SA3

Same design family structure as Ace Result but with distinct premium-tier color palettes (e.g. SA1 uses a violet/purple accent vs. Ace A1's brown). Visually distinguishable from regular Ace results when shown side-by-side. Same design families as race results — Classic Cream (SA1), Royal Navy & Gold (SA2), Editorial Black (SA3), Swiss Minimal (SA4), Carbon Tech (SA5), Inter Bold (SA6), Imperial Gold (SA7), Burgundy Elite (SA8), Platinum Edge (SA9), Stadium Roster (SA10). Arabic: Kaaba Gold (AR-SA1), Ivory Calligraphic (AR-SA2), Kufi Mihrab (AR-SA3).

## Backend integration

```csharp
var injection = $$"""
<script>
  window.SUPERACE_DESIGN = '{{designId}}';
  window.SUPERACE_LANG = '{{lang}}';
  window.SUPERACE_DATA = {{JsonSerializer.Serialize(data, _jsonOpts)}};
</script>
""";
var html = await File.ReadAllTextAsync(templatePath, ct);
html = html.Replace("</head>", $"{injection}\n</head>");

await using var page = await _browser.NewPageAsync();
await page.SetViewportAsync(new ViewPortOptions { Width = 794, Height = 1123, DeviceScaleFactor = 2 });
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
    PrintBackground = true,
    PreferCSSPageSize = true,
    MarginOptions = new MarginOptions { Top = "0", Right = "0", Bottom = "0", Left = "0" }
});
```



## Table structure (rowspan layout)

Each fancier's data is rendered using HTML `rowspan` so the Pos/Fancier/Pigeon/No.Prizes/TotalCoeffi cells span vertically across all the fancier's race sub-rows. This matches the visual layout of the official Pigeon Result Calculator PDFs:

```
+------+----------+-----------+-------+---------+----------+---------+--------------+
| Pos  | Fancier  | Pigeon    | NPriz | Race    | Pos/Tot  | Coeffi  | Total Coeffi |
+------+----------+-----------+-------+---------+----------+---------+--------------+
|      |          |           |       | Race 1  | 1/100    | 0.010   |              |
|      |          |           |       | Race 2  | 15/200   | 0.030   |              |
|  1   | Fancier0 | EG-1230-22|   4   | Race 3  | 5/200    | 0.850   |   100.000    |
|      |          |           |       | Race 4  | 5/200    | 0.450   |              |
+------+----------+-----------+-------+---------+----------+---------+--------------+
```

The `<tr class="fancier-row">` carries the rowspan'd cells for the first race; subsequent `<tr class="sub-row">` rows contain only the race-name/pos-total/coeffi cells. Podium tinting applies to **all rows of a podium fancier**, not just the first.

This structure is preferred over nested-table layouts because:
- Better PDF rendering by Puppeteer (no nested-table edge cases)
- Better screen-reader accessibility
- Compatible with table-extraction tools (e.g., for re-importing the PDF into a spreadsheet)

## Production hardening applied

- ✅ All data backend-injected; no hardcoded sample data
- ✅ `escapeHtml()` on every user-controlled string
- ✅ `<bdi>` wraps mixed RTL/LTR text (names + ring numbers)
- ✅ Arabic font stacks: `'Aref Ruqaa', 'Amiri', 'Noto Naskh Arabic', serif`
- ✅ `data-render-status` lifecycle for headless wait
- ✅ `print-color-adjust: exact` in `@media print`
- ✅ Multi-page with proper `page-break-after`
- ✅ Long fancier names truncate via `text-overflow: ellipsis`
- ✅ QR toggleable: `schema.show_qr: false` removes entirely

## Outstanding at deployment

- ⚠️ Google Fonts CDN — replace with local `/fonts/all.css` (P0.1)
- ⚠️ qrcodejs CDN — bundle locally (P2.1)
- ⚠️ Podium emoji (🥇🥈🥉) requires `fonts-noto-color-emoji` on Linux servers
