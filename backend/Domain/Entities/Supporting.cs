using PigeonRacing.Domain.Common;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.Domain.Entities;

public class Invitation : BaseEntity
{
    public Guid InvitedByUserId { get; set; }
    public Guid ClubId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = Guid.NewGuid().ToString("N");
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
    public DateTime? AcceptedAt { get; set; }
    public Guid? AcceptedByUserId { get; set; }

    // Navigation
    public ApplicationUser InvitedByUser { get; set; } = null!;
    public Club Club { get; set; } = null!;
    public ApplicationUser? AcceptedByUser { get; set; }
}

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public string? Metadata { get; set; }            // JSON: contextual data
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}

public class Report : BaseEntity
{
    public ReportType Type { get; set; }
    public ReportFormat Format { get; set; }
    public Guid? RaceId { get; set; }
    public Guid? CountryResultId { get; set; }
    public Guid? UserId { get; set; }               // fancier-specific report
    public Guid? ClubId { get; set; }
    public string? TemplateId { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public Guid GeneratedByUserId { get; set; }
    public string? Parameters { get; set; }         // JSON: report params used

    // Navigation
    public Race? Race { get; set; }
    public CountryResult? CountryResult { get; set; }
    public ApplicationUser? User { get; set; }
    public Club? Club { get; set; }
    public ApplicationUser GeneratedByUser { get; set; } = null!;
}

public class DataIngestionLog : BaseEntity
{
    public Guid RaceId { get; set; }
    public DataIngestionType IngestionType { get; set; }
    public string? FileName { get; set; }
    public int TotalRowsRead { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }
    public int DuplicateRows { get; set; }
    public string? ErrorSummary { get; set; }       // JSON: list of errors
    public string? RawFileUrl { get; set; }         // stored original file
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public Guid ProcessedByUserId { get; set; }
    public bool IsSuccess { get; set; }

    // Navigation
    public Race Race { get; set; } = null!;
    public ApplicationUser ProcessedByUser { get; set; } = null!;
}
