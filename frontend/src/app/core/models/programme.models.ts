// ── Append to src/app/core/models/index.ts ────────────────────────────────────

export enum ScoringMethod {
  AverageSpeed = 1,
  PointsByRank = 2,
  PointsBySpeedPercentage = 3,
  TotalSpeed = 4
}

export enum SuperAceQualification {
  AllRacesRequired = 1,
  MinimumRaceCount = 2,
  MinimumRacePercentage = 3
}

export enum ProgrammeStatus {
  Draft = 1,
  Active = 2,
  Completed = 3,
  Published = 4,
  Cancelled = 5
}

export interface RaceBreakdownItem {
  raceId: string;
  raceName: string;
  score: number;
  speed: number;
  clubRank: number;
  pigeonsEntered: number;
  dnf: boolean;
}

export interface ProgrammeRaceItem {
  programmeRaceId: string;
  raceId: string;
  raceName: string;
  actualReleaseTime?: string;
  scoreWeight: number;
  sortOrder: number;
  totalEntries: number;
}

export interface Programme {
  id: string;
  clubId: string;
  clubName: string;
  name: string;
  description?: string;
  year: number;
  startDate?: string;
  endDate?: string;
  status: ProgrammeStatus;
  scoringMethod: ScoringMethod;
  pointsForFirst: number;
  maxPointPositions: number;
  bestLoftPigeonsPerRace: number;
  bestLoftMinRaces: number;
  acePigeonMinRaces: number;
  superAceQualification: SuperAceQualification;
  superAceMinRaceCount: number;
  superAceMinRacePercentage: number;
  publishedAt?: string;
  createdAt: string;
  races: ProgrammeRaceItem[];
}

export interface ProgrammeSummary {
  id: string;
  name: string;
  year: number;
  status: ProgrammeStatus;
  scoringMethod: ScoringMethod;
  raceCount: number;
  startDate?: string;
  endDate?: string;
}

export interface BestLoftResult {
  id: string;
  programmeId: string;
  programmeName: string;
  userId?: string;
  fancierName: string;
  loftRank: number;
  totalScore: number;
  averageScore: number;
  racesEntered: number;
  pigeonsEntered: number;
  bestSingleSpeedMperMin: number;
  averageSpeedMperMin: number;
  raceBreakdown: RaceBreakdownItem[];
}

export interface AcePigeonResult {
  id: string;
  programmeId: string;
  programmeName: string;
  userId?: string;
  fancierName: string;
  pigeonId?: string;
  ringNumber: string;
  pigeonName?: string;
  pigeonSex?: string;
  pigeonYearOfBirth?: number;
  aceRank: number;
  totalScore: number;
  averageScore: number;
  racesEntered: number;
  racesInProgramme: number;
  participationRate: number;
  bestSpeedMperMin: number;
  averageSpeedMperMin: number;
  bestClubRank: number;
  raceBreakdown: RaceBreakdownItem[];
}

export interface SuperAcePigeonResult {
  id: string;
  programmeId: string;
  programmeName: string;
  userId?: string;
  fancierName: string;
  pigeonId?: string;
  ringNumber: string;
  pigeonName?: string;
  pigeonSex?: string;
  pigeonYearOfBirth?: number;
  superAceRank: number;
  totalScore: number;
  averageScore: number;
  racesEntered: number;
  racesInProgramme: number;
  participationRate: number;
  bestSpeedMperMin: number;
  averageSpeedMperMin: number;
  bestClubRank: number;
  acePigeonResultId?: string;
  raceBreakdown: RaceBreakdownItem[];
}

export interface CalculationSummary {
  bestLoftEntriesCalculated: number;
  acePigeonEntriesCalculated: number;
  superAcePigeonEntriesCalculated: number;
  racesIncluded: number;
  scoringMethod: string;
  warnings?: string;
}
