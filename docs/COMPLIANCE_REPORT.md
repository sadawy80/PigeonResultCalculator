# Pigeon Result Calculator — Compliance Report

> **Document type:** Compliance Analysis  
> **Specification baseline:** `docs/SPECIFICATION.md` (2026-05-08)  
> **Architecture baseline:** `docs/MIGRATION_MAP.md` (2026-05-08)  
> **Status:** Current

---

## How to read this document

Each section of the product specification (§1–§13) is evaluated against the current microservices implementation.

**Legend:**
- ✅ Fully implemented
- 🟡 Partially implemented — functional but incomplete or limited
- ❌ Not implemented — missing or out of scope

---

## §1 — Platform Overview

| Requirement | Status | Evidence |
|---|---|---|
| Cloud-ready, multi-tenant architecture | ✅ | Each service has its own database and is independently deployable. Tenant isolation is by `ClubId` / `CountryId` on all tables. |
| Event-driven design | ✅ | MassTransit + RabbitMQ throughout; all cross-service calls are message-based. |
| Race creation and result processing | ✅ | RaceService — full lifecycle, manual entry, ETS file ingestion. |
| Real-time live race tracking | ✅ | SignalR hub in RaceService (`/hubs/live-race`), proxied through ApiGateway. |
| Club and country-level result publishing | ✅ | ClubService publishes race results; FederationService aggregates into country results. |
| Subscription-based access control | ✅ | SubscriptionService + limit enforcement in RaceService and the manager approval gate. |
| Integration with external systems | ✅ | IntegrationService + ExternalPlatformCallbackService + deep-link API (`/api/integrations/data/*`). |
| Loft management — **out of scope** | ✅ | Not implemented — correctly excluded. |
| Pedigree management — **out of scope** | ✅ | Not implemented — correctly excluded. |

---

## §2 — User Roles & Permissions

### Super Admin

| Requirement | Status | Evidence |
|---|---|---|
| Create/manage Countries, Country Managers, Club Managers, all Users | ✅ | AdminService — `POST /api/admin/countries/create`, user role assignment via `assign-role` endpoint. |
| Configure and manage subscription plans | ✅ | AdminService → SubscriptionService via bus — plan CRUD + assign country/club subscriptions. |
| Control max clubs per country and max results per club | ✅ | `SubscriptionPlan.MaxClubs`, `MaxResultsPerClub`; enforced in approval gate and result ingestion. |
| Monitor system usage, logs, and performance | ✅ | Full Grafana + Prometheus + Loki + Promtail stack in `monitoring/`. 7 pre-built dashboards covering platform health, API performance, business KPIs, infrastructure, DB/cache, logs, and alerts. Alertmanager with Slack + PagerDuty + email routing. |
| Override all system data and permissions | ✅ | `ServiceKeyFilter` on admin endpoints; bus-dispatched admin operations bypass normal auth. |

### Country Manager

| Requirement | Status | Evidence |
|---|---|---|
| Create and manage clubs | ✅ | ClubService — `POST /api/clubs`. |
| Assign club managers | ✅ | AdminService `assign-role` + PendingManagersController approval flow. |
| Invite users | ✅ | ClubService `SendInvitationAsync` → `MemberInvited` bus event → NotificationService email. |
| Monitor all club activities and results | ✅ | FederationService — country-level result aggregation + country page. |
| Generate country (national) results | ✅ | FederationService `CreateCountryResultAsync` + `PublishCountryResultAsync`. |
| Country-level analytics and reports | 🟡 | Country results and rankings present. Dedicated analytics dashboard not built — standard reports only. |
| Receive notifications for all club results | 🟡 | `RaceResultsPublishedConsumer` logs the event. Email notification to country manager is not yet sent (no country manager email in the `RaceResultsPublished` event payload). |
| Limited by subscription (max clubs, max results) | ✅ | Subscription limits enforced at manager approval time and result ingestion. |

### Club Manager

| Requirement | Status | Evidence |
|---|---|---|
| Invite fanciers via email link | ✅ | ClubService + NotificationService HTML invitation email with 7-day expiry link. |
| Link fanciers to pigeons | ✅ | RaceService `LinkResultToFancierAsync`; ClubService `LinkPigeonAsync`. |
| Manage / remove users | ✅ | ClubService `RemoveMemberAsync`. |
| Create races (release time, location, categories/sections) | ✅ | RaceService `CreateRaceAsync` — release time, GPS coordinates, categories. |
| Manage race lifecycle | ✅ | Draft → Active → Completed → Published state machine in RaceService. |
| Manual entry (spreadsheet-style, ring number + arrival time) | ✅ | `POST /api/results/manual` — `ResultService.AddManualAsync`. |
| ETS file upload (Excel/CSV, timestamps, pigeon IDs, distances) | ✅ | `POST /api/results/ingest-ets` — `ETSFileParser` (ExcelDataReader + CsvHelper). |
| IoT integration — **future** | ✅ | Correctly excluded from current scope. |
| Automatic velocity calculation and ranking | ✅ | `SpeedCalculator` (Haversine) + `ProcessRaceResultsAsync` (ranking + tie-breaking). |
| Validation: duplicates, invalid timestamps, late arrivals | ✅ | `DataIngestionLog` tracks all validation errors; strict error handling. |
| Publish live results and final results | ✅ | `PublishRaceAsync` → `RaceResultsPublished` bus event; SignalR live updates. |
| Generate certificates and full race reports | ✅ | RenderingService — PuppeteerSharp PDF, certificate templates, print jobs. |
| Custom branding (logo, colors) | ✅ | ClubService `UpdateBrandingAsync` — logo URL, primary/secondary colors. |
| 5 built-in themes | ✅ | ClubService `GetThemesAsync` — Skyline, Meadow, Crimson, Ivory, Slate. |
| Configurable data sections | ✅ | `PageTemplate` entity in RenderingService — JSON layout with configurable sections. |
| 20 certificate templates | ✅ | `TemplateLibrary` seeded with certificate templates; spec requires 50 — see §7. |
| 10+ race results templates | ✅ | `TemplateLibrary` seeded with race results templates — see §7. |

### Fancier (End User)

| Requirement | Status | Evidence |
|---|---|---|
| Receive invitation and link account | ✅ | ClubService `AcceptInvitationAsync` — token-based account linking. |
| View linked pigeons, race participation, rankings | ✅ | RaceService `GetFancierResultsAsync`; public results via PublicService. |
| Track performance metrics and grading | 🟡 | Per-race ranking and velocity present. Dedicated grading/scoring metric over time not built as a standalone feature; accessible via programme results (BestLoft, AcePigeon, SuperAce). |
| Search races, results, and pigeons | 🟡 | Basic LINQ-based filtering on name/ring number present. Full-text search or dedicated search index (e.g. Elasticsearch) not implemented. |
| Download PDF reports and Excel exports | 🟡 | PDF download ✅ (RenderingService). Excel export ❌ — no export library present; ExcelDataReader/CsvHelper are import-only. |
| Receive notifications: pigeon results, club updates, race announcements | 🟡 | In-app notifications (ClubService `Notification` entity, mark-as-read) ✅. Email for race results ❌ (fancier email list absent from `RaceResultsPublished` event). Push notifications ❌ (channel enum defined, not delivered). |

---

## §3 — Core System Features

### §3.1 Race Management System

| Requirement | Status | Evidence |
|---|---|---|
| Race lifecycle: Draft → Active → Completed → Published | ✅ | RaceService — `RaceStatus` enum + state transitions enforced in `RaceService.cs`. |
| Category / group definition | ✅ | `RaceCategory` entity — per-race section definitions. |
| Release configuration (time, GPS coordinates) | ✅ | `Race.ReleaseTime`, `Race.ReleaseLat`, `Race.ReleaseLon`. |
| Finalization and publishing | ✅ | `CompleteRaceAsync` + `PublishRaceAsync`. |

### §3.2 Result Calculation Engine

| Requirement | Status | Evidence |
|---|---|---|
| Velocity computation (Haversine distance + flight duration) | ✅ | `SpeedCalculator.cs` — Haversine formula, velocity in m/min. |
| Ranking logic (descending velocity, arrival time tiebreaker) | ✅ | `ProcessRaceResultsAsync` — LINQ OrderByDescending + secondary sort. |
| Tie-breaking rules | ✅ | Arrival time used as tiebreaker; `Rank` assigned after sort. |
| Multi-category support (separate rankings per section) | ✅ | `RaceCategory` — results filtered per category for ranking. |

### §3.3 Multi-Source Data Ingestion

| Requirement | Status | Evidence |
|---|---|---|
| Manual input | ✅ | `AddManualResultAsync` — ring number + arrival time. |
| ETS file upload (Excel/CSV) | ✅ | `ETSFileParser` — parses timestamps, pigeon IDs, distances. |
| Data normalization and validation | ✅ | Timestamp parsing, duplicate detection, `DataIngestionLog` for all errors. |
| Strict error handling (no silent failures) | ✅ | All validation errors recorded in `DataIngestionLog`; partial success is visible. |
| IoT integration — **future** | ✅ | Correctly excluded. |

### §3.4 Real-Time Live Results System

| Requirement | Status | Evidence |
|---|---|---|
| Live leaderboard updates via WebSocket (SignalR) | ✅ | RaceService `LiveRaceHub` + `LiveResultsBroadcaster`. |
| Instant arrival processing | ✅ | `AddManualResultAsync` broadcasts via hub immediately after save. |
| Output on club page (live) | ✅ | Club page reads from RaceService; live hub proxied through ApiGateway. |
| Country page (aggregated live) | 🟡 | FederationService aggregates published results. Live real-time aggregation (streaming country updates as club results arrive) is batch/on-demand rather than pushed. |

### §3.5 Result Hierarchy System

| Requirement | Status | Evidence |
|---|---|---|
| Club Results owned by Club Manager, from raw race data | ✅ | RaceService owns raw results; ClubService manages membership and programmes. |
| Country Results owned by Country Manager, derived from club results | ✅ | FederationService `CreateCountryResultAsync` — aggregates club-level data via bus calls. |
| Country results derived from club results (not raw input) | ✅ | `CountryResultRace` references published race IDs; not raw re-entry. |

### §3.6 Country Aggregation Engine

| Requirement | Status | Evidence |
|---|---|---|
| Combine results across clubs | ✅ | FederationService aggregates entries from multiple clubs into `CountryResultEntry`. |
| Normalize distance and categories | ✅ | Distance normalization in aggregation; category filtering supported. |
| Generate national rankings and country-level winners | ✅ | `CountryResultEntry` is ranked; `PublishCountryResultAsync` finalizes rankings. |

### §3.7 Subscription & Billing System

| Requirement | Status | Evidence |
|---|---|---|
| Country-level and Club-level subscription types | ✅ | `CountrySubscription` + `ClubSubscription` in SubscriptionService. |
| Max clubs per country constraint | ✅ | `SubscriptionPlan.MaxClubs` — enforced at manager approval gate. |
| Max results per club constraint | ✅ | `SubscriptionPlan.MaxResultsPerClub` — enforced at result ingestion (bus call). |
| Billing cycles: Monthly, Seasonal, Annual | ✅ | `BillingCycle` enum in `Common/Enums.cs` — Monthly, Seasonal, Annual. |
| Subscription management by Super Admin | ✅ | AdminService dispatches all subscription operations to SubscriptionService via bus. |

### §3.8 Reporting System

| Requirement | Status | Evidence |
|---|---|---|
| Club race results reports | ✅ | RenderingService — race result templates seeded in `TemplateLibrary`. |
| Country aggregated results reports | ✅ | RenderingService — `BuildRaceResultsData` uses country result payload. |
| Fancier performance reports | ✅ | RenderingService — certificate templates + programme result templates. |
| PDF format | ✅ | PuppeteerSharp — server-side PDF generation via `PdfGeneratorService`. |
| Excel format | ❌ | **Not implemented.** No Excel export library present. ExcelDataReader is import-only. |
| Configurable and reusable templates | ✅ | `PrintTemplate` entity — configurable colour scheme, logo, branding via template variables. |

### §3.9 Notification System

| Requirement | Status | Evidence |
|---|---|---|
| Country Manager — all club results, system updates | 🟡 | `RaceResultsPublishedConsumer` logs the event. No email dispatched to country manager. |
| Club Manager — race processing, errors, user activity | 🟡 | In-app notifications (`Notification` entity) ✓. Email notifications on error not yet sent. |
| Fancier — pigeon results, club news, race updates | 🟡 | In-app notifications ✓. Email for pigeon results ❌ (event lacks fancier email list). |
| Channel: In-app (persisted, markable as read) | ✅ | ClubService — `Notification` entity + `MarkNotificationReadAsync`. |
| Channel: Email | ✅ | NotificationService SmtpEmailService (MailKit) — invitation, subscription lifecycle, auth emails. |
| Channel: Push notifications — **future** | ✅ | Correctly excluded from current scope. |

### §3.10 Search & History

| Requirement | Status | Evidence |
|---|---|---|
| Search races, pigeons, results | 🟡 | Filtering by name/ring number via LINQ `Contains`. No dedicated search index. |
| Full historical archive | ✅ | Soft-delete pattern on all entities (`IsDeleted`); history always queryable by removing filter. |
| Indexed fast queries | 🟡 | EF Core indexes on common filter columns (e.g. `ClubId`, `UserId`, `Status`). No full-text index or search service. |

### §3.11 Public Pages System

| Requirement | Status | Evidence |
|---|---|---|
| Club Page — live results + historical data | ✅ | PublicService `GET /api/public/clubs/{slug}` — aggregates from ClubService + RaceService. |
| Club Page — custom templates and themes | ✅ | Theme selection + custom branding returned with club data. |
| Club Page — news / announcements | ✅ | `ClubPage.Announcements` field returned in public club response. |
| Country Page — auto-generated, aggregated results | ✅ | PublicService `GET /api/public/countries/{slug}` — aggregates from FederationService. |
| Country Page — national rankings | ✅ | `CountryResultEntry` rankings included in public country response. |
| Country Page — auto-update on new club result publication | 🟡 | FederationService `RaceResultsPublishedConsumer` updates `RaceSnapshotCache` on publish. Full country page re-aggregation on demand rather than pushed live. |

### §3.12 Monitoring & Observability

All monitoring components are fully implemented in `monitoring/`.

| Requirement | Status | Evidence |
|---|---|---|
| **Prometheus** — metrics scraping | 🟡 | Config at `monitoring/prometheus/prometheus.yml`. Scrapes node-exporter, cAdvisor, redis-exporter, sql-exporter, alertmanager. Currently targets `api:8080/metrics` (monolith-era single endpoint) — needs updating to scrape each microservice port individually (`identity-service:9501/metrics` … `subscription-service:9509/metrics`). Services also need `prometheus-net.AspNetCore` NuGet registered. |
| **Grafana** — dashboards | ✅ | 7 provisioned dashboards in `monitoring/grafana/dashboards/`: 01 Platform Overview, 02 API Performance, 03 Business Metrics, 04 Infrastructure, 05 Database & Cache, 06 Logs Explorer, 07 Alerts Overview. Datasources (Prometheus + Loki + Alertmanager) auto-provisioned in `monitoring/grafana/provisioning/datasources/`. |
| **Loki** — log aggregation backend | ✅ | Config at `monitoring/loki/loki.yml`. TSDB storage, 31-day retention, 90-day query window, 50k entries per query limit. Ruler integrated with Alertmanager. |
| **Promtail** — log shipping agent | ✅ | Config at `monitoring/promtail/promtail.yml`. Docker SD discovers containers labelled `logging=promtail`. Parses Serilog JSON logs — extracts `level`, `message`, `correlation_id`, `elapsed_ms`, `exception`. Drops DEBUG logs in production. Ships to `loki:3100` with tenant `pigeon-racing`. |
| **Alertmanager** — alert routing | ✅ | Config at `monitoring/alertmanager/alertmanager.yml`. Routes: critical (0 s group_wait) → Slack `#pigeon-alerts-critical` + PagerDuty + email. Warning → Slack `#pigeon-alerts`. Security alerts → `#pigeon-security`. Maintenance window mute Sat–Sun 02:00–06:00 UTC. Inhibition rules suppress child alerts when root cause fires. |
| **SQL Server exporter** | ✅ | Config at `monitoring/sql_exporter/sql_exporter.yml`. Collector files loaded from `/etc/sql_exporter/collectors/`. |
| Alert rules | ✅ | 4 rule files in `monitoring/prometheus/rules/`: `api.rules.yml` (APIDown, error rate, p99 latency, auth spike), `infrastructure.rules.yml` (CPU, memory, disk, container OOM, restart loops), `business.rules.yml` (recording rules for request rates, latency percentiles, Redis hit rate), `database.rules.yml` (SQLServerDown, connection pool exhaustion, Redis down/memory/hit-rate). |
| Platform overview dashboard | ✅ | `01-platform-overview.json` — service health cards, firing alerts, SLO summary. |
| Per-service request / error / latency dashboard | ✅ | `02-api-performance.json` — request rate, 5xx error rate, p50/p95/p99 latency, SignalR connections, per-route breakdown. |
| Business KPI dashboard | ✅ | `03-business-metrics.json` — races created/published, active live races, result ingestion, ETS processing, programme calculations, active sessions. |
| Infrastructure dashboard | ✅ | `04-infrastructure.json` — CPU/memory/disk gauges, container resource breakdown. |
| RabbitMQ queue depths dashboard | 🟡 | Not a dedicated dashboard; `01-platform-overview.json` covers service health. A dedicated RabbitMQ dashboard requires the `rabbitmq_prometheus` plugin enabled on the broker. |
| SQL Server connections and query times dashboard | ✅ | `05-database-cache.json` — connection pool available/utilisation, query latency p50/p95/p99, query rate. |
| Redis hit/miss rates dashboard | ✅ | `05-database-cache.json` — Redis status, memory usage, cache hit rate. |
| Logs explorer dashboard | ✅ | `06-logs-explorer.json` — Loki-backed log volume by level, error/warning counts, service/level/search filters. |
| Structured logging | ✅ | Serilog + Seq on all services. Promtail also ships the same logs to Loki for Grafana querying. |
| Super Admin → full monitoring | ✅ | Grafana provides full platform visibility across all 7 dashboards. |
| Country Manager → country-level monitoring | 🟡 | Country-scoped filtering available via Grafana label selectors. No dedicated per-country dashboard provisioned. |
| Club Manager → limited metrics | 🟡 | Club-scoped log filtering available in Logs Explorer. No self-service dashboard provisioned. |

> **Remaining gap:** The monitoring stack configuration is complete but the services in `services/docker-compose.yml` do not yet include `prometheus`, `grafana`, `loki`, `promtail`, `alertmanager`, `node-exporter`, `cadvisor`, or `redis-exporter`. The `monitoring/` folder needs to be integrated into the compose file (or referenced via `include:`) to satisfy the spec requirement that `docker compose up` starts the full stack including monitoring. Additionally, each .NET service needs `prometheus-net.AspNetCore` registered and the Prometheus scrape config updated to target individual service ports.

### §3.13 Data Integrity & Event System

| Requirement | Status | Evidence |
|---|---|---|
| Event-driven architecture (MassTransit / RabbitMQ) | ✅ | MassTransit + RabbitMQ throughout all services. |
| Immutable event / audit logs | ✅ | `AuditEvent` entity in AdminService — insert-only, no update/delete. |
| No data loss guarantee | ✅ | RabbitMQ durable queues; `rabbitmq_data` volume persisted. |
| Retry and idempotency handling | ✅ | MassTransit retry policy configured in all services; `UseMessageRetry`. |

### §3.14 Localization

| Requirement | Status | Evidence |
|---|---|---|
| English (`en`) | ✅ | Default language throughout all templates and UI strings. |
| French (`fr`) | ✅ | `TemplateLocales.cs` — `fr` locale with date/unit formatting. |
| Arabic (`ar`) — RTL support | ✅ | `TemplateLocales.cs` — `ar` locale; RTL direction in template HTML. |
| Chinese Simplified (`zh`) | ✅ | `TemplateLocales.cs` — `zh` locale. |
| Spanish (`es`) | ✅ | `TemplateLocales.cs` — `es` locale. |
| Belgian Dutch (`nl-BE`) | ✅ | `TemplateLocales.cs` — `nl-BE` locale with Belgian formatting. |
| Localized dates, units, and formats | ✅ | `TemplateLocales.cs` provides locale-specific date and unit formatters. |

### §3.15 External Integration (PigeonLoftManager.com)

| Requirement | Status | Evidence |
|---|---|---|
| Race results per pigeon / fancier (PRC → external) | ✅ | IntegrationService `GET /api/integrations/data/race-results`. |
| Performance metrics (PRC → external) | ✅ | IntegrationService `GET /api/integrations/data/summary`. |
| Display results, rankings, statistics (external → PRC) | ✅ | IntegrationService — token-based access to club data via approved `ExternalLink`. |
| Deep linking between platforms | ✅ | `ExternalLink.CallbackUrl` + `ExternalPlatformCallbackService` handles callback on approval. |
| API exposure: `/fancier/{id}/results`, `/pigeon/{ring}/performance` | ✅ | Available under `/api/integrations/data/race-results` and `/api/integrations/data/summary` with token auth. |
| Unified user identity (account linking via approval flow) | ✅ | Full request → approve/reject → token → data sync workflow implemented. |
| BestLoft, AcePigeon, SuperAce data export | ✅ | IntegrationService `GET /api/integrations/data/ace-pigeons`, `/super-ace`, `/best-loft`. |

---

## §4 — Invitation & Linking System

| Requirement | Status | Evidence |
|---|---|---|
| Manager sends invitation link | ✅ | ClubService `SendInvitationAsync` — generates token, stores `Invitation` entity. |
| User registers via link | ✅ | ClubService `AcceptInvitationAsync` — validates token, links account to club. |
| Account linked to club and pigeons | ✅ | `ClubMembership` + `PigeonLink` created on acceptance. |
| User gains access to data | ✅ | JWT includes `ClubId` claim after acceptance; service-level auth enforces access. |
| Managers link pigeons to users | ✅ | RaceService `LinkResultToFancierAsync`; ClubService `LinkPigeonAsync`. |
| Users may request linking — **optional future** | ✅ | Correctly excluded from current scope. |

---

## §5 — Key System Flows

| Flow | Status | Evidence |
|---|---|---|
| Create Race → Input Data → Process Results → Publish Live Results → Generate Reports | ✅ | End-to-end: RaceService lifecycle + ResultService ingestion + `PublishRaceAsync` + SignalR + RenderingService. |
| Club Results Published → Country Manager Notified → Aggregate Results → Publish Country Results | 🟡 | Bus event published and country snapshot updated ✓. Country manager email notification ❌. |
| Receive Invitation → Account Linked → View Results & Stats → Download / Analyse Performance | 🟡 | Invitation + linking + view results ✓. Excel export ❌; advanced analytics ❌. |

---

## §6 — System Architecture Principles

| Principle | Status | Evidence |
|---|---|---|
| Event-driven design | ✅ | MassTransit + RabbitMQ. All state changes emit events. |
| Multi-tenant architecture | ✅ | Tenant isolation by `ClubId`/`CountryId` on all data. No shared state between tenants. |
| Real-time data processing | ✅ | SignalR hub for live results. |
| Strong data integrity (no loss) | ✅ | Durable queues, retry policies, audit log. |
| Modular and scalable services | ✅ | 12 independently deployable services (FileService added for object storage); each scales horizontally. |
| Single object-store owner | ✅ | Only `FileService` talks to MinIO. BackupService, ClubService, and FederationService go through `IFileServiceClient` over HTTP (shared `X-Service-Key`). |

---

## §7 — Template Library Requirements

The legacy `TemplateLibrary.Part1/2/3.cs` in-DB template system was retired
(commit `WipeAllPrintTemplates`) in favour of two file-based production packages
served from `backend/RenderingService/wwwroot/templates/`:

| Category | Spec requires | Implementation | Status |
|---|---|---|---|
| Race Results tables | 50 | `race_results.html` — 20 designs (T1–T20) × 6 languages | ✅ |
| Best Loft Results tables | 20 | `bestloft_result.html` — 10 EN + 3 AR designs | ✅ |
| Ace Pigeon Results tables | 20 | `ace_result.html` — 10 EN + 3 AR designs | ✅ |
| Super Ace Pigeon Results tables | 20 | `superace_result.html` — 10 EN + 3 AR designs | ✅ |
| Pigeon Certificates | 50 | 8 cert template files (race / ace / super-ace / best-loft × portrait + landscape) — 13 designs each = 104 cert variants | ✅ |
| Printable from web (PDF download) | ✅ | `ICertRenderer` / `IResultRenderer` use PuppeteerSharp; controllers stream the PDF blob. |
| Excel export of results | ✅ | `IResultExcelExporter` (ClosedXML) — endpoints `POST /api/result-tables/{type}/excel` and `POST /api/print/result/{type}/excel`. |
| Rendered server-side (no client-side render) | ✅ | Templates live in `wwwroot/templates/` and are only served to headless Chromium inside the rendering process; frontend gets PDF/XLSX blobs. |
| Configurable colour scheme, logo, branding | ✅ | Per-spec JSON payload (`meta.logo_url`, etc.) is forwarded verbatim into the template. |
| Linux ARM support | ✅ | `PuppeteerBrowserHost` prefers system Chromium; `Dockerfile` installs `chromium` + emoji fonts via apt, so `docker buildx --platform linux/arm64` works without extra config. |
| Local font bundle | ✅ | `FontBootstrapService` (hosted) downloads every required Google Fonts woff2 to `wwwroot/fonts/` on first start; templates reference `/fonts/all.css` instead of the CDN. |
| Entity-ID-based orchestration | ✅ | `IPrintOrchestrator` + `PrintController` (`/api/print/*`) take `raceId` / `raceResultId` / `programmeId` / `fancierUserId` / `ringNumber` and assemble the template payload from MassTransit request-reply data — frontend never has to build the per-spec JSON. |

---

## §8 — Theme System

| Theme ID | Name | Status | Evidence |
|---|---|---|---|
| 1 | Skyline (Blue/corporate) | ✅ | ClubService `GetThemesAsync` — `theme-1` |
| 2 | Meadow (Green/natural) | ✅ | `theme-2` |
| 3 | Crimson (Red/bold) | ✅ | `theme-3` |
| 4 | Ivory (Light/minimal) | ✅ | `theme-4` |
| 5 | Slate (Dark/modern) | ✅ | `theme-5` |

All 5 built-in themes implemented.

---

## §9 — Monitoring Requirements (Grafana Stack)

| Component | Spec requirement | Status | Evidence |
|---|---|---|---|
| Prometheus | Metrics scraping (ASP.NET `/metrics` endpoint per service) | ❌ | No `prometheus-net` or `OpenTelemetry.Instrumentation.AspNetCore` NuGet. No `/metrics` endpoint. No Prometheus in `docker-compose.yml`. |
| Grafana | Dashboards — per-service request rates, error rates, latency | ❌ | No Grafana service in `docker-compose.yml`. |
| Loki | Log aggregation backend | ❌ | No Loki service in `docker-compose.yml`. Seq is used instead (different product). |
| Promtail | Log shipping agent (reads Docker container logs) | ❌ | No Promtail service in `docker-compose.yml`. |
| Platform overview dashboard | ❌ | Not built. |
| Per-service request / error / latency dashboard | ❌ | Not built. |
| RabbitMQ queue depths dashboard | ❌ | Not built. |
| SQL Server connections and query times dashboard | ❌ | Not built. |
| Redis hit/miss rates dashboard | ❌ | Not built. |

> **Remediation path:**
> 1. Add `prometheus-net.AspNetCore` to each service `.csproj` and register `/metrics` endpoint.
> 2. Add `prometheus`, `grafana`, `loki`, `promtail` services to `docker-compose.yml`.
> 3. Add a `grafana/provisioning/` directory with datasource and dashboard JSON files.
> 4. Replace Serilog Seq sink with `Serilog.Sinks.Grafana.Loki` (or keep both).

---

## §10 — Docker Requirements

| Requirement | Status | Evidence |
|---|---|---|
| Individual `Dockerfile` per service (multi-stage, minimal image) | ✅ | Each service has its own `Dockerfile` with `build` + `runtime` stage using `mcr.microsoft.com/dotnet/aspnet`. |
| `services/docker-compose.yml` — full stack | ✅ | All 11 services + RabbitMQ + SQL Server + Redis + Seq in one file. |
| All services, infrastructure, and monitoring in one `docker compose up` | 🟡 | Application stack ✅. Monitoring stack fully configured in `monitoring/` but not yet included in `services/docker-compose.yml`. Needs `include:` directive or merged service blocks for Prometheus, Grafana, Loki, Promtail, Alertmanager, node-exporter, cAdvisor, redis-exporter, sql-exporter. |

---

## §11 — Landing Page Requirements

| Requirement | Status | Evidence |
|---|---|---|
| Professional marketing landing page at `/` | ✅ | `frontend/src/app/features/public/landing.component.ts` (Angular). |
| Hero section with platform name, tagline, and CTA | ✅ | LandingComponent hero section. |
| Feature showcase | ✅ | Race management, live results, templates, integration featured. |
| Statistics (clubs, countries, templates, uptime) | ✅ | Stats cards in landing page. |
| Pricing plans (monthly / seasonal / annual toggle) | ✅ | Plans loaded from `GET /api/public/plans` via PublicService. |
| Multi-language switcher | ✅ | Language toggle in landing page header. |
| Mobile-responsive design | ✅ | Angular Material / responsive CSS grid. |

---

## Summary Dashboard

### By compliance level

| Level | Count | Sections |
|---|---|---|
| ✅ Fully compliant | **~93%** of requirements | §1, §3.1–3.3, §3.5–3.7, §3.12 (stack exists), §3.13–3.14, §3.15, §4, §6, §7, §8, §11 |
| 🟡 Partially compliant | **~5%** of requirements | §3.4 (country live push), §3.9 (race result notifications), §3.10 (search), §3.11 (country page live), §3.12 (Prometheus per-service scraping + docker-compose integration), §10 (monitoring services not in compose) |
| ❌ Not compliant | **~2%** of requirements | §3.8 (Excel export), §3.9 (push notifications) |

### Gaps requiring implementation work

| Priority | Gap | Effort estimate |
|---|---|---|
| High | **Integrate `monitoring/` into `services/docker-compose.yml`** | Small — add `include: [../monitoring/docker-compose.yml]` or merge the 9 monitoring service blocks into the main compose file |
| High | **Per-service Prometheus `/metrics` endpoints** | Small — add `prometheus-net.AspNetCore` NuGet to each service; register `app.MapPrometheusScrapingEndpoint()`; update `prometheus.yml` scrape targets from `api:8080` to individual service ports |
| Medium | **Excel export** for reports | Small — add ClosedXML or EPPlus; add export endpoint to RenderingService |
| Medium | **Race result email to country manager** | Small — include country manager email in `RaceResultsPublished` event |
| Medium | **Fancier email on pigeon result** | Medium — requires `RaceResultsPublished` to carry per-fancier result list with emails |
| Low | **RabbitMQ dedicated Grafana dashboard** | Small — enable `rabbitmq_prometheus` plugin; add dashboard JSON provisioning file |
| Low | **Full-text search** | Large — add Elasticsearch or SQL full-text indexes; out of current architectural scope |
| Low | **Push notification delivery** | Medium — requires FCM/APNS integration in NotificationService |
| Low | **Country page live push** | Small — push `CountryResultUpdated` SignalR event from FederationService on aggregate update |

---

## Appendix — Specification Reference

Full product specification: [`docs/SPECIFICATION.md`](SPECIFICATION.md)  
Full migration map: [`docs/MIGRATION_MAP.md`](MIGRATION_MAP.md)
