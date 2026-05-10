using PRC.Common;

namespace PRC.ClubService.Models;

public class ClubProgramme : BaseEntity
{
    public Guid? ClubId { get; set; }          // null for federation-owned programmes
    public Guid? FederationId { get; set; }    // set when created by a federation
    public string? FederationName { get; set; } // cached
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Year { get; set; } = DateTime.UtcNow.Year;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ProgrammeStatus Status { get; set; } = ProgrammeStatus.Draft;
    public ScoringMethod ScoringMethod { get; set; } = ScoringMethod.AverageSpeed;
    public int PointsForFirst { get; set; } = 10;
    public int MaxPointPositions { get; set; } = 0;
    public int BestLoftPigeonsPerRace { get; set; } = 0;
    public int BestLoftMinRaces { get; set; } = 1;
    public int AcePigeonMinRaces { get; set; } = 3;
    public SuperAceQualification SuperAceQualification { get; set; } = SuperAceQualification.AllRacesRequired;
    public int SuperAceMinRaceCount { get; set; } = 0;
    public double SuperAceMinRacePercentage { get; set; } = 100.0;
    public DateTime? PublishedAt { get; set; }
    public Guid? PublishedByUserId { get; set; }

    public Club? Club { get; set; }
    public ICollection<ProgrammeRace> ProgrammeRaces { get; set; } = new List<ProgrammeRace>();
    public ICollection<BestLoftResult> BestLoftResults { get; set; } = new List<BestLoftResult>();
    public ICollection<AcePigeonResult> AcePigeonResults { get; set; } = new List<AcePigeonResult>();
    public ICollection<SuperAcePigeonResult> SuperAcePigeonResults { get; set; } = new List<SuperAcePigeonResult>();
}

public class ProgrammeRace : BaseEntity
{
    public Guid ProgrammeId { get; set; }
    public Guid RaceId { get; set; }
    public string RaceName { get; set; } = string.Empty;
    public DateTime? ActualReleaseTime { get; set; }
    public double ScoreWeight { get; set; } = 1.0;
    public int SortOrder { get; set; }
    public int TotalEntries { get; set; }

    public ClubProgramme Programme { get; set; } = null!;
}

public class BestLoftResult : BaseEntity
{
    public Guid ProgrammeId { get; set; }
    public Guid? UserId { get; set; }
    public string FancierName { get; set; } = string.Empty;
    public int LoftRank { get; set; }
    public double TotalScore { get; set; }
    public double AverageScore { get; set; }
    public int RacesEntered { get; set; }
    public int PigeonsEntered { get; set; }
    public double BestSingleSpeedMperMin { get; set; }
    public double AverageSpeedMperMin { get; set; }
    public string? RaceBreakdownJson { get; set; }

    public ClubProgramme Programme { get; set; } = null!;
}

public class AcePigeonResult : BaseEntity
{
    public Guid ProgrammeId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? PigeonId { get; set; }
    public string RingNumber { get; set; } = string.Empty;
    public string? PigeonName { get; set; }
    public string? PigeonSex { get; set; }
    public int? PigeonYearOfBirth { get; set; }
    public string FancierName { get; set; } = string.Empty;
    public int AceRank { get; set; }
    public double TotalScore { get; set; }
    public double AverageScore { get; set; }
    public int RacesEntered { get; set; }
    public int RacesInProgramme { get; set; }
    public double ParticipationRate { get; set; }
    public double BestSpeedMperMin { get; set; }
    public double AverageSpeedMperMin { get; set; }
    public int BestClubRank { get; set; }
    public string? RaceBreakdownJson { get; set; }

    public ClubProgramme Programme { get; set; } = null!;
}

public class SuperAcePigeonResult : BaseEntity
{
    public Guid ProgrammeId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? PigeonId { get; set; }
    public string RingNumber { get; set; } = string.Empty;
    public string? PigeonName { get; set; }
    public string? PigeonSex { get; set; }
    public int? PigeonYearOfBirth { get; set; }
    public string FancierName { get; set; } = string.Empty;
    public int SuperAceRank { get; set; }
    public double TotalScore { get; set; }
    public double AverageScore { get; set; }
    public int RacesEntered { get; set; }
    public int RacesInProgramme { get; set; }
    public double ParticipationRate { get; set; }
    public double BestSpeedMperMin { get; set; }
    public double AverageSpeedMperMin { get; set; }
    public int BestClubRank { get; set; }
    public Guid? AcePigeonResultId { get; set; }
    public string? RaceBreakdownJson { get; set; }

    public ClubProgramme Programme { get; set; } = null!;
}
