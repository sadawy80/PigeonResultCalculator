using PRC.Common;

namespace PRC.RenderingService.Models;

public class PrintTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TemplateCategory Category { get; set; }
    public TemplateStyle Style { get; set; } = TemplateStyle.Classic;
    public TemplatePaperSize PaperSize { get; set; } = TemplatePaperSize.A4Portrait;
    public TemplateColourScheme ColourScheme { get; set; } = TemplateColourScheme.Light;
    public string PrimaryColour { get; set; } = "#1E3A5F";
    public string SecondaryColour { get; set; } = "#C9A84C";
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string HtmlTemplate { get; set; } = string.Empty;
    public string VariableSchemaJson { get; set; } = "{}";
    public int MaxRows { get; set; } = 0;
    public bool IsMultiPage { get; set; } = false;
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; } = true;
    public Guid? ClubId { get; set; }
}

public class PrintJob : BaseEntity
{
    public Guid TemplateId { get; set; }
    public Guid ClubId { get; set; }
    public TemplateCategory Category { get; set; }
    public PrintJobStatus Status { get; set; } = PrintJobStatus.Pending;
    public string DataPayloadJson { get; set; } = "{}";
    public string? PdfUrl { get; set; }
    public long? FileSizeBytes { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid GeneratedByUserId { get; set; }
    public Guid? RaceId { get; set; }
    public Guid? ProgrammeId { get; set; }
    public Guid? RaceResultId { get; set; }
    public Guid? FederationResultId { get; set; }
    public Guid? UserId { get; set; }
}

// ── Page-builder template (club/country visual pages) ────────────────────────

public class PageTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    /// <summary>"certificate" | "race_results" | "club_page" | "country_page"</summary>
    public string Category { get; set; } = string.Empty;
    public string PreviewImageUrl { get; set; } = string.Empty;
    public string TemplateJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}
