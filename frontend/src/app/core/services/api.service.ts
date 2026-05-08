import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  ApiResponse, PagedResult, AuthTokens, User, Club, Race, RaceSummary,
  RaceResult, FederationResult, Notification, Theme, ClubMember, Invitation,
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
    role: number; FederationId?: string; invitationToken?: string;
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

  forgotPassword(email: string): Observable<void> {
    return this.post<void>('/auth/forgot-password', { email });
  }

  resetPassword(email: string, token: string, newPassword: string): Observable<void> {
    return this.post<void>('/auth/reset-password', { email, token, newPassword });
  }

  verifyEmail(userId: string, token: string): Observable<void> {
    return this.get<void>(`/auth/verify-email?userId=${encodeURIComponent(userId)}&token=${encodeURIComponent(token)}`);
  }

  resendVerification(email: string): Observable<void> {
    return this.post<void>('/auth/resend-verification', { email });
  }

  // ── Role Upgrade Requests ─────────────────────────────────────────────────

  submitUpgradeRequest(payload: {
    requestedRole: number; federationId?: string; notes?: string;
  }): Observable<any> {
    return this.post<any>('/auth/upgrade-request', payload);
  }

  getMyUpgradeRequests(): Observable<any[]> {
    return this.get<any[]>('/auth/upgrade-requests');
  }

  // Federation-scoped review (FederationManager + SuperAdmin via federation route)
  getFederationUpgradeRequests(params: {
    status?: number; page?: number; pageSize?: number;
  } = {}): Observable<any> {
    return this.get<any>('/federation/upgrade-requests', params);
  }

  approveFederationUpgradeRequest(requestId: string): Observable<any> {
    return this.post<any>(`/federation/upgrade-requests/${requestId}/approve`);
  }

  rejectFederationUpgradeRequest(requestId: string, reason?: string): Observable<any> {
    return this.post<any>(`/federation/upgrade-requests/${requestId}/reject`, { reason });
  }

  // Admin-scoped review (SuperAdmin via admin route)
  getAdminUpgradeRequests(params: {
    federationId?: string; status?: number; page?: number; pageSize?: number;
  } = {}): Observable<any> {
    return this.get<any>('/admin/upgrade-requests', params);
  }

  approveAdminUpgradeRequest(requestId: string): Observable<any> {
    return this.post<any>(`/admin/upgrade-requests/${requestId}/approve`);
  }

  rejectAdminUpgradeRequest(requestId: string, reason?: string): Observable<any> {
    return this.post<any>(`/admin/upgrade-requests/${requestId}/reject`, { reason });
  }

  // Public federation list (for upgrade request form dropdown)
  getPublicFederations(): Observable<{ id: string; name: string; code: string }[]> {
    return this.get<{ id: string; name: string; code: string }[]>('/federation');
  }

  // ── Clubs ─────────────────────────────────────────────────────────────────

  getClub(id: string): Observable<Club> {
    return this.get<Club>(`/clubs/${id}`);
  }

  createClub(payload: Partial<Club> & { FederationId: string }): Observable<Club> {
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

  getClubPageInfo(clubId: string): Observable<{
    slug: string; isPublished: boolean;
    logoUrl?: string; primaryColor?: string; secondaryColor?: string;
    announcementsJson?: string;
  }> {
    return this.get(`/clubs/${clubId}/page-info`);
  }

  updateClubSlug(clubId: string, newSlug: string): Observable<string> {
    return this.put<string>(`/clubs/${clubId}/slug`, { newSlug });
  }

  updateClubAnnouncements(clubId: string, announcementsJson: string): Observable<void> {
    return this.put<void>(`/clubs/${clubId}/announcements`, { announcementsJson });
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

  // ── federation results ───────────────────────────────────────────────────────

  createFederationResult(payload: { FederationId: string; name: string; description?: string; raceIds: string[] }): Observable<FederationResult> {
    return this.post<FederationResult>('/federation-results', payload);
  }

  publishFederationResult(countryResultId: string): Observable<FederationResult> {
    return this.post<FederationResult>(`/federation-results/${countryResultId}/publish`);
  }

  getFederationResult(id: string): Observable<FederationResult> {
    return this.get<FederationResult>(`/federation-results/${id}`);
  }

  getFederationResults(FederationId: string, page = 1, pageSize = 20): Observable<PagedResult<FederationResult>> {
    return this.get<PagedResult<FederationResult>>(`/federation-results/country/${FederationId}`, { page, pageSize });
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

  // ── Public pages ──────────────────────────────────────────────────────────

  getPublicPlans(): Observable<any[]> {
    return this.http.get<ApiResponse<any[]>>(`${this.base}/public/plans`)
      .pipe(map(r => r.data!));
  }

  getPublicClubPage(slug: string): Observable<any> {
    return this.http.get<any>(`${this.base}/public/clubs/${slug}`).pipe(map(r => r.data));
  }

  getPublicFederationPage(slug: string): Observable<any> {
    return this.http.get<any>(`${this.base}/public/countries/${slug}`).pipe(map(r => r.data));
  }

  listPublishedClubs(country?: string, page = 1, pageSize = 20): Observable<any> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (country) params = params.set('country', country);
    return this.http.get<any>(`${this.base}/public/clubs`, { params }).pipe(map(r => r.data));
  }

  // ── federation page management (FederationManager) ───────────────────────────────

  getMyCountryPage(): Observable<any> {
    return this.get<any>('/country/page');
  }

  updateMyCountryPage(payload: { theme?: number; isPublished?: boolean; announcementsJson?: string; headerHtml?: string }): Observable<any> {
    return this.put<any>('/country/page', payload);
  }

  // ── Admin ─────────────────────────────────────────────────────────────────

  adminGetStats(): Observable<any> {
    return this.get<any>('/admin/stats');
  }

  adminGetUsers(params: { search?: string; role?: string; page?: number; pageSize?: number }): Observable<PagedResult<any>> {
    return this.get<PagedResult<any>>('/admin/users', params);
  }

  adminToggleUser(userId: string): Observable<{ id: string; isActive: boolean }> {
    return this.put<{ id: string; isActive: boolean }>(`/admin/users/${userId}/toggle-active`);
  }

  adminAssignRole(userId: string, role: number, FederationId?: string): Observable<any> {
    return this.put<any>(`/admin/users/${userId}/assign-role`, { role, FederationId });
  }

  adminSetUserLimits(userId: string, maxResults: number | null, maxClubs: number | null): Observable<any> {
    return this.put<any>(`/admin/users/${userId}/limits`, { maxResults, maxClubs });
  }

  adminGetClubs(params: { search?: string; FederationId?: string; page?: number; pageSize?: number }): Observable<PagedResult<any>> {
    return this.get<PagedResult<any>>('/admin/clubs', params);
  }

  adminToggleClub(clubId: string): Observable<{ id: string; isActive: boolean }> {
    return this.put<{ id: string; isActive: boolean }>(`/admin/clubs/${clubId}/suspend`);
  }

  adminGetCountries(page = 1, pageSize = 50): Observable<PagedResult<any>> {
    return this.get<PagedResult<any>>('/admin/countries', { page, pageSize });
  }

  adminCreateCountry(name: string, code: string, slug: string): Observable<any> {
    return this.post<any>('/admin/countries', { name, code, slug });
  }

  adminToggleCountry(FederationId: string): Observable<any> {
    return this.put<any>(`/admin/countries/${FederationId}/toggle-active`);
  }

  adminGetSubscriptionPlans(): Observable<any[]> {
    return this.get<any[]>('/admin/subscription-plans');
  }

  adminGetSubscriptions(page = 1, pageSize = 20): Observable<PagedResult<any>> {
    return this.get<PagedResult<any>>('/admin/subscriptions', { page, pageSize });
  }

  adminCreateSubscription(FederationId: string, planId: string, billingCycle: number): Observable<any> {
    return this.post<any>('/admin/subscriptions', { FederationId, planId, billingCycle });
  }

  adminGetEvents(params: { eventType?: string; aggregateType?: string; page?: number; pageSize?: number }): Observable<PagedResult<any>> {
    return this.get<PagedResult<any>>('/admin/events', params);
  }
}
