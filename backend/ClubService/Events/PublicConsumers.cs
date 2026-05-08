using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.Common.Messages;
using PRC.ClubService.Data;

namespace PRC.ClubService.Events;

public class GetPublicClubBySlugConsumer : IConsumer<GetPublicClubBySlugRequest>
{
    private readonly ClubDbContext _db;
    public GetPublicClubBySlugConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetPublicClubBySlugRequest> ctx)
    {
        var page = await _db.ClubPages
            .Include(p => p.Club)
                .ThenInclude(c => c.Memberships)
            .FirstOrDefaultAsync(p => p.Slug == ctx.Message.Slug && p.IsPublished && !p.IsDeleted,
                ctx.CancellationToken);

        if (page == null)
        {
            await ctx.RespondAsync(new PublicClubResult(false, Guid.Empty, "", null, null, null, null, null, null, 0, null, 0, null));
            return;
        }

        var club = page.Club;
        await ctx.RespondAsync(new PublicClubResult(
            true,
            club.Id, club.Name, club.Code, club.Description,
            club.City, club.LogoUrl, club.PrimaryColor, club.SecondaryColor,
            club.Memberships.Count(m => m.IsActive && !m.IsDeleted),
            club.FederationName,
            (int)page.Theme,
            page.AnnouncementsJson));
    }
}

public class ListPublishedClubsForPublicConsumer : IConsumer<ListPublishedClubsForPublicRequest>
{
    private readonly ClubDbContext _db;
    public ListPublishedClubsForPublicConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<ListPublishedClubsForPublicRequest> ctx)
    {
        var m = ctx.Message;
        var q = _db.ClubPages
            .Include(p => p.Club)
            .Where(p => p.IsPublished && !p.IsDeleted && !p.Club.IsDeleted);

        if (!string.IsNullOrEmpty(m.FederationCode))
            q = q.Where(p => p.Club.FederationName != null);

        if (m.FederationId.HasValue)
            q = q.Where(p => p.Club.FederationId == m.FederationId.Value);

        var total = await q.CountAsync(ctx.CancellationToken);
        var items = await q
            .OrderBy(p => p.Club.Name)
            .Skip((m.Page - 1) * m.PageSize)
            .Take(m.PageSize)
            .Select(p => new PublicClubListItem(
                p.Slug, p.Club.Name, p.Club.City,
                null, p.Club.FederationName,
                (int)p.Theme, p.Club.LogoUrl))
            .ToListAsync(ctx.CancellationToken);

        await ctx.RespondAsync(new ListPublishedClubsForPublicResult(total, items));
    }
}
