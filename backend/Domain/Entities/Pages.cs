using PigeonRacing.Domain.Common;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.Domain.Entities;

public class ClubPage : BaseEntity
{
    public Guid ClubId { get; set; }
    public string? CustomDomain { get; set; }
    public string Slug { get; set; } = string.Empty;   // e.g. "my-club"
    public bool IsPublished { get; set; } = false;
    public string? HeaderHtml { get; set; }
    public string? FooterHtml { get; set; }
    public string? CustomCss { get; set; }
    public string? AnnouncementsJson { get; set; }     // JSON: list of announcements
    public string? LayoutConfig { get; set; }          // JSON: configurable layout

    // Theme system — one of 5 built-in themes
    public SiteTheme Theme { get; set; } = SiteTheme.Skyline;

    // Template selection
    public string? CertificateTemplateId { get; set; }
    public string? ResultsTemplateId { get; set; }

    // Navigation
    public Club Club { get; set; } = null!;
}

public class CountryPage : BaseEntity
{
    public Guid CountryId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = false;
    public string? HeaderHtml { get; set; }
    public string? FooterHtml { get; set; }
    public string? CustomCss { get; set; }
    public string? AnnouncementsJson { get; set; }
    public string? LayoutConfig { get; set; }

    // Theme system
    public SiteTheme Theme { get; set; } = SiteTheme.Skyline;

    // Navigation
    public Country Country { get; set; } = null!;
}

/// <summary>
/// Master template library (20 certificate + 10 race result templates).
/// </summary>
public class PageTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;   // "certificate" | "race_results"
    public string PreviewImageUrl { get; set; } = string.Empty;
    public string TemplateJson { get; set; } = string.Empty; // full layout definition
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

/// <summary>
/// Event log — immutable audit trail of all domain events.
/// </summary>
public class DomainEvent : BaseEntity
{
    public string EventType { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;     // JSON
    public Guid? TriggeredByUserId { get; set; }
    public string? CorrelationId { get; set; }
    public bool IsProcessed { get; set; } = false;
    public int RetryCount { get; set; } = 0;
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessingError { get; set; }
}
