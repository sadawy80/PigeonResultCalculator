using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PigeonRacing.Application.Common;
using PigeonRacing.Application.Common.Interfaces;
using PigeonRacing.Domain.Entities;

namespace PigeonRacing.Application.Features.Integration;

// ═════════════════════════════════════════════════════════════════════════════
//  DTOs
// ═════════════════════════════════════════════════════════════════════════════

public record ExternalLinkDto(
    Guid Id,
    Guid UserId,
    string FancierName,
    Guid ClubId,
    string ClubName,
    string ExternalPlatformName,
    string ExternalUserId,
    string ExternalLoftId,
    string ExternalLoftName,
    string LinkToken,
    ExternalLinkStatus Status,
    string StatusName,
    string? RejectionReason,
    string? RevokedReason,
    DateTime RequestedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    DateTime? RevokedAt,
    DateTime? LastDataAccessAt,
    string? ReviewedByName);

// Data the fancier's results, returned to PLM via token-authenticated API
public record IntegrationRaceResultDto(
    string RingNumber,
    string? PigeonName,
    string? PigeonSex,
    int? PigeonYearOfBirth,
    string RaceName,
    string ClubName,
    string ReleaseLocation,
    DateTime RaceDate,
    double DistanceKm,
    double VelocityMperMin,
    double VelocityKmH,
    int? ClubRank,
    int? CategoryRank,
    string? CategoryName,
    // Programme context
    string? ProgrammeName,
    int? ProgrammeYear,
    bool IsAcePigeon,
    bool IsSuperAcePigeon,
    bool IsBestLoft,
    int? AceRank,
    int? SuperAceRank,
    int? LoftRank);

public record IntegrationAcePigeonDto(
    string RingNumber,
    string? PigeonName,
    string? PigeonSex,
    int? PigeonYearOfBirth,
    string ProgrammeName,
    int ProgrammeYear,
    int AceRank,
    double TotalScore,
    double AverageScore,
    int RacesEntered,
    int RacesInProgramme,
    double ParticipationRate,
    double BestVelocityMperMin,
    double AverageVelocityMperMin,
    int BestClubRank);

public record IntegrationSuperAceDto(
    string RingNumber,
    string? PigeonName,
    string? PigeonSex,
    int? PigeonYearOfBirth,
    string ProgrammeName,
    int ProgrammeYear,
    int SuperAceRank,
    double TotalScore,
    double AverageScore,
    int RacesEntered,
    int RacesInProgramme,
    double ParticipationRate,
    double BestVelocityMperMin,
    double AverageVelocityMperMin,
    int BestClubRank);

public record IntegrationBestLoftDto(
    string ProgrammeName,
    int ProgrammeYear,
    int LoftRank,
    double TotalScore,
    double AverageScore,
    int RacesEntered,
    int PigeonsEntered,
    double BestSingleVelocityMperMin,
    double AverageVelocityMperMin);

public record IntegrationSummaryDto(
    string FancierName,
    string ClubName,
    int TotalRaceResults,
    int TotalAcePigeonResults,
    int TotalSuperAcePigeonResults,
    int TotalBestLoftResults,
    int BestEverClubRank,
    double BestEverVelocityMperMin,
    DateTime? LastRaceDate,
    List<IntegrationAchievementDto> Achievements);

public record IntegrationAchievementDto(
    string Category,       // "AcePigeon" | "SuperAcePigeon" | "BestLoft"
    string ProgrammeName,
    int Year,
    int Rank,
    double Score,
    string Description);

// Club manager's view — pending requests awaiting review
public record PendingLinkRequestDto(
    Guid LinkId,
    string ExternalPlatformName,
    string ExternalLoftName,
    string ExternalUserId,
    string? FancierName,         // if the user is already registered on PRC
    DateTime RequestedAt,
    string LinkToken);

// ═════════════════════════════════════════════════════════════════════════════
//  Commands & Queries
// ═════════════════════════════════════════════════════════════════════════════

// Initiated by PLM (unauthenticated — PLM calls on behalf of their user)
public record RequestExternalLinkCommand(
    string ExternalPlatformName,
    string ExternalUserId,
    string ExternalLoftId,
    string ExternalLoftName,
    string CallbackUrl,
    Guid ClubId,
    // Optional: PRC user ID if the fancier is already registered on PRC
    Guid? PrcUserId,
    string? RequestMetadataJson) : IRequest<Result<RequestExternalLinkResponse>>;

public record RequestExternalLinkResponse(
    Guid LinkId,
    string LinkToken,
    string Status,
    string Message);

// Club manager approves or rejects the request
public record ReviewLinkRequestCommand(
    Guid LinkId,
    bool Approve,
    string? RejectionReason) : IRequest<Result<ExternalLinkDto>>;

// Either side revokes an active link
public record RevokeLinkCommand(
    Guid LinkId,
    string? Reason,
    bool IsExternalRevoke = false) : IRequest<Result>;

// Query: get all links for a club (club manager view)
public record GetClubLinksQuery(Guid ClubId, ExternalLinkStatus? Status = null)
    : IRequest<Result<List<ExternalLinkDto>>>;

// Query: get links for the current fancier (fancier view)
public record GetMyLinksQuery : IRequest<Result<List<ExternalLinkDto>>>;

// Data queries — authenticated via access token
public record GetLinkedRaceResultsQuery(
    string AccessToken, int Page = 1, int PageSize = 50)
    : IRequest<Result<PagedResult<IntegrationRaceResultDto>>>;

public record GetLinkedAcePigeonQuery(string AccessToken)
    : IRequest<Result<List<IntegrationAcePigeonDto>>>;

public record GetLinkedSuperAceQuery(string AccessToken)
    : IRequest<Result<List<IntegrationSuperAceDto>>>;

public record GetLinkedBestLoftQuery(string AccessToken)
    : IRequest<Result<List<IntegrationBestLoftDto>>>;

public record GetLinkedSummaryQuery(string AccessToken)
    : IRequest<Result<IntegrationSummaryDto>>;

// ═════════════════════════════════════════════════════════════════════════════
//  RequestExternalLink Handler
// ═════════════════════════════════════════════════════════════════════════════

public class RequestExternalLinkHandler
    : IRequestHandler<RequestExternalLinkCommand, Result<RequestExternalLinkResponse>>
{
    private readonly IAppDbContext _db;
    private readonly INotificationService _notifications;

    public RequestExternalLinkHandler(IAppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<Result<RequestExternalLinkResponse>> Handle(
        RequestExternalLinkCommand cmd, CancellationToken ct)
    {
        // Validate club exists
        var club = await _db.Clubs.FirstOrDefaultAsync(c => c.Id == cmd.ClubId && !c.IsDeleted, ct);
        if (club == null) return Result.NotFound<RequestExternalLinkResponse>("Club");

        // Prevent duplicate pending requests from the same external loft
        var existing = await _db.ExternalLinks.FirstOrDefaultAsync(l =>
            l.ExternalPlatformName == cmd.ExternalPlatformName &&
            l.ExternalLoftId == cmd.ExternalLoftId &&
            l.ClubId == cmd.ClubId &&
            l.Status == ExternalLinkStatus.Pending, ct);

        if (existing != null)
        {
            return Result.Success(new RequestExternalLinkResponse(
                existing.Id,
                existing.LinkToken,
                "Pending",
                "A link request from this loft to this club is already pending."));
        }

        // Check for already-active link
        var active = await _db.ExternalLinks.FirstOrDefaultAsync(l =>
            l.ExternalPlatformName == cmd.ExternalPlatformName &&
            l.ExternalLoftId == cmd.ExternalLoftId &&
            l.ClubId == cmd.ClubId &&
            l.Status == ExternalLinkStatus.Approved, ct);

        if (active != null)
            return Result.Conflict<RequestExternalLinkResponse>(
                "An active link already exists between this loft and club.");

        // Create the link record
        var link = new ExternalLink
        {
            ClubId               = cmd.ClubId,
            UserId               = cmd.PrcUserId ?? Guid.Empty, // resolved later if matched
            ExternalPlatformName = cmd.ExternalPlatformName,
            ExternalUserId       = cmd.ExternalUserId,
            ExternalLoftId       = cmd.ExternalLoftId,
            ExternalLoftName     = cmd.ExternalLoftName,
            CallbackUrl          = cmd.CallbackUrl,
            LinkToken            = Guid.NewGuid().ToString("N"),
            Status               = ExternalLinkStatus.Pending,
            RequestedAt          = DateTime.UtcNow,
            RequestMetadataJson  = cmd.RequestMetadataJson
        };

        // Try to match the external user to a PRC fancier by external ID
        if (cmd.PrcUserId.HasValue && cmd.PrcUserId != Guid.Empty)
        {
            link.UserId = cmd.PrcUserId.Value;
        }
        else
        {
            // Try to find by ExternalLoftSystemId on the user
            var matchedUser = await _db.Set<ApplicationUser>()
                .FirstOrDefaultAsync(u => u.ExternalLoftSystemId == cmd.ExternalUserId, ct);
            link.UserId = matchedUser?.Id ?? Guid.Empty;
        }

        _db.ExternalLinks.Add(link);
        await _db.SaveChangesAsync(ct);

        // Notify all club managers of the pending request
        await _notifications.SendToRoleAsync(
            "ClubManager",
            cmd.ClubId,
            $"New account link request — {cmd.ExternalPlatformName}",
            $"Loft '{cmd.ExternalLoftName}' from {cmd.ExternalPlatformName} wants to link to your club. " +
            $"Review the request in the Integrations panel.",
            ct);

        return Result.Success(new RequestExternalLinkResponse(
            link.Id,
            link.LinkToken,
            "Pending",
            "Link request created. Awaiting club manager approval."));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
//  ReviewLinkRequest Handler (Approve / Reject)
// ═════════════════════════════════════════════════════════════════════════════

public class ReviewLinkRequestHandler
    : IRequestHandler<ReviewLinkRequestCommand, Result<ExternalLinkDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IExternalPlatformCallbackService _callbackService;
    private readonly INotificationService _notifications;

    public ReviewLinkRequestHandler(
        IAppDbContext db,
        ICurrentUserService currentUser,
        IExternalPlatformCallbackService callbackService,
        INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _callbackService = callbackService;
        _notifications = notifications;
    }

    public async Task<Result<ExternalLinkDto>> Handle(
        ReviewLinkRequestCommand cmd, CancellationToken ct)
    {
        var link = await _db.ExternalLinks
            .Include(l => l.User)
            .Include(l => l.Club)
            .FirstOrDefaultAsync(l => l.Id == cmd.LinkId, ct);

        if (link == null) return Result.NotFound<ExternalLinkDto>("Link request");

        if (link.Status != ExternalLinkStatus.Pending)
            return Result.Failure<ExternalLinkDto>(
                $"Link request is already {link.Status}. Only pending requests can be reviewed.",
                "INVALID_STATE");

        // Verify the club manager belongs to this club
        var reviewerId = _currentUser.UserId;
        var isManager = await _db.ClubMemberships
            .AnyAsync(m => m.UserId == reviewerId && m.ClubId == link.ClubId && m.IsActive, ct);

        if (!isManager && link.ClubId != _currentUser.ClubId)
            return Result.Failure<ExternalLinkDto>("You are not a manager of this club.", "FORBIDDEN");

        string? accessToken = null;

        if (cmd.Approve)
        {
            accessToken = Guid.NewGuid().ToString("N");
            link.Status          = ExternalLinkStatus.Approved;
            link.AccessToken     = accessToken;
            link.ApprovedAt      = DateTime.UtcNow;
            link.ReviewedByUserId = reviewerId;
        }
        else
        {
            link.Status           = ExternalLinkStatus.Rejected;
            link.RejectionReason  = cmd.RejectionReason;
            link.RejectedAt       = DateTime.UtcNow;
            link.ReviewedByUserId = reviewerId;
        }

        link.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Fire webhook callback to PLM (non-blocking — failures are logged, not fatal)
        _ = _callbackService.NotifyLinkReviewedAsync(
            link.CallbackUrl,
            link.LinkToken,
            cmd.Approve ? "Approved" : "Rejected",
            accessToken,
            cmd.RejectionReason,
            CancellationToken.None);

        // Notify the fancier if they have a PRC account
        if (link.UserId != Guid.Empty)
        {
            var message = cmd.Approve
                ? $"Your link request to {link.Club.Name} was approved. You can now view your results on {link.ExternalPlatformName}."
                : $"Your link request to {link.Club.Name} was rejected. Reason: {cmd.RejectionReason}";

            await _notifications.SendInAppAsync(
                link.UserId,
                cmd.Approve ? "Link request approved ✓" : "Link request rejected",
                message,
                ct: ct);
        }

        return Result.Success(link.ToDto());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
//  RevokeLink Handler
// ═════════════════════════════════════════════════════════════════════════════

public class RevokeLinkHandler : IRequestHandler<RevokeLinkCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RevokeLinkHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RevokeLinkCommand cmd, CancellationToken ct)
    {
        var link = await _db.ExternalLinks.FirstOrDefaultAsync(l => l.Id == cmd.LinkId, ct);
        if (link == null) return Result.NotFound("Link");

        if (link.Status != ExternalLinkStatus.Approved)
            return Result.Failure("Only approved links can be revoked.", "INVALID_STATE");

        link.Status        = ExternalLinkStatus.Revoked;
        link.RevokedReason = cmd.Reason ?? "Revoked by user";
        link.RevokedAt     = DateTime.UtcNow;
        link.AccessToken   = null; // invalidate the token immediately
        link.UpdatedAt     = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ═════════════════════════════════════════════════════════════════════════════
//  GetClubLinks Handler — club manager view
// ═════════════════════════════════════════════════════════════════════════════

public class GetClubLinksHandler : IRequestHandler<GetClubLinksQuery, Result<List<ExternalLinkDto>>>
{
    private readonly IAppDbContext _db;

    public GetClubLinksHandler(IAppDbContext db) => _db = db;

    public async Task<Result<List<ExternalLinkDto>>> Handle(GetClubLinksQuery query, CancellationToken ct)
    {
        var q = _db.ExternalLinks
            .Include(l => l.User)
            .Include(l => l.Club)
            .Include(l => l.ReviewedBy)
            .Where(l => l.ClubId == query.ClubId && !l.IsDeleted);

        if (query.Status.HasValue)
            q = q.Where(l => l.Status == query.Status);

        var items = await q.OrderByDescending(l => l.RequestedAt).ToListAsync(ct);
        return Result.Success(items.Select(l => l.ToDto()).ToList());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
//  GetMyLinks Handler — fancier view
// ═════════════════════════════════════════════════════════════════════════════

public class GetMyLinksHandler : IRequestHandler<GetMyLinksQuery, Result<List<ExternalLinkDto>>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyLinksHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<List<ExternalLinkDto>>> Handle(GetMyLinksQuery query, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue) return Result.Failure<List<ExternalLinkDto>>("Not authenticated.", "UNAUTHORIZED");

        var items = await _db.ExternalLinks
            .Include(l => l.User)
            .Include(l => l.Club)
            .Include(l => l.ReviewedBy)
            .Where(l => l.UserId == userId && !l.IsDeleted)
            .OrderByDescending(l => l.RequestedAt)
            .ToListAsync(ct);

        return Result.Success(items.Select(l => l.ToDto()).ToList());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
//  Token validation helper — shared by all data handlers
// ═════════════════════════════════════════════════════════════════════════════

public static class IntegrationTokenHelper
{
    public static async Task<(ExternalLink? Link, string? Error)> ValidateTokenAsync(
        IAppDbContext db, string accessToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return (null, "Access token is required.");

        var link = await db.ExternalLinks
            .Include(l => l.User)
            .Include(l => l.Club)
            .FirstOrDefaultAsync(l =>
                l.AccessToken == accessToken &&
                l.Status == ExternalLinkStatus.Approved &&
                !l.IsDeleted, ct);

        if (link == null) return (null, "Invalid or expired access token.");

        if (link.AccessTokenExpiresAt.HasValue && link.AccessTokenExpiresAt < DateTime.UtcNow)
            return (null, "Access token has expired.");

        // Update last access time
        link.LastDataAccessAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return (link, null);
    }
}

// ═════════════════════════════════════════════════════════════════════════════
//  Data Handlers — Race Results, Ace Pigeon, Super Ace, Best Loft, Summary
// ═════════════════════════════════════════════════════════════════════════════

public class GetLinkedRaceResultsHandler
    : IRequestHandler<GetLinkedRaceResultsQuery, Result<PagedResult<IntegrationRaceResultDto>>>
{
    private readonly IAppDbContext _db;

    public GetLinkedRaceResultsHandler(IAppDbContext db) => _db = db;

    public async Task<Result<PagedResult<IntegrationRaceResultDto>>> Handle(
        GetLinkedRaceResultsQuery query, CancellationToken ct)
    {
        var (link, error) = await IntegrationTokenHelper.ValidateTokenAsync(_db, query.AccessToken, ct);
        if (link == null) return Result.Failure<PagedResult<IntegrationRaceResultDto>>(error!, "UNAUTHORIZED");

        // Get all race results for this fancier in this club
        var fancierUserId = link.UserId;
        var clubId = link.ClubId;

        // Load the fancier's published race results for this club's races
        var q = _db.RaceResults
            .Include(r => r.Race).ThenInclude(r => r.Club)
            .Include(r => r.Category)
            .Where(r =>
                r.Race.ClubId == clubId &&
                r.UserId == fancierUserId &&
                r.Status == ResultStatus.Published &&
                !r.IsDeleted)
            .OrderByDescending(r => r.Race.ActualReleaseTime);

        var total = await q.CountAsync(ct);
        var results = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        // For each result, check if the ring number appears in any Ace/SuperAce/BestLoft records
        var ringNumbers = results.Select(r => r.RingNumber).Distinct().ToList();
        var raceIds = results.Select(r => r.RaceId).Distinct().ToList();

        // Load programme results for this fancier
        var clubProgrammeIds = await _db.ClubProgrammes
            .Where(p => p.ClubId == clubId && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var aceResults = await _db.AcePigeonResults
            .Where(a => clubProgrammeIds.Contains(a.ProgrammeId) && ringNumbers.Contains(a.RingNumber))
            .ToListAsync(ct);

        var superAceResults = await _db.SuperAcePigeonResults
            .Where(a => clubProgrammeIds.Contains(a.ProgrammeId) && ringNumbers.Contains(a.RingNumber))
            .ToListAsync(ct);

        var bestLoftResults = await _db.BestLoftResults
            .Include(b => b.Programme)
            .Where(b => clubProgrammeIds.Contains(b.ProgrammeId) && b.UserId == fancierUserId)
            .ToListAsync(ct);

        // Build enriched DTOs
        var dtos = results.Select(r =>
        {
            var ace      = aceResults.FirstOrDefault(a => a.RingNumber == r.RingNumber);
            var superAce = superAceResults.FirstOrDefault(a => a.RingNumber == r.RingNumber);
            var bestLoft = bestLoftResults.FirstOrDefault();

            return new IntegrationRaceResultDto(
                RingNumber          : r.RingNumber,
                PigeonName          : r.PigeonName,
                PigeonSex           : r.PigeonSex,
                PigeonYearOfBirth   : r.PigeonYearOfBirth,
                RaceName            : r.Race.Name,
                ClubName            : r.Race.Club.Name,
                ReleaseLocation     : r.Race.ReleaseLocation,
                RaceDate            : r.Race.ActualReleaseTime ?? r.Race.ScheduledReleaseTime ?? r.CreatedAt,
                DistanceKm          : r.DistanceKm,
                VelocityMperMin     : Math.Round(r.VelocityMperMin, 4),
                VelocityKmH         : Math.Round(r.VelocityKmH, 3),
                ClubRank            : r.ClubRank,
                CategoryRank        : r.CategoryRank,
                CategoryName        : r.CategoryName,
                ProgrammeName       : ace?.Programme?.Name ?? superAce?.Programme?.Name ?? bestLoft?.Programme?.Name,
                ProgrammeYear       : ace?.Programme?.Year ?? superAce?.Programme?.Year ?? bestLoft?.Programme?.Year,
                IsAcePigeon         : ace != null,
                IsSuperAcePigeon    : superAce != null,
                IsBestLoft          : bestLoft != null,
                AceRank             : ace?.AceRank,
                SuperAceRank        : superAce?.SuperAceRank,
                LoftRank            : bestLoft?.LoftRank
            );
        }).ToList();

        return Result.Success(new PagedResult<IntegrationRaceResultDto>
        {
            Items = dtos, TotalCount = total, Page = query.Page, PageSize = query.PageSize
        });
    }
}

public class GetLinkedAcePigeonHandler
    : IRequestHandler<GetLinkedAcePigeonQuery, Result<List<IntegrationAcePigeonDto>>>
{
    private readonly IAppDbContext _db;

    public GetLinkedAcePigeonHandler(IAppDbContext db) => _db = db;

    public async Task<Result<List<IntegrationAcePigeonDto>>> Handle(
        GetLinkedAcePigeonQuery query, CancellationToken ct)
    {
        var (link, error) = await IntegrationTokenHelper.ValidateTokenAsync(_db, query.AccessToken, ct);
        if (link == null) return Result.Failure<List<IntegrationAcePigeonDto>>(error!, "UNAUTHORIZED");

        var programmeIds = await _db.ClubProgrammes
            .Where(p => p.ClubId == link.ClubId && p.Status == ProgrammeStatus.Published && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var results = await _db.AcePigeonResults
            .Include(a => a.Programme)
            .Where(a => programmeIds.Contains(a.ProgrammeId) && a.UserId == link.UserId)
            .OrderBy(a => a.Programme.Year).ThenBy(a => a.AceRank)
            .ToListAsync(ct);

        var dtos = results.Select(a => new IntegrationAcePigeonDto(
            a.RingNumber, a.PigeonName, a.PigeonSex, a.PigeonYearOfBirth,
            a.Programme.Name, a.Programme.Year,
            a.AceRank, Math.Round(a.TotalScore, 2), Math.Round(a.AverageScore, 2),
            a.RacesEntered, a.RacesInProgramme,
            Math.Round(a.ParticipationRate, 1),
            Math.Round(a.BestVelocityMperMin, 4),
            Math.Round(a.AverageVelocityMperMin, 4),
            a.BestClubRank
        )).ToList();

        return Result.Success(dtos);
    }
}

public class GetLinkedSuperAceHandler
    : IRequestHandler<GetLinkedSuperAceQuery, Result<List<IntegrationSuperAceDto>>>
{
    private readonly IAppDbContext _db;

    public GetLinkedSuperAceHandler(IAppDbContext db) => _db = db;

    public async Task<Result<List<IntegrationSuperAceDto>>> Handle(
        GetLinkedSuperAceQuery query, CancellationToken ct)
    {
        var (link, error) = await IntegrationTokenHelper.ValidateTokenAsync(_db, query.AccessToken, ct);
        if (link == null) return Result.Failure<List<IntegrationSuperAceDto>>(error!, "UNAUTHORIZED");

        var programmeIds = await _db.ClubProgrammes
            .Where(p => p.ClubId == link.ClubId && p.Status == ProgrammeStatus.Published && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var results = await _db.SuperAcePigeonResults
            .Include(a => a.Programme)
            .Where(a => programmeIds.Contains(a.ProgrammeId) && a.UserId == link.UserId)
            .OrderBy(a => a.Programme.Year).ThenBy(a => a.SuperAceRank)
            .ToListAsync(ct);

        var dtos = results.Select(a => new IntegrationSuperAceDto(
            a.RingNumber, a.PigeonName, a.PigeonSex, a.PigeonYearOfBirth,
            a.Programme.Name, a.Programme.Year,
            a.SuperAceRank, Math.Round(a.TotalScore, 2), Math.Round(a.AverageScore, 2),
            a.RacesEntered, a.RacesInProgramme,
            Math.Round(a.ParticipationRate, 1),
            Math.Round(a.BestVelocityMperMin, 4),
            Math.Round(a.AverageVelocityMperMin, 4),
            a.BestClubRank
        )).ToList();

        return Result.Success(dtos);
    }
}

public class GetLinkedBestLoftHandler
    : IRequestHandler<GetLinkedBestLoftQuery, Result<List<IntegrationBestLoftDto>>>
{
    private readonly IAppDbContext _db;

    public GetLinkedBestLoftHandler(IAppDbContext db) => _db = db;

    public async Task<Result<List<IntegrationBestLoftDto>>> Handle(
        GetLinkedBestLoftQuery query, CancellationToken ct)
    {
        var (link, error) = await IntegrationTokenHelper.ValidateTokenAsync(_db, query.AccessToken, ct);
        if (link == null) return Result.Failure<List<IntegrationBestLoftDto>>(error!, "UNAUTHORIZED");

        var programmeIds = await _db.ClubProgrammes
            .Where(p => p.ClubId == link.ClubId && p.Status == ProgrammeStatus.Published && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var results = await _db.BestLoftResults
            .Include(b => b.Programme)
            .Where(b => programmeIds.Contains(b.ProgrammeId) && b.UserId == link.UserId)
            .OrderBy(b => b.Programme.Year).ThenBy(b => b.LoftRank)
            .ToListAsync(ct);

        var dtos = results.Select(b => new IntegrationBestLoftDto(
            b.Programme.Name, b.Programme.Year,
            b.LoftRank, Math.Round(b.TotalScore, 2), Math.Round(b.AverageScore, 2),
            b.RacesEntered, b.PigeonsEntered,
            Math.Round(b.BestSingleVelocityMperMin, 4),
            Math.Round(b.AverageVelocityMperMin, 4)
        )).ToList();

        return Result.Success(dtos);
    }
}

public class GetLinkedSummaryHandler
    : IRequestHandler<GetLinkedSummaryQuery, Result<IntegrationSummaryDto>>
{
    private readonly IAppDbContext _db;

    public GetLinkedSummaryHandler(IAppDbContext db) => _db = db;

    public async Task<Result<IntegrationSummaryDto>> Handle(
        GetLinkedSummaryQuery query, CancellationToken ct)
    {
        var (link, error) = await IntegrationTokenHelper.ValidateTokenAsync(_db, query.AccessToken, ct);
        if (link == null) return Result.Failure<IntegrationSummaryDto>(error!, "UNAUTHORIZED");

        var userId = link.UserId;
        var clubId = link.ClubId;

        var programmeIds = await _db.ClubProgrammes
            .Where(p => p.ClubId == clubId && p.Status == ProgrammeStatus.Published && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(ct);

        // Counts
        var raceCount = await _db.RaceResults
            .CountAsync(r => r.Race.ClubId == clubId && r.UserId == userId &&
                             r.Status == ResultStatus.Published && !r.IsDeleted, ct);

        var aceCount = await _db.AcePigeonResults
            .CountAsync(a => programmeIds.Contains(a.ProgrammeId) && a.UserId == userId, ct);

        var superAceCount = await _db.SuperAcePigeonResults
            .CountAsync(a => programmeIds.Contains(a.ProgrammeId) && a.UserId == userId, ct);

        var bestLoftCount = await _db.BestLoftResults
            .CountAsync(b => programmeIds.Contains(b.ProgrammeId) && b.UserId == userId, ct);

        // Best stats
        var bestRankResult = await _db.RaceResults
            .Where(r => r.Race.ClubId == clubId && r.UserId == userId &&
                        r.Status == ResultStatus.Published && !r.IsDeleted &&
                        r.ClubRank.HasValue)
            .OrderBy(r => r.ClubRank)
            .FirstOrDefaultAsync(ct);

        var bestVelocityResult = await _db.RaceResults
            .Where(r => r.Race.ClubId == clubId && r.UserId == userId &&
                        r.Status == ResultStatus.Published && !r.IsDeleted)
            .OrderByDescending(r => r.VelocityMperMin)
            .FirstOrDefaultAsync(ct);

        var lastRace = await _db.RaceResults
            .Include(r => r.Race)
            .Where(r => r.Race.ClubId == clubId && r.UserId == userId &&
                        r.Status == ResultStatus.Published && !r.IsDeleted)
            .OrderByDescending(r => r.Race.ActualReleaseTime)
            .Select(r => r.Race.ActualReleaseTime)
            .FirstOrDefaultAsync(ct);

        // Build achievements list
        var achievements = new List<IntegrationAchievementDto>();

        var aces = await _db.AcePigeonResults
            .Include(a => a.Programme)
            .Where(a => programmeIds.Contains(a.ProgrammeId) && a.UserId == userId)
            .ToListAsync(ct);

        achievements.AddRange(aces.Select(a => new IntegrationAchievementDto(
            "AcePigeon",
            a.Programme.Name,
            a.Programme.Year,
            a.AceRank,
            Math.Round(a.TotalScore, 2),
            $"Ace Pigeon #{a.AceRank} in {a.Programme.Name} ({a.Programme.Year})")));

        var superAces = await _db.SuperAcePigeonResults
            .Include(a => a.Programme)
            .Where(a => programmeIds.Contains(a.ProgrammeId) && a.UserId == userId)
            .ToListAsync(ct);

        achievements.AddRange(superAces.Select(a => new IntegrationAchievementDto(
            "SuperAcePigeon",
            a.Programme.Name,
            a.Programme.Year,
            a.SuperAceRank,
            Math.Round(a.TotalScore, 2),
            $"Super Ace Pigeon #{a.SuperAceRank} in {a.Programme.Name} ({a.Programme.Year})")));

        var bestLofts = await _db.BestLoftResults
            .Include(b => b.Programme)
            .Where(b => programmeIds.Contains(b.ProgrammeId) && b.UserId == userId)
            .ToListAsync(ct);

        achievements.AddRange(bestLofts.Select(b => new IntegrationAchievementDto(
            "BestLoft",
            b.Programme.Name,
            b.Programme.Year,
            b.LoftRank,
            Math.Round(b.TotalScore, 2),
            $"Best Loft #{b.LoftRank} in {b.Programme.Name} ({b.Programme.Year})")));

        return Result.Success(new IntegrationSummaryDto(
            FancierName              : link.User.FullName,
            ClubName                 : link.Club.Name,
            TotalRaceResults         : raceCount,
            TotalAcePigeonResults    : aceCount,
            TotalSuperAcePigeonResults: superAceCount,
            TotalBestLoftResults     : bestLoftCount,
            BestEverClubRank         : bestRankResult?.ClubRank ?? 0,
            BestEverVelocityMperMin  : bestVelocityResult != null ? Math.Round(bestVelocityResult.VelocityMperMin, 4) : 0,
            LastRaceDate             : lastRace,
            Achievements             : achievements.OrderByDescending(a => a.Year).ThenBy(a => a.Rank).ToList()
        ));
    }
}

// ═════════════════════════════════════════════════════════════════════════════
//  Mapping
// ═════════════════════════════════════════════════════════════════════════════

public static class IntegrationMappingExtensions
{
    public static ExternalLinkDto ToDto(this ExternalLink l) => new(
        l.Id,
        l.UserId,
        l.User?.FullName ?? "Unknown",
        l.ClubId,
        l.Club?.Name ?? "",
        l.ExternalPlatformName,
        l.ExternalUserId,
        l.ExternalLoftId,
        l.ExternalLoftName,
        l.LinkToken,
        l.Status,
        l.Status.ToString(),
        l.RejectionReason,
        l.RevokedReason,
        l.RequestedAt,
        l.ApprovedAt,
        l.RejectedAt,
        l.RevokedAt,
        l.LastDataAccessAt,
        l.ReviewedBy?.FullName);
}

// ═════════════════════════════════════════════════════════════════════════════
//  IExternalPlatformCallbackService interface
// ═════════════════════════════════════════════════════════════════════════════

public interface IExternalPlatformCallbackService
{
    Task NotifyLinkReviewedAsync(
        string callbackUrl,
        string linkToken,
        string status,
        string? accessToken,
        string? rejectionReason,
        CancellationToken ct);
}
