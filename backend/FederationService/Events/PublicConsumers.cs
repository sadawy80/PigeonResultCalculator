using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.Common.Messages;
using PRC.FederationService.Data;

namespace PRC.FederationService.Events;

public class GetPublicFederationBySlugConsumer : IConsumer<GetPublicFederationBySlugRequest>
{
    private readonly FederationDbContext _db;
    public GetPublicFederationBySlugConsumer(FederationDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetPublicFederationBySlugRequest> ctx)
    {
        var page = await _db.FederationPages
            .Include(p => p.Federation)
            .FirstOrDefaultAsync(p => p.Slug == ctx.Message.Slug && p.IsPublished && !p.IsDeleted,
                ctx.CancellationToken);

        if (page == null)
        {
            await ctx.RespondAsync(new PublicFederationResult(false, Guid.Empty, "", null, null, 0, null, new List<PublicFederationResultSummary>()));
            return;
        }

        var federation = page.Federation;

        var recentResults = await _db.FederationResults
            .Include(r => r.Entries)
            .Where(r => r.FederationId == federation.Id && r.Status == FederationResultStatus.Published && !r.IsDeleted)
            .OrderByDescending(r => r.PublishedAt)
            .Take(5)
            .ToListAsync(ctx.CancellationToken);

        var summaries = recentResults.Select(r => new PublicFederationResultSummary(
            r.Id, r.Name, r.Description, r.PublishedAt,
            r.TotalEntriesCount, r.TotalClubsCount,
            r.Entries
                .OrderBy(e => e.NationalRank)
                .Take(10)
                .Select(e => new PublicFederationResultEntry(
                    e.NationalRank, e.RingNumber, e.SpeedMperMin,
                    e.UserFullName, e.ClubName ?? ""))
                .ToList()
        )).ToList();

        await ctx.RespondAsync(new PublicFederationResult(
            true,
            federation.Id, federation.Name, federation.Code, federation.Slug,
            (int)page.Theme, page.AnnouncementsJson,
            summaries));
    }
}
