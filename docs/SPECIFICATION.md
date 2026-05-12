# Pigeon Race Results Platform — Comprehensive Feature & Requirements Specification

> **Document type:** Product Requirements  
> **Status:** Baseline (as provided by product owner)  
> **Last updated:** 2026-05-08

---

## 1. Platform Overview

The system is a cloud-based, multi-tenant, event-driven pigeon racing results platform designed for:

- **Fanciers** (end users)
- **Club Managers** (race organizers)
- **Country Managers** (federation level)
- **Super Admin** (platform owner)

The platform enables:

- Race creation and result processing (manual + ETS ingestion)
- Real-time live race tracking
- Club and country-level result publishing
- Subscription-based access control
- Cross-platform integration with external systems

### System Boundaries

| Boundary | Decision |
|---|---|
| Loft management | ❌ Out of scope |
| Pedigree management | ❌ Out of scope |
| External loft system integration | ✅ Full integration-ready |

---

## 2. User Roles & Permissions

### Super Admin (Platform Owner)

Full system control. Responsibilities:

- Create and manage Countries, Country Managers, Club Managers, all Users
- Configure and manage subscription plans
- Control number of clubs per country and results per club
- Monitor system usage, logs, and performance
- Override all system data and permissions

### Country Manager

Country-level authority. Capabilities:

- Create and manage clubs
- Assign club managers
- Invite users (fanciers / admins)
- Monitor all club activities and results within country
- Generate country (national) results by aggregating club results
- Access country-level analytics and reports
- Receive notifications for all club results

**Constraints:** Limited by subscription — maximum clubs and maximum results per club.

### Club Manager

Operational role (core system user).

**User Management:**
- Invite fanciers via email link
- Link fanciers to pigeons (based on results or manual linking)
- Manage / remove users

**Race Management:**
- Create races (release time, location, categories/sections)
- Manage race lifecycle

**Data Ingestion (Multi-Source):**
1. Manual entry — spreadsheet-style interface (ring number + arrival time)
2. ETS file upload — Excel/CSV files; parse and normalize timestamps, pigeon IDs, distances
3. IoT integration — basketing system + live clock ingestion *(future)*

**Result Processing:**
- Automatic velocity calculation and ranking
- Validation: duplicate entries, invalid timestamps, late arrivals

**Publishing & Output:**
- Publish live results and final results
- Generate certificates (winning pigeons) and full race reports

**Club Page Customization:**
- Custom branding (logo, colors)
- Layout templates (5 built-in themes)
- Configurable data sections
- 20 certificate templates, 10+ race results templates

### Fancier (End User)

Capabilities:

- Receive invitation and link account
- View linked pigeons (via results), race participation, rankings
- Track performance metrics and grading within club and country
- Search races, results, and pigeons
- Download PDF reports and Excel exports
- Receive notifications: pigeon results, club updates, race announcements

---

## 3. Core System Features

### 3.1 Race Management System

- Race lifecycle: Draft → Active (started) → Completed → Published
- Category / group definition
- Release configuration (time, GPS coordinates)
- Finalization and publishing

### 3.2 Result Calculation Engine

- Velocity computation (Haversine distance + flight duration)
- Ranking logic (descending velocity, arrival time as tiebreaker)
- Tie-breaking rules
- Multi-category support (separate rankings per section)

### 3.3 Multi-Source Data Ingestion

Supports:
- Manual input
- ETS file upload (Excel/CSV)
- Future: IoT integration

Features: data normalization, validation, strict error handling (no silent failures)

### 3.4 Real-Time Live Results System

- Live leaderboard updates via WebSocket (SignalR)
- Instant arrival processing
- Output on club page (live) and country page (aggregated live)

### 3.5 Result Hierarchy System

| Level | Owner | Source |
|---|---|---|
| Club Results | Club Manager | Raw race data |
| Country Results | Country Manager | Aggregated club results |

> Rule: Country results are **derived from club results**, not raw input.

### 3.6 Country Aggregation Engine

- Combine results across clubs
- Normalize distance and categories
- Generate national rankings and country-level winners

### 3.7 Subscription & Billing System

Managed by Super Admin.

| Attribute | Details |
|---|---|
| Subscription types | Country-level and Club-level |
| Constraints | Max clubs per country; max results per club |
| Billing cycles | Monthly, Seasonal (racing season ~2–3 months), Annual |

### 3.8 Reporting System

Reports:
- Club race results
- Country aggregated results
- Fancier performance reports

Formats: **PDF** and **Excel**

Templates: configurable and reusable (see template library requirements)

### 3.9 Notification System

| Role | Triggers |
|---|---|
| Country Manager | All club results, system updates |
| Club Manager | Race processing, errors, user activity |
| Fancier | Pigeon results, club news, race updates |

Channels:
- In-app (persisted, markable as read)
- Email
- Push notifications *(future)*

### 3.10 Search & History

- Search: races, pigeons, results
- Full historical archive
- Indexed fast queries

### 3.11 Public Pages System

**Club Page:**
- Live results + historical data
- Custom templates and themes
- News / announcements

**Country Page (auto-generated):**
- Aggregated results
- National rankings
- Auto-update on new club result publication

### 3.12 Monitoring & Observability

Stack required:
- **Grafana** — dashboards
- **Loki** — log aggregation
- **Promtail** — log shipping
- **Prometheus** — metrics collection

Access tiers:
- Super Admin → full monitoring
- Country Manager → country-level
- Club Manager → limited metrics

### 3.13 Data Integrity & Event System

- Event-driven architecture (MassTransit / RabbitMQ)
- Immutable event / audit logs
- No data loss guarantee
- Retry and idempotency handling

### 3.14 Localization

Multi-language support with localized dates, units, and formats.

Required languages:
- English (`en`)
- French (`fr`)
- Arabic (`ar`) — RTL support
- Chinese Simplified (`zh`)
- Spanish (`es`)
- Belgian Dutch (`nl-BE`)

### 3.15 External Integration (PigeonLoftManager.com)

Integration goals:

**From PRC to external platform:**
- Race results per pigeon / fancier
- Performance metrics

**From external platform to PRC:**
- Display results, rankings, statistics

Features:
- Deep linking between platforms
- API exposure: `/fancier/{id}/results`, `/pigeon/{ring}/performance`
- Unified user identity (account linking via approval flow)

**Linking workflow (PigeonLoftManager → PRC):**
1. User on PigeonLoftManager sends a link request to a PRC club
2. Club manager sees the request and approves/rejects
3. On approval, a secure access token is issued
4. External platform syncs race results, ace pigeon, super ace, best loft data
5. Data is displayed on the external platform's loft page

---

## 4. Invitation & Linking System

**Invitation flow:**
1. Manager sends invitation link
2. User registers
3. Account is linked to club and pigeons (if applicable)
4. User gains access to data

**Linking features:**
- Managers link pigeons to users
- Users may request linking *(optional future feature)*

---

## 5. Key System Flows

### Race Flow

```
Create Race → Input Data (Manual / ETS) → Process Results → Publish Live Results → Generate Reports
```

### Country Flow

```
Club Results Published → Country Manager Notified → Aggregate Results → Publish Country Results
```

### Fancier Flow

```
Receive Invitation → Account Linked → View Results & Stats → Download / Analyse Performance
```

---

## 6. System Architecture Principles

- Event-driven design
- Multi-tenant architecture
- Real-time data processing
- Strong data integrity (no loss)
- Modular and scalable services

---

## 7. Template Library Requirements

The platform ships two production template packages, both delivered as static
HTML files in `backend/RenderingService/wwwroot/templates/` and rendered to PDF
by headless Chromium (PuppeteerSharp).

### Certificate package (8 files, ~104 design variants)

| File | Format | Designs |
|---|---|---|
| `race_cert_portrait.html` / `race_cert_landscape.html` | A4 P + L | R1–R10 + AR-R1..3 (× 2 orientations) |
| `ace_cert_portrait.html` / `ace_cert_landscape.html` | A4 P + L | A1–A10 + AR-A1..3 (× 2) |
| `superace_cert_portrait.html` / `superace_cert_landscape.html` | A4 P + L | S1–S10 + AR-S1..3 (× 2) |
| `bestloft_cert_portrait.html` / `bestloft_cert_landscape.html` | A4 P + L | L1–L10 + AR-L1..3 (× 2) |

### Result-table package (4 files, ~59 design variants)

| File | Format | Designs |
|---|---|---|
| `race_results.html` | A4 portrait, multi-page | T1–T20 |
| `ace_result.html` | A4 portrait, multi-page | A1–A10 + AR-A1..3 |
| `superace_result.html` | A4 portrait, multi-page | SA1–SA10 + AR-SA1..3 |
| `bestloft_result.html` | A4 portrait, multi-page | L1–L10 + AR-L1..3 |

### Cross-cutting requirements

- Server-side render only — PDF delivered to the user; templates never reach the browser.
- Excel (.xlsx) export of the four result types alongside the PDF — same data, different format.
- Languages: EN + AR for every template; the race-results template also supports FA, ES, DE, ZH.
- Fonts are bundled locally at first start (see `FontBootstrapService`) — no Google Fonts CDN at render time.
- Configurable colour scheme, logo, branding via the per-spec JSON payload (`meta.logo_url`, etc.).
- Must run on Linux ARM64 as well as x64 — `Dockerfile` installs system Chromium so PuppeteerSharp's BrowserFetcher (x64-only) is skipped on ARM.

---

## 8. Theme System

Five built-in site themes selectable per club/country page:

| ID | Name | Character |
|---|---|---|
| 1 | Skyline | Blue/corporate |
| 2 | Meadow | Green/natural |
| 3 | Crimson | Red/bold |
| 4 | Ivory | Light/minimal |
| 5 | Slate | Dark/modern |

---

## 9. Monitoring Requirements (Grafana Stack)

Full observability stack:

| Component | Role |
|---|---|
| Prometheus | Metrics scraping (ASP.NET `metrics` endpoint per service) |
| Grafana | Dashboards — per-service request rates, error rates, latency |
| Loki | Log aggregation backend |
| Promtail | Log shipping agent (reads Docker container logs) |

Required dashboards:
- Platform overview (all services health)
- Per-service request / error / latency
- RabbitMQ queue depths
- SQL Server connections and query times
- Redis hit/miss rates

---

## 10. Docker Requirements

- Individual `Dockerfile` per service (multi-stage, minimal image)
- `services/docker-compose.yml` — full stack including monitoring
- All services, infrastructure, and monitoring in one `docker compose up`

---

## 11. Landing Page Requirements

A professional marketing landing page visible to unauthenticated visitors at `/`:

- Hero section with platform name, tagline, and CTA
- Feature showcase (race management, live results, templates, integration)
- Statistics (clubs, countries, templates, uptime)
- Pricing plans (monthly / seasonal / annual toggle)
- Multi-language switcher
- Mobile-responsive design

---

## 12. Strategic Positioning

> ✅ A federation-grade pigeon racing system  
> ✅ Supporting clubs, countries, and fanciers  
> ✅ Real-time and data-driven  
> ✅ Integration-ready with external ecosystems

---

## 13. Future Enhancements (Out of Current Scope)

- IoT device integration (live clocks, basketing)
- Push notification delivery
- Advanced analytics & AI insights
- Deeper integration with external systems
- User-initiated pigeon linking
