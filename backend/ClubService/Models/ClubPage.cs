using PRC.Common;

namespace PRC.ClubService.Models;

public class ClubPage : BaseEntity
{
    public Guid ClubId { get; set; }
    public string? CustomDomain { get; set; }
    public string Slug { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = false;
    public string? HeaderHtml { get; set; }
    public string? FooterHtml { get; set; }
    public string? CustomCss { get; set; }
    public string? AnnouncementsJson { get; set; }
    public string? LayoutConfig { get; set; }
    public SiteTheme Theme { get; set; } = SiteTheme.Skyline;
    public string? CertificateTemplateId { get; set; }
    public string? ResultsTemplateId { get; set; }

    public Club Club { get; set; } = null!;
}
