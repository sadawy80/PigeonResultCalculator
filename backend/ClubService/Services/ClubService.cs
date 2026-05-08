using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.Common.Messages;
using PRC.ClubService.Data;
using PRC.ClubService.DTOs;
using PRC.ClubService.Models;

namespace PRC.ClubService.Services;

public interface IClubService
{
    Task<Result<ClubDto>> CreateClubAsync(CreateClubRequest req, Guid createdBy, CancellationToken ct);
    Task<Result> UpdateBrandingAsync(Guid clubId, UpdateClubBrandingRequest req, CancellationToken ct);
    Task<Result> SetThemeAsync(Guid clubId, SiteTheme theme, CancellationToken ct);
    Task<Result<ClubDto>> GetClubAsync(Guid clubId, CancellationToken ct);
    Task<Result<PagedResult<ClubMemberDto>>> GetMembersAsync(Guid clubId, PagedQuery paged, CancellationToken ct);
    Task<Result<InvitationDto>> SendInvitationAsync(Guid clubId, string email, Guid invitedBy, CancellationToken ct);
    Task<Result<string>> AcceptInvitationAsync(string token, CancellationToken ct);
    Task<Result> RemoveMemberAsync(Guid clubId, Guid userId, CancellationToken ct);
    Task<Result> LinkPigeonAsync(Guid membershipId, string ringNumber, Guid linkedBy, CancellationToken ct);
    Task<Result<GetClubPageInfoDto>> GetPageInfoAsync(Guid clubId, CancellationToken ct);
    Task<Result> UpdateAnnouncementsAsync(Guid clubId, string announcementsJson, CancellationToken ct);
    Task<Result<string>> UpdateSlugAsync(Guid clubId, string newSlug, CancellationToken ct);
    Task<Result<List<InvitationDto>>> GetInvitationsAsync(Guid clubId, CancellationToken ct);
    Task<Result<PagedResult<NotificationDto>>> GetMyNotificationsAsync(Guid userId, PagedQuery paged, CancellationToken ct);
    Task<Result> MarkNotificationReadAsync(Guid notificationId, CancellationToken ct);
}

public class ClubService : IClubService
{
    private readonly ClubDbContext _db;
    private readonly IPublishEndpoint _bus;
    private readonly IRaceServiceClient _raceClient;
    private readonly IConfiguration _config;

    public ClubService(ClubDbContext db, IPublishEndpoint bus, IRaceServiceClient raceClient, IConfiguration config)
    {
        _db = db;
        _bus = bus;
        _raceClient = raceClient;
        _config = config;
    }

    public async Task<Result<ClubDto>> CreateClubAsync(CreateClubRequest req, Guid createdBy, CancellationToken ct)
    {
        var codeExists = await _db.Clubs.AnyAsync(c => c.FederationId == req.FederationId && c.Code == req.Code, ct);
        if (codeExists) return Result.Conflict<ClubDto>("Club code already exists in this federation.");

        var club = new Club
        {
            FederationId = req.FederationId, Name = req.Name, Code = req.Code,
            Description = req.Description, City = req.City, Address = req.Address,
            PostalCode = req.PostalCode, Latitude = req.Latitude, Longitude = req.Longitude,
            ContactEmail = req.ContactEmail, ContactPhone = req.ContactPhone,
            CreatedBy = createdBy
        };

        _db.Clubs.Add(club);

        _db.ClubPages.Add(new ClubPage
        {
            ClubId = club.Id,
            Slug = await GenerateUniqueSlugAsync(req.Name, ct),
            Theme = SiteTheme.Skyline
        });

        await _db.SaveChangesAsync(ct);
        return Result.Success(await BuildClubDtoAsync(club.Id, ct));
    }

    public async Task<Result> UpdateBrandingAsync(Guid clubId, UpdateClubBrandingRequest req, CancellationToken ct)
    {
        var club = await _db.Clubs.FirstOrDefaultAsync(c => c.Id == clubId, ct);
        if (club == null) return Result.NotFound("Club");

        club.LogoUrl = req.LogoUrl;
        club.PrimaryColor = req.PrimaryColor;
        club.SecondaryColor = req.SecondaryColor;
        club.UpdatedAt = DateTime.UtcNow;

        var page = await _db.ClubPages.FirstOrDefaultAsync(p => p.ClubId == clubId, ct);
        if (page != null) page.Theme = req.Theme;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SetThemeAsync(Guid clubId, SiteTheme theme, CancellationToken ct)
    {
        var page = await _db.ClubPages.FirstOrDefaultAsync(p => p.ClubId == clubId, ct);
        if (page == null) return Result.NotFound("ClubPage");
        page.Theme = theme;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<ClubDto>> GetClubAsync(Guid clubId, CancellationToken ct)
    {
        var club = await _db.Clubs.Include(c => c.Memberships)
            .FirstOrDefaultAsync(c => c.Id == clubId, ct);
        return club == null ? Result.NotFound<ClubDto>("Club") : Result.Success(club.ToDto());
    }

    public async Task<Result<PagedResult<ClubMemberDto>>> GetMembersAsync(Guid clubId, PagedQuery paged, CancellationToken ct)
    {
        var q = _db.ClubMemberships
            .Include(m => m.PigeonLinks)
            .Where(m => m.ClubId == clubId && m.IsActive && !m.IsDeleted);

        if (!string.IsNullOrEmpty(paged.Search))
            q = q.Where(m => (m.UserFullName != null && m.UserFullName.Contains(paged.Search))
                          || (m.UserEmail != null && m.UserEmail.Contains(paged.Search)));

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip(paged.Skip).Take(paged.PageSize)
            .Select(m => new ClubMemberDto(
                m.Id, m.UserId,
                m.UserFullName ?? string.Empty,
                m.UserEmail ?? string.Empty,
                m.UserRole, m.JoinedAt,
                m.PigeonLinks.Count))
            .ToListAsync(ct);

        return Result.Success(new PagedResult<ClubMemberDto>
        {
            Items = items, TotalCount = total, Page = paged.Page, PageSize = paged.PageSize
        });
    }

    public async Task<Result<InvitationDto>> SendInvitationAsync(Guid clubId, string email, Guid invitedBy, CancellationToken ct)
    {
        var club = await _db.Clubs.FirstOrDefaultAsync(c => c.Id == clubId, ct);
        if (club == null) return Result.NotFound<InvitationDto>("Club");

        var alreadyMember = await _db.ClubMemberships
            .AnyAsync(m => m.ClubId == clubId && m.UserEmail == email && m.IsActive, ct);
        if (alreadyMember)
            return Result.Conflict<InvitationDto>("User is already a member of this club.");

        var oldInvites = _db.Invitations.Where(i =>
            i.ClubId == clubId && i.Email == email && i.Status == InvitationStatus.Pending);
        await oldInvites.ForEachAsync(i => i.Status = InvitationStatus.Revoked, ct);

        var invitation = new Invitation
        {
            ClubId = clubId,
            Email = email,
            InvitedByUserId = invitedBy,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.Invitations.Add(invitation);
        await _db.SaveChangesAsync(ct);

        var baseUrl = _config["App:FrontendUrl"] ?? "http://localhost:4300";
        var link = $"{baseUrl}/auth/accept-invitation?token={invitation.Token}";

        await _bus.Publish(new MemberInvited(invitation.Id, email, club.Name, "Club Manager", link), ct);

        return Result.Success(invitation.ToDto(club.Name));
    }

    public async Task<Result<string>> AcceptInvitationAsync(string token, CancellationToken ct)
    {
        var invite = await _db.Invitations
            .FirstOrDefaultAsync(i => i.Token == token
                && i.Status == InvitationStatus.Pending
                && i.ExpiresAt > DateTime.UtcNow, ct);

        if (invite == null)
            return Result.Failure<string>("Invalid or expired invitation.", "INVALID_INVITATION");

        return Result.Success(invite.Token);
    }

    public async Task<Result> RemoveMemberAsync(Guid clubId, Guid userId, CancellationToken ct)
    {
        var membership = await _db.ClubMemberships
            .FirstOrDefaultAsync(m => m.ClubId == clubId && m.UserId == userId, ct);
        if (membership == null) return Result.NotFound("ClubMembership");

        membership.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> LinkPigeonAsync(Guid membershipId, string ringNumber, Guid linkedBy, CancellationToken ct)
    {
        var membership = await _db.ClubMemberships.FindAsync(new object[] { membershipId }, ct);
        if (membership == null) return Result.NotFound("ClubMembership");

        var alreadyLinked = await _db.PigeonLinks
            .AnyAsync(p => p.MembershipId == membershipId && p.RingNumber == ringNumber, ct);
        if (alreadyLinked) return Result.Conflict("Pigeon already linked to this fancier.");

        var pigeonId = await _raceClient.GetPigeonIdAsync(ringNumber, ct);
        var isVerified = pigeonId.HasValue;

        _db.PigeonLinks.Add(new PigeonLink
        {
            MembershipId = membershipId,
            RingNumber = ringNumber,
            PigeonId = pigeonId,
            LinkedByUserId = linkedBy,
            IsVerified = isVerified
        });

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<GetClubPageInfoDto>> GetPageInfoAsync(Guid clubId, CancellationToken ct)
    {
        var page = await _db.ClubPages.Include(p => p.Club)
            .FirstOrDefaultAsync(p => p.ClubId == clubId, ct);
        if (page == null) return Result.NotFound<GetClubPageInfoDto>("ClubPage");

        return Result.Success(new GetClubPageInfoDto(
            page.Slug, page.IsPublished,
            page.Club.LogoUrl, page.Club.PrimaryColor, page.Club.SecondaryColor,
            page.AnnouncementsJson));
    }

    public async Task<Result> UpdateAnnouncementsAsync(Guid clubId, string announcementsJson, CancellationToken ct)
    {
        var page = await _db.ClubPages.FirstOrDefaultAsync(p => p.ClubId == clubId, ct);
        if (page == null) return Result.NotFound("ClubPage");
        page.AnnouncementsJson = announcementsJson;
        page.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<string>> UpdateSlugAsync(Guid clubId, string newSlug, CancellationToken ct)
    {
        var slug = Slugify(newSlug);
        if (string.IsNullOrEmpty(slug))
            return Result.Failure<string>("Slug must contain at least one letter or number.", "VALIDATION_ERROR");

        var page = await _db.ClubPages.FirstOrDefaultAsync(p => p.ClubId == clubId, ct);
        if (page == null) return Result.NotFound<string>("ClubPage");

        if (page.Slug == slug) return Result.Success(slug);

        var taken = await _db.ClubPages.AnyAsync(p => p.Slug == slug && p.ClubId != clubId, ct);
        if (taken) return Result.Conflict<string>("This URL is already taken. Please choose a different one.");

        page.Slug = slug;
        await _db.SaveChangesAsync(ct);
        return Result.Success(slug);
    }

    public async Task<Result<List<InvitationDto>>> GetInvitationsAsync(Guid clubId, CancellationToken ct)
    {
        var club = await _db.Clubs.FindAsync(new object[] { clubId }, ct);
        if (club == null) return Result.NotFound<List<InvitationDto>>("Club");

        var invitations = await _db.Invitations
            .Where(i => i.ClubId == clubId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => i.ToDto(club.Name))
            .ToListAsync(ct);

        return Result.Success(invitations);
    }

    public async Task<Result<PagedResult<NotificationDto>>> GetMyNotificationsAsync(Guid userId, PagedQuery paged, CancellationToken ct)
    {
        var q = _db.Notifications.Where(n => n.UserId == userId && !n.IsDeleted);
        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(n => n.CreatedAt)
            .Skip(paged.Skip).Take(paged.PageSize)
            .Select(n => new NotificationDto(
                n.Id, n.Type, n.Channel, n.Status,
                n.Title, n.Body, n.ActionUrl, n.CreatedAt, n.ReadAt))
            .ToListAsync(ct);

        return Result.Success(new PagedResult<NotificationDto>
        {
            Items = items, TotalCount = total, Page = paged.Page, PageSize = paged.PageSize
        });
    }

    public async Task<Result> MarkNotificationReadAsync(Guid notificationId, CancellationToken ct)
    {
        var notification = await _db.Notifications.FindAsync(new object[] { notificationId }, ct);
        if (notification == null) return Result.NotFound("Notification");
        notification.Status = NotificationStatus.Read;
        notification.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<ClubDto> BuildClubDtoAsync(Guid id, CancellationToken ct)
    {
        var club = await _db.Clubs.Include(c => c.Memberships)
            .FirstAsync(c => c.Id == id, ct);
        return club.ToDto();
    }

    private async Task<string> GenerateUniqueSlugAsync(string name, CancellationToken ct)
    {
        var baseSlug = Slugify(name);
        if (!string.IsNullOrEmpty(baseSlug) && !await _db.ClubPages.AnyAsync(p => p.Slug == baseSlug, ct))
            return baseSlug;

        for (var i = 2; ; i++)
        {
            var candidate = string.IsNullOrEmpty(baseSlug) ? $"club-{i}" : $"{baseSlug}-{i}";
            if (!await _db.ClubPages.AnyAsync(p => p.Slug == candidate, ct))
                return candidate;
        }
    }

    private static string Slugify(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return Regex.Replace(
            sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant(),
            @"[^a-z0-9]+", "-").Trim('-');
    }
}

public static class ClubMappingExtensions
{
    public static ClubDto ToDto(this Club c) => new(
        c.Id, c.FederationId, c.FederationName ?? string.Empty,
        c.Name, c.Code, c.Description, c.City, c.LogoUrl,
        c.PrimaryColor, c.SecondaryColor, c.IsActive,
        c.Memberships.Count(m => m.IsActive), c.CreatedAt);

    public static InvitationDto ToDto(this Invitation i, string clubName) => new(
        i.Id, i.Email, i.Status, i.ExpiresAt, i.CreatedAt, clubName);
}
