using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.Common;
using PRC.ClubService.Services;

namespace PRC.ClubService.Controllers;

[Route("api/notifications")]
[Authorize]
public class NotificationsController : ClubControllerBase
{
    private readonly IClubService _clubs;
    private readonly ICurrentUserService _user;

    public NotificationsController(IClubService clubs, ICurrentUserService user)
    {
        _clubs = clubs;
        _user = user;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] PagedQuery paged, CancellationToken ct)
        => FromResult(await _clubs.GetMyNotificationsAsync(_user.UserId.GetValueOrDefault(), paged, ct));

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken ct)
        => FromResult(await _clubs.MarkNotificationReadAsync(notificationId, ct));
}
