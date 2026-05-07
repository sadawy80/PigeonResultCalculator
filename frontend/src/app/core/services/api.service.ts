import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  ApiResponse, PagedResult, AuthTokens, User, Club, Race, RaceSummary,
  RaceResult, CountryResult, Notification, Theme, ClubMember, Invitation,
  SiteTheme
} from '../models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  private get<T>(path: string, params?: Record<string, any>): Observable<T> {
    let httpParams = new HttpParams();
    if (params) Object.entries(params).filter(([, v]) => v != null)
      .forEach(([k, v]) => httpParams = httpParams.set(k, String(v)));
    return this.http.get<ApiResponse<T>>(`${this.base}${path}`, { params: httpParams })
      .pipe(map(r => r.data!));
  }

  private post<T>(path: string, body?: any): Observable<T> {
    return this.http.post<ApiResponse<T>>(`${this.base}${path}`, body)
      .pipe(map(r => r.data!));
  }

  private put<T>(path: string, body?: any): Observable<T> {
    return this.http.put<ApiResponse<T>>(`${this.base}${path}`, body)
      .pipe(map(r => r.data!));
  }

  private delete<T>(path: string): Observable<T> {
    return this.http.delete<ApiResponse<T>>(`${this.base}${path}`)
      .pipe(map(r => r.data!));
  }

  // ── Auth ──────────────────────────────────────────────────────────────────

  login(email: string, password: string): Observable<AuthTokens> {
    return this.post<AuthTokens>('/auth/login', { email, password });
  }

  register(payload: {
    email: string; password: string; firstName: string; lastName: string;
    role: number; countryId?: string; invitationToken?: string;
  }): Observable<AuthTokens> {
    return this.post<AuthTokens>('/auth/register', payload);
  }

  refreshToken(accessToken: string, refreshToken: string): Observable<AuthTokens> {
    return this.post<AuthTokens>('/auth/refresh', { accessToken, refreshToken });
  }

  revokeToken(refreshToken: string): Observable<void> {
    return this.post<void>('/auth/revoke', { refreshToken });
  }

  getMe(): Observable<User> {
    return this.get<User>('/auth/me');
  }

  changePassword(userId: string, currentPassword: string, newPassword: string): Observable<void> {
    return this.post<void>('/auth/change-password', { userId, currentPassword, newPassword });
  }

  // ── Clubs ─────────────────────────────────────────────────────────────────

  getClub(id: string): Observable<Club> {
    return this.get<Club>(`/clubs/${id}`);
  }

  createClub(payload: Partial<Club> & { countryId: string }): Observable<Club> {
    return this.post<Club>('/clubs', payload);
  }

  updateClubBranding(clubId: string, payload: {
    logoUrl?: string; primaryColor?: string; secondaryColor?: string; theme: SiteTheme;
  }): Observable<void> {
    return this.put<void>(`/clubs/${clubId}/branding`, payload);
  }

  setClubTheme(clubId: string, theme: SiteTheme): Observable<void> {
    return this.put<void>(`/clubs/${clubId}/theme`, { theme });
  }

  getClubMembers(clubId: string, page = 1, pageSize = 20, search?: string): Observable<PagedResult<ClubMember>> {
    return this.get<PagedResult<ClubMember>>(`/clubs/${clubId}/members`, { page, pageSize, search });
  }

  inviteMember(clubId: string, email: string): Observable<Invitation> {
    return this.post<Invitation>(`/clubs/${clubId}/invite`, { email });
  }

  getClubInvitations(clubId: string): Observable<Invitation[]> {
    return this.get<Invitation[]>(`/clubs/${clubId}/invitations`);
  }

  removeMember(clubId: string, userId: string): Observable<void> {
    return this.delete<void>(`/clubs/${clubId}/members/${userId}`);
  }

  linkPigeon(membershipId: string, ringNumber: string): Observable<void> {
    return this.post<void>(`/clubs/memberships/${membershipId}/link-pigeon`, { ringNumber });
  }

  getClubPageInfo(clubId: string): Observable<{ slug: string; isPublished: boolean }> {
    return this.get<{ slug: string; isPublished: boolean }>(`/clubs/${clubId}/page-info`);
  }

  updateClubSlug(clubId: string, newSlug: string): Observable<string> {
    return this.put<string>(`/clubs/${clubId}/slug`, { newSlug });
  }

  // ── Races ─────────────────────────────────────────────────────────────────

  createRace(payload: Partial<Race> & { clubId: string; categories: any[] }): Observable<Race> {
    return this.post<Race>('/races', payload);
  }

  updateRace(raceId: string, payload: Partial<Race>): Observable<Race> {
    return this.put<Race>(`/races/${raceId}`, payload);
  }

  getRace(id: string): Observable<Race> {
    return this.get<Race>(`/races/${id}`);
  }

  getClubRaces(clubId: string, page = 1, pageSize = 20, search?: string): Observable<PagedResult<RaceSummary>> {
    return this.get<PagedResult<RaceSummary>>(`/races/club/${clubId}`, { page, pageSize, search });
  }

  getLiveRaces(clubId: string): Observable<RaceSummary[]> {
    return this.get<RaceSummary[]>(`/races/club/${clubId}/live`);
  }

  startRace(raceId: string, actualReleaseTime: string): Observable<Race> {
    return this.post<Race>(`/races/${raceId}/start`, { raceId, actualReleaseTime });
  }

  completeRace(raceId: string): Observable<Race> {
    return this.post<Race>(`/races/${raceId}/complete`);
  }

  publishRace(raceId: string): Observable<Race> {
    return this.post<Race>(`/races/${raceId}/publish`);
  }

  deleteRace(raceId: string): Observable<void> {
    return this.delete<void>(`/races/${raceId}`);
  }

  // ── Results ───────────────────────────────────────────────────────────────

  addManualResult(payload: {
    raceId: string; categoryId?: string; ringNumber: string;
    arrivalTime: string; pigeonName?: string; pigeonSex?: string; pigeonYearOfBirth?: number;
  }): Observable<RaceResult> {
    return this.post<RaceResult>('/results/manual', payload);
  }

  ingestETSFile(raceId: string, categoryId: string | null, file: File): Observable<any> {
    const form = new FormData();
    form.append('raceId', raceId);
    if (categoryId) form.append('categoryId', categoryId);
    form.append('file', file);
    return this.http.post<ApiResponse<any>>(`${this.base}/results/ingest-ets`, form)
      .pipe(map(r => r.data!));
  }

  processResults(raceId: string): Observable<any> {
    return this.post<any>(`/results/${raceId}/process`);
  }

  getRaceResults(raceId: string, page = 1, pageSize = 50, categoryId?: string, search?: string): Observable<PagedResult<RaceResult>> {
    return this.get<PagedResult<RaceResult>>(`/results/race/${raceId}`, { page, pageSize, categoryId, search });
  }

  getFancierResults(userId: string, page = 1, pageSize = 20): Observable<PagedResult<RaceResult>> {
    return this.get<PagedResult<RaceResult>>(`/results/fancier/${userId}`, { page, pageSize });
  }

  linkResultToFancier(resultId: string, userId: string): Observable<RaceResult> {
    return this.put<RaceResult>(`/results/${resultId}/link-fancier`, { userId });
  }

  deleteResult(resultId: string): Observable<void> {
    return this.delete<void>(`/results/${resultId}`);
  }

  getIngestionLogs(raceId: string): Observable<any[]> {
    return this.get<any[]>(`/results/race/${raceId}/ingestion-logs`);
  }

  // ── Country Results ───────────────────────────────────────────────────────

  createCountryResult(payload: { countryId: string; name: string; description?: string; raceIds: string[] }): Observable<CountryResult> {
    return this.post<CountryResult>('/country-results', payload);
  }

  publishCountryResult(countryResultId: string): Observable<CountryResult> {
    return this.post<CountryResult>(`/country-results/${countryResultId}/publish`);
  }

  getCountryResult(id: string): Observable<CountryResult> {
    return this.get<CountryResult>(`/country-results/${id}`);
  }

  getCountryResults(countryId: string, page = 1, pageSize = 20): Observable<PagedResult<CountryResult>> {
    return this.get<PagedResult<CountryResult>>(`/country-results/country/${countryId}`, { page, pageSize });
  }

  // ── Themes ────────────────────────────────────────────────────────────────

  getThemes(): Observable<Theme[]> {
    return this.get<Theme[]>('/themes');
  }

  // ── Notifications ─────────────────────────────────────────────────────────

  getNotifications(page = 1, pageSize = 20): Observable<PagedResult<Notification>> {
    return this.get<PagedResult<Notification>>('/notifications', { page, pageSize });
  }

  markNotificationRead(notificationId: string): Observable<void> {
    return this.post<void>(`/notifications/${notificationId}/read`);
  }
}
