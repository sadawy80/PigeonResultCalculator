using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.PublicService.Services;

namespace PRC.PublicService.Controllers;

[Route("api/public")]
[AllowAnonymous]
[ApiController]
public class PublicController : ControllerBase
{
    private readonly IPublicQueryService _svc;

    public PublicController(IPublicQueryService svc) => _svc = svc;

    [HttpGet("clubs/{slug}")]
    public async Task<IActionResult> GetClubPage(string slug, CancellationToken ct)
    {
        var club = await _svc.GetClubBySlugAsync(slug, ct);
        if (club == null)
            return NotFound(new { success = false, message = "Club page not found or not published." });

        var raceData = await _svc.GetPublishedRacesAsync(club.ClubId, 10, ct);
        var allFancierIds = raceData?.Published
            .SelectMany(r => r.TopResults.Select(res => res.FancierId))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct() ?? Enumerable.Empty<Guid>();

        var names = await _svc.GetUserNamesAsync(allFancierIds, ct);

        var resultsByRace = (raceData?.Published ?? Enumerable.Empty<PRC.Common.Messages.PublicRaceItem>())
            .ToDictionary(
                r => r.Id.ToString(),
                r => r.TopResults.Select(res => new
                {
                    res.Id,
                    res.RingNumber,
                    res.PigeonName,
                    FancierName = res.FancierId.HasValue && names.TryGetValue(res.FancierId.Value, out var n) ? n : null,
                    SpeedMperMin = res.SpeedMperMin,
                    SpeedKmH = res.SpeedMperMin * 60.0 / 1000.0,
                    res.DistanceKm,
                    res.ClubRank,
                    res.CategoryRank,
                    res.CategoryName,
                    res.ArrivalTime
                }).ToList()
            );

        return Ok(new
        {
            success = true,
            data = new
            {
                club = new
                {
                    club.ClubId,
                    club.Name,
                    club.Code,
                    club.Description,
                    club.City,
                    club.LogoUrl,
                    club.PrimaryColor,
                    club.SecondaryColor,
                    club.MemberCount,
                    club.FederationName
                },
                theme = club.Theme,
                races = raceData?.Published.Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Description,
                    r.Status,
                    r.ReleaseLocation,
                    r.ActualReleaseTime,
                    r.ScheduledReleaseTime,
                    r.PublishedAt,
                    r.TotalPigeonsEntered,
                    r.WindSpeedKmh,
                    r.WindDirection,
                    r.TemperatureCelsius,
                    Categories = r.Categories
                }),
                resultsByRace,
                liveRaces = raceData?.Live,
                announcements = ParseAnnouncements(club.AnnouncementsJson)
            }
        });
    }

    [HttpGet("countries/{slug}")]
    public async Task<IActionResult> GetFederationPage(string slug, CancellationToken ct)
    {
        var federation = await _svc.GetFederationBySlugAsync(slug, ct);
        if (federation == null)
            return NotFound(new { success = false, message = "Federation page not found or not published." });

        var clubs = await _svc.ListPublishedClubsAsync(null, federation.FederationId, 1, 100, ct);

        return Ok(new
        {
            success = true,
            data = new
            {
                federation = new
                {
                    federation.FederationId,
                    federation.Name,
                    federation.Code,
                    Slug = federation.FederationSlug
                },
                theme = federation.Theme,
                clubCount = clubs?.Items.Count ?? 0,
                clubPages = clubs?.Items.Select(c => new
                {
                    c.Slug,
                    c.Name,
                    c.City,
                    c.FederationCode,
                    c.FederationName,
                    c.LogoUrl,
                    c.Theme
                }),
                recentResults = federation.RecentResults.Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Description,
                    r.PublishedAt,
                    r.TotalEntriesCount,
                    r.TotalClubsCount,
                    TopEntries = r.TopEntries.Select(e => new
                    {
                        e.NationalRank,
                        e.RingNumber,
                        e.SpeedMperMin,
                        e.FancierName,
                        e.ClubName
                    })
                }),
                announcements = ParseAnnouncements(federation.AnnouncementsJson)
            }
        });
    }

    [HttpGet("plans")]
    public async Task<IActionResult> GetPublicPlans(CancellationToken ct)
    {
        var result = await _svc.GetPublicPlansAsync(ct);
        return Ok(new { success = true, data = result?.Plans });
    }

    [HttpGet("clubs")]
    public async Task<IActionResult> ListPublishedClubs(
        [FromQuery] string? country = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _svc.ListPublishedClubsAsync(country, null, page, pageSize, ct);
        return Ok(new
        {
            success = true,
            data = new
            {
                items = result?.Items,
                total = result?.Total ?? 0,
                page,
                pageSize,
                totalPages = result == null ? 0 : (int)Math.Ceiling((double)result.Total / pageSize)
            }
        });
    }

    private static List<object> ParseAnnouncements(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new();
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<object>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }
}
