namespace PRC.BackupService.Models;

public enum BackupStatus { InProgress = 1, Completed = 2, Failed = 3 }

public class BackupEntry
{
    public Guid         Id               { get; set; } = Guid.NewGuid();
    public string       DatabaseName     { get; set; } = "";
    public string       ObjectKey        { get; set; } = "";
    public long         SizeBytes        { get; set; }
    public DateTime     CreatedAt        { get; set; } = DateTime.UtcNow;
    public DateTime?    CompletedAt      { get; set; }
    public BackupStatus Status           { get; set; } = BackupStatus.InProgress;
    public string?      ErrorMessage     { get; set; }
    public bool         UploadedToMinIO  { get; set; }
    public bool         UploadedToPCloud { get; set; }
    public string       TriggeredBy      { get; set; } = "schedule";
}
