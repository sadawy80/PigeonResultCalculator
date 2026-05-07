using MediatR;
using Microsoft.EntityFrameworkCore;
using PigeonRacing.Application.Common;
using PigeonRacing.Application.Common.Interfaces;
using PigeonRacing.Domain.Entities;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.Application.Features.Programmes;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record ProgrammeDto(
    Guid Id,
    Guid ClubId,
    string ClubName,
    string Name,
    string? Description,
    int Year,
    DateTime? StartDate,
    DateTime? EndDate,
    ProgrammeStatus Status,
    ScoringMethod ScoringMethod,
    int PointsForFirst,
    int MaxPointPositions,
    int BestLoftPigeonsPerRace,
    int BestLoftMinRaces,
    int AcePigeonMinRaces,
    SuperAceQualification SuperAceQualification,
    int SuperAceMinRaceCount,
    double SuperAceMinRacePercentage,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    List<ProgrammeRaceDto> Races);

public record ProgrammeRaceDto(
    Guid ProgrammeRaceId,
    Guid RaceId,
    string RaceName,
    DateTime? ActualReleaseTime,
    double ScoreWeight,
    int SortOrder,
    int TotalEntries);

public record ProgrammeSummaryDto(
    Guid Id,
    string Name,
    int Year,
    ProgrammeStatus Status,
    ScoringMethod ScoringMethod,
    int RaceCount,
    DateTime? StartDate,
    DateTime? EndDate);

// ── Commands ──────────────────────────────────────────────────────────────────

public record CreateProgrammeCommand(
    Guid ClubId,
    string Name,
    string? Description,
    int Year,
    DateTime? StartDate,
    DateTime? EndDate,
    ScoringMethod ScoringMethod,
    int PointsForFirst,
    int MaxPointPositions,
    int BestLoftPigeonsPerRace,
    int BestLoftMinRaces,
    int AcePigeonMinRaces,
    SuperAceQualification SuperAceQualification,
    int SuperAceMinRaceCount,
    double SuperAceMinRacePercentage) : IRequest<Result<ProgrammeDto>>;

public record UpdateProgrammeCommand(
    Guid ProgrammeId,
    string Name,
    string? Description,
    DateTime? StartDate,
    DateTime? EndDate,
    ScoringMethod ScoringMethod,
    int PointsForFirst,
    int MaxPointPositions,
    int BestLoftPigeonsPerRace,
    int BestLoftMinRaces,
    int AcePigeonMinRaces,
    SuperAceQualification SuperAceQualification,
    int SuperAceMinRaceCount,
    double SuperAceMinRacePercentage) : IRequest<Result<ProgrammeDto>>;

public record AddRaceToProgrammeCommand(
    Guid ProgrammeId,
    Guid RaceId,
    double ScoreWeight,
    int SortOrder) : IRequest<Result<ProgrammeDto>>;

public record RemoveRaceFromProgrammeCommand(
    Guid ProgrammeId,
    Guid RaceId) : IRequest<Result>;

public record PublishProgrammeCommand(Guid ProgrammeId) : IRequest<Result<ProgrammeDto>>;
public record DeleteProgrammeCommand(Guid ProgrammeId) : IRequest<Result>;

// ── Queries ───────────────────────────────────────────────────────────────────

public record GetProgrammeQuery(Guid ProgrammeId) : IRequest<Result<ProgrammeDto>>;
public record GetClubProgrammesQuery(Guid ClubId, PagedQuery Paged) : IRequest<Result<PagedResult<ProgrammeSummaryDto>>>;

// ── Create Programme Handler ──────────────────────────────────────────────────

public class CreateProgrammeHandler : IRequestHandler<CreateProgrammeCommand, Result<ProgrammeDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateProgrammeHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<ProgrammeDto>> Handle(CreateProgrammeCommand cmd, CancellationToken ct)
    {
        var club = await _db.Clubs.FirstOrDefaultAsync(c => c.Id == cmd.ClubId && !c.IsDeleted, ct);
        if (club == null) return Result.NotFound<ProgrammeDto>("Club");

        var prog = new ClubProgramme
        {
            ClubId = cmd.ClubId,
            Name = cmd.Name,
            Description = cmd.Description,
            Year = cmd.Year,
            StartDate = cmd.StartDate,
            EndDate = cmd.EndDate,
            ScoringMethod = cmd.ScoringMethod,
            PointsForFirst = cmd.PointsForFirst,
            MaxPointPositions = cmd.MaxPointPositions,
            BestLoftPigeonsPerRace = cmd.BestLoftPigeonsPerRace,
            BestLoftMinRaces = cmd.BestLoftMinRaces,
            AcePigeonMinRaces = cmd.AcePigeonMinRaces,
            SuperAceQualification = cmd.SuperAceQualification,
            SuperAceMinRaceCount = cmd.SuperAceMinRaceCount,
            SuperAceMinRacePercentage = cmd.SuperAceMinRacePercentage,
            Status = ProgrammeStatus.Draft,
            CreatedBy = _currentUser.UserId
        };

        _db.ClubProgrammes.Add(prog);
        await _db.SaveChangesAsync(ct);
        return Result.Success(await LoadDtoAsync(prog.Id, ct));
    }

    private async Task<ProgrammeDto> LoadDtoAsync(Guid id, CancellationToken ct)
    {
        var p = await _db.ClubProgrammes
            .Include(x => x.Club)
            .Include(x => x.ProgrammeRaces).ThenInclude(r => r.Race)
            .FirstAsync(x => x.Id == id, ct);
        return p.ToDto();
    }
}

// ── Update Programme Handler ──────────────────────────────────────────────────

public class UpdateProgrammeHandler : IRequestHandler<UpdateProgrammeCommand, Result<ProgrammeDto>>
{
    private readonly IAppDbContext _db;

    public UpdateProgrammeHandler(IAppDbContext db) => _db = db;

    public async Task<Result<ProgrammeDto>> Handle(UpdateProgrammeCommand cmd, CancellationToken ct)
    {
        var prog = await _db.ClubProgrammes
            .Include(x => x.Club)
            .Include(x => x.ProgrammeRaces).ThenInclude(r => r.Race)
            .FirstOrDefaultAsync(x => x.Id == cmd.ProgrammeId && !x.IsDeleted, ct);

        if (prog == null) return Result.NotFound<ProgrammeDto>("Programme");
        if (prog.Status == ProgrammeStatus.Published)
            return Result.Failure<ProgrammeDto>("Cannot edit a published programme.", "PROGRAMME_PUBLISHED");

        prog.Name = cmd.Name;
        prog.Description = cmd.Description;
        prog.StartDate = cmd.StartDate;
        prog.EndDate = cmd.EndDate;
        prog.ScoringMethod = cmd.ScoringMethod;
        prog.PointsForFirst = cmd.PointsForFirst;
        prog.MaxPointPositions = cmd.MaxPointPositions;
        prog.BestLoftPigeonsPerRace = cmd.BestLoftPigeonsPerRace;
        prog.BestLoftMinRaces = cmd.BestLoftMinRaces;
        prog.AcePigeonMinRaces = cmd.AcePigeonMinRaces;
        prog.SuperAceQualification = cmd.SuperAceQualification;
        prog.SuperAceMinRaceCount = cmd.SuperAceMinRaceCount;
        prog.SuperAceMinRacePercentage = cmd.SuperAceMinRacePercentage;
        prog.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result.Success(prog.ToDto());
    }
}

// ── Add Race To Programme Handler ─────────────────────────────────────────────

public class AddRaceToProgrammeHandler : IRequestHandler<AddRaceToProgrammeCommand, Result<ProgrammeDto>>
{
    private readonly IAppDbContext _db;

    public AddRaceToProgrammeHandler(IAppDbContext db) => _db = db;

    public async Task<Result<ProgrammeDto>> Handle(AddRaceToProgrammeCommand cmd, CancellationToken ct)
    {
        var prog = await _db.ClubProgrammes
            .Include(x => x.Club)
            .Include(x => x.ProgrammeRaces).ThenInclude(r => r.Race)
            .FirstOrDefaultAsync(x => x.Id == cmd.ProgrammeId && !x.IsDeleted, ct);

        if (prog == null) return Result.NotFound<ProgrammeDto>("Programme");

        var race = await _db.Races.FirstOrDefaultAsync(r => r.Id == cmd.RaceId && !r.IsDeleted, ct);
        if (race == null) return Result.NotFound<ProgrammeDto>("Race");

        // Race must belong to the same club
        if (race.ClubId != prog.ClubId)
            return Result.Failure<ProgrammeDto>("Race does not belong to this club.", "RACE_CLUB_MISMATCH");

        // Prevent duplicates
        if (prog.ProgrammeRaces.Any(r => r.RaceId == cmd.RaceId))
            return Result.Conflict<ProgrammeDto>("Race is already in this programme.");

        prog.ProgrammeRaces.Add(new ProgrammeRace
        {
            ProgrammeId = cmd.ProgrammeId,
            RaceId = cmd.RaceId,
            ScoreWeight = cmd.ScoreWeight,
            SortOrder = cmd.SortOrder
        });

        await _db.SaveChangesAsync(ct);
        return Result.Success(prog.ToDto());
    }
}

// ── Remove Race From Programme Handler ────────────────────────────────────────

public class RemoveRaceFromProgrammeHandler : IRequestHandler<RemoveRaceFromProgrammeCommand, Result>
{
    private readonly IAppDbContext _db;

    public RemoveRaceFromProgrammeHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(RemoveRaceFromProgrammeCommand cmd, CancellationToken ct)
    {
        var pr = await _db.ProgrammeRaces
            .FirstOrDefaultAsync(r => r.ProgrammeId == cmd.ProgrammeId && r.RaceId == cmd.RaceId, ct);

        if (pr == null) return Result.NotFound("ProgrammeRace");

        pr.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── Publish Programme Handler ─────────────────────────────────────────────────

public class PublishProgrammeHandler : IRequestHandler<PublishProgrammeCommand, Result<ProgrammeDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public PublishProgrammeHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<ProgrammeDto>> Handle(PublishProgrammeCommand cmd, CancellationToken ct)
    {
        var prog = await _db.ClubProgrammes
            .Include(x => x.Club)
            .Include(x => x.ProgrammeRaces).ThenInclude(r => r.Race)
            .FirstOrDefaultAsync(x => x.Id == cmd.ProgrammeId && !x.IsDeleted, ct);

        if (prog == null) return Result.NotFound<ProgrammeDto>("Programme");

        var hasResults = await _db.BestLoftResults.AnyAsync(r => r.ProgrammeId == cmd.ProgrammeId, ct);
        if (!hasResults)
            return Result.Failure<ProgrammeDto>(
                "Calculate all results before publishing the programme.", "NO_RESULTS");

        prog.Status = ProgrammeStatus.Published;
        prog.PublishedAt = DateTime.UtcNow;
        prog.PublishedByUserId = _currentUser.UserId;
        prog.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result.Success(prog.ToDto());
    }
}

// ── Delete Programme Handler ──────────────────────────────────────────────────

public class DeleteProgrammeHandler : IRequestHandler<DeleteProgrammeCommand, Result>
{
    private readonly IAppDbContext _db;

    public DeleteProgrammeHandler(IAppDbContext db) => _db = db;

    public async Task<Result> Handle(DeleteProgrammeCommand cmd, CancellationToken ct)
    {
        var prog = await _db.ClubProgrammes.FirstOrDefaultAsync(x => x.Id == cmd.ProgrammeId, ct);
        if (prog == null) return Result.NotFound("Programme");
        if (prog.Status == ProgrammeStatus.Published)
            return Result.Failure("Cannot delete a published programme.", "PROGRAMME_PUBLISHED");

        prog.IsDeleted = true;
        prog.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ── Get Programme Handler ─────────────────────────────────────────────────────

public class GetProgrammeHandler : IRequestHandler<GetProgrammeQuery, Result<ProgrammeDto>>
{
    private readonly IAppDbContext _db;

    public GetProgrammeHandler(IAppDbContext db) => _db = db;

    public async Task<Result<ProgrammeDto>> Handle(GetProgrammeQuery query, CancellationToken ct)
    {
        var prog = await _db.ClubProgrammes
            .Include(x => x.Club)
            .Include(x => x.ProgrammeRaces).ThenInclude(r => r.Race)
            .FirstOrDefaultAsync(x => x.Id == query.ProgrammeId && !x.IsDeleted, ct);

        return prog == null
            ? Result.NotFound<ProgrammeDto>("Programme")
            : Result.Success(prog.ToDto());
    }
}

// ── Get Club Programmes Handler ───────────────────────────────────────────────

public class GetClubProgrammesHandler : IRequestHandler<GetClubProgrammesQuery, Result<PagedResult<ProgrammeSummaryDto>>>
{
    private readonly IAppDbContext _db;

    public GetClubProgrammesHandler(IAppDbContext db) => _db = db;

    public async Task<Result<PagedResult<ProgrammeSummaryDto>>> Handle(GetClubProgrammesQuery query, CancellationToken ct)
    {
        var q = _db.ClubProgrammes
            .Include(x => x.ProgrammeRaces)
            .Where(x => x.ClubId == query.ClubId && !x.IsDeleted);

        if (!string.IsNullOrEmpty(query.Paged.Search))
            q = q.Where(x => x.Name.Contains(query.Paged.Search));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.Year).ThenBy(x => x.Name)
            .Skip(query.Paged.Skip).Take(query.Paged.PageSize)
            .Select(x => new ProgrammeSummaryDto(
                x.Id, x.Name, x.Year, x.Status, x.ScoringMethod,
                x.ProgrammeRaces.Count(r => !r.IsDeleted),
                x.StartDate, x.EndDate))
            .ToListAsync(ct);

        return Result.Success(new PagedResult<ProgrammeSummaryDto>
        {
            Items = items, TotalCount = total, Page = query.Paged.Page, PageSize = query.Paged.PageSize
        });
    }
}

// ── Mapping ───────────────────────────────────────────────────────────────────

public static class ProgrammeMappingExtensions
{
    public static ProgrammeDto ToDto(this ClubProgramme p) => new(
        p.Id, p.ClubId, p.Club?.Name ?? string.Empty,
        p.Name, p.Description, p.Year, p.StartDate, p.EndDate,
        p.Status, p.ScoringMethod, p.PointsForFirst, p.MaxPointPositions,
        p.BestLoftPigeonsPerRace, p.BestLoftMinRaces,
        p.AcePigeonMinRaces, p.SuperAceQualification,
        p.SuperAceMinRaceCount, p.SuperAceMinRacePercentage,
        p.PublishedAt, p.CreatedAt,
        p.ProgrammeRaces
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.SortOrder)
            .Select(r => new ProgrammeRaceDto(
                r.Id, r.RaceId,
                r.Race?.Name ?? string.Empty,
                r.Race?.ActualReleaseTime,
                r.ScoreWeight, r.SortOrder,
                r.Race?.TotalPigeonsEntered ?? 0))
            .ToList());
}
