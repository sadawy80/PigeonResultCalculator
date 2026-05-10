using System.Text.RegularExpressions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.ClubService.Data;
using PRC.ClubService.Models;
using PRC.Common;
using PRC.Common.Messages;

namespace PRC.ClubService.Events;

public class GetClubStatsConsumer : IConsumer<GetClubStatsRequest>
{
    private readonly ClubDbContext _db;
    public GetClubStatsConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetClubStatsRequest> ctx)
    {
        var yearStart = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var total            = await _db.Clubs.CountAsync(c => !c.IsDeleted);
        var active           = await _db.Clubs.CountAsync(c => !c.IsDeleted && c.IsActive);
        var members          = await _db.ClubMemberships.CountAsync(ctx.CancellationToken);
        var aceCount         = await _db.AcePigeonResults.CountAsync(ctx.CancellationToken);
        var superAce         = await _db.SuperAcePigeonResults.CountAsync(ctx.CancellationToken);
        var bestLoft         = await _db.BestLoftResults.CountAsync(ctx.CancellationToken);
        var programmes       = await _db.ClubProgrammes.CountAsync(ctx.CancellationToken);
        var progsThisYear    = await _db.ClubProgrammes.CountAsync(p => p.Year == DateTime.UtcNow.Year, ctx.CancellationToken);
        var clubsThisYear    = await _db.Clubs.CountAsync(c => !c.IsDeleted && c.CreatedAt >= yearStart, ctx.CancellationToken);
        var aceThisYear      = await _db.AcePigeonResults.CountAsync(r => r.CreatedAt >= yearStart, ctx.CancellationToken);
        var superAceThisYear = await _db.SuperAcePigeonResults.CountAsync(r => r.CreatedAt >= yearStart, ctx.CancellationToken);
        var bestLoftThisYear = await _db.BestLoftResults.CountAsync(r => r.CreatedAt >= yearStart, ctx.CancellationToken);

        await ctx.RespondAsync(new ClubStatsResult(total, active, members,
            aceCount, superAce, bestLoft, programmes, progsThisYear,
            clubsThisYear, aceThisYear, superAceThisYear, bestLoftThisYear));
    }
}

public class GetAllClubsConsumer : IConsumer<GetAllClubsRequest>
{
    private readonly ClubDbContext _db;
    public GetAllClubsConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetAllClubsRequest> ctx)
    {
        var m = ctx.Message;
        var q = _db.Clubs.Where(c => !c.IsDeleted);

        if (!string.IsNullOrEmpty(m.Search))
            q = q.Where(c => c.Name.Contains(m.Search) || c.Code.Contains(m.Search));

        if (m.FederationId.HasValue)
            q = q.Where(c => c.FederationId == m.FederationId.Value);

        var total = await q.CountAsync();
        var items = await q
            .OrderBy(c => c.Name)
            .Skip((m.Page - 1) * m.PageSize)
            .Take(m.PageSize)
            .Select(c => new AdminClubItem(
                c.Id, c.Name, c.Code, c.City,
                c.FederationId, c.FederationName,
                c.IsActive, c.CreatedAt, c.SubscriptionExpiresAt))
            .ToListAsync();

        await ctx.RespondAsync(new AllClubsResult(items, total));
    }
}

public class SetClubSubscriptionExpiryConsumer : IConsumer<SetClubSubscriptionExpiryRequest>
{
    private readonly ClubDbContext _db;
    public SetClubSubscriptionExpiryConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<SetClubSubscriptionExpiryRequest> ctx)
    {
        var club = await _db.Clubs.FindAsync(ctx.Message.ClubId);
        if (club is null)
        {
            await ctx.RespondAsync(new SetClubSubscriptionExpiryResult(false, "Club not found."));
            return;
        }
        club.SubscriptionExpiresAt = ctx.Message.ExpiresAt;
        club.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await ctx.RespondAsync(new SetClubSubscriptionExpiryResult(true, null));
    }
}

public class ToggleClubActiveConsumer : IConsumer<ToggleClubActiveRequest>
{
    private readonly ClubDbContext _db;
    public ToggleClubActiveConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<ToggleClubActiveRequest> ctx)
    {
        var club = await _db.Clubs.FindAsync(ctx.Message.ClubId);
        if (club is null)
        {
            await ctx.RespondAsync(new ToggleClubActiveResult(ctx.Message.ClubId, false, "Club not found."));
            return;
        }

        club.IsActive  = !club.IsActive;
        club.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await ctx.RespondAsync(new ToggleClubActiveResult(club.Id, club.IsActive, null));
    }
}

public class AdminCreateClubConsumer : IConsumer<AdminCreateClubRequest>
{
    private readonly ClubDbContext _db;
    public AdminCreateClubConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<AdminCreateClubRequest> ctx)
    {
        var m = ctx.Message;
        // Uniqueness check: code must be unique within the same federation (or globally if no federation)
        var codeExists = m.FederationId.HasValue
            ? await _db.Clubs.AnyAsync(c => c.FederationId == m.FederationId && c.Code == m.Code, ctx.CancellationToken)
            : await _db.Clubs.AnyAsync(c => c.FederationId == null && c.Code == m.Code, ctx.CancellationToken);
        if (codeExists)
        {
            await ctx.RespondAsync(new AdminCreateClubResult(false, null, $"Club code '{m.Code}' already exists."));
            return;
        }

        var club = new Club { FederationId = m.FederationId, Name = m.Name, Code = m.Code, City = m.City, CreatedBy = m.CreatedBy };
        _db.Clubs.Add(club);

        var slug = SlugOf(m.Name);
        if (string.IsNullOrEmpty(slug)) slug = "club";
        if (await _db.ClubPages.AnyAsync(p => p.Slug == slug, ctx.CancellationToken))
            for (var i = 2; ; i++) { var c = $"{slug}-{i}"; if (!await _db.ClubPages.AnyAsync(p => p.Slug == c, ctx.CancellationToken)) { slug = c; break; } }

        _db.ClubPages.Add(new ClubPage { ClubId = club.Id, Slug = slug, Theme = SiteTheme.Skyline });
        await _db.SaveChangesAsync(ctx.CancellationToken);
        await ctx.RespondAsync(new AdminCreateClubResult(true, club.Id, null));
    }

    private static string SlugOf(string s) =>
        Regex.Replace(s.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
}

public class AdminAssignClubManagerConsumer : IConsumer<AdminAssignClubManagerRequest>
{
    private readonly ClubDbContext _db;
    public AdminAssignClubManagerConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<AdminAssignClubManagerRequest> ctx)
    {
        var m = ctx.Message;

        var conflict = await _db.ClubMemberships
            .FirstOrDefaultAsync(cm => cm.UserId == m.UserId && cm.UserRole == UserRole.ClubManager
                && cm.IsActive && cm.ClubId != m.ClubId, ctx.CancellationToken);

        if (conflict != null && !m.Force)
        {
            var cc = await _db.Clubs.FindAsync(new object[] { conflict.ClubId }, ctx.CancellationToken);
            await ctx.RespondAsync(new AdminAssignClubManagerResult(false, true, conflict.ClubId, cc?.Name, null, null));
            return;
        }
        if (conflict != null) conflict.IsActive = false;

        var membership = await _db.ClubMemberships
            .FirstOrDefaultAsync(cm => cm.ClubId == m.ClubId && cm.UserId == m.UserId, ctx.CancellationToken);

        if (membership == null)
            _db.ClubMemberships.Add(new ClubMembership
            {
                ClubId = m.ClubId, UserId = m.UserId,
                UserFullName = m.UserFullName, UserEmail = m.UserEmail,
                UserRole = UserRole.ClubManager, JoinedAt = DateTime.UtcNow, IsActive = true
            });
        else
        {
            membership.UserRole = UserRole.ClubManager;
            membership.IsActive = true;
            membership.UserFullName = m.UserFullName;
            membership.UserEmail = m.UserEmail;
        }

        await _db.SaveChangesAsync(ctx.CancellationToken);
        var club = await _db.Clubs.FindAsync(new object[] { m.ClubId }, ctx.CancellationToken);
        await ctx.RespondAsync(new AdminAssignClubManagerResult(true, false, null, null, club?.FederationId, null));
    }
}

public class GetActiveClubCountForFederationConsumer : IConsumer<GetActiveClubCountForFederationRequest>
{
    private readonly ClubDbContext _db;
    public GetActiveClubCountForFederationConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetActiveClubCountForFederationRequest> ctx)
    {
        var count = await _db.Clubs
            .CountAsync(c => c.FederationId == ctx.Message.FederationId && c.IsActive);
        await ctx.RespondAsync(new GetActiveClubCountForFederationResult(count));
    }
}

public class GetAdminProgrammesConsumer : IConsumer<GetAdminProgrammesRequest>
{
    private readonly ClubDbContext _db;
    public GetAdminProgrammesConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetAdminProgrammesRequest> ctx)
    {
        var m = ctx.Message;
        var q = _db.ClubProgrammes.Include(p => p.Club).AsQueryable();

        if (!string.IsNullOrWhiteSpace(m.Search))
            q = q.Where(p => p.Name.Contains(m.Search)
                          || (p.Club != null && p.Club.Name.Contains(m.Search))
                          || (p.FederationName != null && p.FederationName.Contains(m.Search)));

        if (m.ClubId.HasValue)
            q = q.Where(p => p.ClubId == m.ClubId.Value);

        var total = await q.CountAsync(ctx.CancellationToken);
        var items = await q
            .OrderByDescending(p => p.Year).ThenBy(p => p.Name)
            .Skip((m.Page - 1) * m.PageSize)
            .Take(m.PageSize)
            .Select(p => new AdminProgrammeItem(
                p.Id, p.Name, p.Year,
                p.ClubId ?? Guid.Empty,
                p.Club != null ? p.Club.Name : (p.FederationName ?? "Federation"),
                p.AcePigeonResults.Count,
                p.SuperAcePigeonResults.Count,
                p.BestLoftResults.Count,
                (int)p.Status, p.CreatedAt))
            .ToListAsync(ctx.CancellationToken);

        await ctx.RespondAsync(new AdminProgrammesResult(items, total));
    }
}

public class GetAdminAcePigeonResultsConsumer : IConsumer<GetAdminAcePigeonResultsRequest>
{
    private readonly ClubDbContext _db;
    public GetAdminAcePigeonResultsConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetAdminAcePigeonResultsRequest> ctx)
    {
        var m = ctx.Message;
        var q = _db.AcePigeonResults.Include(r => r.Programme).ThenInclude(p => p.Club).AsQueryable();

        if (!string.IsNullOrWhiteSpace(m.Search))
            q = q.Where(r => r.FancierName.Contains(m.Search) || r.RingNumber.Contains(m.Search));

        if (m.ClubId.HasValue)
            q = q.Where(r => r.Programme.ClubId == m.ClubId.Value);

        if (m.ProgrammeId.HasValue)
            q = q.Where(r => r.ProgrammeId == m.ProgrammeId.Value);

        var total = await q.CountAsync(ctx.CancellationToken);
        var items = await q
            .OrderBy(r => r.AceRank)
            .Skip((m.Page - 1) * m.PageSize)
            .Take(m.PageSize)
            .Select(r => new AdminAcePigeonItem(
                r.Id, r.ProgrammeId, r.Programme.Name, r.Programme.Year,
                r.Programme.ClubId ?? Guid.Empty,
                r.Programme.Club != null ? r.Programme.Club.Name : (r.Programme.FederationName ?? "Federation"),
                r.FancierName, r.RingNumber, r.PigeonName,
                r.AceRank, r.TotalScore, r.RacesEntered))
            .ToListAsync(ctx.CancellationToken);

        await ctx.RespondAsync(new AdminAcePigeonResultsResult(items, total));
    }
}

public class GetAdminSuperAceResultsConsumer : IConsumer<GetAdminSuperAceResultsRequest>
{
    private readonly ClubDbContext _db;
    public GetAdminSuperAceResultsConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetAdminSuperAceResultsRequest> ctx)
    {
        var m = ctx.Message;
        var q = _db.SuperAcePigeonResults.Include(r => r.Programme).ThenInclude(p => p.Club).AsQueryable();

        if (!string.IsNullOrWhiteSpace(m.Search))
            q = q.Where(r => r.FancierName.Contains(m.Search) || r.RingNumber.Contains(m.Search));

        if (m.ClubId.HasValue)
            q = q.Where(r => r.Programme.ClubId == m.ClubId.Value);

        if (m.ProgrammeId.HasValue)
            q = q.Where(r => r.ProgrammeId == m.ProgrammeId.Value);

        var total = await q.CountAsync(ctx.CancellationToken);
        var items = await q
            .OrderBy(r => r.SuperAceRank)
            .Skip((m.Page - 1) * m.PageSize)
            .Take(m.PageSize)
            .Select(r => new AdminAcePigeonItem(
                r.Id, r.ProgrammeId, r.Programme.Name, r.Programme.Year,
                r.Programme.ClubId ?? Guid.Empty,
                r.Programme.Club != null ? r.Programme.Club.Name : (r.Programme.FederationName ?? "Federation"),
                r.FancierName, r.RingNumber, r.PigeonName,
                r.SuperAceRank, r.TotalScore, r.RacesEntered))
            .ToListAsync(ctx.CancellationToken);

        await ctx.RespondAsync(new AdminSuperAceResultsResult(items, total));
    }
}

public class GetAdminBestLoftResultsConsumer : IConsumer<GetAdminBestLoftResultsRequest>
{
    private readonly ClubDbContext _db;
    public GetAdminBestLoftResultsConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetAdminBestLoftResultsRequest> ctx)
    {
        var m = ctx.Message;
        var q = _db.BestLoftResults.Include(r => r.Programme).ThenInclude(p => p.Club).AsQueryable();

        if (!string.IsNullOrWhiteSpace(m.Search))
            q = q.Where(r => r.FancierName.Contains(m.Search) || r.Programme.Club.Name.Contains(m.Search));

        if (m.ClubId.HasValue)
            q = q.Where(r => r.Programme.ClubId == m.ClubId.Value);

        if (m.ProgrammeId.HasValue)
            q = q.Where(r => r.ProgrammeId == m.ProgrammeId.Value);

        var total = await q.CountAsync(ctx.CancellationToken);
        var items = await q
            .OrderBy(r => r.LoftRank)
            .Skip((m.Page - 1) * m.PageSize)
            .Take(m.PageSize)
            .Select(r => new AdminBestLoftItem(
                r.Id, r.ProgrammeId, r.Programme.Name, r.Programme.Year,
                r.Programme.ClubId ?? Guid.Empty,
                r.Programme.Club != null ? r.Programme.Club.Name : (r.Programme.FederationName ?? "Federation"),
                r.FancierName, r.LoftRank, r.TotalScore, r.RacesEntered))
            .ToListAsync(ctx.CancellationToken);

        await ctx.RespondAsync(new AdminBestLoftResultsResult(items, total));
    }
}

public class AdminDeleteProgrammeConsumer : IConsumer<AdminDeleteProgrammeRequest>
{
    private readonly ClubDbContext _db;
    public AdminDeleteProgrammeConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<AdminDeleteProgrammeRequest> ctx)
    {
        var prog = await _db.ClubProgrammes
            .Include(p => p.ProgrammeRaces)
            .Include(p => p.AcePigeonResults)
            .Include(p => p.SuperAcePigeonResults)
            .Include(p => p.BestLoftResults)
            .FirstOrDefaultAsync(p => p.Id == ctx.Message.ProgrammeId, ctx.CancellationToken);

        if (prog is null)
        {
            await ctx.RespondAsync(new AdminDeleteProgrammeResult(false, "Programme not found.", null));
            return;
        }

        var name = prog.Name;
        var now  = DateTime.UtcNow;
        prog.IsDeleted = true;
        prog.UpdatedAt = now;
        foreach (var pr in prog.ProgrammeRaces.Where(r => !r.IsDeleted))
            { pr.IsDeleted = true; pr.UpdatedAt = now; }
        foreach (var r in prog.AcePigeonResults.Where(r => !r.IsDeleted))
            { r.IsDeleted = true; r.UpdatedAt = now; }
        foreach (var r in prog.SuperAcePigeonResults.Where(r => !r.IsDeleted))
            { r.IsDeleted = true; r.UpdatedAt = now; }
        foreach (var r in prog.BestLoftResults.Where(r => !r.IsDeleted))
            { r.IsDeleted = true; r.UpdatedAt = now; }
        await _db.SaveChangesAsync(ctx.CancellationToken);
        await ctx.RespondAsync(new AdminDeleteProgrammeResult(true, null, name));
    }
}

public class NotifyClubManagersConsumer : IConsumer<NotifyClubManagersRequest>
{
    private readonly ClubDbContext _db;
    public NotifyClubManagersConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<NotifyClubManagersRequest> ctx)
    {
        var m = ctx.Message;
        var managers = await _db.ClubMemberships
            .Where(cm => cm.ClubId == m.ClubId && cm.UserRole == UserRole.ClubManager && cm.IsActive)
            .ToListAsync(ctx.CancellationToken);

        if (managers.Count == 0)
        {
            await ctx.RespondAsync(new NotifyClubManagersResult(true, 0));
            return;
        }

        var now = DateTime.UtcNow;
        var notifications = managers.Select(cm => new Notification
        {
            Id        = Guid.NewGuid(),
            UserId    = cm.UserId,
            Type      = NotificationType.SystemUpdate,
            Channel   = NotificationChannel.InApp,
            Status    = NotificationStatus.Pending,
            Title     = m.Title,
            Body      = m.Message,
            ActionUrl = m.EntityType != null && m.EntityId != null
                          ? $"/{m.EntityType.ToLower()}/{m.EntityId}"
                          : null,
            CreatedAt = now
        }).ToList();

        _db.Notifications.AddRange(notifications);
        await _db.SaveChangesAsync(ctx.CancellationToken);
        await ctx.RespondAsync(new NotifyClubManagersResult(true, notifications.Count));
    }
}

public class GetAdminNotificationsConsumer : IConsumer<GetAdminNotificationsRequest>
{
    private readonly ClubDbContext _db;
    public GetAdminNotificationsConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetAdminNotificationsRequest> ctx)
    {
        var m = ctx.Message;
        var q = _db.Notifications.AsQueryable();
        if (!string.IsNullOrWhiteSpace(m.Search))
            q = q.Where(n => n.Title.Contains(m.Search) || (n.Body != null && n.Body.Contains(m.Search)));

        var total = await q.CountAsync(ctx.CancellationToken);
        var items = await q
            .OrderByDescending(n => n.CreatedAt)
            .Skip((m.Page - 1) * m.PageSize)
            .Take(m.PageSize)
            .Select(n => new AdminNotificationItem(
                n.Id, n.UserId, n.Title, n.Body,
                n.Type.ToString(), n.Status.ToString(), n.Channel.ToString(),
                n.CreatedAt, n.ReadAt))
            .ToListAsync(ctx.CancellationToken);

        await ctx.RespondAsync(new GetAdminNotificationsResult(items, total));
    }
}

public class AdminSendNotificationConsumer : IConsumer<AdminSendNotificationBusRequest>
{
    private readonly ClubDbContext _db;
    public AdminSendNotificationConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<AdminSendNotificationBusRequest> ctx)
    {
        var m = ctx.Message;
        var managers = await _db.ClubMemberships
            .Where(cm => cm.ClubId == m.ClubId && cm.IsActive)
            .ToListAsync(ctx.CancellationToken);

        if (managers.Count == 0)
        {
            await ctx.RespondAsync(new AdminSendNotificationBusResult(true, 0, null));
            return;
        }

        var now = DateTime.UtcNow;
        var notifications = managers.Select(cm => new Notification
        {
            Id        = Guid.NewGuid(),
            UserId    = cm.UserId,
            Type      = NotificationType.SystemUpdate,
            Channel   = NotificationChannel.InApp,
            Status    = NotificationStatus.Pending,
            Title     = m.Title,
            Body      = m.Message,
            CreatedAt = now,
        }).ToList();

        _db.Notifications.AddRange(notifications);
        await _db.SaveChangesAsync(ctx.CancellationToken);
        await ctx.RespondAsync(new AdminSendNotificationBusResult(true, notifications.Count, null));
    }
}

public class AdminDeleteClubConsumer : IConsumer<AdminDeleteClubRequest>
{
    private readonly ClubDbContext _db;
    public AdminDeleteClubConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<AdminDeleteClubRequest> ctx)
    {
        var m = ctx.Message;
        var club = await _db.Clubs
            .Include(c => c.ClubPage)
            .Include(c => c.Invitations)
            .Include(c => c.Memberships)
                .ThenInclude(m => m.PigeonLinks)
            .Include(c => c.Programmes)
                .ThenInclude(p => p.ProgrammeRaces)
            .Include(c => c.Programmes)
                .ThenInclude(p => p.BestLoftResults)
            .Include(c => c.Programmes)
                .ThenInclude(p => p.AcePigeonResults)
            .Include(c => c.Programmes)
                .ThenInclude(p => p.SuperAcePigeonResults)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == m.ClubId, ctx.CancellationToken);

        if (club is null)
        {
            await ctx.RespondAsync(new AdminDeleteClubResult(false, "Club not found.", null));
            return;
        }
        if (club.IsActive)
        {
            await ctx.RespondAsync(new AdminDeleteClubResult(false, "Club must be suspended before deletion.", club.Name));
            return;
        }

        var now = DateTime.UtcNow;

        if (club.ClubPage != null)   { club.ClubPage.IsDeleted = true;   club.ClubPage.UpdatedAt = now; }
        foreach (var inv in club.Invitations) { inv.IsDeleted = true; inv.UpdatedAt = now; }

        foreach (var m2 in club.Memberships)
        {
            foreach (var link in m2.PigeonLinks) { link.IsDeleted = true; link.UpdatedAt = now; }
            m2.IsDeleted = true;
            m2.UpdatedAt = now;
        }

        foreach (var prog in club.Programmes)
        {
            foreach (var pr  in prog.ProgrammeRaces)       { pr.IsDeleted  = true; pr.UpdatedAt  = now; }
            foreach (var blr in prog.BestLoftResults)      { blr.IsDeleted = true; blr.UpdatedAt = now; }
            foreach (var ace in prog.AcePigeonResults)     { ace.IsDeleted = true; ace.UpdatedAt = now; }
            foreach (var sa  in prog.SuperAcePigeonResults){ sa.IsDeleted  = true; sa.UpdatedAt  = now; }
            prog.IsDeleted = true;
            prog.UpdatedAt = now;
        }

        club.IsDeleted = true;
        club.UpdatedAt = now;

        await _db.SaveChangesAsync(ctx.CancellationToken);
        await ctx.RespondAsync(new AdminDeleteClubResult(true, null, club.Name));
    }
}
