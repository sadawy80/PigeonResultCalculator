using PRC.Common;

namespace PRC.ClubService.DTOs;

public record ClubDto(
    Guid Id, Guid FederationId, string FederationName, string Name, string Code,
    string? Description, string? City, string? LogoUrl,
    string? PrimaryColor, string? SecondaryColor,
    bool IsActive, int MemberCount, DateTime CreatedAt);

public record ClubMemberDto(
    Guid MembershipId, Guid UserId, string FullName, string Email,
    UserRole Role, DateTime JoinedAt, int LinkedPigeonCount);

public record InvitationDto(
    Guid Id, string Email, InvitationStatus Status,
    DateTime ExpiresAt, DateTime CreatedAt, string ClubName);

public record NotificationDto(
    Guid Id, NotificationType Type, NotificationChannel Channel,
    NotificationStatus Status, string Title, string Body,
    string? ActionUrl, DateTime CreatedAt, DateTime? ReadAt);

public record ThemeDto(
    int Id, string Name, string Description,
    string PrimaryColor, string AccentColor,
    string BackgroundColor, string SurfaceColor,
    string TextColor, string PreviewImageUrl);

public record GetClubPageInfoDto(
    string Slug, bool IsPublished,
    string? LogoUrl, string? PrimaryColor, string? SecondaryColor,
    string? AnnouncementsJson);

public static class BuiltInThemes
{
    public static readonly List<ThemeDto> All = new()
    {
        new((int)SiteTheme.Skyline, "Skyline",
            "Modern dark navy with electric blue accents. Clean grid layout.",
            "#0D1B2A", "#1E90FF", "#0A1520", "#132030", "#E8F0FE",
            "/assets/themes/skyline-preview.jpg"),
        new((int)SiteTheme.Meadow, "Meadow",
            "Earthy greens and warm amber. Nature-inspired rounded cards.",
            "#2D6A4F", "#F4A261", "#F9F3E8", "#FFFFFF", "#1B3A2D",
            "/assets/themes/meadow-preview.jpg"),
        new((int)SiteTheme.Crimson, "Crimson",
            "Bold red and charcoal. High-contrast sport-grade feel.",
            "#C1121F", "#2B2D42", "#F5F5F5", "#FFFFFF", "#1A1A2E",
            "/assets/themes/crimson-preview.jpg"),
        new((int)SiteTheme.Ivory, "Ivory",
            "Light cream and gold accents. Classic formal federation look.",
            "#B8860B", "#8B6914", "#FAF7F0", "#FFFFFF", "#3D3320",
            "/assets/themes/ivory-preview.jpg"),
        new((int)SiteTheme.Slate, "Slate",
            "Cool grey with cyan highlights. Minimal data-forward dashboard style.",
            "#4A5568", "#00B4D8", "#F7FAFC", "#FFFFFF", "#2D3748",
            "/assets/themes/slate-preview.jpg"),
    };

    public static ThemeDto Get(SiteTheme theme) => All.First(t => t.Id == (int)theme);
}

// Requests
public record CreateClubRequest(
    Guid FederationId, string Name, string Code,
    string? Description, string? City, string? Address, string? PostalCode,
    double? Latitude, double? Longitude,
    string? ContactEmail, string? ContactPhone);

public record UpdateClubBrandingRequest(
    string? LogoUrl, string? PrimaryColor, string? SecondaryColor, SiteTheme Theme);

public record SetThemeRequest(SiteTheme Theme);
public record InviteRequest(string Email);
public record RemoveMemberRequest(Guid UserId);
public record LinkPigeonRequest(string RingNumber);
public record UpdateSlugRequest(string NewSlug);
public record UpdateAnnouncementsRequest(string AnnouncementsJson);
