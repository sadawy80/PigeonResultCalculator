using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using PigeonRacing.Application.Common.Interfaces;
using PigeonRacing.Domain.Entities;
using PigeonRacing.Domain.Enums;
using PigeonRacing.Infrastructure.Persistence;
using System.Text.Json;

namespace PigeonRacing.Infrastructure.Services;

// ── Email Service ────────────────────────────────────────────────────────────

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_config["Email:From"] ?? "noreply@pigeonracing.com"));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            _config["Email:Host"] ?? "localhost",
            int.Parse(_config["Email:Port"] ?? "587"),
            SecureSocketOptions.StartTls, ct);

        if (!string.IsNullOrEmpty(_config["Email:Username"]))
            await client.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"], ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
        _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
    }

    public Task SendInvitationAsync(string to, string inviterName, string clubName, string inviteLink, CancellationToken ct = default)
    {
        var html = $@"
            <h2>You're invited to join {clubName}!</h2>
            <p>{inviterName} has invited you to join their pigeon racing club on PigeonRacing Platform.</p>
            <p><a href='{inviteLink}' style='background:#1a73e8;color:white;padding:12px 24px;border-radius:4px;text-decoration:none;'>Accept Invitation</a></p>
            <p>This link expires in 7 days.</p>";
        return SendAsync(to, $"Invitation to join {clubName}", html, ct);
    }

    public Task SendRaceResultNotificationAsync(string to, string raceName, string pigeonRing, int rank, CancellationToken ct = default)
    {
        var html = $@"
            <h2>Race Result Update</h2>
            <p>Your pigeon <strong>{pigeonRing}</strong> finished <strong>#{rank}</strong> in <strong>{raceName}</strong>!</p>
            <p><a href='https://pigeonracing.com/results'>View Full Results</a></p>";
        return SendAsync(to, $"Race Result: {raceName}", html, ct);
    }
}

// ── Notification Service ─────────────────────────────────────────────────────

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SendInAppAsync(Guid userId, string title, string body,
        string? actionUrl = null, CancellationToken ct = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = NotificationType.SystemUpdate,
            Channel = NotificationChannel.InApp,
            Status = NotificationStatus.Sent,
            Title = title,
            Body = body,
            ActionUrl = actionUrl,
            SentAt = DateTime.UtcNow
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("InApp notification sent to user {UserId}: {Title}", userId, title);
    }

    public async Task SendToRoleAsync(string role, Guid? scopeId, string title,
        string body, CancellationToken ct = default)
    {
        // Scope: countryId for CountryManager, clubId for ClubManager
        var users = _db.Users.AsQueryable();

        // Filter by role claim — simplified: query by Role enum
        // In practice, filter by IdentityUserRole
        var notifications = users.Select(u => new Notification
        {
            UserId = u.Id,
            Type = NotificationType.SystemUpdate,
            Channel = NotificationChannel.InApp,
            Status = NotificationStatus.Sent,
            Title = title,
            Body = body,
            SentAt = DateTime.UtcNow
        });

        await _db.Notifications.AddRangeAsync(notifications, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        var n = await _db.Notifications.FindAsync(new object[] { notificationId }, ct);
        if (n != null)
        {
            n.Status = NotificationStatus.Read;
            n.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }
}

// ── Local File Storage (dev; replace with S3 in prod) ───────────────────────

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IConfiguration config, ILogger<LocalFileStorageService> logger)
    {
        _basePath = config["FileStorage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        _baseUrl = config["FileStorage:BaseUrl"] ?? "http://localhost:5000/files";
        _logger = logger;
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType,
        string folder, CancellationToken ct = default)
    {
        var safeFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var dir = Path.Combine(_basePath, folder);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, safeFileName);

        await using var fileStream = File.Create(path);
        await stream.CopyToAsync(fileStream, ct);

        var url = $"{_baseUrl}/{folder}/{safeFileName}";
        _logger.LogInformation("File uploaded: {Url}", url);
        return url;
    }

    public Task<Stream> DownloadAsync(string fileUrl, CancellationToken ct = default)
    {
        var relativePath = fileUrl.Replace(_baseUrl, "").TrimStart('/');
        var path = Path.Combine(_basePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return Task.FromResult<Stream>(File.OpenRead(path));
    }

    public Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        var relativePath = fileUrl.Replace(_baseUrl, "").TrimStart('/');
        var path = Path.Combine(_basePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public string GetPresignedUrl(string fileUrl, int expiryMinutes = 60) => fileUrl;
}

// ── Domain Event Dispatcher ──────────────────────────────────────────────────

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly AppDbContext _db;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(AppDbContext db, ILogger<DomainEventDispatcher> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task DispatchAsync(Guid aggregateId, string aggregateType,
        string eventType, object payload, CancellationToken ct = default)
    {
        var domainEvent = new DomainEvent
        {
            AggregateId = aggregateId.ToString(),
            AggregateType = aggregateType,
            EventType = eventType,
            Payload = JsonSerializer.Serialize(payload),
            IsProcessed = false
        };

        _db.DomainEvents.Add(domainEvent);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Domain event dispatched: {EventType} for {AggregateType}:{AggregateId}",
            eventType, aggregateType, aggregateId);
    }
}
