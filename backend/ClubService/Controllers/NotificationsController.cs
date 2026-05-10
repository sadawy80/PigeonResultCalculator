using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.Common;
using PRC.ClubService.DTOs;
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
    public async Task<IActionResult> GetMine([FromQuery] PagedQuery paged, [FromQuery] bool unreadOnly = false, CancellationToken ct = default)
        => FromResult(await _clubs.GetMyNotificationsAsync(_user.UserId.GetValueOrDefault(), paged, unreadOnly, ct));

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken ct)
        => FromResult(await _clubs.MarkNotificationReadAsync(notificationId, ct));

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
        => FromResult(await _clubs.MarkAllNotificationsReadAsync(_user.UserId.GetValueOrDefault(), ct));

    [HttpDelete("{notificationId:guid}")]
    public async Task<IActionResult> Dismiss(Guid notificationId, CancellationToken ct)
        => FromResult(await _clubs.DismissNotificationAsync(notificationId, ct));

    [HttpDelete]
    public async Task<IActionResult> DismissAll(CancellationToken ct)
        => FromResult(await _clubs.DismissAllNotificationsAsync(_user.UserId.GetValueOrDefault(), ct));
}
