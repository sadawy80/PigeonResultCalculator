# Best Loft Certificate — Production Spec

Covers **both** `bestloft_cert_portrait_prod.html` and `bestloft_cert_landscape_prod.html`. **Different schema from race/ace/super ace certs** — the loft itself is the hero, not a single bird.

## Files

| File | Format | Designs |
|---|---|---|
| `bestloft_cert_portrait_prod.html` | A4 portrait | L1, L2, L3, L4, L5, L6, L7, L8, L9, L10, AR-L1, AR-L2, AR-L3 |
| `bestloft_cert_landscape_prod.html` | A4 landscape | L1L, L2L, L3L, L4L, L5L, L6L, L7L, L8L, L9L, L10L, AR-L1L, AR-L2L, AR-L3L |

## What's different from other certs

In race/ace/super ace certs, the structure is:
- Header → **Loft strip** → Body with **bird hero** → Stats → Signatures

In Best Loft, the structure is:
- Header → **Federation strip** (replaces loft strip) → Body with **loft hero** (replaces bird hero) → Stats → Signatures

This means:
- `data.loft` becomes the hero (not `data.bird`)
- `data.federation` populates the strip above the hero (not `data.loft`)
- `bird` schema is replaced by `loft` schema
- A new `federation` object describes the sanctioning body

## Data sources, URL params, window globals

Same as other certs: `?data_url=`, `?data=` (base64), `window.CERT_DATA`, postMessage. Render config via `?design=`, `?lang=`, `?print=`, `window.CERT_DESIGN`, etc.

## JSON schema

```json
{
  "meta": {
    "eyebrow_en": "Best Loft Championship",
    "eyebrow_ar": "بطولة أفضل لوفت",
    "title_en": "Best Loft",
    "title_ar": "أفضل لوفت",
    "subtitle_en": "— 2024 Season Champion —",
    "subtitle_ar": "— بطل موسم ٢٠٢٤ —",
    "logo_url": "https://cdn.example.com/federation-logo.png",
    "qr_content": "https://verify.example.com/bestloft/abc123",
    "qr_label_en": "Verify",
    "qr_label_ar": "تحقق",
    "awarded_to_en": "Awarded to",
    "awarded_to_ar": "تُمنح إلى",
    "citation_en": "For outstanding consistent performance throughout the 2024 racing season, achieving the highest combined ranking across all federation events.",
    "citation_ar": "للأداء المتميز والمتسق طوال موسم سباقات ٢٠٢٤، محققًا أعلى ترتيب مجمع عبر جميع فعاليات الاتحاد."
  },

  "loft": {
    "name_en": "Janssen Loft International",
    "name_ar": "حمامية يانسن الدولية",
    "owner_en": "A. Janssen",
    "owner_ar": "أ. يانسن",
    "established": "Est. 1987",
    "phone": "+32 14 555 0142",
    "email": "info@janssenloft.be"
  },

  "federation": {
    "name_en": "KBDB Belgium",
    "name_ar": "الاتحاد البلجيكي",
    "region_en": "Flanders Region",
    "region_ar": "منطقة فلاندرز",
    "season_en": "2024 Racing Season",
    "season_ar": "موسم سباقات ٢٠٢٤",
    "phone": "+32 14 555 0100",
    "email": "info@kbdb.be"
  },

  "stats": {
    "championship_rank": "1st",
    "total_points": "8,247",
    "top_finishes": "18",
    "birds_entered": "127"
  },

  "meta_row": {
    "races_participated_en": "32 / 35",
    "races_participated_ar": "٣٢ / ٣٥",
    "national_wins_en": "7",
    "national_wins_ar": "٧",
    "notes_en": "Long Distance Champion",
    "notes_ar": "بطل المسافات الطويلة"
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
    "championship_rank_en": "Championship Rank", "championship_rank_ar": "مركز البطولة",
    "total_points_en": "Total Points", "total_points_ar": "مجموع النقاط",
    "top_finishes_en": "Top-10 Finishes", "top_finishes_ar": "مراكز أعلى-١٠",
    "birds_entered_en": "Birds Entered", "birds_entered_ar": "حمام مشارك",
    "races_participated_en": "Races:", "races_participated_ar": "السباقات:",
    "national_wins_en": "Wins:", "national_wins_ar": "الفوز:",
    "notes_en": "Notes:", "notes_ar": "ملاحظات:"
  },

  "schema": {
    "show_federation": true,
    "show_qr": true
  }
}
```

## Field reference — what's different from other certs

### `meta` extra keys (relative to other cert types)

| Key | Notes |
|---|---|
| `awarded_to_<lang>` | Moved here from the `bird` object (which no longer exists). Default "Awarded to" / "تُمنح إلى". |
| `citation_<lang>` | The citation paragraph. Moved here from `bird`. |

### `loft` — the hero (replaces `bird`)

| Key | Notes |
|---|---|
| `name_<lang>` | Loft name. Hero text. Overflow-protected. |
| `owner_<lang>` | Owner name (smaller text below name) |
| `established` | "Est. 1987" — shown where the bird ring would have been. Always LTR. |
| `phone` | LTR |
| `email` | LTR |

### `federation` — strip above body (replaces `loft` strip)

| Key | Notes |
|---|---|
| `name_<lang>` | Federation name (bold) |
| `region_<lang>` | Region within federation |
| `season_<lang>` | Season string |
| `phone` | LTR |
| `email` | LTR |

Toggle off via `schema.show_federation: false`.

### `stats` — 4 stat boxes (Best-Loft-specific)

| Key | Notes |
|---|---|
| `championship_rank` | "1st", "1st Federation" |
| `total_points` | Total points across all races contributing to championship |
| `top_finishes` | Number of top-10 finishes across the season |
| `birds_entered` | Total bird-entries across the season |

### `meta_row` — three meta items below stats (Best-Loft-specific)

| Key | Notes |
|---|---|
| `races_participated_<lang>` | "32 / 35" (participated / total in federation) |
| `national_wins_<lang>` | Count of national-level wins |
| `notes_<lang>` | Free-form notes — e.g. "Long Distance Champion", "Yearling Specialist", etc. |

### `schema`

| Key | Default | Notes |
|---|---|---|
| `show_federation` | `true` | Show the federation strip above body |
| `show_qr` | `true` | Show the QR code in the footer |

## Design IDs reference

**Portrait (10 EN + 3 AR):**
- L1 — Cream Classical (federation-formal)
- L2 — Royal Navy & Gold
- L3 — Elite Black & Gold
- L4 — Editorial Green
- L5 — Trophy Green (federation award)
- L6 — Burgundy Royale
- L7 — Champion Federation (tricolor strip)
- L8 — Carbon Premium
- L9 — Ivory Italiana
- L10 — Cosmic Champion

**Arabic (3):**
- AR-L1 — Kaaba Gold
- AR-L2 — Ivory Calligraphic
- AR-L3 — Modern Kufi Mihrab

**Landscape:** L1L–L10L, AR-L1L–AR-L3L (same families, landscape geometry).

## Production hardening

Same as Race/Ace/Super Ace cert — all P0/P1 items applied. Outstanding deployment items (Google Fonts CDN, qrcodejs CDN) are the same.

## Backend integration note

The schema difference means the backend's data model for Best Loft is **not interchangeable** with other certs. If you have a unified `CertRenderRequest` model in ASP.NET, Best Loft should be a separate request type (or use polymorphism with a `CertType` discriminator).

```csharp
public class BestLoftCertData {
    public Dictionary<string, object>? Meta { get; set; }
    public Dictionary<string, object>? Loft { get; set; }
    public Dictionary<string, object>? Federation { get; set; }
    public Dictionary<string, object>? Stats { get; set; }
    public Dictionary<string, object>? MetaRow { get; set; }
    public Dictionary<string, object>? SigLeft { get; set; }
    public Dictionary<string, object>? SigRight { get; set; }
    public Dictionary<string, object>? Labels { get; set; }
    public Dictionary<string, object>? Schema { get; set; }
}
```

For everything else (race, ace, super ace), the existing `CertData` model with `loft` + `bird` works.
