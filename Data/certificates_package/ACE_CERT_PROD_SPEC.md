# Ace Pigeon Certificate — Production Spec

Covers **both** `ace_cert_portrait_prod.html` and `ace_cert_landscape_prod.html`. Same JSON contract; only rendering geometry differs.

## Files

| File | Format | Designs |
|---|---|---|
| `ace_cert_portrait_prod.html` | A4 portrait | A1, A2, A3, A4, A5, A6, A7, A8, A9, A10, AR-A1, AR-A2, AR-A3 |
| `ace_cert_landscape_prod.html` | A4 landscape | A1L, A2L, A3L, A4L, A5L, A6L, A7L, A8L, A9L, A10L, AR-A1L, AR-A2L, AR-A3L |

## Data sources (priority order)

1. `?data_url=<absolute-url>` — fetched as JSON
2. `?data=<base64-json>` — inline
3. `window.CERT_DATA = {...}` — **preferred for headless rendering**
4. `window.postMessage({type:'cert_data', payload:{...}})` — for iframe embed

## URL params and window globals

- `?design=A1..A10` (portrait) or `A1L..A10L` (landscape) or `AR-A1..AR-A3` / `AR-A1L..AR-A3L`
- `?lang=en|ar` — Arabic designs auto-force `lang=ar`
- `?print=1` — auto window.print()
- `window.CERT_DESIGN`, `window.CERT_LANG`, `window.CERT_AUTOPRINT` — alternatives

## Headless render contract

- `body[data-render-status="rendering"]` initially
- `body[data-render-status="complete"]` once QR + fonts ready
- `body[data-render-status="error"]` + `body[data-error-message]` on failure

## JSON schema

```json
{
  "meta": {
    "eyebrow_en": "Certificate of Ace Pigeon Performance",
    "eyebrow_ar": "شهادة أداء البطل",
    "title_en": "Ace Pigeon",
    "title_ar": "البطل",
    "subtitle_en": "— 2024 Season Champion —",
    "subtitle_ar": "— بطل موسم ٢٠٢٤ —",
    "logo_url": "https://cdn.example.com/loft-logos/janssen.png",
    "qr_content": "https://verify.example.com/ace/abc123",
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
    "citation_en": "For exceptional performance across the 2024 season, finishing in the top ranks of multiple races and demonstrating consistent excellence.",
    "citation_ar": "لإنجازه الاستثنائي خلال موسم ٢٠٢٤، حيث حقق مراكز متقدمة في عدة سباقات."
  },

  "stats": {
    "rank": "1st",
    "points": "1,847",
    "races": "12",
    "avg_velocity": "1,395 mpm"
  },

  "meta_row": {
    "category_en": "Long Distance",
    "category_ar": "مسافات طويلة",
    "season": "2024",
    "federation_en": "KBDB Belgium",
    "federation_ar": "الاتحاد البلجيكي"
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
    "rank_en": "Rank", "rank_ar": "الترتيب",
    "points_en": "Points", "points_ar": "النقاط",
    "races_en": "Races", "races_ar": "السباقات",
    "avg_velocity_en": "Avg. Velocity", "avg_velocity_ar": "متوسط السرعة",
    "category_en": "Category:", "category_ar": "الفئة:",
    "season_en": "Season:", "season_ar": "الموسم:",
    "federation_en": "Fed:", "federation_ar": "الاتحاد:"
  },

  "schema": {
    "show_loft": true,
    "show_qr": true
  }
}
```

## Field reference

### `meta` — header chrome + QR

| Key | Notes |
|---|---|
| `eyebrow_<lang>` | Eyebrow line above title |
| `title_<lang>` | Document title. Required. |
| `subtitle_<lang>` | Italic subtitle line |
| `logo_url` | Absolute HTTPS URL or `data:` URI |
| `qr_content` | URL or text. If empty AND `show_qr` true, QR slot reserves space. If `show_qr: false`, QR removed entirely. |
| `qr_label_<lang>` | Label under QR (default "Verify" / "تحقق") |

### `loft` — issuing loft strip (above body)

| Key | Notes |
|---|---|
| `name_<lang>` | Loft name (bold) |
| `owner_<lang>` | Owner |
| `address_<lang>` | Location |
| `phone` | LTR |
| `email` | LTR |

Toggle off via `schema.show_loft: false`.

### `bird` — the ace pigeon being recognized (hero block)

| Key | Notes |
|---|---|
| `awarded_to_<lang>` | Default "Awarded to" / "تُمنح إلى" |
| `name_<lang>` | Bird name. Overflow-protected. |
| `ring` | Ring number. Always LTR. |
| `citation_<lang>` | "For exceptional..." paragraph |

### `stats` — 4 stat boxes (ace-specific)

| Key | Notes |
|---|---|
| `rank` | "1st", "1st National" |
| `points` | Aggregated points across all races |
| `races` | Total races contributing to ace ranking |
| `avg_velocity` | "1,395 mpm" or whatever unit |

### `meta_row` — three meta items below stats (ace-specific)

| Key | Notes |
|---|---|
| `category_<lang>` | "Long Distance" / "Short Distance" / "Sprint" / "Yearlings" etc. |
| `season` | "2024" |
| `federation_<lang>` | Sanctioning federation |

### `sig_left`, `sig_right` — signature blocks

Same as race cert. Both per-language.

### `labels` — overridable labels

Every stat box label and meta_row label is overridable per language. If a key is missing, built-in default used.

### `schema`

| Key | Default | Notes |
|---|---|---|
| `show_loft` | `true` | Show the loft strip between header and body |
| `show_qr` | `true` | Show the QR code in the footer |

## Design IDs reference

**Portrait (10 EN + 3 AR):**
- A1 — Bronze Classical
- A2 — Silver Pinnacle
- A3 — Golden Excellence
- A4 — Crimson Editorial
- A5 — Forest Champion
- A6 — Vintage Sand
- A7 — Burgundy Bodoni
- A8 — Carbon Modern
- A9 — Ivory Italiana
- A10 — Cosmic Violet
- AR-A1 — Kaaba Gold (Gulf Formal)
- AR-A2 — Ivory Calligraphic (Levantine)
- AR-A3 — Modern Kufi Mihrab

**Landscape (10 EN + 3 AR):** A1L–A10L, AR-A1L–AR-A3L — same families, landscape-native.

## Production hardening applied

- ✅ All data backend-injected; no hardcoded sample content
- ✅ `escapeHtml()` on every user-controlled string; no raw `innerHTML` with data
- ✅ `<bdi>` wraps mixed RTL/LTR text
- ✅ Arabic font stacks include multi-tier fallback
- ✅ `data-render-status` lifecycle for headless rendering
- ✅ Standardized `@media print` with `print-color-adjust: exact`
- ✅ Bird name + stat values have `overflow:hidden; text-overflow:ellipsis`
- ✅ QR fully toggleable: `schema.show_qr: false` hides entirely

## Outstanding at deployment

- ⚠️ Google Fonts CDN — replace with local `/fonts/all.css` (punch-list P0.1)
- ⚠️ qrcodejs CDN — bundle locally (P2.1)
