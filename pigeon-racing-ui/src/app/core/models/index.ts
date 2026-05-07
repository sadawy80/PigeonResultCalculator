// ── src/app/core/models/index.ts ──────────────────────────────────────────────

export enum UserRole { SuperAdmin = 1, CountryManager = 2, ClubManager = 3, Fancier = 4 }
export enum RaceStatus { Draft = 1, Scheduled = 2, InProgress = 3, Completed = 4, Published = 5, Cancelled = 6 }
export enum ResultStatus { Pending = 1, Validated = 2, Published = 3, Rejected = 4 }
export enum DataIngestionType { Manual = 1, ETSFile = 2, IoT = 3 }
export enum NotificationStatus { Pending = 1, Sent = 2, Failed = 3, Read = 4 }
export enum NotificationType { RaceResult = 1, ClubUpdate = 2, RaceAnnouncement = 3, SystemUpdate = 4, InvitationSent = 5, ErrorAlert = 6 }
export enum CountryResultStatus { Draft = 1, Published = 2 }
export enum InvitationStatus { Pending = 1, Accepted = 2, Expired = 3, Revoked = 4 }

export enum SiteTheme { Skyline = 1, Meadow = 2, Crimson = 3, Ivory = 4, Slate = 5 }

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errorCode?: string;
  errors?: string[];
  timestamp?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  role: UserRole;
  countryId?: string;
  clubId?: string;
  profileImageUrl?: string;
  isActive: boolean;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface Club {
  id: string;
  countryId: string;
  countryName: string;
  name: string;
  code: string;
  description?: string;
  city?: string;
  logoUrl?: string;
  primaryColor?: string;
  secondaryColor?: string;
  isActive: boolean;
  memberCount: number;
  createdAt: string;
}

export interface Race {
  id: string;
  clubId: string;
  clubName: string;
  name: string;
  description?: string;
  status: RaceStatus;
  releaseLocation: string;
  releaseLongitude: number;
  releaseLatitude: number;
  scheduledReleaseTime?: string;
  actualReleaseTime?: string;
  windSpeedKmh?: number;
  windDirection?: string;
  temperatureCelsius?: number;
  totalPigeonsEntered?: number;
  isLiveTracking: boolean;
  publishedAt?: string;
  createdAt: string;
  categories: RaceCategory[];
}

export interface RaceCategory { id: string; name: string; description?: string; sortOrder: number; }

export interface RaceSummary {
  id: string;
  name: string;
  status: RaceStatus;
  scheduledReleaseTime?: string;
  actualReleaseTime?: string;
  totalPigeonsEntered?: number;
  clubName: string;
  clubId: string;
}

export interface RaceResult {
  id: string;
  raceId: string;
  raceName: string;
  categoryId?: string;
  categoryName?: string;
  userId?: string;
  fancierName?: string;
  ringNumber: string;
  pigeonName?: string;
  pigeonSex?: string;
  pigeonYearOfBirth?: number;
  arrivalTime: string;
  distanceKm: number;
  velocityMperMin: number;
  velocityKmH: number;
  clubRank?: number;
  categoryRank?: number;
  status: ResultStatus;
  isDuplicate: boolean;
  isLateArrival: boolean;
  hasInvalidTimestamp: boolean;
  validationNotes?: string;
  ingestionType: DataIngestionType;
}

export interface CountryResult {
  id: string;
  countryId: string;
  countryName: string;
  name: string;
  description?: string;
  status: CountryResultStatus;
  totalEntriesCount: number;
  totalClubsCount: number;
  publishedAt?: string;
  createdAt: string;
  topEntries: CountryResultEntry[];
}

export interface CountryResultEntry {
  id: string;
  nationalRank: number;
  nationalCategoryRank?: number;
  ringNumber: string;
  fancierName?: string;
  clubName: string;
  velocityMperMin: number;
  distanceKm: number;
}

export interface Notification {
  id: string;
  type: NotificationType;
  status: NotificationStatus;
  title: string;
  body: string;
  actionUrl?: string;
  createdAt: string;
  readAt?: string;
}

export interface Theme {
  id: SiteTheme;
  name: string;
  description: string;
  primaryColor: string;
  accentColor: string;
  backgroundColor: string;
  surfaceColor: string;
  textColor: string;
  previewImageUrl: string;
}

export interface ClubMember {
  membershipId: string;
  userId: string;
  fullName: string;
  email: string;
  role: UserRole;
  joinedAt: string;
  linkedPigeonCount: number;
}

export interface Invitation {
  id: string;
  email: string;
  status: InvitationStatus;
  expiresAt: string;
  createdAt: string;
  clubName: string;
}
