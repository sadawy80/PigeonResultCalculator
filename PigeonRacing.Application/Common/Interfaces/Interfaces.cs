using Microsoft.EntityFrameworkCore;
using PigeonRacing.Domain.Entities;

namespace PigeonRacing.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Country> Countries { get; }
    DbSet<Club> Clubs { get; }
    DbSet<ClubMembership> ClubMemberships { get; }
    DbSet<Race> Races { get; }
    DbSet<RaceCategory> RaceCategories { get; }
    DbSet<RaceResult> RaceResults { get; }
    DbSet<DataIngestionLog> DataIngestionLogs { get; }
    DbSet<Pigeon> Pigeons { get; }
    DbSet<PigeonLink> PigeonLinks { get; }
    DbSet<CountryResult> CountryResults { get; }
    DbSet<CountryResultRace> CountryResultRaces { get; }
    DbSet<CountryResultEntry> CountryResultEntries { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<CountrySubscription> CountrySubscriptions { get; }
    DbSet<ClubSubscription> ClubSubscriptions { get; }
    DbSet<Invitation> Invitations { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<Report> Reports { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<ClubPage> ClubPages { get; }
    DbSet<CountryPage> CountryPages { get; }
    DbSet<PageTemplate> PageTemplates { get; }
    DbSet<DomainEvent> DomainEvents { get; }

    // Programmes & aggregate results
    DbSet<ClubProgramme> ClubProgrammes { get; }
    DbSet<ProgrammeRace> ProgrammeRaces { get; }
    DbSet<BestLoftResult> BestLoftResults { get; }
    DbSet<AcePigeonResult> AcePigeonResults { get; }
    DbSet<SuperAcePigeonResult> SuperAcePigeonResults { get; }

    // Print templates & jobs
    DbSet<PrintTemplate> PrintTemplates { get; }
    DbSet<PrintJob> PrintJobs { get; }

    // External platform integrations
    DbSet<ExternalLink> ExternalLinks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    string? Role { get; }
    Guid? CountryId { get; }
    Guid? ClubId { get; }
    bool IsAuthenticated { get; }
}

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
    Task SendInvitationAsync(string to, string inviterName, string clubName, string inviteLink, CancellationToken ct = default);
    Task SendRaceResultNotificationAsync(string to, string raceName, string pigeonRing, int rank, CancellationToken ct = default);
}

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, string folder, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string fileUrl, CancellationToken ct = default);
    Task DeleteAsync(string fileUrl, CancellationToken ct = default);
    string GetPresignedUrl(string fileUrl, int expiryMinutes = 60);
}

public interface IVelocityCalculator
{
    /// <summary>
    /// Calculates velocity in meters per minute.
    /// </summary>
    double Calculate(double distanceKm, TimeSpan flightDuration);

    /// <summary>
    /// Calculates distance in km between two lat/lon points using Haversine.
    /// </summary>
    double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
}

public interface INotificationService
{
    Task SendInAppAsync(Guid userId, string title, string body, string? actionUrl = null, CancellationToken ct = default);
    Task SendToRoleAsync(string role, Guid? scopeId, string title, string body, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default);
}

public interface IDomainEventDispatcher
{
    Task DispatchAsync(Guid aggregateId, string aggregateType, string eventType, object payload, CancellationToken ct = default);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken ct = default);
}

public interface IETSFileParser
{
    Task<ETSParseResult> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default);
}

public class ETSParseResult
{
    public bool IsSuccess { get; set; }
    public List<ETSRow> Rows { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public int TotalRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }
    public int DuplicateRows { get; set; }
}

public class ETSRow
{
    public string RingNumber { get; set; } = string.Empty;
    public DateTime ArrivalTime { get; set; }
    public string? PigeonName { get; set; }
    public string? Sex { get; set; }
    public int? YearOfBirth { get; set; }
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
}
