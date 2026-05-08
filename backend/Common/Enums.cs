namespace PRC.Common;

public enum UserRole          { Pending = 0, SuperAdmin = 1, FederationManager = 2, ClubManager = 3, Fancier = 4 }
public enum RaceStatus        { Draft = 1, Scheduled = 2, InProgress = 3, Completed = 4, Published = 5, Cancelled = 6 }
public enum ResultStatus      { Pending = 1, Validated = 2, Published = 3, Rejected = 4 }
public enum DataIngestionType { Manual = 1, ETSFile = 2, IoT = 3 }
public enum SubscriptionType  { Federation = 1, Club = 2 }
public enum SubscriptionStatus{ Active = 1, Expired = 2, Suspended = 3, Trial = 4, Cancelled = 5 }
public enum BillingCycle      { Monthly = 1, Annual = 2, Seasonal = 3 }
public enum NotificationChannel{ InApp = 1, Email = 2, Push = 3 }
public enum NotificationType  { RaceResult = 1, ClubUpdate = 2, RaceAnnouncement = 3, SystemUpdate = 4, InvitationSent = 5, ErrorAlert = 6 }
public enum NotificationStatus{ Pending = 1, Sent = 2, Failed = 3, Read = 4 }
public enum ReportFormat      { PDF = 1, Excel = 2 }
public enum ReportType        { ClubRaceResults = 1, FederationAggregated = 2, FancierPerformance = 3, Certificate = 4 }
public enum InvitationStatus  { Pending = 1, Accepted = 2, Expired = 3, Revoked = 4 }
public enum FederationResultStatus{ Draft = 1, Published = 2 }
public enum WindDirection     { N = 1, NE = 2, E = 3, SE = 4, S = 5, SW = 6, W = 7, NW = 8 }
public enum ExternalLinkStatus{ Pending = 1, Approved = 2, Rejected = 3, Revoked = 4 }
public enum SiteTheme         { Skyline = 1, Meadow = 2, Crimson = 3, Ivory = 4, Slate = 5 }

// Programme enums
public enum ScoringMethod         { AverageSpeed = 1, PointsByRank = 2, PointsBySpeedPercentage = 3, TotalSpeed = 4 }
public enum SuperAceQualification { AllRacesRequired = 1, MinimumRaceCount = 2, MinimumRacePercentage = 3 }
public enum ProgrammeStatus       { Draft = 1, Active = 2, Completed = 3, Published = 4, Cancelled = 5 }
public enum AggregateResultType   { BestLoft = 1, AcePigeon = 2, SuperAcePigeon = 3 }

// Audit enums
public enum AuditSeverity        { Info = 1, Warning = 2, Critical = 3 }
public enum UpgradeRequestStatus { Pending = 0, Approved = 1, Rejected = 2 }

// Template enums
public enum TemplateCategory  { RaceResults = 1, BestLoft = 2, AcePigeon = 3, SuperAcePigeon = 4, Certificate = 5 }
public enum TemplateStyle     { Classic = 1, Modern = 2, Elegant = 3, Minimal = 4, Sporty = 5, Heritage = 6, Corporate = 7, Vibrant = 8, Dark = 9, Branded = 10 }
public enum TemplatePaperSize { A4Portrait = 1, A4Landscape = 2, A3Portrait = 3, A3Landscape = 4, LetterPortrait = 5, LetterLandscape = 6 }
public enum TemplateColourScheme { Light = 1, Dark = 2, Branded = 3, Gold = 4, Navy = 5, Crimson = 6, Forest = 7, Monochrome = 8 }
public enum PrintJobStatus    { Pending = 1, Rendering = 2, Complete = 3, Failed = 4 }
