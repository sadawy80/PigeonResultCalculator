using System.Security.Claims;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRC.AdminService.Data;
using PRC.AdminService.DTOs;
using PRC.Common;
using PRC.Common.Messages;

namespace PRC.AdminService.Controllers;

[Route("api/admin/contact")]
[Authorize(Roles = "SuperAdmin")]
public class AdminContactController : AdminControllerBase
{
    private readonly AdminDbContext _db;
    private readonly IPublishEndpoint _bus;

    public AdminContactController(AdminDbContext db, IPublishEndpoint bus)
    {
        _db = db;
        _bus = bus;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.ContactMessages.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(m => m.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(m =>
                EF.Functions.Like(m.Subject,     $"%{s}%") ||
                EF.Functions.Like(m.SenderName,  $"%{s}%") ||
                EF.Functions.Like(m.SenderEmail, $"%{s}%"));
        }

        var total = await q.CountAsync(ct);

        // Return the full message in the list — admin inbox is low cardinality
        // (dozens, not thousands) and the new card UI displays the body inline,
        // so paying the body bytes here avoids an N+1 from the frontend.
        var items = await q
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new ContactMessageDetail(
                m.Id, m.SenderRole, m.SenderName, m.SenderEmail, m.SenderPhone,
                m.Subject, m.Body, m.Status, m.AdminReply, m.RepliedAt, m.RepliedByAdminId,
                m.CreatedAt, m.UpdatedAt))
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { items, total, page, pageSize }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var m = await _db.ContactMessages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return NotFound();
        return Ok(ApiResponse<ContactMessageDetail>.Ok(new ContactMessageDetail(
            m.Id, m.SenderRole, m.SenderName, m.SenderEmail, m.SenderPhone,
            m.Subject, m.Body, m.Status, m.AdminReply, m.RepliedAt, m.RepliedByAdminId,
            m.CreatedAt, m.UpdatedAt)));
    }

    [HttpPost("{id:guid}/reply")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ContactReplyDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Reply))
            return Problem(detail: "Reply text is required.", statusCode: 400);
        if (dto.Reply.Length > 5000)
            return Problem(detail: "Reply exceeds 5000 character limit.", statusCode: 400);

        var m = await _db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return NotFound();

        var adminId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var u) ? u : (Guid?)null;

        m.AdminReply        = dto.Reply.Trim();
        m.RepliedAt         = DateTime.UtcNow;
        m.RepliedByAdminId  = adminId;
        m.Status            = "Replied";
        m.UpdatedAt         = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var encodedName  = System.Net.WebUtility.HtmlEncode(m.SenderName);
        var encodedReply = System.Net.WebUtility.HtmlEncode(m.AdminReply).Replace("\n", "<br/>");
        var encodedBody  = System.Net.WebUtility.HtmlEncode(m.Body).Replace("\n", "<br/>");

        await _bus.Publish(new SendEmailEvent(
            To:       m.SenderEmail,
            Subject:  $"Re: {m.Subject}",
            HtmlBody: $"<p>Hello {encodedName},</p><p>{encodedReply}</p><hr/><p><em>Your original message:</em><br/>{encodedBody}</p>"
        ), ct);

        return Ok(ApiResponse<object>.Ok(new { id = m.Id, status = m.Status }));
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        var m = await _db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return NotFound();
        m.Status    = "Closed";
        m.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { id = m.Id, status = m.Status }));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var m = await _db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return NotFound();
        m.IsDeleted = true;
        m.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { id = m.Id }));
    }
}
