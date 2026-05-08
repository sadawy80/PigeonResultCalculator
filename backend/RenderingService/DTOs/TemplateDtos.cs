using PRC.Common;

namespace PRC.RenderingService.DTOs;

public record PrintTemplateDto(
    Guid Id, string Name, string Description,
    TemplateCategory Category, string CategoryName,
    TemplateStyle Style, string StyleName,
    TemplatePaperSize PaperSize, string PaperSizeName,
    TemplateColourScheme ColourScheme,
    string PrimaryColour, string SecondaryColour,
    string ThumbnailUrl, int MaxRows, bool IsMultiPage,
    bool IsSystem, int SortOrder, string VariableSchemaJson);

public record PrintJobDto(
    Guid Id, Guid TemplateId, string TemplateName,
    TemplateCategory Category, PrintJobStatus Status,
    string? PdfUrl, long? FileSizeBytes,
    DateTime CreatedAt, DateTime? CompletedAt);

public record RenderRequest(
    Guid TemplateId,
    TemplateCategory Category,
    Guid? RaceId,
    Guid? ProgrammeId,
    Guid? RaceResultId,
    string? CertificateRecipientName,
    string? CertificateRank,
    string? CertificateAchievement,
    string? Locale = null);
