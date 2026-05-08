using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.ClubService.Data;
using PRC.Common.Messages;

namespace PRC.ClubService.Events;

public class GetClubStatsConsumer : IConsumer<GetClubStatsRequest>
{
    private readonly ClubDbContext _db;
    public GetClubStatsConsumer(ClubDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetClubStatsRequest> ctx)
    {
        var total   = await _db.Clubs.CountAsync(c => !c.IsDeleted);
        var active  = await _db.Clubs.CountAsync(c => !c.IsDeleted && c.IsActive);
        var members = await _db.ClubMemberships.CountAsync();
        await ctx.RespondAsync(new ClubStatsResult(total, active, members));
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
                c.IsActive, c.CreatedAt))
            .ToListAsync();

        await ctx.RespondAsync(new AllClubsResult(items, total));
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
