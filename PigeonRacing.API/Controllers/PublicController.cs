using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PigeonRacing.Application.Common;
using PigeonRacing.Application.Common.Interfaces;
using PigeonRacing.Application.Features.Results;
using PigeonRacing.Domain.Entities;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.API.Controllers;

/// <summary>
/// Unauthenticated endpoints powering the public-facing club pages at /p/:slug
/// </summary>
[Route("api/public")]
[AllowAnonymous]
[ApiController]
public class PublicController : ControllerBase
{
    private readonly IAppDbContext _db;

    public PublicController(IAppDbContext db) => _db = db;

    /// <summary>
    /// Returns everything needed to render /p/:slug — club info, theme, published races + top results.
    /// </summary>
    [HttpGet("clubs/{slug}")]
    public async Task<IActionResult> GetClubPage(string slug, CancellationToken ct)
    {
        var page = await _db.ClubPages
            .Include(p => p.Club)
                .ThenInclude(c => c.Country)
            .Include(p => p.Club)
                .ThenInclude(c => c.Memberships)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished && !p.IsDeleted, ct);

        if (page == null)
            return NotFound(new { success = false, message = "Club page not found or not published." });

        var club = page.Club;

        // Get published races
        var races = await _db.Races
            .Include(r => r.Categories)
            .Where(r => r.ClubId == club.Id && r.Status == RaceStatus.Published)
            .OrderByDescending(r => r.PublishedAt)
            .Take(10)
            .ToListAsync(ct);

        // Get top 20 results per race (for the leaderboard)
        var raceIds = races.Select(r => r.Id).ToList();
        var allResults = await _db.RaceResults
            .Include(r => r.User)
            .Include(r => r.Category)
            .Where(r => raceIds.Contains(r.RaceId)
                     && r.Status == ResultStatus.Published
                     && !r.IsDeleted)
            .OrderBy(r => r.ClubRank)
            .ToListAsync(ct);

        var resultsByRace = allResults
            .GroupBy(r => r.RaceId)
            .ToDictionary(
                g => g.Key.ToString(),
                g => g.Take(20).Select(r => new
                {
                    r.Id,
                    r.RingNumber,
                    r.PigeonName,
                    FancierName = r.User?.FullName,
                    r.VelocityMperMin,
                    VelocityKmH = r.VelocityKmH,
                    r.DistanceKm,
                    r.ClubRank,
                    r.CategoryRank,
                    CategoryName = r.Category?.Name,
                    r.ArrivalTime
                }).ToList()
            );

        // Get live races (not included in published above)
        var liveRaces = await _db.Races
            .Where(r => r.ClubId == club.Id && r.Status == RaceStatus.InProgress)
            .Select(r => new { r.Id, r.Name, r.TotalPigeonsEntered })
            .ToListAsync(ct);

        // Parse announcements from JSON
        List<object> announcements = new();
        if (!string.IsNullOrEmpty(page.AnnouncementsJson))
        {
            try
            {
                announcements = System.Text.Json.JsonSerializer.Deserialize<List<object>>(page.AnnouncementsJson)
                    ?? new();
            }
            catch { /* ignore malformed JSON */ }
        }

        return Ok(new
        {
            success = true,
            data = new
            {
                club = new
                {
                    club.Id,
                    club.Name,
                    club.Code,
                    club.Description,
                    club.City,
                    club.LogoUrl,
                    club.PrimaryColor,
                    club.SecondaryColor,
                    MemberCount = club.Memberships.Count(m => m.IsActive),
                    CountryName = club.Country?.Name
                },
                theme = (int)page.Theme,
                races = races.Select(r => new
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
                    Categories = r.Categories.Select(c => new { c.Id, c.Name, c.SortOrder })
                }),
                resultsByRace,
                liveRaces,
                announcements
            }
        });
    }

    /// <summary>
    /// Returns a minimal list of all published club pages (for a directory / sitemap).
    /// </summary>
    [HttpGet("clubs")]
    public async Task<IActionResult> ListPublishedClubs(
        [FromQuery] string? country = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var q = _db.ClubPages
            .Include(p => p.Club).ThenInclude(c => c.Country)
            .Where(p => p.IsPublished && !p.IsDeleted);

        if (!string.IsNullOrEmpty(country))
            q = q.Where(p => p.Club.Country.Code == country.ToUpperInvariant());

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(p => p.Club.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Slug,
                p.Club.Name,
                p.Club.City,
                CountryCode = p.Club.Country.Code,
                CountryName = p.Club.Country.Name,
                Theme = (int)p.Theme,
                p.Club.LogoUrl
            })
            .ToListAsync(ct);

        return Ok(new
        {
            success = true,
            data = new { items, total, page, pageSize, totalPages = (int)Math.Ceiling((double)total / pageSize) }
        });
    }
}
