using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PigeonRacing.Application.Common;
using PigeonRacing.Application.Features.Auth;
using PigeonRacing.Application.Features.Clubs;
using PigeonRacing.Application.Features.CountryResults;
using PigeonRacing.Application.Features.Races;
using PigeonRacing.Application.Features.Results;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.API.Controllers;

// ── Base Controller ───────────────────────────────────────────────────────────

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected IActionResult FromResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(ApiResponse<T>.Ok(result.Value!));

        return result.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(ApiResponse<T>.Fail(result.Error, result.ErrorCode)),
            "FORBIDDEN" => Forbid(),
            "CONFLICT" => Conflict(ApiResponse<T>.Fail(result.Error, result.ErrorCode)),
            "VALIDATION_ERROR" => BadRequest(ApiResponse<T>.Fail(result.Error, result.ErrorCode)),
            _ => BadRequest(ApiResponse<T>.Fail(result.Error, result.ErrorCode))
        };
    }

    protected IActionResult FromResult(Result result)
    {
        if (result.IsSuccess) return Ok(ApiResponse<object>.Ok(null, "Success"));

        return result.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(ApiResponse<object>.Fail(result.Error, result.ErrorCode)),
            "FORBIDDEN" => Forbid(),
            "CONFLICT" => Conflict(ApiResponse<object>.Fail(result.Error, result.ErrorCode)),
            _ => BadRequest(ApiResponse<object>.Fail(result.Error, result.ErrorCode))
        };
    }
}

// ── Auth Controller ───────────────────────────────────────────────────────────

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterCommand cmd, CancellationToken ct)
        => FromResult(await Mediator.Send(cmd, ct));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand cmd, CancellationToken ct)
        => FromResult(await Mediator.Send(cmd, ct));

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand cmd, CancellationToken ct)
        => FromResult(await Mediator.Send(cmd, ct));

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenCommand cmd, CancellationToken ct)
        => FromResult(await Mediator.Send(cmd, ct));

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
        => FromResult(await Mediator.Send(new GetCurrentUserQuery(), ct));

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand cmd, CancellationToken ct)
        => FromResult(await Mediator.Send(cmd, ct));
}

// ── Races Controller ──────────────────────────────────────────────────────────

[Route("api/races")]
[Authorize]
public class RacesController : ApiControllerBase
{
    [HttpPost]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateRaceCommand cmd, CancellationToken ct)
        => FromResult(await Mediator.Send(cmd, ct));

    [HttpPut("{raceId:guid}")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> Update(Guid raceId, [FromBody] UpdateRaceCommand cmd, CancellationToken ct)
        => FromResult(await Mediator.Send(cmd with { RaceId = raceId }, ct));

    [HttpPost("{raceId:guid}/start")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> Start(Guid raceId, [FromBody] StartRaceCommand cmd, CancellationToken ct)
        => FromResult(await Mediator.Send(cmd with { RaceId = raceId }, ct));

    [HttpPost("{raceId:guid}/complete")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> Complete(Guid raceId, CancellationToken ct)
        => FromResult(await Mediator.Send(new CompleteRaceCommand(raceId), ct));

    [HttpPost("{raceId:guid}/publish")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> Publish(Guid raceId, CancellationToken ct)
        => FromResult(await Mediator.Send(new PublishRaceCommand(raceId), ct));

    [HttpDelete("{raceId:guid}")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> Delete(Guid raceId, CancellationToken ct)
        => FromResult(await Mediator.Send(new DeleteRaceCommand(raceId), ct));

    [HttpGet("{raceId:guid}")]
    public async Task<IActionResult> Get(Guid raceId, CancellationToken ct)
        => FromResult(await Mediator.Send(new GetRaceQuery(raceId), ct));

    [HttpGet("club/{clubId:guid}")]
    public async Task<IActionResult> GetByClub(
        Guid clubId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, CancellationToken ct = default)
        => FromResult(await Mediator.Send(
            new GetClubRacesQuery(clubId, new PagedQuery { Page = page, PageSize = pageSize, Search = search }), ct));

    [HttpGet("club/{clubId:guid}/live")]
    public async Task<IActionResult> GetLive(Guid clubId, CancellationToken ct)
        => FromResult(await Mediator.Send(new GetLiveRacesQuery(clubId), ct));
}

// ── Results Controller ────────────────────────────────────────────────────────

[Route("api/results")]
[Authorize]
public class ResultsController : ApiControllerBase
{
    [HttpPost("manual")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> AddManual([FromBody] AddManualResultCommand cmd, CancellationToken ct)
        => FromResult(await Mediator.Send(cmd, ct));

    [HttpPost("ingest-ets")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB
    public async Task<IActionResult> IngestETS(
        [FromForm] Guid raceId, [FromForm] Guid? categoryId,
        IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file provided."));

        var cmd = new IngestETSFileCommand(raceId, categoryId, file.OpenReadStream(), file.FileName);
        return FromResult(await Mediator.Send(cmd, ct));
    }

    [HttpPost("{raceId:guid}/process")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> Process(Guid raceId, CancellationToken ct)
        => FromResult(await Mediator.Send(new ProcessRaceResultsCommand(raceId), ct));

    [HttpGet("race/{raceId:guid}")]
    public async Task<IActionResult> GetByRace(
        Guid raceId, [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null, CancellationToken ct = default)
        => FromResult(await Mediator.Send(
            new GetRaceResultsQuery(raceId, categoryId,
                new PagedQuery { Page = page, PageSize = pageSize, Search = search }), ct));

    [HttpGet("fancier/{userId:guid}")]
    public async Task<IActionResult> GetByFancier(
        Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => FromResult(await Mediator.Send(
            new GetFancierResultsQuery(userId, new PagedQuery { Page = page, PageSize = pageSize }), ct));

    [HttpPut("{resultId:guid}/link-fancier")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> LinkFancier(Guid resultId, [FromBody] LinkFancierRequest req, CancellationToken ct)
        => FromResult(await Mediator.Send(new LinkResultToFancierCommand(resultId, req.UserId), ct));

    [HttpDelete("{resultId:guid}")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> Delete(Guid resultId, CancellationToken ct)
        => FromResult(await Mediator.Send(new DeleteRaceResultCommand(resultId), ct));

    [HttpGet("race/{raceId:guid}/ingestion-logs")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> GetIngestionLogs(Guid raceId, CancellationToken ct)
        => FromResult(await Mediator.Send(new GetIngestionLogsQuery(raceId), ct));
}

public record LinkFancierRequest(Guid UserId);

// ── Country Results Controller ────────────────────────────────────────────────

[Route("api/country-results")]
[Authorize]
public class CountryResultsController : ApiControllerBase
{
    [HttpPost]
    [Authorize(Roles = "CountryManager,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateCountryResultCommand cmd, CancellationToken ct)
        => FromResult(await Mediator.Send(cmd, ct));

    [HttpPost("{countryResultId:guid}/publish")]
    [Authorize(Roles = "CountryManager,SuperAdmin")]
    public async Task<IActionResult> Publish(Guid countryResultId, CancellationToken ct)
        => FromResult(await Mediator.Send(new PublishCountryResultCommand(countryResultId), ct));

    [HttpGet("{countryResultId:guid}")]
    public async Task<IActionResult> Get(Guid countryResultId, CancellationToken ct)
        => FromResult(await Mediator.Send(new GetCountryResultQuery(countryResultId), ct));

    [HttpGet("country/{countryId:guid}")]
    public async Task<IActionResult> GetByCountry(
        Guid countryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => FromResult(await Mediator.Send(
            new GetCountryResultsQuery(countryId, new PagedQuery { Page = page, PageSize = pageSize }), ct));
}

// ── Clubs Controller ──────────────────────────────────────────────────────────

[Route("api/clubs")]
[Authorize]
public class ClubsController : ApiControllerBase
{
    [HttpPost]
    [Authorize(Roles = "CountryManager,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateClubCommand cmd, CancellationToken ct)
        => FromResult(await Mediator.Send(cmd, ct));

    [HttpPut("{clubId:guid}/branding")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> UpdateBranding(Guid clubId, [FromBody] UpdateClubBrandingCommand cmd, CancellationToken ct)
        => FromResult(await Mediator.Send(cmd with { ClubId = clubId }, ct));

    [HttpPut("{clubId:guid}/theme")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> SetTheme(Guid clubId, [FromBody] SetThemeRequest req, CancellationToken ct)
        => FromResult(await Mediator.Send(new SetThemeCommand(clubId, req.Theme), ct));

    [HttpGet("{clubId:guid}")]
    public async Task<IActionResult> Get(Guid clubId, CancellationToken ct)
        => FromResult(await Mediator.Send(new GetClubQuery(clubId), ct));

    [HttpGet("{clubId:guid}/members")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> GetMembers(
        Guid clubId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, CancellationToken ct = default)
        => FromResult(await Mediator.Send(
            new GetClubMembersQuery(clubId, new PagedQuery { Page = page, PageSize = pageSize, Search = search }), ct));

    [HttpPost("{clubId:guid}/invite")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> Invite(Guid clubId, [FromBody] InviteRequest req, CancellationToken ct)
        => FromResult(await Mediator.Send(new SendInvitationCommand(clubId, req.Email), ct));

    [HttpGet("{clubId:guid}/invitations")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> GetInvitations(Guid clubId, CancellationToken ct)
        => FromResult(await Mediator.Send(new GetClubInvitationsQuery(clubId), ct));

    [HttpDelete("{clubId:guid}/members/{userId:guid}")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> RemoveMember(Guid clubId, Guid userId, CancellationToken ct)
        => FromResult(await Mediator.Send(new RemoveMemberCommand(clubId, userId), ct));

    [HttpPost("memberships/{membershipId:guid}/link-pigeon")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> LinkPigeon(Guid membershipId, [FromBody] LinkPigeonRequest req, CancellationToken ct)
        => FromResult(await Mediator.Send(new LinkPigeonCommand(membershipId, req.RingNumber), ct));

    [HttpGet("{clubId:guid}/page-info")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> GetPageInfo(Guid clubId, CancellationToken ct)
        => FromResult(await Mediator.Send(new GetClubPageInfoQuery(clubId), ct));

    [HttpPut("{clubId:guid}/slug")]
    [Authorize(Roles = "ClubManager,CountryManager,SuperAdmin")]
    public async Task<IActionResult> UpdateSlug(Guid clubId, [FromBody] UpdateSlugRequest req, CancellationToken ct)
        => FromResult(await Mediator.Send(new UpdateSlugCommand(clubId, req.NewSlug), ct));
}

public record SetThemeRequest(SiteTheme Theme);
public record InviteRequest(string Email);
public record LinkPigeonRequest(string RingNumber);
public record UpdateSlugRequest(string NewSlug);

// ── Themes Controller ─────────────────────────────────────────────────────────

[Route("api/themes")]
public class ThemesController : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => FromResult(await Mediator.Send(new GetThemesQuery(), ct));
}

// ── Notifications Controller ──────────────────────────────────────────────────

[Route("api/notifications")]
[Authorize]
public class NotificationsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => FromResult(await Mediator.Send(
            new GetMyNotificationsQuery(new PagedQuery { Page = page, PageSize = pageSize }), ct));

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken ct)
        => FromResult(await Mediator.Send(new MarkNotificationReadCommand(notificationId), ct));
}
