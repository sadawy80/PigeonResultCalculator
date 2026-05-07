// src/app/core/models/integration.models.ts

export enum ExternalLinkStatus {
  Pending  = 1,
  Approved = 2,
  Rejected = 3,
  Revoked  = 4
}

export interface ExternalLink {
  id: string;
  userId: string;
  fancierName: string;
  clubId: string;
  clubName: string;
  externalPlatformName: string;
  externalUserId: string;
  externalLoftId: string;
  externalLoftName: string;
  linkToken: string;
  status: ExternalLinkStatus;
  statusName: string;
  rejectionReason?: string;
  revokedReason?: string;
  requestedAt: string;
  approvedAt?: string;
  rejectedAt?: string;
  revokedAt?: string;
  lastDataAccessAt?: string;
  reviewedByName?: string;
}

export interface IntegrationRaceResult {
  ringNumber: string;
  pigeonName?: string;
  pigeonSex?: string;
  pigeonYearOfBirth?: number;
  raceName: string;
  clubName: string;
  releaseLocation: string;
  raceDate: string;
  distanceKm: number;
  velocityMperMin: number;
  velocityKmH: number;
  clubRank?: number;
  categoryRank?: number;
  categoryName?: string;
  programmeName?: string;
  programmeYear?: number;
  isAcePigeon: boolean;
  isSuperAcePigeon: boolean;
  isBestLoft: boolean;
  aceRank?: number;
  superAceRank?: number;
  loftRank?: number;
}

export interface IntegrationAcePigeon {
  ringNumber: string;
  pigeonName?: string;
  pigeonSex?: string;
  pigeonYearOfBirth?: number;
  programmeName: string;
  programmeYear: number;
  aceRank: number;
  totalScore: number;
  averageScore: number;
  racesEntered: number;
  racesInProgramme: number;
  participationRate: number;
  bestVelocityMperMin: number;
  averageVelocityMperMin: number;
  bestClubRank: number;
}

export interface IntegrationSuperAce extends IntegrationAcePigeon {
  superAceRank: number;
}

export interface IntegrationBestLoft {
  programmeName: string;
  programmeYear: number;
  loftRank: number;
  totalScore: number;
  averageScore: number;
  racesEntered: number;
  pigeonsEntered: number;
  bestSingleVelocityMperMin: number;
  averageVelocityMperMin: number;
}

export interface IntegrationAchievement {
  category: string;
  programmeName: string;
  year: number;
  rank: number;
  score: number;
  description: string;
}

export interface IntegrationSummary {
  fancierName: string;
  clubName: string;
  totalRaceResults: number;
  totalAcePigeonResults: number;
  totalSuperAcePigeonResults: number;
  totalBestLoftResults: number;
  bestEverClubRank: number;
  bestEverVelocityMperMin: number;
  lastRaceDate?: string;
  achievements: IntegrationAchievement[];
}
