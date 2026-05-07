using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PigeonRacing.Application.Common;
using PigeonRacing.Application.Common.Interfaces;
using PigeonRacing.Domain.Entities;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.Application.Features.Clubs;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record ClubDto(
    Guid Id, Guid CountryId, string CountryName, string Name, string Code,
    string? Description, string? City, string? LogoUrl,
    string? PrimaryColor, string? SecondaryColor,
    bool IsActive, int MemberCount, DateTime CreatedAt);

public record ClubMemberDto(
    Guid MembershipId, Guid UserId, string FullName, string Email,
    UserRole Role, DateTime JoinedAt, int LinkedPigeonCount);

public record InvitationDto(
    Guid Id, string Email, InvitationStatus Status,
    DateTime ExpiresAt, DateTime CreatedAt, string ClubName);

public record NotificationDto(
    Guid Id, NotificationType Type, NotificationChannel Channel,
    NotificationStatus Status, string Title, string Body,
    string? ActionUrl, DateTime CreatedAt, DateTime? ReadAt);

public record ThemeDto(
    int Id, string Name, string Description,
    string PrimaryColor, string AccentColor,
    string BackgroundColor, string SurfaceColor,
    string TextColor, string PreviewImageUrl);

// ── Theme Definitions (static — shipped with the platform) ───────────────────

public static class BuiltInThemes
{
    public static readonly List<ThemeDto> All = new()
    {
        new(
            (int)SiteTheme.Skyline,
            "Skyline",
            "Modern dark navy with electric blue accents. Clean grid layout.",
            "#0D1B2A", "#1E90FF",
            "#0A1520", "#132030",
            "#E8F0FE", "/assets/themes/skyline-preview.jpg"),

        new(
            (int)SiteTheme.Meadow,
            "Meadow",
            "Earthy greens and warm amber. Nature-inspired rounded cards.",
            "#2D6A4F", "#F4A261",
            "#F9F3E8", "#FFFFFF",
            "#1B3A2D", "/assets/themes/meadow-preview.jpg"),

        new(
            (int)SiteTheme.Crimson,
            "Crimson",
            "Bold red and charcoal. High-contrast sport-grade feel.",
            "#C1121F", "#2B2D42",
            "#F5F5F5", "#FFFFFF",
            "#1A1A2E", "/assets/themes/crimson-preview.jpg"),

        new(
            (int)SiteTheme.Ivory,
            "Ivory",
            "Light cream and gold accents. Classic formal federation look.",
            "#B8860B", "#8B6914",
            "#FAF7F0", "#FFFFFF",
            "#3D3320", "/assets/themes/ivory-preview.jpg"),

        new(
            (int)SiteTheme.Slate,
            "Slate",
            "Cool grey with cyan highlights. Minimal data-forward dashboard style.",
            "#4A5568", "#00B4D8",
            "#F7FAFC", "#FFFFFF",
            "#2D3748", "/assets/themes/slate-preview.jpg"),
    };

    public static ThemeDto Get(SiteTheme theme) =>
        All.First(t => t.Id == (int)theme);
}

// ── Commands ──────────────────────────────────────────────────────────────────

public record CreateClubCommand(
    Guid CountryId, string Name, string Code,
    string? Description, string? City, string? Address, string? PostalCode,
    double? Latitude, double? Longitude,
    string? ContactEmail, string? ContactPhone) : IRequest<Result<ClubDto>>;

public record UpdateClubBrandingCommand(
    Guid ClubId, string? LogoUrl,
    string? PrimaryColor, string? SecondaryColor,
    SiteTheme Theme) : IRequest<Result>;

public record SendInvitationCommand(
    Guid ClubId, string Email) : IRequest<Result<InvitationDto>>;

public record AcceptInvitationCommand(
    string Token, string Password,
    string FirstName, string LastName) : IRequest<Result<string>>;

public record RemoveMemberCommand(Guid ClubId, Guid UserId) : IRequest<Result>;

public record LinkPigeonCommand(
    Guid MembershipId, string RingNumber) : IRequest<Result>;

public record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result>;

public record GetThemesQuery : IRequest<Result<List<ThemeDto>>>;

public record SetThemeCommand(Guid ClubId, SiteTheme Theme) : IRequest<Result>;

public record GetClubPageInfoDto(string Slug, bool IsPublished);
public record GetClubPageInfoQuery(Guid ClubId) : IRequest<Result<GetClubPageInfoDto>>;
public record UpdateSlugCommand(Guid ClubId, string NewSlug) : IRequest<Result<string>>;

// ── Queries ───────────────────────────────────────────────────────────────────

public record GetClubQuery(Guid ClubId) : IRequest<Result<ClubDto>>;
public record GetClubMembersQuery(Guid ClubId, PagedQuery Paged) : IRequest<Result<PagedResult<ClubMemberDto>>>;
public record GetClubInvitationsQuery(Guid ClubId) : IRequest<Result<List<InvitationDto>>>;
public record GetMyNotificationsQuery(PagedQuery Paged) : IRequest<Result<PagedResult<NotificationDto>>>;

// ── Create Club Handler ───────────────────────────────────────────────────────

public class CreateClubHandler : IRequestHandler<CreateClubCommand, Result<ClubDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateClubHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<ClubDto>> Handle(CreateClubCommand cmd, CancellationToken ct)
    {
        // Check subscription limit
        var subscription = await _db.CountrySubscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.CountryId == cmd.CountryId
                && s.Status == SubscriptionStatus.Active, ct);

        if (subscription != null)
        {
            var clubCount = await _db.Clubs.CountAsync(c => c.CountryId == cmd.CountryId && c.IsActive, ct);
            if (clubCount >= subscription.Plan.MaxClubs)
                return Result.Failure<ClubDto>(
                    $"Club limit of {subscription.Plan.MaxClubs} reached for this country's subscription.",
                    "SUBSCRIPTION_LIMIT");
        }

        var codeExists = await _db.Clubs.AnyAsync(c => c.CountryId == cmd.CountryId && c.Code == cmd.Code, ct);
        if (codeExists) return Result.Conflict<ClubDto>("Club code already exists in this country.");

        var club = new Club
        {
            CountryId = cmd.CountryId, Name = cmd.Name, Code = cmd.Code,
            Description = cmd.Description, City = cmd.City, Address = cmd.Address,
            PostalCode = cmd.PostalCode, Latitude = cmd.Latitude, Longitude = cmd.Longitude,
            ContactEmail = cmd.ContactEmail, ContactPhone = cmd.ContactPhone,
            CreatedBy = _currentUser.UserId
        };

        _db.Clubs.Add(club);

        // Auto-create a default club page
        _db.ClubPages.Add(new ClubPage
        {
            ClubId = club.Id,
            Slug = await GenerateUniqueSlugAsync(cmd.Name, cmd.CountryId, ct),
            Theme = SiteTheme.Skyline
        });

        await _db.SaveChangesAsync(ct);

        return Result.Success(await BuildClubDtoAsync(club.Id, ct));
    }

    private async Task<ClubDto> BuildClubDtoAsync(Guid id, CancellationToken ct)
    {
        var club = await _db.Clubs.Include(c => c.Country).Include(c => c.Memberships)
            .FirstAsync(c => c.Id == id, ct);
        return club.ToDto();
    }

    private async Task<string> GenerateUniqueSlugAsync(string name, Guid countryId, CancellationToken ct)
    {
        var baseSlug = Slugify(name);
        if (!string.IsNullOrEmpty(baseSlug) && !await _db.ClubPages.AnyAsync(p => p.Slug == baseSlug, ct))
            return baseSlug;

        var country = await _db.Countries.FirstOrDefaultAsync(c => c.Id == countryId, ct);
        var countryCode = (country?.Code ?? "xx").ToLowerInvariant();
        var withCountry = string.IsNullOrEmpty(baseSlug) ? countryCode : $"{baseSlug}-{countryCode}";
        if (!await _db.ClubPages.AnyAsync(p => p.Slug == withCountry, ct))
            return withCountry;

        for (var i = 2; ; i++)
        {
            var candidate = $"{withCountry}-{i}";
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

// ── Update Club Branding Handler ──────────────────────────────────────────────

public class UpdateClubBrandingHandler : IRequestHandler<UpdateClubBrandingCommand, Result>
{
    private readonly IAppDbContext _db;

    public UpdateClubBrandingHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateClubBrandingCommand cmd, CancellationToken ct)
    {
        var club = await _db.Clubs.FirstOrDefaultAsync(c => c.Id == cmd.ClubId, ct);
        if (club == null) return Result.NotFound("Club");

        club.LogoUrl = cmd.LogoUrl;
        club.PrimaryColor = cmd.PrimaryColor;
        club.SecondaryColor = cmd.SecondaryColor;
        club.UpdatedAt = DateTime.UtcNow;

        // Update club page theme
        var page = await _db.ClubPages.FirstOrDefaultAsync(p => p.ClubId == cmd.ClubId, ct);
        if (page != null) page.Theme = cmd.Theme;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── Set Theme Handler ─────────────────────────────────────────────────────────

public class SetThemeHandler : IRequestHandler<SetThemeCommand, Result>
{
    private readonly IAppDbContext _db;

    public SetThemeHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(SetThemeCommand cmd, CancellationToken ct)
    {
        var page = await _db.ClubPages.FirstOrDefaultAsync(p => p.ClubId == cmd.ClubId, ct);
        if (page == null) return Result.NotFound("ClubPage");

        page.Theme = cmd.Theme;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── Get Themes Handler ────────────────────────────────────────────────────────

public class GetThemesHandler : IRequestHandler<GetThemesQuery, Result<List<ThemeDto>>>
{
    public Task<Result<List<ThemeDto>>> Handle(GetThemesQuery _, CancellationToken ct)
        => Task.FromResult(Result.Success(BuiltInThemes.All));
}

// ── Send Invitation Handler ───────────────────────────────────────────────────

public class SendInvitationHandler : IRequestHandler<SendInvitationCommand, Result<InvitationDto>>
{
    private readonly IAppDbContext _db;
    private readonly IEmailService _email;
    private readonly ICurrentUserService _currentUser;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

    public SendInvitationHandler(IAppDbContext db, IEmailService email,
        ICurrentUserService currentUser,
        Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _db = db;
        _email = email;
        _currentUser = currentUser;
        _config = config;
    }

    public async Task<Result<InvitationDto>> Handle(SendInvitationCommand cmd, CancellationToken ct)
    {
        var club = await _db.Clubs.Include(c => c.Country)
            .FirstOrDefaultAsync(c => c.Id == cmd.ClubId, ct);
        if (club == null) return Result.NotFound<InvitationDto>("Club");

        // Check if already a member
        var alreadyMember = await _db.Users
            .AnyAsync(u => u.Email == cmd.Email &&
                _db.ClubMemberships.Any(m => m.ClubId == cmd.ClubId && m.UserId == u.Id), ct);

        if (alreadyMember)
            return Result.Conflict<InvitationDto>("User is already a member of this club.");

        // Expire old pending invitations for same email+club
        var oldInvites = _db.Invitations.Where(i =>
            i.ClubId == cmd.ClubId && i.Email == cmd.Email && i.Status == InvitationStatus.Pending);
        await oldInvites.ForEachAsync(i => i.Status = InvitationStatus.Revoked, ct);

        var invitation = new Invitation
        {
            ClubId = cmd.ClubId,
            Email = cmd.Email,
            InvitedByUserId = _currentUser.UserId!.Value,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.Invitations.Add(invitation);
        await _db.SaveChangesAsync(ct);

        var baseUrl = _config["App:BaseUrl"] ?? "https://pigeonracing.com";
        var link = $"{baseUrl}/auth/accept-invitation?token={invitation.Token}";
        var sender = await _db.Users.FindAsync(new object[] { _currentUser.UserId!.Value }, ct);

        await _email.SendInvitationAsync(cmd.Email, sender?.FullName ?? "Club Manager", club.Name, link, ct);

        return Result.Success(invitation.ToDto(club.Name));
    }
}

// ── Accept Invitation Handler ─────────────────────────────────────────────────

public class AcceptInvitationHandler : IRequestHandler<AcceptInvitationCommand, Result<string>>
{
    private readonly IAppDbContext _db;

    public AcceptInvitationHandler(IAppDbContext db) => _db = db;

    public async Task<Result<string>> Handle(AcceptInvitationCommand cmd, CancellationToken ct)
    {
        var invite = await _db.Invitations
            .FirstOrDefaultAsync(i => i.Token == cmd.Token
                && i.Status == InvitationStatus.Pending
                && i.ExpiresAt > DateTime.UtcNow, ct);

        if (invite == null)
            return Result.Failure<string>("Invalid or expired invitation.", "INVALID_INVITATION");

        // Return token so frontend can proceed to registration with the token
        return Result.Success(invite.Token);
    }
}

// ── Remove Member Handler ─────────────────────────────────────────────────────

public class RemoveMemberHandler : IRequestHandler<RemoveMemberCommand, Result>
{
    private readonly IAppDbContext _db;

    public RemoveMemberHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(RemoveMemberCommand cmd, CancellationToken ct)
    {
        var membership = await _db.ClubMemberships
            .FirstOrDefaultAsync(m => m.ClubId == cmd.ClubId && m.UserId == cmd.UserId, ct);

        if (membership == null) return Result.NotFound("ClubMembership");

        membership.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── Link Pigeon Handler ───────────────────────────────────────────────────────

public class LinkPigeonHandler : IRequestHandler<LinkPigeonCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public LinkPigeonHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(LinkPigeonCommand cmd, CancellationToken ct)
    {
        var membership = await _db.ClubMemberships.FindAsync(new object[] { cmd.MembershipId }, ct);
        if (membership == null) return Result.NotFound("ClubMembership");

        var alreadyLinked = await _db.PigeonLinks
            .AnyAsync(p => p.MembershipId == cmd.MembershipId && p.RingNumber == cmd.RingNumber, ct);

        if (alreadyLinked) return Result.Conflict("Pigeon already linked to this fancier.");

        // Try to resolve a pigeon record by ring number
        var pigeon = await _db.Pigeons.FirstOrDefaultAsync(p => p.RingNumber == cmd.RingNumber, ct);

        _db.PigeonLinks.Add(new PigeonLink
        {
            MembershipId = cmd.MembershipId,
            RingNumber = cmd.RingNumber,
            PigeonId = pigeon?.Id,
            LinkedByUserId = _currentUser.UserId!.Value,
            IsVerified = pigeon != null
        });

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── Get Club Handler ──────────────────────────────────────────────────────────

public class GetClubHandler : IRequestHandler<GetClubQuery, Result<ClubDto>>
{
    private readonly IAppDbContext _db;

    public GetClubHandler(IAppDbContext db) => _db = db;

    public async Task<Result<ClubDto>> Handle(GetClubQuery query, CancellationToken ct)
    {
        var club = await _db.Clubs
            .Include(c => c.Country)
            .Include(c => c.Memberships)
            .FirstOrDefaultAsync(c => c.Id == query.ClubId, ct);

        return club == null ? Result.NotFound<ClubDto>("Club") : Result.Success(club.ToDto());
    }
}

// ── Get Club Members Handler ──────────────────────────────────────────────────

public class GetClubMembersHandler : IRequestHandler<GetClubMembersQuery, Result<PagedResult<ClubMemberDto>>>
{
    private readonly IAppDbContext _db;

    public GetClubMembersHandler(IAppDbContext db) => _db = db;

    public async Task<Result<PagedResult<ClubMemberDto>>> Handle(GetClubMembersQuery query, CancellationToken ct)
    {
        var q = _db.ClubMemberships
            .Include(m => m.User)
            .Include(m => m.PigeonLinks)
            .Where(m => m.ClubId == query.ClubId && m.IsActive);

        if (!string.IsNullOrEmpty(query.Paged.Search))
            q = q.Where(m => m.User.FirstName.Contains(query.Paged.Search)
                          || m.User.LastName.Contains(query.Paged.Search)
                          || m.User.Email!.Contains(query.Paged.Search));

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip(query.Paged.Skip)
            .Take(query.Paged.PageSize)
            .Select(m => new ClubMemberDto(
                m.Id, m.UserId,
                m.User.FirstName + " " + m.User.LastName,
                m.User.Email!,
                m.User.Role,
                m.JoinedAt,
                m.PigeonLinks.Count))
            .ToListAsync(ct);

        return Result.Success(new PagedResult<ClubMemberDto>
        {
            Items = items, TotalCount = total, Page = query.Paged.Page, PageSize = query.Paged.PageSize
        });
    }
}

// ── Get My Notifications Handler ──────────────────────────────────────────────

public class GetMyNotificationsHandler : IRequestHandler<GetMyNotificationsQuery, Result<PagedResult<NotificationDto>>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyNotificationsHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<NotificationDto>>> Handle(GetMyNotificationsQuery query, CancellationToken ct)
    {
        if (!_currentUser.UserId.HasValue)
            return Result.Failure<PagedResult<NotificationDto>>("Not authenticated.", "UNAUTHENTICATED");

        var q = _db.Notifications.Where(n => n.UserId == _currentUser.UserId.Value);
        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(n => n.CreatedAt)
            .Skip(query.Paged.Skip)
            .Take(query.Paged.PageSize)
            .Select(n => new NotificationDto(
                n.Id, n.Type, n.Channel, n.Status,
                n.Title, n.Body, n.ActionUrl, n.CreatedAt, n.ReadAt))
            .ToListAsync(ct);

        return Result.Success(new PagedResult<NotificationDto>
        {
            Items = items, TotalCount = total, Page = query.Paged.Page, PageSize = query.Paged.PageSize
        });
    }
}

// ── Mark Notification Read Handler ────────────────────────────────────────────

public class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly INotificationService _notifications;

    public MarkNotificationReadHandler(INotificationService notifications) => _notifications = notifications;

    public async Task<Result> Handle(MarkNotificationReadCommand cmd, CancellationToken ct)
    {
        await _notifications.MarkAsReadAsync(cmd.NotificationId, ct);
        return Result.Success();
    }
}

// ── Get Club Invitations Handler ──────────────────────────────────────────────

public class GetClubInvitationsHandler : IRequestHandler<GetClubInvitationsQuery, Result<List<InvitationDto>>>
{
    private readonly IAppDbContext _db;

    public GetClubInvitationsHandler(IAppDbContext db) => _db = db;

    public async Task<Result<List<InvitationDto>>> Handle(GetClubInvitationsQuery query, CancellationToken ct)
    {
        var club = await _db.Clubs.FindAsync(new object[] { query.ClubId }, ct);
        if (club == null) return Result.NotFound<List<InvitationDto>>("Club");

        var invitations = await _db.Invitations
            .Where(i => i.ClubId == query.ClubId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => i.ToDto(club.Name))
            .ToListAsync(ct);

        return Result.Success(invitations);
    }
}

// ── Get Club Page Info Handler ────────────────────────────────────────────────

public class GetClubPageInfoHandler : IRequestHandler<GetClubPageInfoQuery, Result<GetClubPageInfoDto>>
{
    private readonly IAppDbContext _db;

    public GetClubPageInfoHandler(IAppDbContext db) => _db = db;

    public async Task<Result<GetClubPageInfoDto>> Handle(GetClubPageInfoQuery query, CancellationToken ct)
    {
        var page = await _db.ClubPages.FirstOrDefaultAsync(p => p.ClubId == query.ClubId, ct);
        if (page == null) return Result.NotFound<GetClubPageInfoDto>("ClubPage");
        return Result.Success(new GetClubPageInfoDto(page.Slug, page.IsPublished));
    }
}

// ── Update Slug Handler ───────────────────────────────────────────────────────

public class UpdateSlugHandler : IRequestHandler<UpdateSlugCommand, Result<string>>
{
    private readonly IAppDbContext _db;

    public UpdateSlugHandler(IAppDbContext db) => _db = db;

    public async Task<Result<string>> Handle(UpdateSlugCommand cmd, CancellationToken ct)
    {
        var slug = Slugify(cmd.NewSlug);
        if (string.IsNullOrEmpty(slug))
            return Result.Failure<string>("Slug must contain at least one letter or number.", "VALIDATION_ERROR");

        var page = await _db.ClubPages.FirstOrDefaultAsync(p => p.ClubId == cmd.ClubId, ct);
        if (page == null) return Result.NotFound<string>("ClubPage");

        if (page.Slug == slug) return Result.Success(slug);

        var taken = await _db.ClubPages.AnyAsync(p => p.Slug == slug && p.ClubId != cmd.ClubId, ct);
        if (taken) return Result.Conflict<string>("This URL is already taken. Please choose a different one.");

        page.Slug = slug;
        await _db.SaveChangesAsync(ct);
        return Result.Success(slug);
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

// ── Mapping ───────────────────────────────────────────────────────────────────

public static class ClubMappingExtensions
{
    public static ClubDto ToDto(this Club c) => new(
        c.Id, c.CountryId, c.Country?.Name ?? string.Empty,
        c.Name, c.Code, c.Description, c.City, c.LogoUrl,
        c.PrimaryColor, c.SecondaryColor, c.IsActive,
        c.Memberships.Count(m => m.IsActive), c.CreatedAt);

    public static InvitationDto ToDto(this Invitation i, string clubName) => new(
        i.Id, i.Email, i.Status, i.ExpiresAt, i.CreatedAt, clubName);
}
