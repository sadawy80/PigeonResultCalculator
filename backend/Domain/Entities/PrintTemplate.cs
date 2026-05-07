using PigeonRacing.Domain.Common;
using PigeonRacing.Domain.Enums;

namespace PigeonRacing.Domain.Entities;

// ─────────────────────────────────────────────────────────────────────────────
//  PrintTemplate
//  Stores one of 160 built-in print/PDF templates (seeded, read-only in prod).
//  Each template is a self-contained HTML document with embedded CSS and
//  Handlebars-style {{variable}} placeholders substituted at render time.
// ─────────────────────────────────────────────────────────────────────────────

public class PrintTemplate : BaseEntity
{
    /// Human-readable name shown in the template browser.
    public string Name { get; set; } = string.Empty;

    /// Short marketing description shown on the template card.
    public string Description { get; set; } = string.Empty;

    /// Which result type this template renders.
    public TemplateCategory Category { get; set; }

    /// Visual style family — used for filtering and grouping.
    public TemplateStyle Style { get; set; } = TemplateStyle.Classic;

    /// Paper orientation the template is designed for.
    public TemplatePaperSize PaperSize { get; set; } = TemplatePaperSize.A4Portrait;

    /// Colour scheme — light/dark/branded.
    public TemplateColourScheme ColourScheme { get; set; } = TemplateColourScheme.Light;

    /// Primary accent hex colour used in the template (for preview swatches).
    public string PrimaryColour { get; set; } = "#1E3A5F";

    /// Secondary accent hex colour.
    public string SecondaryColour { get; set; } = "#C9A84C";

    /// Data URI or CDN URL of a 400×280 px JPEG thumbnail for the picker grid.
    public string ThumbnailUrl { get; set; } = string.Empty;

    /// The full self-contained HTML+CSS template.
    /// Placeholders use {{variable}} syntax — resolved server-side before returning to client.
    public string HtmlTemplate { get; set; } = string.Empty;

    /// JSON schema describing which variables this template uses
    /// (for validation and the variable reference panel in the UI).
    public string VariableSchemaJson { get; set; } = "{}";

    /// Maximum number of result rows this template is designed for (0 = unlimited).
    public int MaxRows { get; set; } = 0;

    /// Whether this template supports multi-page output.
    public bool IsMultiPage { get; set; } = false;

    /// Display order within the category.
    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    /// Whether this is a built-in system template (cannot be deleted by users).
    public bool IsSystem { get; set; } = true;

    /// Club that owns this template (null = system/available to all clubs).
    public Guid? ClubId { get; set; }
    public Club? Club { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
//  PrintJob — audit trail of every PDF generated
// ─────────────────────────────────────────────────────────────────────────────

public class PrintJob : BaseEntity
{
    public Guid TemplateId { get; set; }
    public Guid ClubId { get; set; }
    public TemplateCategory Category { get; set; }
    public PrintJobStatus Status { get; set; } = PrintJobStatus.Pending;

    /// JSON of the data payload passed to the renderer.
    public string DataPayloadJson { get; set; } = "{}";

    /// URL of the generated PDF (stored in file storage).
    public string? PdfUrl { get; set; }

    public long? FileSizeBytes { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid GeneratedByUserId { get; set; }

    // Context IDs — at most one is set
    public Guid? RaceId { get; set; }
    public Guid? ProgrammeId { get; set; }
    public Guid? RaceResultId { get; set; }      // for single-result certificates

    public PrintTemplate Template { get; set; } = null!;
    public Club Club { get; set; } = null!;
    public ApplicationUser GeneratedByUser { get; set; } = null!;
}
