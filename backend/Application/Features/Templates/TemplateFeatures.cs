using MediatR;
using Microsoft.EntityFrameworkCore;
using PigeonRacing.Application.Common;
using PigeonRacing.Application.Common.Interfaces;
using PigeonRacing.Domain.Entities;
using PigeonRacing.Domain.Enums;
using PigeonRacing.Infrastructure.Templates;

namespace PigeonRacing.Application.Features.Templates;

// ═════════════════════════════════════════════════════════════════════════════
//  DTOs
// ═════════════════════════════════════════════════════════════════════════════

public record PrintTemplateDto(
    Guid Id,
    string Name,
    string Description,
    TemplateCategory Category,
    string CategoryName,
    TemplateStyle Style,
    string StyleName,
    TemplatePaperSize PaperSize,
    string PaperSizeName,
    TemplateColourScheme ColourScheme,
    string PrimaryColour,
    string SecondaryColour,
    string ThumbnailUrl,
    int MaxRows,
    bool IsMultiPage,
    bool IsSystem,
    int SortOrder,
    string VariableSchemaJson);

public record RenderTemplateRequest(
    Guid TemplateId,
    TemplateCategory Category,
    // Context identifiers — at most one set per render
    Guid? RaceId,
    Guid? ProgrammeId,
    Guid? RaceResultId,
    // Optional: override recipient info for certificates
    string? CertificateRecipientName,
    string? CertificateRank,
    string? CertificateAchievement,
    // Locale for translated column headers (e.g. "en", "fr", "nl-BE", "ar", "zh", "es")
    string? Locale = null);

public record RenderTemplateResult(
    string Html,
    TemplatePaperSize PaperSize,
    string TemplateName);

// ═════════════════════════════════════════════════════════════════════════════
//  Queries
// ═════════════════════════════════════════════════════════════════════════════

public record GetTemplatesQuery(
    TemplateCategory? Category = null,
    TemplateStyle? Style = null,
    bool IncludeInactive = false)
    : IRequest<Result<List<PrintTemplateDto>>>;

public record GetTemplateQuery(Guid TemplateId) : IRequest<Result<PrintTemplateDto>>;

public record GetPrintJobsQuery(Guid ClubId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<PrintJobDto>>>;

public record PrintJobDto(
    Guid Id,
    Guid TemplateId,
    string TemplateName,
    TemplateCategory Category,
    PrintJobStatus Status,
    string? PdfUrl,
    long? FileSizeBytes,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string GeneratedByUserName);

// ═════════════════════════════════════════════════════════════════════════════
//  Commands
// ═════════════════════════════════════════════════════════════════════════════

public record RenderTemplateCommand(RenderTemplateRequest Request) : IRequest<Result<RenderTemplateResult>>;
public record CreatePrintJobCommand(RenderTemplateRequest Request) : IRequest<Result<PrintJobDto>>;

// ═════════════════════════════════════════════════════════════════════════════
//  Get Templates Handler
// ═════════════════════════════════════════════════════════════════════════════

public class GetTemplatesHandler : IRequestHandler<GetTemplatesQuery, Result<List<PrintTemplateDto>>>
{
    private readonly IAppDbContext _db;

    public GetTemplatesHandler(IAppDbContext db) => _db = db;

    public async Task<Result<List<PrintTemplateDto>>> Handle(GetTemplatesQuery query, CancellationToken ct)
    {
        var q = _db.PrintTemplates.AsQueryable();

        if (!query.IncludeInactive)
            q = q.Where(t => t.IsActive);

        if (query.Category.HasValue)
            q = q.Where(t => t.Category == query.Category.Value);

        if (query.Style.HasValue)
            q = q.Where(t => t.Style == query.Style.Value);

        var templates = await q.OrderBy(t => t.SortOrder).ToListAsync(ct);
        return Result.Success(templates.Select(t => t.MapToDto()).ToList());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
//  Get Single Template Handler
// ═════════════════════════════════════════════════════════════════════════════

public class GetTemplateHandler : IRequestHandler<GetTemplateQuery, Result<PrintTemplateDto>>
{
    private readonly IAppDbContext _db;

    public GetTemplateHandler(IAppDbContext db) => _db = db;

    public async Task<Result<PrintTemplateDto>> Handle(GetTemplateQuery query, CancellationToken ct)
    {
        var t = await _db.PrintTemplates.FirstOrDefaultAsync(x => x.Id == query.TemplateId, ct);
        return t == null
            ? Result.NotFound<PrintTemplateDto>("Template")
            : Result.Success(t.MapToDto());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
//  Render Template Handler
//  Fetches real data, substitutes all template variables, returns HTML.
// ═════════════════════════════════════════════════════════════════════════════

public class RenderTemplateHandler : IRequestHandler<RenderTemplateCommand, Result<RenderTemplateResult>>
{
    private readonly IAppDbContext _db;

    public RenderTemplateHandler(IAppDbContext db) => _db = db;

    public async Task<Result<RenderTemplateResult>> Handle(RenderTemplateCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;

        // Load template
        var template = await _db.PrintTemplates.FirstOrDefaultAsync(t => t.Id == req.TemplateId, ct);
        if (template == null) return Result.NotFound<RenderTemplateResult>("Template");

        // Build data payload based on category
        var labels = PigeonRacing.Infrastructure.Templates.TemplateLocales.Get(req.Locale);

        object data = req.Category switch
        {
            TemplateCategory.RaceResults    => await BuildRaceResultsData(req, labels, ct),
            TemplateCategory.BestLoft       => await BuildBestLoftData(req, labels, ct),
            TemplateCategory.AcePigeon      => await BuildAcePigeonData(req, labels, ct),
            TemplateCategory.SuperAcePigeon => await BuildSuperAceData(req, labels, ct),
            TemplateCategory.Certificate    => await BuildCertificateData(req, labels, ct),
            _                               => new { }
        };

        // Render
        var html = TemplateRenderer.Render(template.HtmlTemplate, data);
        return Result.Success(new RenderTemplateResult(html, template.PaperSize, template.Name));
    }

    // ── Data builders ─────────────────────────────────────────────────────────

    private async Task<object> BuildRaceResultsData(RenderTemplateRequest req, PigeonRacing.Infrastructure.Templates.TemplateLabels labels, CancellationToken ct)
    {
        if (!req.RaceId.HasValue) return new { };

        var race = await _db.Races
            .Include(r => r.Club)
            .FirstOrDefaultAsync(r => r.Id == req.RaceId, ct);

        var results = await _db.RaceResults
            .Include(r => r.Category)
            .Include(r => r.User)
            .Where(r => r.RaceId == req.RaceId && r.Status == ResultStatus.Published && !r.IsDeleted)
            .OrderBy(r => r.ClubRank)
            .Select(r => new {
                rank            = r.ClubRank ?? 0,
                ringNumber      = r.RingNumber,
                pigeonName      = r.PigeonName ?? "",
                pigeonSex       = r.PigeonSex ?? "",
                pigeonYear      = r.PigeonYearOfBirth,
                fancierName     = r.User != null ? r.User.FirstName + " " + r.User.LastName : "Unknown",
                arrivalTime     = r.ArrivalTime.ToString("HH:mm:ss"),
                distanceKm      = Math.Round(r.DistanceKm, 3).ToString("F3"),
                velocityMperMin = Math.Round(r.VelocityMperMin, 4).ToString("F4"),
                velocityKmH     = Math.Round(r.VelocityMperMin * 60.0 / 1000.0, 3).ToString("F3"),
                categoryName    = r.Category != null ? r.Category.Name : "Open",
                categoryRank    = r.CategoryRank ?? 0,
                clubRank        = r.ClubRank ?? 0
            })
            .ToListAsync(ct);

        return new {
            labels,
            race = new {
                name            = race?.Name ?? "",
                releaseLocation = race?.ReleaseLocation ?? "",
                date            = race?.ActualReleaseTime?.ToString("dd MMM yyyy") ?? "",
                releaseTime     = race?.ActualReleaseTime?.ToString("HH:mm") ?? "",
                distance        = race?.NominatedDistanceKm?.ToString("F0")
                                  ?? results.Select(r => double.TryParse(r.distanceKm, out double d) ? d : 0).DefaultIfEmpty(0).Max().ToString("F0"),
                totalEntries    = race?.TotalPigeonsEntered ?? results.Count,
                wind            = race?.WindDescription ?? "",
                temperature     = race?.TemperatureDescription ?? ""
            },
            club = BuildClubData(race?.Club),
            results,
            season      = race?.ActualReleaseTime?.Year.ToString() ?? DateTime.UtcNow.Year.ToString(),
            printDate   = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")
        };
    }

    private async Task<object> BuildBestLoftData(RenderTemplateRequest req, PigeonRacing.Infrastructure.Templates.TemplateLabels labels, CancellationToken ct)
    {
        if (!req.ProgrammeId.HasValue) return new { };

        var prog = await _db.ClubProgrammes.Include(p => p.Club)
            .FirstOrDefaultAsync(p => p.Id == req.ProgrammeId, ct);

        var results = await _db.BestLoftResults
            .Where(r => r.ProgrammeId == req.ProgrammeId)
            .OrderBy(r => r.LoftRank)
            .Select(r => new {
                loftRank                  = r.LoftRank,
                fancierName               = r.FancierName,
                racesEntered              = r.RacesEntered,
                pigeonsEntered            = r.PigeonsEntered,
                bestSingleVelocityMperMin = Math.Round(r.BestSingleVelocityMperMin, 4).ToString("F4"),
                averageVelocityMperMin    = Math.Round(r.AverageVelocityMperMin, 4).ToString("F4"),
                totalScore                = Math.Round(r.TotalScore, 2).ToString("F2"),
                averageScore              = Math.Round(r.AverageScore, 2).ToString("F2")
            })
            .ToListAsync(ct);

        return new {
            programme = new {
                name          = prog?.Name ?? "",
                year          = prog?.Year ?? DateTime.UtcNow.Year,
                scoringMethod = prog?.ScoringMethod.ToString() ?? "",
                raceCount     = prog?.ProgrammeRaces.Count(r => !r.IsDeleted) ?? 0
            },
            club       = BuildClubData(prog?.Club),
            results,
            totalLofts = results.Count,
            season     = prog?.Year.ToString() ?? DateTime.UtcNow.Year.ToString(),
            printDate  = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")
        };
    }

    private async Task<object> BuildAcePigeonData(RenderTemplateRequest req, PigeonRacing.Infrastructure.Templates.TemplateLabels labels, CancellationToken ct)
    {
        if (!req.ProgrammeId.HasValue) return new { };

        var prog = await _db.ClubProgrammes.Include(p => p.Club)
            .FirstOrDefaultAsync(p => p.Id == req.ProgrammeId, ct);

        var results = await _db.AcePigeonResults
            .Where(r => r.ProgrammeId == req.ProgrammeId)
            .OrderBy(r => r.AceRank)
            .Select(r => new {
                aceRank              = r.AceRank,
                ringNumber           = r.RingNumber,
                pigeonName           = r.PigeonName ?? "",
                pigeonSex            = r.PigeonSex ?? "",
                pigeonYearOfBirth    = r.PigeonYearOfBirth,
                fancierName          = r.FancierName,
                racesEntered         = r.RacesEntered,
                racesInProgramme     = r.RacesInProgramme,
                participationRate    = Math.Round(r.ParticipationRate, 1).ToString("F1"),
                bestVelocityMperMin  = Math.Round(r.BestVelocityMperMin, 4).ToString("F4"),
                averageVelocityMperMin = Math.Round(r.AverageVelocityMperMin, 4).ToString("F4"),
                totalScore           = Math.Round(r.TotalScore, 2).ToString("F2"),
                averageScore         = Math.Round(r.AverageScore, 2).ToString("F2"),
                bestClubRank         = r.BestClubRank
            })
            .ToListAsync(ct);

        return new {
            programme = new {
                name              = prog?.Name ?? "",
                year              = prog?.Year ?? DateTime.UtcNow.Year,
                scoringMethod     = prog?.ScoringMethod.ToString() ?? "",
                raceCount         = prog?.ProgrammeRaces.Count(r => !r.IsDeleted) ?? 0,
                acePigeonMinRaces = prog?.AcePigeonMinRaces ?? 0
            },
            club         = BuildClubData(prog?.Club),
            results,
            totalPigeons = results.Count,
            season       = prog?.Year.ToString() ?? DateTime.UtcNow.Year.ToString(),
            printDate    = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")
        };
    }

    private async Task<object> BuildSuperAceData(RenderTemplateRequest req, PigeonRacing.Infrastructure.Templates.TemplateLabels labels, CancellationToken ct)
    {
        if (!req.ProgrammeId.HasValue) return new { };

        var prog = await _db.ClubProgrammes.Include(p => p.Club)
            .FirstOrDefaultAsync(p => p.Id == req.ProgrammeId, ct);

        var results = await _db.SuperAcePigeonResults
            .Where(r => r.ProgrammeId == req.ProgrammeId)
            .OrderBy(r => r.SuperAceRank)
            .Select(r => new {
                superAceRank           = r.SuperAceRank,
                ringNumber             = r.RingNumber,
                pigeonName             = r.PigeonName ?? "",
                pigeonSex              = r.PigeonSex ?? "",
                pigeonYearOfBirth      = r.PigeonYearOfBirth,
                fancierName            = r.FancierName,
                racesEntered           = r.RacesEntered,
                racesInProgramme       = r.RacesInProgramme,
                participationRate      = Math.Round(r.ParticipationRate, 1).ToString("F1"),
                bestVelocityMperMin    = Math.Round(r.BestVelocityMperMin, 4).ToString("F4"),
                averageVelocityMperMin = Math.Round(r.AverageVelocityMperMin, 4).ToString("F4"),
                bestClubRank           = r.BestClubRank,
                totalScore             = Math.Round(r.TotalScore, 2).ToString("F2"),
                averageScore           = Math.Round(r.AverageScore, 2).ToString("F2")
            })
            .ToListAsync(ct);

        return new {
            programme = new {
                name                   = prog?.Name ?? "",
                year                   = prog?.Year ?? DateTime.UtcNow.Year,
                scoringMethod          = prog?.ScoringMethod.ToString() ?? "",
                raceCount              = prog?.ProgrammeRaces.Count(r => !r.IsDeleted) ?? 0,
                superAceQualification  = prog?.SuperAceQualification.ToString() ?? ""
            },
            club             = BuildClubData(prog?.Club),
            results,
            totalQualifiers  = results.Count,
            season           = prog?.Year.ToString() ?? DateTime.UtcNow.Year.ToString(),
            printDate        = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")
        };
    }

    private async Task<object> BuildCertificateData(RenderTemplateRequest req, PigeonRacing.Infrastructure.Templates.TemplateLabels labels, CancellationToken ct)
    {
        Club? club = null;

        // Certificate can be for a race result or a programme result
        if (req.RaceResultId.HasValue)
        {
            var result = await _db.RaceResults
                .Include(r => r.Race).ThenInclude(r => r.Club)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == req.RaceResultId, ct);

            if (result != null)
            {
                club = result.Race?.Club;
                return new {
                    certificate = new {
                        recipientName      = req.CertificateRecipientName ?? result.User?.FullName ?? "",
                        rank               = req.CertificateRank ?? OrdinalRank(result.ClubRank ?? 0),
                        achievement        = req.CertificateAchievement ?? "",
                        ringNumber         = result.RingNumber,
                        pigeonName         = result.PigeonName ?? "",
                        pigeonSex          = result.PigeonSex ?? "",
                        velocityMperMin    = Math.Round(result.VelocityMperMin, 4).ToString("F4"),
                        distanceKm         = Math.Round(result.DistanceKm, 3).ToString("F3"),
                        arrivalTime        = result.ArrivalTime.ToString("HH:mm:ss"),
                        raceName           = result.Race?.Name ?? "",
                        aceRank            = 0, superAceRank = 0, loftRank = 0,
                        totalScore = "", racesEntered = 0, racesInProgramme = 0, pigeonsEntered = 0, bestVelocityMperMin = ""
                    },
                    race = new {
                        name            = result.Race?.Name ?? "",
                        date            = result.Race?.ActualReleaseTime?.ToString("dd MMM yyyy") ?? "",
                        releaseLocation = result.Race?.ReleaseLocation ?? ""
                    },
                    programme = new { name = "", year = 0, superAceQualification = "" },
                    club      = BuildClubData(club),
                    season    = result.Race?.ActualReleaseTime?.Year.ToString() ?? "",
                    printDate = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")
                };
            }
        }

        // Programme-based certificate (ace pigeon / best loft / super ace)
        if (req.ProgrammeId.HasValue)
        {
            var prog = await _db.ClubProgrammes.Include(p => p.Club)
                .FirstOrDefaultAsync(p => p.Id == req.ProgrammeId, ct);
            club = prog?.Club;

            return new {
                certificate = new {
                    recipientName     = req.CertificateRecipientName ?? "",
                    rank              = req.CertificateRank ?? "",
                    achievement       = req.CertificateAchievement ?? "",
                    ringNumber = "", pigeonName = "", pigeonSex = "",
                    velocityMperMin = "", distanceKm = "", arrivalTime = "", raceName = "",
                    aceRank = 0, superAceRank = 0, loftRank = 0,
                    totalScore = "", racesEntered = 0, racesInProgramme = 0, pigeonsEntered = 0, bestVelocityMperMin = ""
                },
                race      = new { name = "", date = "", releaseLocation = "" },
                programme = new {
                    name                  = prog?.Name ?? "",
                    year                  = prog?.Year ?? 0,
                    superAceQualification = prog?.SuperAceQualification.ToString() ?? ""
                },
                club      = BuildClubData(club),
                season    = prog?.Year.ToString() ?? "",
                printDate = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")
            };
        }

        return new { certificate = new { }, race = new { }, programme = new { }, club = new { }, season = "", printDate = "" };
    }

    private static object BuildClubData(Club? club) => new {
        name           = club?.Name ?? "",
        logoUrl        = club?.LogoUrl ?? "",
        primaryColour  = club?.PrimaryColor ?? "#1E3A5F",
        secondaryColour= club?.SecondaryColor ?? "#C9A84C",
        primaryColor   = club?.PrimaryColor ?? "#1E3A5F",
        secondaryColor = club?.SecondaryColor ?? "#C9A84C"
    };

    private static string OrdinalRank(int n) => n switch {
        1 => "1st", 2 => "2nd", 3 => "3rd", _ => $"{n}th"
    };
}

// ═════════════════════════════════════════════════════════════════════════════
//  Create Print Job Handler
// ═════════════════════════════════════════════════════════════════════════════

public class CreatePrintJobHandler : IRequestHandler<CreatePrintJobCommand, Result<PrintJobDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public CreatePrintJobHandler(IAppDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<Result<PrintJobDto>> Handle(CreatePrintJobCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;

        var template = await _db.PrintTemplates.FirstOrDefaultAsync(t => t.Id == req.TemplateId, ct);
        if (template == null) return Result.NotFound<PrintJobDto>("Template");

        var clubId = _currentUser.ClubId ?? req.RaceId.HasValue
            ? (await _db.Races.Select(r => new { r.Id, r.ClubId }).FirstOrDefaultAsync(r => r.Id == req.RaceId, ct))?.ClubId ?? Guid.Empty
            : req.ProgrammeId.HasValue
                ? (await _db.ClubProgrammes.Select(p => new { p.Id, p.ClubId }).FirstOrDefaultAsync(p => p.Id == req.ProgrammeId, ct))?.ClubId ?? Guid.Empty
                : Guid.Empty;

        var job = new PrintJob
        {
            TemplateId         = req.TemplateId,
            ClubId             = _currentUser.ClubId ?? Guid.Empty,
            Category           = req.Category,
            Status             = PrintJobStatus.Pending,
            RaceId             = req.RaceId,
            ProgrammeId        = req.ProgrammeId,
            RaceResultId       = req.RaceResultId,
            GeneratedByUserId  = _currentUser.UserId ?? Guid.Empty,
            DataPayloadJson    = System.Text.Json.JsonSerializer.Serialize(req)
        };

        _db.PrintJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        return Result.Success(new PrintJobDto(
            job.Id, job.TemplateId, template.Name, job.Category,
            job.Status, null, null, job.CreatedAt, null, ""));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
//  Get Print Jobs Handler
// ═════════════════════════════════════════════════════════════════════════════

public class GetPrintJobsHandler : IRequestHandler<GetPrintJobsQuery, Result<PagedResult<PrintJobDto>>>
{
    private readonly IAppDbContext _db;

    public GetPrintJobsHandler(IAppDbContext db) => _db = db;

    public async Task<Result<PagedResult<PrintJobDto>>> Handle(GetPrintJobsQuery query, CancellationToken ct)
    {
        var q = _db.PrintJobs
            .Include(j => j.Template)
            .Include(j => j.GeneratedByUser)
            .Where(j => j.ClubId == query.ClubId)
            .OrderByDescending(j => j.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);

        var dtos = items.Select(j => new PrintJobDto(
            j.Id, j.TemplateId, j.Template?.Name ?? "",
            j.Category, j.Status, j.PdfUrl, j.FileSizeBytes,
            j.CreatedAt, j.CompletedAt,
            j.GeneratedByUser?.FullName ?? "")).ToList();

        return Result.Success(new PagedResult<PrintJobDto>
        {
            Items = dtos, TotalCount = total, Page = query.Page, PageSize = query.PageSize
        });
    }
}

// ═════════════════════════════════════════════════════════════════════════════
//  Mapping
// ═════════════════════════════════════════════════════════════════════════════

public static class TemplateMappingExtensions
{
    public static PrintTemplateDto MapToDto(this PrintTemplate t) => new(
        t.Id, t.Name, t.Description, t.Category, t.Category.ToString(),
        t.Style, t.Style.ToString(), t.PaperSize, t.PaperSize.ToString(),
        t.ColourScheme, t.PrimaryColour, t.SecondaryColour, t.ThumbnailUrl,
        t.MaxRows, t.IsMultiPage, t.IsSystem, t.SortOrder, t.VariableSchemaJson);
}
