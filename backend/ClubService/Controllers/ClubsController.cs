using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.ClubService.DTOs;
using PRC.ClubService.Services;
using PRC.ClubService.Data;

namespace PRC.ClubService.Controllers;

[Route("api/clubs")]
[Authorize]
public class ClubsController : ClubControllerBase
{
    private readonly IClubService _clubs;
    private readonly ICurrentUserService _user;
    private readonly ClubDbContext _db;

    public ClubsController(IClubService clubs, ICurrentUserService user, ClubDbContext db)
    {
        _clubs = clubs;
        _user = user;
        _db = db;
    }

    [HttpPost]
    [Authorize(Roles = "FederationManager,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateClubRequest req, CancellationToken ct)
        => FromResult(await _clubs.CreateClubAsync(req, _user.UserId.GetValueOrDefault(), ct));

    [HttpGet("{clubId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid clubId, CancellationToken ct)
        => FromResult(await _clubs.GetClubAsync(clubId, ct));

    [HttpPut("{clubId:guid}/branding")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> UpdateBranding(Guid clubId, [FromBody] UpdateClubBrandingRequest req, CancellationToken ct)
        => FromResult(await _clubs.UpdateBrandingAsync(clubId, req, ct));

    [HttpPut("{clubId:guid}/theme")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> SetTheme(Guid clubId, [FromBody] SetThemeRequest req, CancellationToken ct)
        => FromResult(await _clubs.SetThemeAsync(clubId, req.Theme, ct));

    [HttpGet("{clubId:guid}/members")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> GetMembers(Guid clubId, [FromQuery] PagedQuery paged, CancellationToken ct)
        => FromResult(await _clubs.GetMembersAsync(clubId, paged, ct));

    [HttpDelete("{clubId:guid}/members/{userId:guid}")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> RemoveMember(Guid clubId, Guid userId, CancellationToken ct)
        => FromResult(await _clubs.RemoveMemberAsync(clubId, userId, ct));

    [HttpPost("{clubId:guid}/invite")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> Invite(Guid clubId, [FromBody] InviteRequest req, CancellationToken ct)
        => FromResult(await _clubs.SendInvitationAsync(clubId, req.Email, _user.UserId.GetValueOrDefault(), ct));

    [HttpGet("{clubId:guid}/invitations")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> GetInvitations(Guid clubId, CancellationToken ct)
        => FromResult(await _clubs.GetInvitationsAsync(clubId, ct));

    [HttpPost("memberships/{membershipId:guid}/link-pigeon")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> LinkPigeon(Guid membershipId, [FromBody] LinkPigeonRequest req, CancellationToken ct)
        => FromResult(await _clubs.LinkPigeonAsync(membershipId, req.RingNumber, _user.UserId.GetValueOrDefault(), ct));

    [HttpGet("{clubId:guid}/page-info")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> GetPageInfo(Guid clubId, CancellationToken ct)
        => FromResult(await _clubs.GetPageInfoAsync(clubId, ct));

    [HttpPut("{clubId:guid}/announcements")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> UpdateAnnouncements(Guid clubId, [FromBody] UpdateAnnouncementsRequest req, CancellationToken ct)
        => FromResult(await _clubs.UpdateAnnouncementsAsync(clubId, req.AnnouncementsJson, ct));

    [HttpPut("{clubId:guid}/slug")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> UpdateSlug(Guid clubId, [FromBody] UpdateSlugRequest req, CancellationToken ct)
        => FromResult(await _clubs.UpdateSlugAsync(clubId, req.NewSlug, ct));

    [HttpGet("{clubId:guid}/stats")]
    [Authorize(Roles = "ClubManager,FederationManager,SuperAdmin")]
    public async Task<IActionResult> GetStats(Guid clubId, CancellationToken ct)
    {
        var thisYear = DateTime.UtcNow.Year;
        var totalProgrammes     = await _db.ClubProgrammes.CountAsync(p => p.ClubId == clubId, ct);
        var programmesThisYear  = await _db.ClubProgrammes.CountAsync(p => p.ClubId == clubId && p.Year == thisYear, ct);
        var totalMembers        = await _db.ClubMemberships.CountAsync(m => m.ClubId == clubId && m.IsActive, ct);
        return Ok(ApiResponse<object>.Ok(new
        {
            totalProgrammes,
            programmesThisYear,
            totalMembers
        }));
    }
}
