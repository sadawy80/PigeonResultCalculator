using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.Common.Messages;
using PRC.RaceService.Data;

namespace PRC.RaceService.Events;

public class GetRaceStatsConsumer : IConsumer<GetRaceStatsRequest>
{
    private readonly RaceDbContext _db;
    public GetRaceStatsConsumer(RaceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetRaceStatsRequest> ctx)
    {
        var now        = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearStart  = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var total            = await _db.Races.CountAsync(r => !r.IsDeleted, ctx.CancellationToken);
        var published        = await _db.Races.CountAsync(r => !r.IsDeleted && r.Status == RaceStatus.Published, ctx.CancellationToken);
        var thisMonth        = await _db.Races.CountAsync(r => !r.IsDeleted && r.CreatedAt >= monthStart, ctx.CancellationToken);
        var thisYear         = await _db.Races.CountAsync(r => !r.IsDeleted && r.CreatedAt >= yearStart, ctx.CancellationToken);
        var totalResults     = await _db.RaceResults.CountAsync(r => !r.IsDeleted, ctx.CancellationToken);
        var resultsThisYear  = await _db.RaceResults.CountAsync(r => !r.IsDeleted && r.CreatedAt >= yearStart, ctx.CancellationToken);
        var totalPigeons     = await _db.Pigeons.CountAsync(p => !p.IsDeleted, ctx.CancellationToken);
        var pigeonsThisYear  = await _db.Pigeons.CountAsync(p => !p.IsDeleted && p.CreatedAt >= yearStart, ctx.CancellationToken);

        await ctx.RespondAsync(new RaceStatsResult(total, published, thisMonth, totalResults,
            totalPigeons, thisYear, resultsThisYear, pigeonsThisYear));
    }
}

public class GetAdminPigeonsConsumer : IConsumer<GetAdminPigeonsRequest>
{
    private readonly RaceDbContext _db;
    public GetAdminPigeonsConsumer(RaceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetAdminPigeonsRequest> ctx)
    {
        var m = ctx.Message;
        var q = _db.Pigeons.Where(p => !p.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(m.Search))
            q = q.Where(p => p.RingNumber.Contains(m.Search) || (p.Name != null && p.Name.Contains(m.Search)));

        if (m.FederationId.HasValue)
            q = q.Where(p => p.FederationId == m.FederationId.Value);

        if (m.ClubId.HasValue)
            q = q.Where(p => p.ClubId == m.ClubId.Value);

        if (!string.IsNullOrWhiteSpace(m.FancierSearch))
        {
            var ringsByFancier = _db.RaceResults
                .Where(r => !r.IsDeleted && r.FancierName != null && r.FancierName.Contains(m.FancierSearch))
                .Select(r => r.RingNumber)
                .Distinct();
            q = q.Where(p => ringsByFancier.Contains(p.RingNumber));
        }

        var total = await q.CountAsync(ctx.CancellationToken);
        var items = await q
            .OrderBy(p => p.RingNumber)
            .Skip((m.Page - 1) * m.PageSize)
            .Take(m.PageSize)
            .Select(p => new AdminPigeonItem(
                p.Id, p.RingNumber, p.Name, p.Sex, p.YearOfBirth,
                p.Color, p.FederationId, p.CreatedAt,
                _db.RaceResults
                    .Where(r => r.RingNumber == p.RingNumber && !r.IsDeleted && r.FancierName != null)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => r.FancierName)
                    .FirstOrDefault()))
            .ToListAsync(ctx.CancellationToken);

        await ctx.RespondAsync(new AdminPigeonsResult(items, total));
    }
}

public class AdminUpdatePigeonConsumer : IConsumer<AdminUpdatePigeonRequest>
{
    private readonly RaceDbContext _db;
    public AdminUpdatePigeonConsumer(RaceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<AdminUpdatePigeonRequest> ctx)
    {
        var m      = ctx.Message;
        var pigeon = await _db.Pigeons.FindAsync([m.PigeonId], ctx.CancellationToken);
        if (pigeon is null)
        {
            await ctx.RespondAsync(new AdminUpdatePigeonResult(false, "Pigeon not found."));
            return;
        }

        pigeon.Name        = m.Name;
        pigeon.Sex         = m.Sex;
        pigeon.YearOfBirth = m.YearOfBirth;
        pigeon.Color       = m.Color;
        pigeon.UpdatedAt   = DateTime.UtcNow;
        await _db.SaveChangesAsync(ctx.CancellationToken);
        await ctx.RespondAsync(new AdminUpdatePigeonResult(true, null));
    }
}

public class AdminDeletePigeonConsumer : IConsumer<AdminDeletePigeonRequest>
{
    private readonly RaceDbContext _db;
    public AdminDeletePigeonConsumer(RaceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<AdminDeletePigeonRequest> ctx)
    {
        var pigeon = await _db.Pigeons.FindAsync([ctx.Message.PigeonId], ctx.CancellationToken);
        if (pigeon is null)
        {
            await ctx.RespondAsync(new AdminDeletePigeonResult(false, "Pigeon not found.", null));
            return;
        }

        var ring       = pigeon.RingNumber;
        pigeon.IsDeleted = true;
        pigeon.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ctx.CancellationToken);
        await ctx.RespondAsync(new AdminDeletePigeonResult(true, null, ring));
    }
}

public class GetAdminRacesConsumer : IConsumer<GetAdminRacesRequest>
{
    private readonly RaceDbContext _db;
    public GetAdminRacesConsumer(RaceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetAdminRacesRequest> ctx)
    {
        var m = ctx.Message;
        var q = _db.Races.Where(r => !r.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(m.Search))
            q = q.Where(r => r.Name.Contains(m.Search) || r.ClubName.Contains(m.Search));

        if (m.ClubId.HasValue)
            q = q.Where(r => r.ClubId == m.ClubId.Value);

        if (m.Status.HasValue)
            q = q.Where(r => (int)r.Status == m.Status.Value);

        if (m.DateFrom.HasValue)
            q = q.Where(r => r.ScheduledReleaseTime >= m.DateFrom.Value || r.ActualReleaseTime >= m.DateFrom.Value);

        if (m.DateTo.HasValue)
            q = q.Where(r => r.ScheduledReleaseTime <= m.DateTo.Value || r.ActualReleaseTime <= m.DateTo.Value);

        var total = await q.CountAsync(ctx.CancellationToken);

        var items = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((m.Page - 1) * m.PageSize)
            .Take(m.PageSize)
            .Select(r => new AdminRaceItem(
                r.Id, r.Name, r.ClubId, r.ClubName, r.FederationId,
                (int)r.Status, r.ScheduledReleaseTime, r.PublishedAt,
                r.Results.Count(res => !res.IsDeleted),
                r.CreatedAt))
            .ToListAsync(ctx.CancellationToken);

        await ctx.RespondAsync(new AdminRacesResult(items, total));
    }
}

public class AdminDeleteRaceConsumer : IConsumer<AdminDeleteRaceRequest>
{
    private readonly RaceDbContext _db;
    public AdminDeleteRaceConsumer(RaceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<AdminDeleteRaceRequest> ctx)
    {
        var race = await _db.Races
            .Include(r => r.Results)
            .FirstOrDefaultAsync(r => r.Id == ctx.Message.RaceId && !r.IsDeleted,
                ctx.CancellationToken);

        if (race is null)
        {
            await ctx.RespondAsync(new AdminDeleteRaceResult(false, "Race not found.", null, null));
            return;
        }

        var raceName = race.Name;
        var clubId   = race.ClubId;

        // Soft-delete race and its results
        race.IsDeleted = true;
        race.UpdatedAt = DateTime.UtcNow;
        foreach (var result in race.Results.Where(res => !res.IsDeleted))
        {
            result.IsDeleted = true;
            result.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ctx.CancellationToken);
        await ctx.RespondAsync(new AdminDeleteRaceResult(true, null, raceName, clubId));
    }
}

public class GetAdminFanciersConsumer : IConsumer<GetAdminFanciersRequest>
{
    private readonly RaceDbContext _db;
    public GetAdminFanciersConsumer(RaceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetAdminFanciersRequest> ctx)
    {
        var m = ctx.Message;
        var q = _db.Fanciers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(m.Search))
            q = q.Where(f => f.Name.Contains(m.Search));
        if (m.ClubId.HasValue)
            q = q.Where(f => f.ClubId == m.ClubId.Value);
        if (m.FederationId.HasValue)
            q = q.Where(f => f.FederationId == m.FederationId.Value);
        if (m.IsLinked.HasValue)
            q = q.Where(f => m.IsLinked.Value ? f.LinkedUserId != null : f.LinkedUserId == null);

        var total = await q.CountAsync(ctx.CancellationToken);
        var items = await q
            .OrderBy(f => f.Name)
            .Skip((m.Page - 1) * m.PageSize).Take(m.PageSize)
            .Select(f => new AdminFancierItem(
                f.Id, f.Name, f.ClubId, f.ClubName, f.FederationId, f.FederationName, f.Country,
                f.LinkedUserId != null, f.LinkedUserId, f.LinkedUserName, f.LinkedUserEmail,
                f.LinkedAt, f.CreatedAt))
            .ToListAsync(ctx.CancellationToken);

        await ctx.RespondAsync(new GetAdminFanciersResult(items, total));
    }
}

public class LinkFancierToUserConsumer : IConsumer<LinkFancierToUserRequest>
{
    private readonly RaceDbContext _db;
    public LinkFancierToUserConsumer(RaceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<LinkFancierToUserRequest> ctx)
    {
        var m = ctx.Message;
        var fancier = await _db.Fanciers.FirstOrDefaultAsync(f => f.Id == m.FancierId, ctx.CancellationToken);
        if (fancier is null) { await ctx.RespondAsync(new LinkFancierToUserResult(false, "Fancier not found.")); return; }

        fancier.LinkedUserId   = m.UserId;
        fancier.LinkedUserName = m.UserName;
        fancier.LinkedUserEmail = m.UserEmail;
        fancier.LinkedAt       = DateTime.UtcNow;
        fancier.UpdatedAt      = DateTime.UtcNow;

        await _db.RaceResults
            .Where(r => r.FancierId == m.FancierId && r.UserId == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.UserId, m.UserId), ctx.CancellationToken);

        await _db.SaveChangesAsync(ctx.CancellationToken);
        await ctx.RespondAsync(new LinkFancierToUserResult(true, null));
    }
}

public class UnlinkFancierConsumer : IConsumer<UnlinkFancierRequest>
{
    private readonly RaceDbContext _db;
    public UnlinkFancierConsumer(RaceDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<UnlinkFancierRequest> ctx)
    {
        var fancier = await _db.Fanciers.FirstOrDefaultAsync(f => f.Id == ctx.Message.FancierId, ctx.CancellationToken);
        if (fancier is null) { await ctx.RespondAsync(new UnlinkFancierResult(false, "Fancier not found.")); return; }

        fancier.LinkedUserId    = null;
        fancier.LinkedUserName  = null;
        fancier.LinkedUserEmail = null;
        fancier.LinkedAt        = null;
        fancier.UpdatedAt       = DateTime.UtcNow;

        await _db.SaveChangesAsync(ctx.CancellationToken);
        await ctx.RespondAsync(new UnlinkFancierResult(true, null));
    }
}
