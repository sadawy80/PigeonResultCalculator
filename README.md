# 🕊️ PigeonRacing Platform

Federation-grade pigeon racing results management — multi-tenant, real-time, cloud-ready.

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│            Angular 18 SPA  (nginx : 4200)           │
├─────────────────────────────────────────────────────┤
│         ASP.NET Core 9 REST + SignalR (:5000)       │
├──────────────┬──────────────────┬───────────────────┤
│  SQL Server  │      Redis       │   File Storage    │
│    : 1433    │     : 6379       │   /uploads        │
└──────────────┴──────────────────┴───────────────────┘
Observability: Prometheus :9090 | Grafana :3000 | Loki :3100
```

---

## Quick Start

```bash
git clone https://github.com/your-org/pigeon-racing.git
cd pigeon-racing
cp .env.example .env          # Fill in JWT_KEY, SQL_PASSWORD, SLACK_WEBHOOK_URL
docker compose up -d
```

SQL Server takes ~45s on first boot. Then:

| Service | URL | Credentials |
|---------|-----|-------------|
| App | http://localhost:4200 | admin@pigeonracing.com / Admin123! |
| Swagger | http://localhost:5000/swagger | — |
| Grafana | http://localhost:3000 | admin / admin123 |
| Prometheus | http://localhost:9090 | — |
| Alertmanager | http://localhost:9093 | — |

---

## Development

### Backend
```bash
docker compose up sqlserver redis -d
dotnet restore
cd PigeonRacing.API && dotnet run
```

### Frontend
```bash
cd pigeon-racing-ui
npm install && npm start        # http://localhost:4200
```

### Migrations
```bash
cd PigeonRacing.API
dotnet ef database update --project ../PigeonRacing.Infrastructure
```

---

## Project Structure

```
pigeon-racing/
├── PigeonRacing.Domain/            # Entities, enums, base classes
│   ├── Entities/
│   │   ├── Race.cs                 # Race, RaceCategory, RaceResult
│   │   ├── Programme.cs            # ClubProgramme, BestLoftResult,
│   │   │                           #   AcePigeonResult, SuperAcePigeonResult
│   │   ├── PrintTemplate.cs        # PrintTemplate (160 templates) + PrintJob
│   │   ├── Club.cs                 # Country, Club, ClubMembership
│   │   ├── ApplicationUser.cs      # Identity user
│   │   ├── Pigeon.cs               # Pigeon, PigeonLink
│   │   ├── Pages.cs                # ClubPage, CountryPage
│   │   └── Supporting.cs           # Invitations, Notifications, Reports
│   └── Enums/
│       ├── Enums.cs                # Core enums
│       ├── ProgrammeEnums.cs       # ScoringMethod, SuperAceQualification
│       └── TemplateEnums.cs        # TemplateCategory, TemplateStyle, PaperSize
│
├── PigeonRacing.Application/       # CQRS — MediatR handlers + DTOs
│   ├── Common/
│   │   ├── Interfaces/Interfaces.cs   # IAppDbContext, ICurrentUserService …
│   │   └── Result.cs                  # Result<T>, ApiResponse<T>, PagedResult<T>
│   └── Features/
│       ├── Auth/                   # Login, register, refresh token
│       ├── Races/                  # Race CRUD + ETS ingestion
│       ├── Results/                # Race result processing + ranking
│       ├── Clubs/                  # Club management + 5 built-in themes
│       ├── CountryResults/         # Aggregate country-level results
│       ├── Programmes/
│       │   ├── ProgrammeFeatures.cs      # Programme CRUD + race membership
│       │   └── CalculationEngines.cs     # Best Loft, Ace Pigeon, Super Ace
│       └── Templates/
│           └── TemplateFeatures.cs       # Template query + render + print jobs
│
├── PigeonRacing.Infrastructure/    # EF Core, migrations, services
│   ├── Persistence/AppDbContext.cs
│   ├── Services/                   # VelocityCalc, ETSParser, Cache, Email, Storage
│   ├── wwwroot/templates/            # 12 production HTML templates (8 cert + 4 result)
│   ├── wwwroot/fonts/                # Google Fonts bundled at startup by FontBootstrapService
│   ├── Services/
│   │   ├── PuppeteerBrowserHost.cs   # Shared headless Chromium (ARM-aware)
│   │   ├── CertRenderer.cs           # 8 cert templates → PDF
│   │   ├── ResultRenderer.cs         # 4 result-table templates → PDF
│   │   ├── ResultExcelExporter.cs    # ClosedXML → XLSX
│   │   ├── DesignCatalog.cs          # Lists design IDs per type/orientation
│   │   ├── PrintOrchestrator.cs      # Entity-ID → JSON → renderer
│   │   └── FontBootstrapService.cs   # Downloads Google Fonts on first start
│   ├── Migrations/
│   │   ├── …001_InitialCreate.cs
│   │   ├── …002_AddProgrammeAndAggregateResults.cs
│   │   └── …003_AddPrintTemplatesAndJobs.cs
│   └── DependencyInjection.cs      # Wires all services + TemplateSeedHostedService
│
├── PigeonRacing.API/               # ASP.NET Core 9 Web API
│   ├── Controllers/
│   │   ├── Controllers.cs          # Auth, Races, Results, CountryResults, Clubs, Themes
│   │   ├── ProgrammeControllers.cs # Programmes, BestLoft, AcePigeon, SuperAcePigeon
│   │   ├── TemplatesController.cs  # GET /render /print /jobs
│   │   ├── PublicController.cs     # /api/public/clubs/:slug (no auth)
│   │   └── AdminController.cs      # SuperAdmin console endpoints
│   ├── Hubs/LiveRaceHub.cs         # SignalR live race tracking
│   ├── Observability/Metrics.cs    # 20 Prometheus metrics + health checks
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
│
├── pigeon-racing-ui/               # Angular 18 SPA
│   ├── src/app/
│   │   ├── core/
│   │   │   ├── models/             # index.ts, programme.models.ts, template.models.ts
│   │   │   └── services/           # api, programme-api, template-api, auth, theme, signalr
│   │   ├── features/
│   │   │   ├── auth/               # Login + register
│   │   │   ├── club/
│   │   │   │   ├── club-dashboard.component.ts
│   │   │   │   ├── race-form.component.ts
│   │   │   │   ├── race-detail.component.ts      # ETS upload + live SignalR
│   │   │   │   ├── programme-list.component.ts   # Programme list + detail
│   │   │   │   ├── programme-form.component.ts   # Scoring configuration
│   │   │   │   ├── result-pages.component.ts     # 4 result pages + Print button
│   │   │   │   ├── theme-picker.component.ts
│   │   │   │   ├── club-members.component.ts
│   │   │   │   └── templates/
│   │   │   │       ├── design-picker.component.ts     # Design + language + orientation picker
│   │   │   │       ├── print-button.component.ts      # Drop-in print button
│   │   │   │       ├── certificate-picker.component.ts# Cert-specific entry
│   │   │   │       └── templates-page.component.ts    # /club/templates landing
│   │   │   ├── country/            # Country manager views
│   │   │   ├── fancier/            # Fancier dashboard + notifications
│   │   │   ├── public/             # Public club pages
│   │   │   └── admin/              # SuperAdmin console
│   │   └── shared/components/      # ShellComponent (nav + theme), UnauthorizedComponent
│   ├── Dockerfile
│   └── nginx.conf
│
├── monitoring/
│   ├── prometheus/
│   │   ├── prometheus.yml
│   │   └── rules/                  # api, infrastructure, database, business (4 files)
│   ├── grafana/
│   │   ├── dashboards/             # 7 JSON dashboards (auto-provisioned)
│   │   └── provisioning/           # datasources, dashboards, alerting
│   ├── loki/loki.yml
│   ├── promtail/promtail.yml
│   └── alertmanager/alertmanager.yml
│
├── docker-compose.yml
├── .env.example
└── README.md
```

---

## API Reference

### Auth
| Method | Endpoint | Auth |
|--------|----------|------|
| POST | /api/auth/register | — |
| POST | /api/auth/login | — |
| POST | /api/auth/refresh | — |
| GET | /api/auth/me | JWT |
| POST | /api/auth/logout | JWT |

### Races
| Method | Endpoint |
|--------|----------|
| GET | /api/races/club/:clubId |
| POST | /api/races |
| GET/PUT/DELETE | /api/races/:id |
| POST | /api/races/:id/publish |
| POST | /api/races/:id/ingest (ETS upload) |
| GET | /api/results/race/:raceId |
| POST | /api/results/:raceId/process |

### Programmes & Aggregate Results
| Method | Endpoint |
|--------|----------|
| GET/POST | /api/programmes |
| GET/PUT/DELETE | /api/programmes/:id |
| POST/DELETE | /api/programmes/:id/races |
| POST | /api/programmes/:id/calculate |
| POST | /api/programmes/:id/publish |
| GET | /api/best-loft/programme/:id |
| GET | /api/ace-pigeon/programme/:id |
| GET | /api/super-ace-pigeon/programme/:id |

### Printing — Certificates & Results
| Method | Endpoint | Returns |
|--------|----------|---------|
| GET  | /api/print/designs/cert/{race\|ace\|super-ace\|best-loft} | `{portrait, landscape}` design lists |
| GET  | /api/print/designs/result/{race\|ace\|super-ace\|best-loft} | flat design list |
| POST | /api/print/cert/{race\|ace\|super-ace\|best-loft} | **PDF** (entity-ID-driven) |
| POST | /api/print/result/{type}/pdf | **PDF** (multi-page table) |
| POST | /api/print/result/{type}/excel | **XLSX** (ClosedXML) |
| POST | /api/certificates/{type} | low-level: raw JSON payload → PDF |
| POST | /api/result-tables/{type}/{pdf\|excel} | low-level: raw JSON payload → blob |

The high-level `/api/print/*` endpoints take entity IDs (`raceResultId`, `programmeId`, `ringNumber`, `fancierUserId`) and assemble the per-template JSON internally — the frontend never has to know the template schema.

### Monitoring
| Endpoint | Description |
|----------|-------------|
| GET /metrics | Prometheus scrape |
| GET /health | Full health check (JSON) |
| GET /health/ready | DB connectivity |
| GET /health/live | API self-check |

---

## Templates (file-based packages)

Templates live as static HTML files in [`backend/RenderingService/wwwroot/templates/`](backend/RenderingService/wwwroot/templates/) and are rendered server-side by headless Chromium. The old DB-template library was retired; the table is wiped on next migrate.

### Certificate package — 8 files, ~104 design variants

| Type | Portrait file | Landscape file | Designs (per orientation) |
|---|---|---|---|
| Race | `race_cert_portrait.html` | `race_cert_landscape.html` | R1–R10, AR-R1..3 |
| Ace | `ace_cert_portrait.html` | `ace_cert_landscape.html` | A1–A10, AR-A1..3 |
| Super Ace | `superace_cert_portrait.html` | `superace_cert_landscape.html` | S1–S10, AR-S1..3 |
| Best Loft | `bestloft_cert_portrait.html` | `bestloft_cert_landscape.html` | L1–L10, AR-L1..3 |

### Result-table package — 4 files, ~59 design variants

| File | Designs | Languages |
|---|---|---|
| `race_results.html` | T1–T20 | en, ar, fa, es, de, zh |
| `ace_result.html` | A1–A10 + AR-A1..3 | en, ar |
| `superace_result.html` | SA1–SA10 + AR-SA1..3 | en, ar |
| `bestloft_result.html` | L1–L10 + AR-L1..3 | en, ar |

### Print / PDF / Excel workflow

1. Frontend opens the design picker, fetches the design list, user picks design + language (+ orientation for certs).
2. Frontend `POST`s to `/api/print/...` with entity IDs.
3. Backend `PrintOrchestrator` asks RaceService / ClubService / FederationService over RabbitMQ for the underlying data, then assembles the spec-shaped JSON.
4. `CertRenderer` / `ResultRenderer` injects the JSON into the template via `window.CERT_DATA` / `window.RACE_DATA` etc., waits for `body[data-render-status="complete"]`, and returns the PDF.
5. For result tables, `ResultExcelExporter` (ClosedXML) renders the same payload to an `.xlsx` workbook on request.

### Deployment notes

- **Fonts**: `FontBootstrapService` downloads all required Google Fonts woff2 files to `wwwroot/fonts/` on first start and writes `all.css`. Templates reference `/fonts/all.css` so renders work offline.
- **Linux ARM**: `Dockerfile` installs system Chromium (`apt-get install chromium`), so `docker buildx --platform linux/arm64,linux/amd64` produces working images on both architectures. PuppeteerSharp's `BrowserFetcher` is skipped on ARM via `PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium`.

---

## Roles

| Role | Access |
|------|--------|
| SuperAdmin | Everything |
| CountryManager | Country + all clubs in country |
| ClubManager | Own club — races, results, programmes, print |
| Fancier | Own pigeons, results, notifications |

---

## Programme Scoring

| Method | Formula |
|--------|---------|
| AverageVelocity | Average m/min across races |
| PointsByRank | 1st=N pts, 2nd=N-1 pts … |
| PointsByVelocityPercentage | % of race winner's velocity |
| TotalVelocity | Sum of all velocities |

**Super Ace qualification options:** AllRacesRequired | MinimumRaceCount | MinimumRacePercentage

---

## Monitoring Dashboards (Grafana)

| # | Dashboard | Key content |
|---|-----------|-------------|
| 01 | Platform Overview | SLO indicators, service health, error rate |
| 02 | API Performance | Latency heatmap, top slow routes, .NET runtime |
| 03 | Business Metrics | Race activity, ETS ingestion, programme calculations |
| 04 | Infrastructure | Host CPU/RAM/disk, container resources |
| 05 | Database & Cache | SQL Server queries, connection pool, Redis hit rate |
| 06 | Logs Explorer | Error logs, slow requests, auth events, ETS events |
| 07 | Alerts Overview | Firing alerts, SLO compliance, error budget burn |

Alert routing: Critical → Slack + PagerDuty + email. Warning → Slack only.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Angular 18, Standalone Components, Signals |
| Backend | ASP.NET Core 9, Clean Architecture, MediatR |
| ORM | Entity Framework Core 9 + SQL Server 2022 |
| Auth | JWT Bearer + Refresh Tokens |
| Cache | Redis 7 (in-memory fallback) |
| Real-time | SignalR |
| Metrics | Prometheus + Grafana 10 |
| Logs | Serilog JSON → Promtail → Loki |
| Alerts | Alertmanager → Slack / PagerDuty / Email |
| Containers | Docker + Docker Compose |

---

## License

MIT
