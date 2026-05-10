using PRC.Common;

namespace PRC.FederationService.Models;

public class FederationPage : BaseEntity
{
    public Guid FederationId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = false;
    public string? HeaderHtml { get; set; }
    public string? FooterHtml { get; set; }
    public string? CustomCss { get; set; }
    public string? AnnouncementsJson { get; set; }
    public string? LayoutConfig { get; set; }
    public SiteTheme Theme { get; set; } = SiteTheme.Skyline;

    public Federation? Federation { get; set; }
}
