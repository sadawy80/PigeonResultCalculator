# Monolith → Microservices Migration Map

This document maps every entity, service, handler, controller action, and infrastructure class
from the monolith (`backend/`) to its new home in the microservices (`services/`).

---

## 1. Domain Entities

| Monolith entity (`backend/Domain/Entities/`) | Microservice | New location |
|---|---|---|
| `ApplicationUser` | IdentityService | `Models/ApplicationUser.cs` |
| `RefreshToken` | IdentityService | `Models/ApplicationUser.cs` |
| `Country` | FederationService | `Models/Country.cs` |
| `CountryPage` | FederationService | `Models/CountryPage.cs` |
| `CountryResult` | FederationService | `Models/CountryResult.cs` |
| `CountryResultRace` | FederationService | `Models/CountryResult.cs` |
| `CountryResultEntry` | FederationService | `Models/CountryResult.cs` |
| `Club` | ClubService | `Models/Club.cs` |
| `ClubMembership` | ClubService | `Models/ClubMembership.cs` |
| `ClubPage` | ClubService | `Models/ClubPage.cs` |
| `Invitation` | ClubService | `Models/Invitation.cs` |
| `Notification` | ClubService | `Models/Notification.cs` |
| `ClubProgramme` | ClubService | `Models/ClubProgramme.cs` |
| `ProgrammeRace` | ClubService | `Models/ClubProgramme.cs` |
| `BestLoftResult` | ClubService | `Models/ClubProgramme.cs` |
| `AcePigeonResult` | ClubService | `Models/ClubProgramme.cs` |
| `SuperAcePigeonResult` | ClubService | `Models/ClubProgramme.cs` |
| `Race` | RaceService | `Models/Race.cs` |
| `RaceCategory` | RaceService | `Models/Race.cs` |
| `RaceResult` | RaceService | `Models/Race.cs` |
| `DataIngestionLog` | RaceService | `Models/Race.cs` |
| `Pigeon` | RaceService | `Models/Pigeon.cs` |
| `PigeonLink` | ClubService | `Models/PigeonLink.cs` |
| `PrintTemplate` | RenderingService | `Models/PrintTemplate.cs` |
| `PrintJob` | RenderingService | `Models/PrintTemplate.cs` |
| `PageTemplate` | RenderingService | `Models/PrintTemplate.cs` |
| `ExternalLink` | IntegrationService | `Models/ExternalLink.cs` |
| `SubscriptionPlan` | SubscriptionService | `Models/Subscription.cs` |
| `CountrySubscription` | SubscriptionService | `Models/Subscription.cs` |
| `ClubSubscription` | SubscriptionService | `Models/Subscription.cs` |
| `DomainEvent` | AdminService | `Models/AuditEvent.cs` |
| `Report` | *(not migrated — absorbed into PrintJob + RenderingService)* | — |

**New entities (microservices-only):**

| New entity | Service | Purpose |
|---|---|---|
| `RaceSnapshotCache` | FederationService | Caches published race summaries from RaceService to avoid repeated bus calls |

---

## 2. Enums

| Monolith file | Microservice location | Notes |
|---|---|---|
| `Domain/Enums/Enums.cs` — all enums | `Common/Enums.cs` | Shared across all services |
| `Domain/Enums/ProgrammeEnums.cs` | `Common/Enums.cs` | Merged into single file |
| `Domain/Enums/TemplateEnums.cs` | `Common/Enums.cs` | Merged into single file |
| `ExternalLinkStatus` *(was inline on entity)* | `Common/Enums.cs` | Promoted to shared enum |
| `AuditSeverity` *(new)* | `Common/Enums.cs` | Added for AdminService audit log |

---

## 3. Domain Common / Base Types

| Monolith | Microservice | Notes |
|---|---|---|
| `Domain/Common/BaseEntity.cs` — `BaseEntity` | `Common/BaseEntity.cs` | Identical |
| `Domain/Common/BaseEntity.cs` — `AuditableEntity` | `Common/BaseEntity.cs` | Identical |
| `Application/Common/Result.cs` — `Result`, `Result<T>` | `Common/Result.cs` | Identical |
| `Application/Common/Result.cs` — `ApiResponse<T>` | `Common/Result.cs` | Identical |
| `Application/Common/Result.cs` — `PagedResult<T>`, `PagedQuery` | `Common/Result.cs` | Identical |

---

## 4. Application Interfaces

| Monolith interface | Microservice | New form |
|---|---|---|
| `IAppDbContext` | *(split)* | Each service has its own `DbContext`; no single shared interface |
| `ICurrentUserService` | All API services | Each service has its own `ICurrentUserService` / `CurrentUserService` |
| `IEmailService` | NotificationService | `Services/EmailService.cs` — `IEmailService` + `SmtpEmailService` |
| `IFileStorageService` | Common | `Common/Services/IFileStorageService.cs` — `LocalDiskFileStorageService` |
| `IVelocityCalculator` | RaceService | `Services/SpeedCalculator.cs` — renamed `ISpeedCalculator` / `SpeedCalculator` |
| `INotificationService` | NotificationService | Replaced by MassTransit event publishing (`IPublishEndpoint`) |
| `ILiveResultsBroadcaster` | RaceService | `Hubs/LiveRaceHub.cs` + `LiveResultsBroadcaster` |
| `IDomainEventDispatcher` | AdminService | `Services/AuditService.cs` — `AuditService` |
| `IPdfGeneratorService` | RenderingService | `Services/PdfGeneratorService.cs` — unchanged interface |
| `ICacheService` (Redis) | Common | `Common/Services/ICacheService.cs` — `RedisCacheService` + `MemoryCacheService` |
| `IETSFileParser` | RaceService | `Services/ETSFileParser.cs` — unchanged |
| `IExternalPlatformCallbackService` | IntegrationService | `Services/ExternalPlatformCallbackService.cs` — unchanged |

---

## 5. Application Feature Handlers → Service Methods

### Auth (`backend/Application/Features/Auth/`)

| Monolith handler / command | Microservice | New location / method |
|---|---|---|
| `RegisterHandler` | IdentityService | `Services/AuthService.cs` — `RegisterAsync` |
| `LoginHandler` | IdentityService | `Services/AuthService.cs` — `LoginAsync` |
| `RefreshTokenHandler` | IdentityService | `Services/AuthService.cs` — `RefreshTokenAsync` |
| `RevokeTokenHandler` | IdentityService | `Services/AuthService.cs` — `RevokeTokenAsync` |
| `GetCurrentUserHandler` | IdentityService | `Services/AuthService.cs` — `GetCurrentUserAsync` |
| `ForgotPasswordHandler` | IdentityService | `Services/AuthService.cs` — `ForgotPasswordAsync` |
| `ResetPasswordHandler` | IdentityService | `Services/AuthService.cs` — `ResetPasswordAsync` |
| `VerifyEmailHandler` | IdentityService | `Services/AuthService.cs` — `VerifyEmailAsync` |
| `ResendVerificationHandler` | IdentityService | `Services/AuthService.cs` — `ResendVerificationAsync` |
| `JwtTokenService` | IdentityService | `Services/TokenService.cs` — `TokenService` |

### Clubs (`backend/Application/Features/Clubs/`)

| Monolith handler | Microservice | New location / method |
|---|---|---|
| `CreateClubHandler` | ClubService | `Services/ClubService.cs` — `CreateClubAsync` |
| `UpdateClubBrandingHandler` | ClubService | `Services/ClubService.cs` — `UpdateBrandingAsync` |
| `SetThemeHandler` | ClubService | `Services/ClubService.cs` — `SetThemeAsync` |
| `GetThemesHandler` | ClubService | `Services/ClubService.cs` — `GetThemesAsync` |
| `SendInvitationHandler` | ClubService | `Services/ClubService.cs` — `SendInvitationAsync` → publishes `MemberInvited` bus event |
| `AcceptInvitationHandler` | ClubService | `Services/ClubService.cs` — `AcceptInvitationAsync` |
| `RemoveMemberHandler` | ClubService | `Services/ClubService.cs` — `RemoveMemberAsync` |
| `LinkPigeonHandler` | ClubService | `Services/ClubService.cs` — `LinkPigeonAsync` |
| `GetClubHandler` | ClubService | `Services/ClubService.cs` — `GetClubAsync` |
| `GetClubMembersHandler` | ClubService | `Services/ClubService.cs` — `GetMembersAsync` |
| `GetMyNotificationsHandler` | ClubService | `Services/ClubService.cs` — `GetNotificationsAsync` |
| `MarkNotificationReadHandler` | ClubService | `Services/ClubService.cs` — `MarkNotificationReadAsync` |
| `GetClubInvitationsHandler` | ClubService | `Services/ClubService.cs` — `GetInvitationsAsync` |
| `GetClubPageInfoHandler` | ClubService | `Services/ClubService.cs` — `GetClubPageInfoAsync` |
| `UpdateClubAnnouncementsHandler` | ClubService | `Services/ClubService.cs` — `UpdateAnnouncementsAsync` |
| `UpdateSlugHandler` | ClubService | `Services/ClubService.cs` — `UpdateSlugAsync` |
| `BuiltInThemes` (static class) | ClubService | `Services/ClubService.cs` — inline static data |

### Races (`backend/Application/Features/Races/`)

| Monolith handler | Microservice | New location / method |
|---|---|---|
| `CreateRaceHandler` | RaceService | `Services/RaceService.cs` — `CreateRaceAsync` |
| `UpdateRaceHandler` | RaceService | `Services/RaceService.cs` — `UpdateRaceAsync` |
| `StartRaceHandler` | RaceService | `Services/RaceService.cs` — `StartRaceAsync` |
| `CompleteRaceHandler` | RaceService | `Services/RaceService.cs` — `CompleteRaceAsync` |
| `PublishRaceHandler` | RaceService | `Services/RaceService.cs` — `PublishRaceAsync` → publishes `RaceResultsPublished` bus event |
| `DeleteRaceHandler` | RaceService | `Services/RaceService.cs` — `DeleteRaceAsync` |
| `GetRaceHandler` | RaceService | `Services/RaceService.cs` — `GetRaceAsync` |
| `GetClubRacesHandler` | RaceService | `Services/RaceService.cs` — `GetClubRacesAsync` |
| `GetLiveRacesHandler` | RaceService | `Services/RaceService.cs` — `GetLiveRacesAsync` |

### Results (`backend/Application/Features/Results/`)

| Monolith handler | Microservice | New location / method |
|---|---|---|
| `AddManualResultHandler` | RaceService | `Services/ResultService.cs` — `AddManualResultAsync` |
| `IngestETSFileHandler` | RaceService | `Services/ResultService.cs` — `IngestETSFileAsync` |
| `ProcessRaceResultsHandler` | RaceService | `Services/ResultService.cs` — `ProcessRaceResultsAsync` |
| `GetRaceResultsHandler` | RaceService | `Services/ResultService.cs` — `GetRaceResultsAsync` |
| `GetFancierResultsHandler` | RaceService | `Services/ResultService.cs` — `GetFancierResultsAsync` |
| `LinkResultToFancierHandler` | RaceService | `Services/ResultService.cs` — `LinkResultToFancierAsync` |
| `DeleteRaceResultHandler` | RaceService | `Services/ResultService.cs` — `DeleteRaceResultAsync` |
| `GetIngestionLogsHandler` | RaceService | `Services/ResultService.cs` — `GetIngestionLogsAsync` |
| `ResultLimitHelper` (static) | RaceService | `Services/ResultService.cs` — inline helpers |

### Programmes (`backend/Application/Features/Programmes/`)

| Monolith handler | Microservice | New location / method |
|---|---|---|
| `CreateProgrammeHandler` | ClubService | `Services/ProgrammeService.cs` — `CreateProgrammeAsync` |
| `UpdateProgrammeHandler` | ClubService | `Services/ProgrammeService.cs` — `UpdateProgrammeAsync` |
| `AddRaceToProgrammeHandler` | ClubService | `Services/ProgrammeService.cs` — `AddRaceAsync` |
| `RemoveRaceFromProgrammeHandler` | ClubService | `Services/ProgrammeService.cs` — `RemoveRaceAsync` |
| `PublishProgrammeHandler` | ClubService | `Services/ProgrammeService.cs` — `PublishProgrammeAsync` |
| `DeleteProgrammeHandler` | ClubService | `Services/ProgrammeService.cs` — `DeleteProgrammeAsync` |
| `GetProgrammeHandler` | ClubService | `Services/ProgrammeService.cs` — `GetProgrammeAsync` |
| `GetClubProgrammesHandler` | ClubService | `Services/ProgrammeService.cs` — `GetClubProgrammesAsync` |
| `CalculateProgrammeResultsHandler` | ClubService | `Services/CalculationEngines.cs` — `CalculateProgrammeResultsAsync` |
| `CalculateBestLoft` (private) | ClubService | `Services/CalculationEngines.cs` — `CalculateBestLoft` |
| `CalculateAcePigeon` (private) | ClubService | `Services/CalculationEngines.cs` — `CalculateAcePigeon` |
| `CalculateSuperAce` (private) | ClubService | `Services/CalculationEngines.cs` — `CalculateSuperAce` |
| `ComputeScore` (private) | ClubService | `Services/CalculationEngines.cs` — `ComputeScore` |
| `GetBestLoftResultsHandler` | ClubService | `Services/ProgrammeService.cs` — `GetBestLoftResultsAsync` |
| `GetAcePigeonResultsHandler` | ClubService | `Services/ProgrammeService.cs` — `GetAcePigeonResultsAsync` |
| `GetSuperAcePigeonResultsHandler` | ClubService | `Services/ProgrammeService.cs` — `GetSuperAceResultsAsync` |

### Templates (`backend/Application/Features/Templates/`)

| Monolith handler | Microservice | New location / method |
|---|---|---|
| `GetTemplatesHandler` | RenderingService | `Services/TemplateService.cs` — kept for legacy `/api/templates` reads (DB now empty) |
| `RenderTemplateHandler` | RenderingService | Replaced by `Services/PrintOrchestrator.cs` (file-based templates) |
| `RenderTemplateHandler.Build*Data` | RenderingService | Replaced by per-type builders inside `PrintOrchestrator` |
| `CreatePrintJobHandler` / `GetPrintJobsHandler` | RenderingService | Job table retained for audit; new requests bypass it and stream PDF/XLSX directly. |

#### New rendering pipeline (added 2026-05-12)

| Capability | New file |
|---|---|
| Headless Chromium host (singleton; ARM Linux aware) | `RenderingService/Services/PuppeteerBrowserHost.cs` |
| File-based cert renderer (8 templates) | `RenderingService/Services/CertRenderer.cs` |
| File-based result renderer (4 templates) | `RenderingService/Services/ResultRenderer.cs` |
| Excel export (ClosedXML) of result tables | `RenderingService/Services/ResultExcelExporter.cs` |
| Local font bundling at startup | `RenderingService/Services/FontBootstrapService.cs` |
| Hardcoded design catalogue per type | `RenderingService/Services/DesignCatalog.cs` |
| Entity-ID-based orchestration over MassTransit | `RenderingService/Services/PrintOrchestrator.cs` |
| Low-level cert/result endpoints (raw JSON payload) | `RenderingService/Controllers/CertificatesController.cs` + `ResultsRenderController.cs` |
| High-level entity-ID endpoints | `RenderingService/Controllers/PrintController.cs` (`/api/print/*`) |
| 12 production HTML templates | `RenderingService/wwwroot/templates/*.html` |

### Country Results (`backend/Application/Features/CountryResults/`)

| Monolith handler | Microservice | New location / method |
|---|---|---|
| `CreateCountryResultHandler` | FederationService | `Services/FederationService.cs` — `CreateCountryResultAsync` |
| `PublishCountryResultHandler` | FederationService | `Services/FederationService.cs` — `PublishCountryResultAsync` |
| `GetCountryResultHandler` | FederationService | `Services/FederationService.cs` — `GetCountryResultAsync` |
| `GetCountryResultsHandler` | FederationService | `Services/FederationService.cs` — `GetCountryResultsAsync` |

### Integration (`backend/Application/Features/Integration/`)

| Monolith handler | Microservice | New location / method |
|---|---|---|
| `RequestExternalLinkHandler` | IntegrationService | `Services/IntegrationService.cs` — `RequestExternalLinkAsync` |
| `ReviewLinkRequestHandler` | IntegrationService | `Services/IntegrationService.cs` — `ReviewLinkRequestAsync` |
| `RevokeLinkHandler` | IntegrationService | `Services/IntegrationService.cs` — `RevokeLinkAsync` |
| `GetClubLinksHandler` | IntegrationService | `Services/IntegrationService.cs` — `GetClubLinksAsync` |
| `GetMyLinksHandler` | IntegrationService | `Services/IntegrationService.cs` — `GetMyLinksAsync` |
| `GetLinkedRaceResultsHandler` | IntegrationService | `Services/IntegrationDataService.cs` — `GetFancierRaceResultsAsync` |
| `GetLinkedAcePigeonHandler` | IntegrationService | `Services/IntegrationDataService.cs` — `GetAcePigeonResultsAsync` |
| `GetLinkedSuperAceHandler` | IntegrationService | `Services/IntegrationDataService.cs` — `GetSuperAceResultsAsync` |
| `GetLinkedBestLoftHandler` | IntegrationService | `Services/IntegrationDataService.cs` — `GetBestLoftResultsAsync` |
| `GetLinkedSummaryHandler` | IntegrationService | `Services/IntegrationDataService.cs` — `GetSummaryAsync` |
| `IntegrationTokenHelper` (static) | IntegrationService | `Services/IntegrationService.cs` — `ValidateTokenAsync` |

---

## 6. Infrastructure Services

| Monolith class (`backend/Infrastructure/Services/`) | Microservice | New location |
|---|---|---|
| `MinIO storage`        | **FileService** | `FileService/Services/MinioFileStorageService.cs` — owns the public + private buckets. Other services call it via `IFileServiceClient` in Common. |
| `VelocityCalculator` | RaceService | `Services/SpeedCalculator.cs` — renamed `SpeedCalculator`; same Haversine formula |
| `RedisCacheService` | Common | `Common/Services/ICacheService.cs` — `RedisCacheService` |
| `MemoryCacheService` | Common | `Common/Services/ICacheService.cs` — `MemoryCacheService` |
| `CurrentUserService` | All API services | Each service has its own `Services/CurrentUserService.cs` |
| `ETSFileParser` | RaceService | `Services/ETSFileParser.cs` — unchanged |
| `ExternalPlatformCallbackService` | IntegrationService | `Services/ExternalPlatformCallbackService.cs` — unchanged |
| `PdfGeneratorService` | RenderingService | `Services/PdfGeneratorService.cs` — unchanged |
| `PrintJobProcessorService` | RenderingService | `Services/PrintJobProcessorService.cs` — unchanged |
| `SmtpEmailService` (in SupportingServices) | NotificationService | `Services/EmailService.cs` — `SmtpEmailService` |
| `INotificationService` (in SupportingServices) | NotificationService | Replaced by MassTransit consumers |

---

## 7. Template Engine

The original DB-stored template library (`TemplateLibrary.Part1/2/3.cs`) was
retired. Templates now live as static HTML files served from
`backend/RenderingService/wwwroot/templates/` and rendered by PuppeteerSharp.

| Old location | Status |
|---|---|
| `Infrastructure/Templates/TemplateLibrary.Part1/2/3.cs` | Deleted. |
| `Infrastructure/Templates/TemplateSeeder.cs` | Stubbed to a no-op (kept so Program.cs still compiles). |
| `Infrastructure/Templates/TemplateRenderer.cs` | Retained — still used by the legacy DB-template path if anyone re-seeds. |
| `Infrastructure/Templates/TemplateLocales.cs` | Retained. |

| New location | Purpose |
|---|---|
| `RenderingService/wwwroot/templates/*.html` | 12 production templates (8 cert + 4 result). |
| `RenderingService/wwwroot/fonts/` | Bundled Google Fonts (downloaded at startup). |
| `RenderingService/Data/Migrations/…_WipeAllPrintTemplates.cs` | `DELETE FROM PrintTemplates` on next migrate. |

---

## 8. Database Contexts

| Monolith | Microservice | Database |
|---|---|---|
| `AppDbContext` (one context, all tables) | IdentityService `IdentityDbContext` | `PRC_Identity` |
| | ClubService `ClubDbContext` | `PRC_Club` |
| | RaceService `RaceDbContext` | `PRC_Race` |
| | FederationService `FederationDbContext` | `PRC_Federation` |
| | RenderingService `RenderingDbContext` | `PRC_Rendering` |
| | IntegrationService `IntegrationDbContext` | `PRC_Integration` |
| | SubscriptionService `SubscriptionDbContext` | `PRC_Subscription` |
| | AdminService `AdminDbContext` | `PRC_Admin` |
| NotificationService | *(no DB — fire and forget)* | — |
| PublicService | *(no DB — reads via bus)* | — |
| ApiGateway | *(no DB — routes only)* | — |

---

## 9. API Controllers & Action Methods

### AuthController

| Monolith `backend/API/Controllers/Controllers.cs` | Microservice | Service |
|---|---|---|
| `POST /api/auth/register` | `POST /api/auth/register` | IdentityService |
| `POST /api/auth/login` | `POST /api/auth/login` | IdentityService |
| `POST /api/auth/refresh` | `POST /api/auth/refresh` | IdentityService |
| `POST /api/auth/revoke` | `POST /api/auth/revoke` | IdentityService |
| `GET /api/auth/me` | `GET /api/auth/me` | IdentityService |
| `POST /api/auth/change-password` | `POST /api/auth/change-password` | IdentityService |
| `POST /api/auth/forgot-password` | `POST /api/auth/forgot-password` | IdentityService |
| `POST /api/auth/reset-password` | `POST /api/auth/reset-password` | IdentityService |
| `GET /api/auth/verify-email` | `GET /api/auth/verify-email` | IdentityService |
| `POST /api/auth/resend-verification` | `POST /api/auth/resend-verification` | IdentityService |

### RacesController

| Monolith `POST /api/races` | Microservice | Notes |
|---|---|---|
| `POST /api/races` | `POST /api/races` | RaceService |
| `PUT /api/races/{raceId}` | `PUT /api/races/{raceId}` | RaceService |
| `POST /api/races/{raceId}/start` | `POST /api/races/{raceId}/start` | RaceService |
| `POST /api/races/{raceId}/complete` | `POST /api/races/{raceId}/complete` | RaceService |
| `POST /api/races/{raceId}/publish` | `POST /api/races/{raceId}/publish` | RaceService — also publishes `RaceResultsPublished` event |
| `DELETE /api/races/{raceId}` | `DELETE /api/races/{raceId}` | RaceService |
| `GET /api/races/{raceId}` | `GET /api/races/{raceId}` | RaceService |
| `GET /api/races/club/{clubId}` | `GET /api/races/club/{clubId}` | RaceService |
| `GET /api/races/club/{clubId}/live` | `GET /api/races/club/{clubId}/live` | RaceService |

### ResultsController

| Monolith | Microservice | Notes |
|---|---|---|
| `POST /api/results/manual` | `POST /api/results/manual` | RaceService |
| `POST /api/results/ingest-ets` | `POST /api/results/ingest-ets` | RaceService |
| `POST /api/results/{raceId}/process` | `POST /api/results/{raceId}/process` | RaceService |
| `GET /api/results/race/{raceId}` | `GET /api/results/race/{raceId}` | RaceService |
| `GET /api/results/fancier/{userId}` | `GET /api/results/fancier/{userId}` | RaceService |
| `GET /api/results/ingestion-logs/{raceId}` | `GET /api/results/ingestion-logs/{raceId}` | RaceService |
| `DELETE /api/results/{resultId}` | `DELETE /api/results/{resultId}` | RaceService |
| `POST /api/results/{resultId}/link-fancier` | `POST /api/results/{resultId}/link-fancier` | RaceService |

### ClubsController

| Monolith | Microservice | Notes |
|---|---|---|
| `POST /api/clubs` | `POST /api/clubs` | ClubService |
| `GET /api/clubs/{clubId}` | `GET /api/clubs/{clubId}` | ClubService |
| `PUT /api/clubs/{clubId}` | `PUT /api/clubs/{clubId}` | ClubService |
| `POST /api/clubs/{clubId}/invite` | `POST /api/clubs/{clubId}/invite` | ClubService — now publishes `MemberInvited` bus event instead of sending email directly |
| `POST /api/clubs/{clubId}/members/{userId}/remove` | `POST /api/clubs/{clubId}/members/{userId}/remove` | ClubService |
| `GET /api/themes` | `GET /api/themes` | ClubService |
| `POST /api/themes/{clubId}/set` | `POST /api/themes/{clubId}/set` | ClubService |
| `GET /api/notifications` | `GET /api/notifications` | ClubService |
| `POST /api/notifications/{id}/read` | `POST /api/notifications/{id}/read` | ClubService |

### ProgrammesController (`backend/API/Controllers/ProgrammeControllers.cs`)

| Monolith | Microservice |
|---|---|
| `POST /api/programmes` | `POST /api/programmes` — ClubService |
| `GET /api/programmes` | `GET /api/programmes` — ClubService |
| `GET /api/programmes/{id}` | `GET /api/programmes/{id}` — ClubService |
| `PUT /api/programmes/{id}` | `PUT /api/programmes/{id}` — ClubService |
| `POST /api/programmes/{id}/races` | `POST /api/programmes/{id}/races` — ClubService |
| `DELETE /api/programmes/{id}/races/{raceId}` | `DELETE /api/programmes/{id}/races/{raceId}` — ClubService |
| `POST /api/programmes/{id}/calculate` | `POST /api/programmes/{id}/calculate` — ClubService |
| `GET /api/programmes/{id}/best-loft` | `GET /api/programmes/{id}/best-loft` — ClubService |
| `GET /api/programmes/{id}/ace-pigeons` | `GET /api/programmes/{id}/ace-pigeons` — ClubService |
| `GET /api/programmes/{id}/super-ace-pigeons` | `GET /api/programmes/{id}/super-ace-pigeons` — ClubService |

### TemplatesController (`backend/API/Controllers/TemplatesController.cs`)

| Monolith | Microservice |
|---|---|
| `GET /api/templates` | `GET /api/templates` — RenderingService |
| `GET /api/templates/{id}` | `GET /api/templates/{id}` — RenderingService |
| `POST /api/templates/render` | `POST /api/templates/render` — RenderingService |
| `POST /api/templates/print-jobs` | `POST /api/templates/print-jobs` — RenderingService |
| `GET /api/templates/print-jobs/{clubId}` | `GET /api/templates/print-jobs/{clubId}` — RenderingService |

### CountryController / CountryResultsController

| Monolith | Microservice | Notes |
|---|---|---|
| `GET /api/country` (list countries) | `GET /api/country` — FederationService | |
| `GET /api/country/{id}` | `GET /api/country/{id}` — FederationService | |
| `POST /api/country` (create) | Bus message `CreateCountryRequest` → FederationService consumer | Admin-only; goes via AdminService |
| `PUT /api/country/{id}` | `PUT /api/country/{id}` — FederationService | |
| `GET /api/country/{id}/page` | `GET /api/country/{id}/page` — FederationService | |
| `POST /api/country/{id}/approve-manager` | Admin bus operation — AdminService | |
| `GET /api/country/pending-managers` | IdentityService `PendingManagersController` | |
| `POST /api/country-results` | `POST /api/country-results` — FederationService | |
| `GET /api/country-results/{id}` | `GET /api/country-results/{id}` — FederationService | |
| `GET /api/country-results/country/{countryId}` | `GET /api/country-results/country/{countryId}` — FederationService | |
| `POST /api/country-results/{id}/publish` | `POST /api/country-results/{id}/publish` — FederationService | |

### IntegrationController (`backend/API/Controllers/IntegrationController.cs`)

| Monolith | Microservice | Notes |
|---|---|---|
| `POST /api/integrations/request-link` | `POST /api/integrations/request-link` — IntegrationService | |
| `POST /api/integrations/{id}/approve` | `POST /api/integrations/{id}/approve` — IntegrationService | Publishes `ExternalLinkRequested` bus event |
| `POST /api/integrations/{id}/reject` | `POST /api/integrations/{id}/reject` — IntegrationService | |
| `POST /api/integrations/{id}/revoke` | `POST /api/integrations/{id}/revoke` — IntegrationService | |
| `GET /api/integrations/my-links` | `GET /api/integrations/my-links` — IntegrationService | |
| `GET /api/integrations/{clubId}/links` | `GET /api/integrations/{clubId}/links` — IntegrationService | |
| `GET /api/integrations/data/race-results` | `GET /api/integrations/data/race-results` — IntegrationService | Data fetched via bus from RaceService |
| `GET /api/integrations/data/ace-pigeons` | `GET /api/integrations/data/ace-pigeons` — IntegrationService | Data fetched via bus from ClubService |
| `GET /api/integrations/data/super-ace` | `GET /api/integrations/data/super-ace` — IntegrationService | |
| `GET /api/integrations/data/best-loft` | `GET /api/integrations/data/best-loft` — IntegrationService | |
| `GET /api/integrations/data/summary` | `GET /api/integrations/data/summary` — IntegrationService | |

### AdminController (`backend/API/Controllers/AdminController.cs`)

| Monolith | Microservice | Notes |
|---|---|---|
| `POST /api/admin/users/toggle-active` | `POST /api/admin/users/toggle-active` — AdminService | Bus call → IdentityService consumer |
| `POST /api/admin/users/assign-role` | `POST /api/admin/users/assign-role` — AdminService | Bus call → IdentityService consumer |
| `POST /api/admin/users/set-limits` | `POST /api/admin/users/set-limits` — AdminService | Bus call → IdentityService consumer |
| `GET /api/admin/users` | `GET /api/admin/users` — AdminService | Bus call → IdentityService consumer |
| `POST /api/admin/clubs/toggle-active` | `POST /api/admin/clubs/toggle-active` — AdminService | Bus call → ClubService consumer |
| `GET /api/admin/clubs` | `GET /api/admin/clubs` — AdminService | Bus call → ClubService consumer |
| `POST /api/admin/countries/create` | `POST /api/admin/countries/create` — AdminService | Bus call → FederationService consumer |
| `POST /api/admin/countries/toggle-active` | `POST /api/admin/countries/toggle-active` — AdminService | Bus call → FederationService consumer |
| `GET /api/admin/countries` | `GET /api/admin/countries` — AdminService | Bus call → FederationService consumer |
| `GET /api/admin/subscriptions/plans` | `GET /api/admin/subscriptions/plans` — AdminService | Bus call → SubscriptionService consumer |
| `GET /api/admin/subscriptions/country-subscriptions` | `GET /api/admin/subscriptions/country-subscriptions` — AdminService | Bus call → SubscriptionService consumer |
| `POST /api/admin/subscriptions/create` | `POST /api/admin/subscriptions/create` — AdminService | Bus call → SubscriptionService consumer |
| `GET /api/admin/stats` | `GET /api/admin/stats` — AdminService | Aggregates bus calls to all 5 services |
| `POST /api/admin/login` | `POST /api/admin-auth/login` — AdminService | `AdminAuthController` |

### ClientLogsController (`backend/API/Controllers/Controllers.cs`)

| Monolith | Microservice |
|---|---|
| `POST /api/logs/client` | `POST /api/logs/client` — AdminService `LogsController` |

### PublicController (`backend/API/Controllers/PublicController.cs`)

| Monolith | Microservice | Notes |
|---|---|---|
| `GET /api/public/clubs/{slug}` | `GET /api/public/clubs/{slug}` — PublicService | Bus calls → ClubService + RaceService + IdentityService |
| `GET /api/public/countries/{slug}` | `GET /api/public/countries/{slug}` — PublicService | Bus calls → FederationService + ClubService |
| `GET /api/public/plans` | `GET /api/public/plans` — PublicService | Bus call → SubscriptionService |
| `GET /api/public/clubs` | `GET /api/public/clubs` — PublicService | Bus call → ClubService |

---

## 10. SignalR Hubs

| Monolith | Microservice | Notes |
|---|---|---|
| `API/Hubs/LiveRaceHub.cs` — `LiveRaceHub` | RaceService `Hubs/LiveRaceHub.cs` | Identical interface |
| `API/Hubs/LiveRaceHub.cs` — `LiveResultsBroadcaster` | RaceService `Hubs/LiveRaceHub.cs` | Identical |
| Hub URL: `/hubs/live-race` | `/hubs/live-race` — RaceService | Proxied through ApiGateway |

---

## 11. Cross-Service Communication (What Replaced Direct DB Joins)

In the monolith everything went through a single `IAppDbContext`. In the microservices, cross-entity reads
use one of three patterns:

| Monolith (direct DB join) | Microservices pattern | Bus message |
|---|---|---|
| `Race.Club.Name` (nav property) | RaceService caches `ClubName` column at race creation time | — |
| `Race.Club.Latitude/Longitude` | RaceService caches `ClubLatitude`, `ClubLongitude` at race creation time | — |
| `Club.Country.Name` | ClubService caches `CountryName` column on Club | — |
| `CountryResultEntry.User.FullName` | FederationService stores `UserFullName` column (denormalized at publish time) | — |
| `CountryResultEntry.Club.Name` | FederationService stores `ClubName` column (denormalized at publish time) | — |
| `RaceResult.User.FullName` (for rendering) | RenderingService bus-calls IdentityService | `GetUserNamesRequest` → `UserNamesResult` |
| `Race.*` (for rendering) | RenderingService bus-calls RaceService | `GetRaceForRenderRequest` → `RaceForRenderResult` |
| `ClubProgramme.*` (for rendering) | RenderingService bus-calls ClubService | `GetProgrammeForRenderRequest` → `ProgrammeForRenderResult` |
| `Club.LogoUrl`, `.PrimaryColor` (for rendering) | RenderingService bus-calls ClubService | `GetClubBrandingRequest` → `ClubBrandingResult` |
| `ProgrammeRace.Race.*` (for calculation) | ClubService bus-calls RaceService | `GetRaceSnapshotRequest`, `GetPublishedResultsForProgrammeRequest` |
| `Pigeon.RingNumber` lookup (for calculation) | ClubService bus-calls RaceService | `GetPigeonLookupRequest` |
| `Race/Results` (for integration data) | IntegrationService bus-calls RaceService | `GetFancierRaceResultsRequest` |
| `Programme results` (for integration data) | IntegrationService bus-calls ClubService | `GetFancierProgrammeResultsRequest` |
| `Club/Race/User` (for public page) | PublicService bus-calls ClubService + RaceService + IdentityService | `GetPublicClubBySlugRequest`, `GetPublishedRacesForPublicRequest`, `GetUserNamesRequest` |
| `Country/CountryResult` (for public page) | PublicService bus-calls FederationService | `GetPublicCountryBySlugRequest` |
| `SubscriptionPlan` (for public plans) | PublicService bus-calls SubscriptionService | `GetPublicSubscriptionPlansRequest` |
| Subscription check during result ingestion | ✓ Wired — ResultService calls SubscriptionService | `CheckResultLimitRequest` / `IncrementResultUsageRequest` |
| All entity counts (admin stats) | AdminService bus-calls 5 services in parallel | `GetIdentityStatsRequest`, `GetClubStatsRequest`, `GetRaceStatsRequest`, `GetFederationStatsRequest`, `GetSubscriptionStatsRequest` |

---

## 12. Events (Fire-and-Forget Notifications)

| Trigger | Bus event | Consumer(s) |
|---|---|---|
| Race published | `RaceResultsPublished` | FederationService (`RaceResultsPublishedConsumer`) — updates RaceSnapshotCache; NotificationService — logs (no fancier email list in event) |
| Member invited | `MemberInvited` | NotificationService (`MemberInvitedConsumer`) — sends invitation email ✓ |
| Identity email (password reset, verify) | `SendEmailEvent` | NotificationService (`SendEmailEventConsumer`) — relays to SmtpEmailService ✓ |
| External link approved | `ExternalLinkRequested` | NotificationService (`ExternalLinkRequestedConsumer`) — logs |
| Subscription confirmed (with contact) | `SubscriptionConfirmedEmail` | NotificationService (`SubscriptionConfirmedEmailConsumer`) — sends confirmation email ✓ |
| Subscription expired (with contact) | `SubscriptionExpiredEmail` | NotificationService (`SubscriptionExpiredEmailConsumer`) — sends expiry email ✓ |
| Subscription cancelled (with contact) | `SubscriptionCancelledEmail` | NotificationService (`SubscriptionCancelledEmailConsumer`) — sends cancellation email ✓ |
| Subscription activated (legacy, no contact info) | `SubscriptionActivated` | NotificationService (`SubscriptionActivatedConsumer`) — logs |
| Subscription expired (legacy, no contact info) | `SubscriptionExpiredEvent` | NotificationService (`SubscriptionExpiredConsumer`) — logs |
| Subscription cancelled (legacy, no contact info) | `SubscriptionCancelledEvent` | NotificationService (`SubscriptionCancelledConsumer`) — logs |

---

## 13. Migration Completeness

All items from the original deferred list have been resolved:

| Monolith feature | Status | Implementation |
|---|---|---|
| `ICacheService` (Redis) | ✓ **Migrated** | `ICacheService` / `RedisCacheService` / `MemoryCacheService` in `Common/Services/`. Register via `IConnectionMultiplexer` (StackExchange.Redis). Redis added to `docker-compose.yml` (port 6379). |
| `IFileStorageService` | ✓ **Migrated** | `IFileStorageService` / `LocalDiskFileStorageService` in `Common/Services/`. Configurable base path via `FileStorage:BasePath`. Replace with Azure Blob or S3 implementation by swapping the registered implementation. |
| Subscription limit enforcement | ✓ **Wired** | `ResultService.AddManualAsync` and `IngestETSFileAsync` both call `CheckResultLimitRequest` (bus) before saving; `IncrementResultUsageRequest` fires after save. SubscriptionService enforces `ClubSubscription.MaxResultsPerClub` (0 = unlimited). |
| Email sending (forgot-password, verify-email) | ✓ **Working** | `NoOpEmailService` replaced with `BusEmailService` in IdentityService. `BusEmailService` publishes `SendEmailEvent`. NotificationService `SendEmailEventConsumer` relays to `SmtpEmailService` (MailKit). |
| `Report` entity | ✓ **Absorbed** | `PrintJob` in RenderingService is a functional superset of `Report`: tracks PDF URL, file size, generation timestamp, source IDs (`RaceId`, `ProgrammeId`, `RaceResultId`, `CountryResultId`, `UserId`), status lifecycle, and error messages. `Report.Parameters` maps to `PrintJob.DataPayloadJson`. |
| `PageTemplate` entity | ✓ **Migrated** | `PageTemplate` model added to RenderingService (`Models/PrintTemplate.cs`). Fields: `Name`, `Category` (string), `PreviewImageUrl`, `TemplateJson`, `IsActive`, `SortOrder`. Added to `RenderingDbContext` with category index. |
| Country manager approval workflow | ✓ **Complete** | `PendingManagersController.ApproveManager` now calls `GetCountrySubscriptionLimitsRequest` (SubscriptionService) and `GetActiveClubCountForCountryRequest` (ClubService) via bus before approving. Rejects if `ActiveClubCount >= MaxClubs` and subscription is not unlimited. |
| Client-side logging (`/api/logs/client`) | ✓ **Present** | `LogsController` in AdminService — forwards structured log events to Seq. |

### Remaining minor gaps (not blocking production)

| Feature | Notes |
|---|---|
| Subscription lifecycle emails with contact info | `SubscriptionConfirmedEmail`, `SubscriptionExpiredEmail`, `SubscriptionCancelledEmail` events are defined and consumed. Publishers (SubscriptionService) need to supply `ContactEmail`/`ContactName` when they have access to the country manager's email address (requires a bus call to IdentityService at subscription creation time). |
| `ICacheService` registration per service | Interface and implementations are in Common. Each service that wants caching must register `IConnectionMultiplexer` + `RedisCacheService` in its `Program.cs`. No services auto-register it yet — wire in as needed per service. |
| `IFileStorageService` registration per service | Same pattern as caching — register `LocalDiskFileStorageService` in any service that needs file uploads (RaceService for ETS, RenderingService for PDFs). |
| User-level result override (`MaxResultsOverride`) | Monolith checked per-user override before the plan limit. Microservice currently enforces only the plan limit. User override would require a bus call to IdentityService from the limit check path. |

---

## 14. New Things That Did Not Exist in the Monolith

| Addition | Service | Purpose |
|---|---|---|
| `RaceSnapshotCache` entity | FederationService | Local cache of published race summaries; avoids repeated bus calls from FederationService to RaceService |
| `BusMessages.cs` (55+ message types) | Common | All cross-service request/response and event contracts |
| `ICacheService` / `RedisCacheService` / `MemoryCacheService` | Common | Pluggable caching abstraction — Redis for production, in-memory for dev/testing |
| `IFileStorageService` / `LocalDiskFileStorageService` | Common | Pluggable file storage abstraction — local disk by default, swap for Azure Blob / S3 |
| `BusRaceServiceClient` | ClubService | Implements `IRaceServiceClient` over MassTransit instead of HTTP |
| `ApiGateway` (YARP) | ApiGateway | Single entry point; JWT validation; routes all `/api/*` and `/hubs/*` to the right service |
| `services/docker-compose.yml` | Infrastructure | Full stack: RabbitMQ, SQL Server, Seq, Redis, all 11 services |
| Per-service `CorrelationIdMiddleware` | All services | Propagates X-Correlation-ID header for distributed tracing |
| Per-service `ServiceKeyFilter` | AdminService, SubscriptionService | API-key guard on internal admin endpoints |
| `BusEmailService` | IdentityService | Publishes `SendEmailEvent` instead of calling SMTP directly — decouples auth from mail infrastructure |
| `PageTemplate` entity | RenderingService | Page-builder layout templates (visual club/country page builder) |
| `SendEmailEvent` consumer | NotificationService | Relays transactional emails (password reset, verification) from the bus to MailKit/SMTP |
| `SubscriptionConfirmedEmail` / `SubscriptionExpiredEmail` / `SubscriptionCancelledEmail` | NotificationService | Typed notification events with real HTML email bodies |

---

## 15. Compliance with Requirements

### Functional requirements

| Requirement | Status | Notes |
|---|---|---|
| User registration and authentication (JWT) | ✓ | IdentityService — register, login, refresh, revoke, change-password |
| Email verification and password reset | ✓ | IdentityService publishes `SendEmailEvent` → NotificationService sends via SMTP |
| Club management (create, update, delete, members) | ✓ | ClubService — full CRUD + membership + invitations |
| Race management (create, start, publish) | ✓ | RaceService — full lifecycle + SignalR live hub |
| Result ingestion (manual + ETS file) | ✓ | RaceService — ETSFileParser + VelocityCalculator |
| Subscription limit enforcement on results | ✓ | `CheckResultLimitRequest` bus call before save; `IncrementResultUsageRequest` after |
| Programme calculation (BestLoft, AcePigeon, SuperAce) | ✓ | ClubService — CalculationEngines + dedicated consumers |
| PDF generation (race results, certificates, programmes) | ✓ | RenderingService — PuppeteerSharp + PrintJobProcessorService |
| Country / federation management | ✓ | FederationService — countries, country results, country pages |
| External platform integrations | ✓ | IntegrationService — ExternalPlatformCallbackService |
| Admin panel (users, clubs, countries, subscriptions, stats) | ✓ | AdminService — all operations via MassTransit request/response |
| Public read API (clubs, countries, races, plans) | ✓ | PublicService — bus calls to ClubService, RaceService, FederationService, SubscriptionService |
| Notifications (email on invite, subscription events) | ✓ | NotificationService — SmtpEmailService via MailKit |
| Country manager approval with subscription limit | ✓ | PendingManagersController checks club count + subscription limits via bus |
| Subscription management (plans, country subs, club subs) | ✓ | SubscriptionService — full CRUD |
| Caching abstraction | ✓ | `ICacheService` in Common — Redis or in-memory |
| File storage abstraction | ✓ | `IFileStorageService` in Common — local disk or cloud |
| Distributed tracing | ✓ | `CorrelationIdMiddleware` in all services |
| Structured logging | ✓ | Serilog + Seq in all services |
| Health checks | ✓ | `GET /health → 200` on all services |

### Non-functional requirements

| Requirement | Status | Notes |
|---|---|---|
| Each service independently deployable | ✓ | Separate Dockerfile, separate database, independent startup |
| No cross-service database references | ✓ | Cross-service data via bus request/response only |
| All inter-service communication via MassTransit | ✓ | No HTTP calls between services; only bus |
| Single entry point for frontend | ✓ | ApiGateway (YARP) on port 9500 |
| Database per service | ✓ | PRC_Identity, PRC_Club, PRC_Race, PRC_Federation, PRC_Rendering, PRC_Integration, PRC_Admin, PRC_Subscription |
| Migrations auto-run on startup | ✓ | `db.Database.MigrateAsync()` in every service startup |
| Docker Compose full-stack | ✓ | `services/docker-compose.yml` — infra + all 11 services + Redis |
| CI pipeline | ✓ | `.github/workflows/services-ci.yml` — build + test all 11 services; Docker push on main |
| Frontend proxy preserved | ✓ | `proxy.conf.json` updated to point at ApiGateway:9500 |
