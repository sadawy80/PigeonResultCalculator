import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  ExternalLink, ExternalLinkStatus,
  IntegrationRaceResult, IntegrationAcePigeon,
  IntegrationSuperAce, IntegrationBestLoft, IntegrationSummary
} from '../models/integration.models';

interface ApiResponse<T> { success: boolean; data: T; message?: string; }
interface PagedResult<T> { items: T[]; totalCount: number; page: number; pageSize: number; totalPages: number; }

@Injectable({ providedIn: 'root' })
export class IntegrationApiService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/integrations`;

  // ── Club Manager endpoints ─────────────────────────────────────────────────

  getClubLinks(clubId: string, status?: ExternalLinkStatus): Observable<ExternalLink[]> {
    let params = new HttpParams();
    if (status != null) params = params.set('status', status.toString());
    return this.http.get<ApiResponse<ExternalLink[]>>(
      `${this.base}/club/${clubId}/links`, { params }
    ).pipe(map(r => r.data));
  }

  approveLink(linkId: string): Observable<ExternalLink> {
    return this.http.post<ApiResponse<ExternalLink>>(
      `${this.base}/link/${linkId}/approve`, {}
    ).pipe(map(r => r.data));
  }

  rejectLink(linkId: string, reason?: string): Observable<ExternalLink> {
    return this.http.post<ApiResponse<ExternalLink>>(
      `${this.base}/link/${linkId}/reject`, { reason }
    ).pipe(map(r => r.data));
  }

  revokeLink(linkId: string, reason?: string): Observable<void> {
    return this.http.delete<ApiResponse<void>>(
      `${this.base}/link/${linkId}`, { body: { reason } }
    ).pipe(map(r => r.data));
  }

  // ── Fancier endpoints ──────────────────────────────────────────────────────

  getMyLinks(): Observable<ExternalLink[]> {
    return this.http.get<ApiResponse<ExternalLink[]>>(`${this.base}/my-links`)
      .pipe(map(r => r.data));
  }

  revokeMyLink(linkId: string): Observable<void> {
    return this.http.delete<ApiResponse<void>>(`${this.base}/my-links/${linkId}`)
      .pipe(map(r => r.data));
  }

  // ── Fancier JWT-authenticated self-data ────────────────────────────────────

  getMySummary(linkId: string): Observable<IntegrationSummary> {
    return this.http.get<ApiResponse<IntegrationSummary>>(
      `${this.base}/my-links/${linkId}/summary`
    ).pipe(map(r => r.data));
  }

  getMyResults(linkId: string, page = 1, pageSize = 50): Observable<PagedResult<IntegrationRaceResult>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedResult<IntegrationRaceResult>>>(
      `${this.base}/my-links/${linkId}/results`, { params }
    ).pipe(map(r => r.data));
  }

  getMyAce(linkId: string): Observable<IntegrationAcePigeon[]> {
    return this.http.get<ApiResponse<IntegrationAcePigeon[]>>(
      `${this.base}/my-links/${linkId}/ace-pigeon`
    ).pipe(map(r => r.data));
  }

  getMySuperAce(linkId: string): Observable<IntegrationSuperAce[]> {
    return this.http.get<ApiResponse<IntegrationSuperAce[]>>(
      `${this.base}/my-links/${linkId}/super-ace`
    ).pipe(map(r => r.data));
  }

  getMyBestLoft(linkId: string): Observable<IntegrationBestLoft[]> {
    return this.http.get<ApiResponse<IntegrationBestLoft[]>>(
      `${this.base}/my-links/${linkId}/best-loft`
    ).pipe(map(r => r.data));
  }

  getResults(token: string, page = 1, pageSize = 50): Observable<PagedResult<IntegrationRaceResult>> {
    const params = new HttpParams().set('token', token).set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedResult<IntegrationRaceResult>>>(
      `${this.base}/data/results`, { params }
    ).pipe(map(r => r.data));
  }

  getAcePigeon(token: string): Observable<IntegrationAcePigeon[]> {
    return this.http.get<ApiResponse<IntegrationAcePigeon[]>>(
      `${this.base}/data/ace-pigeon`, { params: new HttpParams().set('token', token) }
    ).pipe(map(r => r.data));
  }

  getSuperAce(token: string): Observable<IntegrationSuperAce[]> {
    return this.http.get<ApiResponse<IntegrationSuperAce[]>>(
      `${this.base}/data/super-ace`, { params: new HttpParams().set('token', token) }
    ).pipe(map(r => r.data));
  }

  getBestLoft(token: string): Observable<IntegrationBestLoft[]> {
    return this.http.get<ApiResponse<IntegrationBestLoft[]>>(
      `${this.base}/data/best-loft`, { params: new HttpParams().set('token', token) }
    ).pipe(map(r => r.data));
  }

  getSummary(token: string): Observable<IntegrationSummary> {
    return this.http.get<ApiResponse<IntegrationSummary>>(
      `${this.base}/data/summary`, { params: new HttpParams().set('token', token) }
    ).pipe(map(r => r.data));
  }
}
