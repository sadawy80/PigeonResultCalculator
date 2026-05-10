using MassTransit;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.Common.Messages;
using PRC.IntegrationService.Data;
using PRC.IntegrationService.DTOs;
using PRC.IntegrationService.Models;

namespace PRC.IntegrationService.Services;

public class IntegrationService : IIntegrationService
{
    private readonly IntegrationDbContext _db;
    private readonly IPublishEndpoint _bus;
    private readonly IExternalPlatformCallbackService _callback;

    public IntegrationService(
        IntegrationDbContext db,
        IPublishEndpoint bus,
        IExternalPlatformCallbackService callback)
    {
        _db = db;
        _bus = bus;
        _callback = callback;
    }

    public async Task<Result<RequestExternalLinkResponse>> RequestLinkAsync(
        string externalPlatformName, string externalUserId, string externalLoftId,
        string externalLoftName, string callbackUrl, Guid clubId, Guid? prcUserId,
        string? metadataJson, CancellationToken ct)
    {
        var existing = await _db.ExternalLinks.FirstOrDefaultAsync(l =>
            l.ExternalPlatformName == externalPlatformName &&
            l.ExternalLoftId == externalLoftId &&
            l.ClubId == clubId &&
            l.Status == ExternalLinkStatus.Pending, ct);

        if (existing != null)
            return Result.Success(new RequestExternalLinkResponse(
                existing.Id, existing.LinkToken, "Pending",
                "A link request from this loft to this club is already pending."));

        var active = await _db.ExternalLinks.FirstOrDefaultAsync(l =>
            l.ExternalPlatformName == externalPlatformName &&
            l.ExternalLoftId == externalLoftId &&
            l.ClubId == clubId &&
            l.Status == ExternalLinkStatus.Approved, ct);

        if (active != null)
            return Result.Conflict<RequestExternalLinkResponse>(
                "An active link already exists between this loft and club.");

        var link = new ExternalLink
        {
            ClubId               = clubId,
            UserId               = prcUserId ?? Guid.Empty,
            ExternalPlatformName = externalPlatformName,
            ExternalUserId       = externalUserId,
            ExternalLoftId       = externalLoftId,
            ExternalLoftName     = externalLoftName,
            CallbackUrl          = callbackUrl,
            LinkToken            = Guid.NewGuid().ToString("N"),
            Status               = ExternalLinkStatus.Pending,
            RequestedAt          = DateTime.UtcNow,
            RequestMetadataJson  = metadataJson
        };

        _db.ExternalLinks.Add(link);
        await _db.SaveChangesAsync(ct);

        // Notify club managers via bus
        await _bus.Publish(new ExternalLinkRequested(
            link.Id, clubId, externalPlatformName, externalLoftName), ct);

        return Result.Success(new RequestExternalLinkResponse(
            link.Id, link.LinkToken, "Pending",
            "Link request created. Awaiting club manager approval."));
    }

    public async Task<Result<ExternalLinkDto>> ReviewLinkAsync(
        Guid linkId, bool approve, string? rejectionReason, Guid reviewerUserId, CancellationToken ct)
    {
        var link = await _db.ExternalLinks.FirstOrDefaultAsync(l => l.Id == linkId, ct);
        if (link == null) return Result.NotFound<ExternalLinkDto>("Link request");

        if (link.Status != ExternalLinkStatus.Pending)
            return Result.Failure<ExternalLinkDto>(
                $"Link request is already {link.Status}. Only pending requests can be reviewed.",
                "INVALID_STATE");

        string? accessToken = null;
        if (approve)
        {
            accessToken = Guid.NewGuid().ToString("N");
            link.Status           = ExternalLinkStatus.Approved;
            link.AccessToken      = accessToken;
            link.ApprovedAt       = DateTime.UtcNow;
            link.ReviewedByUserId = reviewerUserId;
        }
        else
        {
            link.Status           = ExternalLinkStatus.Rejected;
            link.RejectionReason  = rejectionReason;
            link.RejectedAt       = DateTime.UtcNow;
            link.ReviewedByUserId = reviewerUserId;
        }

        link.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _ = _callback.NotifyLinkReviewedAsync(
            link.CallbackUrl, link.LinkToken,
            approve ? "Approved" : "Rejected",
            accessToken, rejectionReason,
            CancellationToken.None);

        return Result.Success(link.ToDto());
    }

    public async Task<Result> RevokeLinkAsync(Guid linkId, string? reason, CancellationToken ct)
    {
        var link = await _db.ExternalLinks.FirstOrDefaultAsync(l => l.Id == linkId, ct);
        if (link == null) return Result.NotFound("Link");

        if (link.Status != ExternalLinkStatus.Approved)
            return Result.Failure("Only approved links can be revoked.", "INVALID_STATE");

        link.Status        = ExternalLinkStatus.Revoked;
        link.RevokedReason = reason ?? "Revoked by user";
        link.RevokedAt     = DateTime.UtcNow;
        link.AccessToken   = null;
        link.UpdatedAt     = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RevokeByTokenAsync(string linkToken, string? reason, CancellationToken ct)
    {
        var link = await _db.ExternalLinks.FirstOrDefaultAsync(l => l.LinkToken == linkToken, ct);
        if (link == null) return Result.NotFound("Link");
        return await RevokeLinkAsync(link.Id, reason ?? "Revoked by external platform", ct);
    }

    public async Task<Result<List<ExternalLinkDto>>> GetClubLinksAsync(
        Guid clubId, ExternalLinkStatus? status, CancellationToken ct)
    {
        var q = _db.ExternalLinks
            .Where(l => l.ClubId == clubId);

        if (status.HasValue) q = q.Where(l => l.Status == status);

        var items = await q.OrderByDescending(l => l.RequestedAt).ToListAsync(ct);
        return Result.Success(items.Select(l => l.ToDto()).ToList());
    }

    public async Task<Result<List<ExternalLinkDto>>> GetMyLinksAsync(Guid userId, CancellationToken ct)
    {
        var items = await _db.ExternalLinks
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.RequestedAt)
            .ToListAsync(ct);

        return Result.Success(items.Select(l => l.ToDto()).ToList());
    }

    public async Task<Result<List<ExternalLinkDto>>> GetAllLinksAsync(
        ExternalLinkStatus? status, int page, int pageSize, CancellationToken ct)
    {
        var q = _db.ExternalLinks.AsQueryable();
        if (status.HasValue) q = q.Where(l => l.Status == status);

        var items = await q
            .OrderByDescending(l => l.RequestedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return Result.Success(items.Select(l => l.ToDto()).ToList());
    }

    public async Task<Result<(ExternalLink Link, string? Error)>> ValidateTokenAsync(
        string accessToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return Result.Failure<(ExternalLink, string?)>("Access token is required.", "UNAUTHORIZED");

        var link = await _db.ExternalLinks.FirstOrDefaultAsync(l =>
            l.AccessToken == accessToken &&
            l.Status == ExternalLinkStatus.Approved, ct);

        if (link == null)
            return Result.Failure<(ExternalLink, string?)>("Invalid or expired access token.", "UNAUTHORIZED");

        if (link.AccessTokenExpiresAt.HasValue && link.AccessTokenExpiresAt < DateTime.UtcNow)
            return Result.Failure<(ExternalLink, string?)>("Access token has expired.", "UNAUTHORIZED");

        link.LastDataAccessAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result.Success<(ExternalLink, string?)>((link, null));
    }
}

public static class ExternalLinkExtensions
{
    public static ExternalLinkDto ToDto(this ExternalLink l) => new(
        l.Id, l.UserId, l.ClubId,
        l.ExternalPlatformName, l.ExternalUserId, l.ExternalLoftId, l.ExternalLoftName,
        l.LinkToken, l.Status, l.Status.ToString(),
        l.RejectionReason, l.RevokedReason,
        l.RequestedAt, l.ApprovedAt, l.RejectedAt, l.RevokedAt, l.LastDataAccessAt);
}
