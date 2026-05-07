namespace PigeonRacing.Domain.Enums;

public enum UserRole
{
    SuperAdmin = 1,
    CountryManager = 2,
    ClubManager = 3,
    Fancier = 4
}

public enum RaceStatus
{
    Draft = 1,
    Scheduled = 2,
    InProgress = 3,
    Completed = 4,
    Published = 5,
    Cancelled = 6
}

public enum ResultStatus
{
    Pending = 1,
    Validated = 2,
    Published = 3,
    Rejected = 4
}

public enum DataIngestionType
{
    Manual = 1,
    ETSFile = 2,
    IoT = 3
}

public enum SubscriptionType
{
    Country = 1,
    Club = 2
}

public enum SubscriptionStatus
{
    Active = 1,
    Expired = 2,
    Suspended = 3,
    Trial = 4
}

public enum BillingCycle
{
    Monthly = 1,
    Yearly = 2
}

public enum NotificationChannel
{
    InApp = 1,
    Email = 2,
    Push = 3
}

public enum NotificationType
{
    RaceResult = 1,
    ClubUpdate = 2,
    RaceAnnouncement = 3,
    SystemUpdate = 4,
    InvitationSent = 5,
    ErrorAlert = 6
}

public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3,
    Read = 4
}

public enum ReportFormat
{
    PDF = 1,
    Excel = 2
}

public enum ReportType
{
    ClubRaceResults = 1,
    CountryAggregated = 2,
    FancierPerformance = 3,
    Certificate = 4
}

public enum InvitationStatus
{
    Pending = 1,
    Accepted = 2,
    Expired = 3,
    Revoked = 4
}

public enum CountryResultStatus
{
    Draft = 1,
    Published = 2
}

public enum WindDirection
{
    N = 1,
    NE = 2,
    E = 3,
    SE = 4,
    S = 5,
    SW = 6,
    W = 7,
    NW = 8
}

public enum EventType
{
    RaceCreated = 1,
    RaceStarted = 2,
    RaceCompleted = 3,
    ResultProcessed = 4,
    ResultPublished = 5,
    CountryResultPublished = 6,
    UserInvited = 7,
    UserLinked = 8,
    SubscriptionChanged = 9
}

/// <summary>
/// The 5 built-in site themes selectable by Club/Country Managers.
/// Each theme ships with a full CSS variable set + Angular component class.
/// </summary>
public enum SiteTheme
{
    /// Skyline — modern dark navy + electric blue. Clean grid layout.
    Skyline = 1,

    /// Meadow — earthy greens + warm amber. Nature-inspired, rounded cards.
    Meadow = 2,

    /// Crimson — bold red + charcoal. High-contrast, sport-grade feel.
    Crimson = 3,

    /// Ivory — light cream + gold accents. Classic, formal federation look.
    Ivory = 4,

    /// Slate — cool grey + cyan highlights. Minimal, data-forward dashboard style.
    Slate = 5
}
