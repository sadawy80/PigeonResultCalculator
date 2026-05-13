using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRC.AdminService.Data;
using PRC.AdminService.DTOs;
using PRC.AdminService.Models;
using PRC.Common;

namespace PRC.AdminService.Controllers;

[ApiController]
[Route("api/contact")]
// Accept BOTH the admin scheme (default) and the user IdentityService scheme so
// a logged-in fancier/club-manager/federation-manager submitting from /support
// has their JWT decoded — letting us stamp the real SenderRole + UserId on the
// message. The endpoint itself is [AllowAnonymous], so the request still goes
// through when no token is present at all.
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + ",User")]
public class ContactController : ControllerBase
{
    private static readonly Regex EmailFormat = new(
        @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly AdminDbContext _db;

    public ContactController(AdminDbContext db) => _db = db;

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Submit([FromBody] ContactSubmissionDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)
            || string.IsNullOrWhiteSpace(dto.Email)
            || string.IsNullOrWhiteSpace(dto.Subject)
            || string.IsNullOrWhiteSpace(dto.Body))
            return Problem(detail: "Name, email, subject and message are required.", statusCode: 400);

        if (dto.Body.Length > 5000)
            return Problem(detail: "Message exceeds 5000 character limit.", statusCode: 400);

        if (!EmailFormat.IsMatch(dto.Email))
            return Problem(detail: "Email address is not valid.", statusCode: 400);

        var role = User.Identity?.IsAuthenticated == true
            ? (User.FindFirstValue(ClaimTypes.Role) ?? "Anonymous")
            : "Anonymous";

        var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var u) ? u : (Guid?)null;

        var msg = new ContactMessage
        {
            SenderRole  = role,
            UserId      = userId,
            SenderName  = dto.Name.Trim(),
            SenderEmail = dto.Email.Trim(),
            SenderPhone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
            Subject     = dto.Subject.Trim(),
            Body        = dto.Body.Trim()
        };

        _db.ContactMessages.Add(msg);
        _db.AdminNotifications.Add(new AdminNotification
        {
            Type      = "ContactMessage",
            Title     = $"New contact message: {msg.Subject}",
            Body      = $"From {msg.SenderName} <{msg.SenderEmail}>",
            ActionUrl = $"/admin/contact/{msg.Id}",
            SourceId  = msg.Id.ToString()
        });
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { id = msg.Id }, "Thanks — we have received your message."));
    }
}
