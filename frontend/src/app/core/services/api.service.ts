import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  ApiResponse, PagedResult, AuthTokens, AdminAuthTokens, User, Club, Race, RaceSummary,
  RaceResult, FederationResult, Theme, ClubMember, Invitation,
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

  adminLogin(email: string, password: string): Observable<AdminAuthTokens> {
    return this.post<AdminAuthTokens>('/admin/auth/login', { email, password });
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

  updateProfile(firstName: string, lastName: string): Observable<User> {
    return this.put<User>('/auth/profile', { firstName, lastName });
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
    requestedRole: number; federationId?: string; clubName?: string; notes?: string;
  }): Observable<any> {
    return this.post<any>('/auth/upgrade-request', payload);
  }

  getMyUpgradeRequests(): Observable<any[]> {
    return this.get<any[]>('/auth/upgrade-requests');
  }

  sendUpgradeReminder(requestId: string): Observable<any> {
    return this.post<any>(`/auth/upgrade-request/${requestId}/remind`);
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

  revokeFederationUpgradeRequest(requestId: string): Observable<any> {
    return this.post<any>(`/federation/upgrade-requests/${requestId}/revoke`);
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

  revokeAdminUpgradeRequest(requestId: string): Observable<any> {
    return this.post<any>(`/admin/upgrade-requests/${requestId}/revoke`);
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

  getClubRaces(clubId: string, page = 1, pageSize = 20, search?: string, year?: number): Observable<PagedResult<RaceSummary>> {
    return this.get<PagedResult<RaceSummary>>(`/races/club/${clubId}`, { page, pageSize, search, year });
  }

  getClubStats(clubId: string): Observable<{ totalProgrammes: number; programmesThisYear: number; totalMembers: number }> {
    return this.get(`/clubs/${clubId}/stats`);
  }

  getFederationStats(): Observable<{ totalResults: number; resultsThisYear: number }> {
    return this.get('/federation/stats');
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

  publishFederationResult(federationResultId: string): Observable<FederationResult> {
    return this.post<FederationResult>(`/federation-results/${federationResultId}/publish`);
  }

  getFederationResult(id: string): Observable<FederationResult> {
    return this.get<FederationResult>(`/federation-results/${id}`);
  }

  getFederationResults(FederationId: string, page = 1, pageSize = 20): Observable<PagedResult<FederationResult>> {
    return this.get<PagedResult<FederationResult>>(`/federation-results/federation/${FederationId}`, { page, pageSize });
  }

  // ── Themes ────────────────────────────────────────────────────────────────

  getThemes(): Observable<Theme[]> {
    return this.get<Theme[]>('/themes');
  }

  // ── Notifications ─────────────────────────────────────────────────────────

  getNotifications(page = 1, pageSize = 20, unreadOnly = false): Observable<any> {
    const params: Record<string, any> = { page, pageSize };
    if (unreadOnly) params['unreadOnly'] = true;
    return this.get<any>('/notifications', params);
  }

  markNotificationRead(notificationId: string): Observable<void> {
    return this.post<void>(`/notifications/${notificationId}/read`);
  }

  markAllNotificationsRead(): Observable<void> {
    return this.post<void>('/notifications/read-all');
  }

  dismissNotification(notificationId: string): Observable<void> {
    return this.delete<void>(`/notifications/${notificationId}`);
  }

  dismissAllNotifications(): Observable<void> {
    return this.delete<void>('/notifications');
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

  getMyFederationPage(): Observable<any> {
    return this.get<any>('/federation/page');
  }

  updateMyFederationPage(payload: { theme?: number; isPublished?: boolean; announcementsJson?: string; headerHtml?: string }): Observable<any> {
    return this.put<any>('/federation/page', payload);
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

  adminDeleteUser(userId: string): Observable<any> {
    return this.delete<any>(`/admin/users/${userId}`);
  }

  adminGetFanciers(params: { search?: string; clubId?: string; federationId?: string; isLinked?: boolean; page?: number; pageSize?: number } = {}): Observable<any> {
    return this.get<any>('/admin/fanciers', params);
  }

  adminLinkFancier(fancierId: string, userId: string, userName: string, userEmail: string): Observable<any> {
    return this.post<any>(`/admin/fanciers/${fancierId}/link`, { userId, userName, userEmail });
  }

  adminUnlinkFancier(fancierId: string): Observable<any> {
    return this.delete<any>(`/admin/fanciers/${fancierId}/link`);
  }

  adminGetLinkRequests(params: { status?: number; page?: number; pageSize?: number } = {}): Observable<any> {
    return this.get<any>('/admin/link-requests', params);
  }

  adminApproveLinkRequest(linkId: string): Observable<any> {
    return this.post<any>(`/admin/link-requests/${linkId}/approve`);
  }

  adminRejectLinkRequest(linkId: string, reason?: string): Observable<any> {
    return this.post<any>(`/admin/link-requests/${linkId}/reject`, { reason });
  }

  adminRevokeLinkRequest(linkId: string): Observable<any> {
    return this.delete<any>(`/admin/link-requests/${linkId}`);
  }

  adminGetClubs(params: { search?: string; FederationId?: string; page?: number; pageSize?: number }): Observable<PagedResult<any>> {
    return this.get<PagedResult<any>>('/admin/clubs', params);
  }

  adminToggleClub(clubId: string): Observable<{ id: string; isActive: boolean }> {
    return this.put<{ id: string; isActive: boolean }>(`/admin/clubs/${clubId}/suspend`);
  }

  adminSetClubSubscriptionExpiry(clubId: string, expiresAt: string | null): Observable<any> {
    return this.put<any>(`/admin/clubs/${clubId}/subscription-expiry`, { expiresAt });
  }

  adminGetFederations(page = 1, pageSize = 50): Observable<PagedResult<any>> {
    return this.get<PagedResult<any>>('/admin/federations', { page, pageSize });
  }

  adminCreateFederation(name: string, code: string, slug: string): Observable<any> {
    return this.post<any>('/admin/federations', { name, code, slug });
  }

  adminToggleFederation(FederationId: string): Observable<any> {
    return this.put<any>(`/admin/federations/${FederationId}/toggle-active`);
  }

  adminAssignFederationManager(federationId: string, email: string): Observable<any> {
    return this.put<any>(`/admin/federations/${federationId}/assign-manager`, { email });
  }

  adminDeleteFederation(federationId: string): Observable<any> {
    return this.delete<any>(`/admin/federations/${federationId}`);
  }

  adminCreateClub(body: { federationId?: string; name: string; code: string; city?: string }): Observable<any> {
    return this.post<any>('/admin/clubs', body);
  }

  adminAssignClubManager(clubId: string, email: string, force = false): Observable<any> {
    return this.put<any>(`/admin/clubs/${clubId}/assign-manager`, { email, force });
  }

  adminDeleteClub(clubId: string): Observable<any> {
    return this.delete<any>(`/admin/clubs/${clubId}`);
  }

  adminUpdateSubscriptionPlan(planId: string, body: {
    name: string; description: string | null; price: number;
    maxClubs: number; maxResultsPerClub: number;
    isActive: boolean; isHighlighted: boolean; sortOrder: number; features: string | null;
  }): Observable<any> {
    return this.put<any>(`/admin/subscription-plans/${planId}`, body);
  }

  adminGetSubscriptionPlans(): Observable<any[]> {
    return this.get<{ plans: any[] }>('/admin/subscription-plans')
      .pipe(map(r => r?.plans ?? []));
  }

  adminGetSubscriptions(params: { page?: number; pageSize?: number; search?: string; billingCycle?: string; dateFrom?: string; dateTo?: string } = {}): Observable<PagedResult<any>> {
    return this.get<PagedResult<any>>('/admin/subscriptions', params);
  }

  adminCreateSubscription(payload: {
    federationId: string; federationName: string;
    planId: string; billingCycle: string;
    amountPaid: number; paymentReference?: string | null; notes?: string | null;
  }): Observable<any> {
    return this.post<any>('/admin/subscriptions', payload);
  }

  adminGetNotifications(params: { unreadOnly?: boolean; page?: number; pageSize?: number } = {}): Observable<any> {
    return this.get<any>('/admin/notifications', params);
  }

  adminMarkNotificationRead(notificationId: string): Observable<any> {
    return this.put<any>(`/admin/notifications/${notificationId}/read`);
  }

  adminMarkAllNotificationsRead(): Observable<any> {
    return this.put<any>('/admin/notifications/read-all');
  }

  adminDismissNotification(notificationId: string): Observable<any> {
    return this.delete<any>(`/admin/notifications/${notificationId}`);
  }

  adminDismissAllNotifications(): Observable<any> {
    return this.delete<any>('/admin/notifications');
  }

  adminCreateSubscriptionPlan(body: {
    name: string; description?: string | null; type: string; billingCycle: string;
    price: number; currency?: string; maxClubs: number; maxResultsPerClub: number;
    isHighlighted: boolean; sortOrder: number; features?: string | null;
  }): Observable<any> {
    return this.post<any>('/admin/subscription-plans', body);
  }

  adminDeleteSubscriptionPlan(planId: string): Observable<any> {
    return this.delete<any>(`/admin/subscription-plans/${planId}`);
  }

  adminGetEvents(params: { action?: string; entityType?: string; page?: number; pageSize?: number }): Observable<PagedResult<any>> {
    return this.get<PagedResult<any>>('/admin/events', params);
  }

  adminGetRaces(params: { search?: string; clubId?: string; status?: number; dateFrom?: string; dateTo?: string; page?: number; pageSize?: number }): Observable<any> {
    return this.get<any>('/admin/races', params);
  }

  adminDeleteRace(raceId: string): Observable<any> {
    return this.delete<any>(`/admin/races/${raceId}`);
  }

  adminGetProgrammes(params: { search?: string; clubId?: string; page?: number; pageSize?: number }): Observable<any> {
    return this.get<any>('/admin/programmes', params);
  }

  adminGetAceResults(params: { search?: string; clubId?: string; programmeId?: string; page?: number; pageSize?: number }): Observable<any> {
    return this.get<any>('/admin/results/ace', params);
  }

  adminGetSuperAceResults(params: { search?: string; clubId?: string; programmeId?: string; page?: number; pageSize?: number }): Observable<any> {
    return this.get<any>('/admin/results/super-ace', params);
  }

  adminGetBestLoftResults(params: { search?: string; clubId?: string; programmeId?: string; page?: number; pageSize?: number }): Observable<any> {
    return this.get<any>('/admin/results/best-loft', params);
  }

  adminDeleteProgramme(programmeId: string): Observable<any> {
    return this.delete<any>(`/admin/programmes/${programmeId}`);
  }

  adminGetPigeons(params: { search?: string; federationId?: string; clubId?: string; fancierSearch?: string; page?: number; pageSize?: number }): Observable<any> {
    return this.get<any>('/admin/pigeons', params);
  }

  adminUpdatePigeon(pigeonId: string, body: { name?: string; sex?: string; yearOfBirth?: number; color?: string }): Observable<any> {
    return this.put<any>(`/admin/pigeons/${pigeonId}`, body);
  }

  adminDeletePigeon(pigeonId: string): Observable<any> {
    return this.delete<any>(`/admin/pigeons/${pigeonId}`);
  }

  // ── Contact Us ────────────────────────────────────────────────────────────

  submitContactMessage(body: { name: string; email: string; phone?: string; subject: string; body: string }): Observable<{ id: string }> {
    return this.post<{ id: string }>('/contact', body);
  }

  adminListContactMessages(params: { status?: string; search?: string; page?: number; pageSize?: number }): Observable<any> {
    return this.get<any>('/admin/contact', params);
  }

  adminGetContactMessage(id: string): Observable<any> {
    return this.get<any>(`/admin/contact/${id}`);
  }

  adminReplyContactMessage(id: string, reply: string): Observable<any> {
    return this.post<any>(`/admin/contact/${id}/reply`, { reply });
  }

  adminCloseContactMessage(id: string): Observable<any> {
    return this.post<any>(`/admin/contact/${id}/close`, {});
  }

  adminDeleteContactMessage(id: string): Observable<any> {
    return this.delete<any>(`/admin/contact/${id}`);
  }
}
