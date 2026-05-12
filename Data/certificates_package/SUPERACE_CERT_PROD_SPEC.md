# Super Ace Pigeon Certificate — Production Spec

Covers **both** `superace_cert_portrait_prod.html` and `superace_cert_landscape_prod.html`. Premium tier — for top-of-season national/international champions across multiple distance categories.

## Files

| File | Format | Designs |
|---|---|---|
| `superace_cert_portrait_prod.html` | A4 portrait | S1, S2, S3, S4, S5, S6, S7, S8, S9, S10, AR-S1, AR-S2, AR-S3 |
| `superace_cert_landscape_prod.html` | A4 landscape | S1L, S2L, S3L, S4L, S5L, S6L, S7L, S8L, S9L, S10L, AR-S1L, AR-S2L, AR-S3L |

## Data sources

Same as Ace Cert: `?data_url=`, `?data=` (base64), `window.CERT_DATA`, `postMessage({type:'cert_data', payload:{...}})`.

## URL params and window globals

- `?design=S1..S10|S1L..S10L|AR-S1..AR-S3|AR-S1L..AR-S3L`
- `?lang=en|ar`
- `?print=1`
- `window.CERT_DESIGN`, `window.CERT_LANG`, `window.CERT_AUTOPRINT`

## Headless render contract

Same as race/ace cert: `data-render-status` lifecycle on body.

## JSON schema

```json
{
  "meta": {
    "eyebrow_en": "Certificate of Super Ace Distinction",
    "eyebrow_ar": "شهادة تميز سوبر",
    "title_en": "Super Ace Pigeon",
    "title_ar": "سوبر بطل",
    "subtitle_en": "— National Champion 2024 —",
    "subtitle_ar": "— بطل وطني ٢٠٢٤ —",
    "logo_url": "https://cdn.example.com/loft-logos/eagle.png",
    "qr_content": "https://verify.example.com/superace/abc123",
    "qr_label_en": "Verify",
    "qr_label_ar": "تحقق"
  },

  "loft": {
    "name_en": "Eagle Loft International",
    "name_ar": "حمامية النسر الدولية",
    "owner_en": "Owner Name",
    "owner_ar": "اسم المالك",
    "address_en": "City, Country",
    "address_ar": "المدينة، البلد",
    "phone": "+1 555 0142",
    "email": "info@eagleloft.com"
  },

  "bird": {
    "awarded_to_en": "Awarded to",
    "awarded_to_ar": "تُمنح إلى",
    "name_en": "Empire Storm",
    "name_ar": "عاصفة الإمبراطورية",
    "ring": "US-2024-7891234",
    "citation_en": "For dominant performance across all distance categories during the 2024 season, claiming the highest aggregate score in national competition.",
    "citation_ar": "للأداء المتفوق عبر جميع فئات المسافات خلال موسم ٢٠٢٤، محققًا أعلى نقاط إجمالية في البطولة الوطنية."
  },

  "stats": {
    "national_rank": "1st",
    "total_points": "5,247",
    "races": "24",
    "categories": "4"
  },

  "meta_row": {
    "distances_en": "100–800 km",
    "distances_ar": "١٠٠–٨٠٠ كم",
    "season": "2024",
    "federation_en": "American Racing Pigeon Union",
    "federation_ar": "الاتحاد الأمريكي للحمام"
  },

  "sig_left": {
    "name_en": "President Name",
    "name_ar": "اسم الرئيس",
    "title_en": "Federation President",
    "title_ar": "رئيس الاتحاد"
  },

  "sig_right": {
    "name_en": "Chairman Name",
    "name_ar": "اسم الرئيس التنفيذي",
    "title_en": "Board Chairman",
    "title_ar": "رئيس مجلس الإدارة"
  },

  "labels": {
    "national_rank_en": "National Rank", "national_rank_ar": "الترتيب الوطني",
    "total_points_en": "Total Points", "total_points_ar": "مجموع النقاط",
    "races_en": "Races", "races_ar": "السباقات",
    "categories_en": "Categories", "categories_ar": "الفئات",
    "distances_en": "Distances:", "distances_ar": "المسافات:",
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

### Stats (super-ace-specific)

| Key | Notes |
|---|---|
| `national_rank` | "1st", "1st National", "Champion" |
| `total_points` | Aggregate across all categories |
| `races` | Total race count |
| `categories` | Number of distance categories competed |

### Meta row (super-ace-specific)

| Key | Notes |
|---|---|
| `distances_<lang>` | "100–800 km" or category range |
| `season` | "2024" |
| `federation_<lang>` | Sanctioning federation |

### Everything else

`meta`, `loft`, `bird`, `sig_left`, `sig_right`, `labels`, `schema` — same shape and behavior as Ace Cert. See `ACE_CERT_PROD_SPEC.md` for full reference.

## Design IDs

**Portrait (10 EN + 3 AR):**
- S1 — Royal Navy Premium
- S2 — Onyx Bodoni
- S3 — Crimson Royale
- S4 — Forest Royal
- S5 — Platinum Modern
- S6 — Ivory Italiana
- S7 — Carbon Premium
- S8 — Art Deco Gold
- S9 — Vintage Bodoni
- S10 — Cosmic Luxe

**Arabic (3):**
- AR-S1 — Kaaba Gold Premium
- AR-S2 — Burgundy Calligraphic
- AR-S3 — Emerald Kufi

**Landscape:** S1L–S10L, AR-S1L–AR-S3L (same families, landscape geometry).

## Production hardening

Same as Ace Cert. All P0/P1 items from the punch list applied except the Google-Fonts/qrcodejs CDN bundling (deployment-time tasks).

## Differences from Ace Cert

- **Premium-tier visual palettes** — Super Ace designs lean darker, more gold/platinum accents, deeper jewel tones. The S2 design (Onyx Bodoni) and S5 (Platinum Modern) are particularly suited for top-tier annual awards.
- **Different stat semantics** — `national_rank` instead of `rank` (this is for the top-of-season champion, not a single race winner); `categories` instead of `avg_velocity` (super ace spans multiple distance categories).
- **Different meta_row** — `distances` instead of `category` (super ace covers ranges, not a single distance bracket).
