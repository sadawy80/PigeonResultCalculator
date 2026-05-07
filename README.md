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
│   ├── Templates/
│   │   ├── TemplateLibrary.Part1.cs  # RR-01..25 (Race Results)
│   │   ├── TemplateLibrary.Part2.cs  # RR-26..50, BL-01..20, AP-01..20
│   │   ├── TemplateLibrary.Part3.cs  # SA-01..20, CERT-01..50
│   │   ├── TemplateRenderer.cs       # {{variable}}, #each, #if resolver
│   │   └── TemplateSeeder.cs         # Seeds all 160 on startup (idempotent)
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
│   │   │   │       ├── template-browser.component.ts  # 160-template grid picker
│   │   │   │       ├── print-button.component.ts      # Drop-in print/PDF button
│   │   │   │       └── templates-page.component.ts    # /club/templates
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

### Print Templates
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/templates | List all 160 templates (filterable) |
| GET | /api/templates/:id | Template metadata |
| POST | /api/templates/:id/render | Returns substituted HTML |
| GET | /api/templates/:id/print | Auto-print HTML page |
| POST | /api/templates/jobs | Create print job record |
| GET | /api/templates/jobs/club/:id | Job history |

### Monitoring
| Endpoint | Description |
|----------|-------------|
| GET /metrics | Prometheus scrape |
| GET /health | Full health check (JSON) |
| GET /health/ready | DB connectivity |
| GET /health/live | API self-check |

---

## Templates (160 total)

| Category | Count | Styles available |
|----------|-------|-----------------|
| Race Results | 50 | Classic, Modern, Elegant, Minimal, Sporty, Heritage, Dark, Branded, Landscape/Portrait |
| Best Loft | 20 | All 10 style families |
| Ace Pigeon | 20 | All 10 style families |
| Super Ace Pigeon | 20 | All 10 style families |
| Certificates | 50 | Portrait + Landscape, Podium (🥇🥈🥉), Programme-specific, Branded |

### Print / PDF workflow
The `/api/templates/:id/print` endpoint returns a self-contained HTML page with `window.print()` injected. Opening in a new tab triggers the browser's print dialog. Select **Save as PDF** for a formatted PDF — no client-side dependencies needed.

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
