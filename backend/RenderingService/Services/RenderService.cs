using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.Common.Messages;
using PRC.RenderingService.Data;
using PRC.RenderingService.DTOs;

namespace PRC.RenderingService.Services;

public class RenderService : IRenderService
{
    private readonly RenderingDbContext _db;
    private readonly IRequestClient<GetRaceForRenderRequest> _raceClient;
    private readonly IRequestClient<GetRaceResultForRenderRequest> _resultClient;
    private readonly IRequestClient<GetClubBrandingRequest> _brandingClient;
    private readonly IRequestClient<GetUserNamesRequest> _userNamesClient;
    private readonly IRequestClient<GetProgrammeForRenderRequest> _programmeClient;

    public RenderService(
        RenderingDbContext db,
        IRequestClient<GetRaceForRenderRequest> raceClient,
        IRequestClient<GetRaceResultForRenderRequest> resultClient,
        IRequestClient<GetClubBrandingRequest> brandingClient,
        IRequestClient<GetUserNamesRequest> userNamesClient,
        IRequestClient<GetProgrammeForRenderRequest> programmeClient)
    {
        _db = db;
        _raceClient = raceClient;
        _resultClient = resultClient;
        _brandingClient = brandingClient;
        _userNamesClient = userNamesClient;
        _programmeClient = programmeClient;
    }

    public async Task<Result<RenderResult>> RenderAsync(RenderRequest req, CancellationToken ct)
    {
        var template = await _db.PrintTemplates.FirstOrDefaultAsync(t => t.Id == req.TemplateId, ct);
        if (template == null) return Result.NotFound<RenderResult>("Template");

        var labels = TemplateLocales.Get(req.Locale);

        object data = req.Category switch
        {
            TemplateCategory.RaceResults    => await BuildRaceResultsData(req, labels, ct),
            TemplateCategory.BestLoft       => await BuildProgrammeData(req, labels, "BestLoft", ct),
            TemplateCategory.AcePigeon      => await BuildProgrammeData(req, labels, "AcePigeon", ct),
            TemplateCategory.SuperAcePigeon => await BuildProgrammeData(req, labels, "SuperAce", ct),
            TemplateCategory.Certificate    => await BuildCertificateData(req, labels, ct),
            _                               => new { }
        };

        var html = TemplateRenderer.Render(template.HtmlTemplate, data);
        return Result.Success(new RenderResult(html, template.PaperSize, template.Name));
    }

    private async Task<object> BuildRaceResultsData(RenderRequest req, TemplateLabels labels, CancellationToken ct)
    {
        if (!req.RaceId.HasValue) return new { };

        var raceResp = await Ask<GetRaceForRenderRequest, RaceForRenderResult>(
            _raceClient, new GetRaceForRenderRequest(req.RaceId.Value), ct);
        if (raceResp == null || !raceResp.Found) return new { };

        var userIds = raceResp.Results
            .Where(r => r.UserId.HasValue)
            .Select(r => r.UserId!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, string> names = new();
        if (userIds.Any())
        {
            var namesResp = await Ask<GetUserNamesRequest, UserNamesResult>(
                _userNamesClient, new GetUserNamesRequest(userIds), ct);
            if (namesResp != null) names = namesResp.Names.ToDictionary(k => k.Key, v => v.Value);
        }

        var brandingResp = await Ask<GetClubBrandingRequest, ClubBrandingResult>(
            _brandingClient, new GetClubBrandingRequest(raceResp.ClubId), ct);

        var results = raceResp.Results.Select(r =>
        {
            var fancierName = r.UserId.HasValue
                ? (names.TryGetValue(r.UserId.Value, out var n) ? n : "Unknown")
                : "Unknown";
            return new
            {
                rank            = r.ClubRank ?? 0,
                ringNumber      = r.RingNumber,
                pigeonName      = r.PigeonName ?? "",
                pigeonSex       = r.PigeonSex ?? "",
                pigeonYear      = r.PigeonYearOfBirth,
                fancierName,
                arrivalTime     = r.ArrivalTime.ToString("HH:mm:ss"),
                distanceKm      = Math.Round(r.DistanceKm, 3).ToString("F3"),
                speedMperMin    = Math.Round(r.SpeedMperMin, 4).ToString("F4"),
                speedKmH        = Math.Round(r.SpeedMperMin * 60.0 / 1000.0, 3).ToString("F3"),
                categoryName    = r.CategoryName,
                categoryRank    = r.CategoryRank ?? 0,
                clubRank        = r.ClubRank ?? 0
            };
        }).ToList();

        var season    = raceResp.ActualReleaseTime?.Year.ToString() ?? DateTime.UtcNow.Year.ToString();
        var printDate = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm");

        return new
        {
            labels,
            race = new
            {
                name            = raceResp.RaceName,
                releaseLocation = raceResp.ReleaseLocation ?? "",
                date            = raceResp.ActualReleaseTime?.ToString("dd MMM yyyy") ?? "",
                releaseTime     = raceResp.ActualReleaseTime?.ToString("HH:mm") ?? "",
                distance        = raceResp.NominatedDistanceKm?.ToString("F0") ?? "",
                totalEntries    = raceResp.TotalPigeonsEntered,
                wind            = raceResp.WindDescription ?? "",
                temperature     = raceResp.TemperatureDescription ?? ""
            },
            club = BuildClubData(brandingResp),
            results,
            season,
            printDate
        };
    }

    private async Task<object> BuildProgrammeData(
        RenderRequest req, TemplateLabels labels, string resultType, CancellationToken ct)
    {
        if (!req.ProgrammeId.HasValue) return new { };

        var progResp = await Ask<GetProgrammeForRenderRequest, ProgrammeForRenderResult>(
            _programmeClient, new GetProgrammeForRenderRequest(req.ProgrammeId.Value), ct);
        if (progResp == null || !progResp.Found) return new { };

        var club = new
        {
            name            = progResp.ClubName,
            logoUrl         = progResp.ClubLogoUrl ?? "",
            primaryColour   = progResp.ClubPrimaryColor,
            secondaryColour = progResp.ClubSecondaryColor,
            primaryColor    = progResp.ClubPrimaryColor,
            secondaryColor  = progResp.ClubSecondaryColor
        };

        var programme = new
        {
            name                  = progResp.Name,
            year                  = progResp.Year,
            scoringMethod         = progResp.ScoringMethod,
            raceCount             = progResp.RaceCount,
            acePigeonMinRaces     = progResp.AcePigeonMinRaces,
            superAceQualification = progResp.SuperAceQualification
        };

        var season    = progResp.Year.ToString();
        var printDate = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm");

        if (resultType == "BestLoft")
        {
            var results = progResp.BestLoftResults.Select(r => new
            {
                loftRank                  = r.LoftRank,
                fancierName               = r.FancierName,
                racesEntered              = r.RacesEntered,
                pigeonsEntered            = r.PigeonsEntered,
                bestSingleSpeedMperMin = Math.Round(r.BestSingleSpeedMperMin, 4).ToString("F4"),
                averageSpeedMperMin    = Math.Round(r.AverageSpeedMperMin, 4).ToString("F4"),
                totalScore                = Math.Round(r.TotalScore, 2).ToString("F2"),
                averageScore              = Math.Round(r.AverageScore, 2).ToString("F2")
            }).ToList();
            var totalLofts = results.Count;
            return new { labels, programme, club, results, totalLofts, season, printDate };
        }

        if (resultType == "AcePigeon")
        {
            var results = progResp.AcePigeonResults.Select(r => new
            {
                aceRank                = r.AceRank,
                ringNumber             = r.RingNumber,
                pigeonName             = r.PigeonName ?? "",
                pigeonSex              = r.PigeonSex ?? "",
                pigeonYearOfBirth      = r.PigeonYearOfBirth,
                fancierName            = r.FancierName,
                racesEntered           = r.RacesEntered,
                racesInProgramme       = r.RacesInProgramme,
                participationRate      = Math.Round(r.ParticipationRate, 1).ToString("F1"),
                bestSpeedMperMin    = Math.Round(r.BestSpeedMperMin, 4).ToString("F4"),
                averageSpeedMperMin = Math.Round(r.AverageSpeedMperMin, 4).ToString("F4"),
                totalScore             = Math.Round(r.TotalScore, 2).ToString("F2"),
                averageScore           = Math.Round(r.AverageScore, 2).ToString("F2"),
                bestClubRank           = r.BestClubRank
            }).ToList();
            var totalPigeons = results.Count;
            return new { labels, programme, club, results, totalPigeons, season, printDate };
        }

        // SuperAce
        var saResults = progResp.SuperAceResults.Select(r => new
        {
            superAceRank           = r.SuperAceRank,
            ringNumber             = r.RingNumber,
            pigeonName             = r.PigeonName ?? "",
            pigeonSex              = r.PigeonSex ?? "",
            pigeonYearOfBirth      = r.PigeonYearOfBirth,
            fancierName            = r.FancierName,
            racesEntered           = r.RacesEntered,
            racesInProgramme       = r.RacesInProgramme,
            participationRate      = Math.Round(r.ParticipationRate, 1).ToString("F1"),
            bestSpeedMperMin    = Math.Round(r.BestSpeedMperMin, 4).ToString("F4"),
            averageSpeedMperMin = Math.Round(r.AverageSpeedMperMin, 4).ToString("F4"),
            bestClubRank        = r.BestClubRank,
            totalScore          = Math.Round(r.TotalScore, 2).ToString("F2"),
            averageScore        = Math.Round(r.AverageScore, 2).ToString("F2")
        }).ToList();
        var totalQualifiers = saResults.Count;
        return new { labels, programme, club, results = saResults, totalQualifiers, season, printDate };
    }

    private async Task<object> BuildCertificateData(RenderRequest req, TemplateLabels labels, CancellationToken ct)
    {
        if (req.RaceResultId.HasValue)
        {
            var rr = await Ask<GetRaceResultForRenderRequest, RaceResultForRenderResult>(
                _resultClient, new GetRaceResultForRenderRequest(req.RaceResultId.Value), ct);
            if (rr != null && rr.Found)
            {
                var brandResp = await Ask<GetClubBrandingRequest, ClubBrandingResult>(
                    _brandingClient, new GetClubBrandingRequest(rr.ClubId), ct);
                string fancierName = "";
                if (rr.UserId.HasValue)
                {
                    var nm = await Ask<GetUserNamesRequest, UserNamesResult>(
                        _userNamesClient, new GetUserNamesRequest(new[] { rr.UserId.Value }), ct);
                    if (nm != null) nm.Names.TryGetValue(rr.UserId.Value, out fancierName!);
                }

                var printDate1 = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm");
                return new
                {
                    certificate = new
                    {
                        recipientName   = req.CertificateRecipientName ?? fancierName,
                        rank            = req.CertificateRank ?? OrdinalRank(rr.ClubRank ?? 0),
                        achievement     = req.CertificateAchievement ?? "",
                        ringNumber      = rr.RingNumber,
                        pigeonName      = rr.PigeonName ?? "",
                        pigeonSex       = rr.PigeonSex ?? "",
                        speedMperMin    = Math.Round(rr.SpeedMperMin, 4).ToString("F4"),
                        distanceKm      = Math.Round(rr.DistanceKm, 3).ToString("F3"),
                        arrivalTime     = rr.ArrivalTime.ToString("HH:mm:ss"),
                        raceName        = rr.RaceName,
                        aceRank = 0, superAceRank = 0, loftRank = 0,
                        totalScore = "", racesEntered = 0, racesInProgramme = 0,
                        pigeonsEntered = 0, bestSpeedMperMin = ""
                    },
                    race = new
                    {
                        name = rr.RaceName,
                        date = rr.RaceDate?.ToString("dd MMM yyyy") ?? "",
                        releaseLocation = rr.ReleaseLocation ?? ""
                    },
                    programme = new { name = "", year = 0, superAceQualification = "" },
                    club = BuildClubData(brandResp),
                    season    = rr.RaceDate?.Year.ToString() ?? "",
                    printDate = printDate1
                };
            }
        }

        if (req.ProgrammeId.HasValue)
        {
            var prog = await Ask<GetProgrammeForRenderRequest, ProgrammeForRenderResult>(
                _programmeClient, new GetProgrammeForRenderRequest(req.ProgrammeId.Value), ct);
            if (prog != null && prog.Found)
            {
                var club = new
                {
                    name            = prog.ClubName,
                    logoUrl         = prog.ClubLogoUrl ?? "",
                    primaryColour   = prog.ClubPrimaryColor,
                    secondaryColour = prog.ClubSecondaryColor,
                    primaryColor    = prog.ClubPrimaryColor,
                    secondaryColor  = prog.ClubSecondaryColor
                };
                var printDate2 = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm");
                return new
                {
                    certificate = new
                    {
                        recipientName   = req.CertificateRecipientName ?? "",
                        rank            = req.CertificateRank ?? "",
                        achievement     = req.CertificateAchievement ?? "",
                        ringNumber = "", pigeonName = "", pigeonSex = "",
                        speedMperMin = "", distanceKm = "", arrivalTime = "", raceName = "",
                        aceRank = 0, superAceRank = 0, loftRank = 0,
                        totalScore = "", racesEntered = 0, racesInProgramme = 0,
                        pigeonsEntered = 0, bestSpeedMperMin = ""
                    },
                    race      = new { name = "", date = "", releaseLocation = "" },
                    programme = new { name = prog.Name, year = prog.Year, superAceQualification = prog.SuperAceQualification },
                    club,
                    season    = prog.Year.ToString(),
                    printDate = printDate2
                };
            }
        }

        return new
        {
            certificate = new { },
            race        = new { },
            programme   = new { },
            club        = new { },
            season      = "",
            printDate   = ""
        };
    }

    private static object BuildClubData(ClubBrandingResult? branding) => new
    {
        name            = branding?.Name ?? "",
        logoUrl         = branding?.LogoUrl ?? "",
        primaryColour   = branding?.PrimaryColor ?? "#1E3A5F",
        secondaryColour = branding?.SecondaryColor ?? "#C9A84C",
        primaryColor    = branding?.PrimaryColor ?? "#1E3A5F",
        secondaryColor  = branding?.SecondaryColor ?? "#C9A84C"
    };

    private static string OrdinalRank(int n) => n switch
    {
        1 => "1st",
        2 => "2nd",
        3 => "3rd",
        _ => $"{n}th"
    };

    private static async Task<T?> Ask<TRequest, T>(
        IRequestClient<TRequest> client, TRequest request, CancellationToken ct)
        where TRequest : class
        where T : class
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            var response = await client.GetResponse<T>(request, cts.Token);
            return response.Message;
        }
        catch
        {
            return null;
        }
    }
}
