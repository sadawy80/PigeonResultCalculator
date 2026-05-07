import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models';
import {
  Programme, ProgrammeSummary,
  BestLoftResult, AcePigeonResult, SuperAcePigeonResult,
  CalculationSummary, ScoringMethod, SuperAceQualification
} from '../models/programme.models';

@Injectable({ providedIn: 'root' })
export class ProgrammeApiService {
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

  // ── Programmes ────────────────────────────────────────────────────────────

  createProgramme(payload: {
    clubId: string; name: string; description?: string; year: number;
    startDate?: string; endDate?: string;
    scoringMethod: ScoringMethod; pointsForFirst: number; maxPointPositions: number;
    bestLoftPigeonsPerRace: number; bestLoftMinRaces: number;
    acePigeonMinRaces: number;
    superAceQualification: SuperAceQualification;
    superAceMinRaceCount: number; superAceMinRacePercentage: number;
  }): Observable<Programme> {
    return this.post<Programme>('/programmes', payload);
  }

  updateProgramme(id: string, payload: Partial<Programme>): Observable<Programme> {
    return this.put<Programme>(`/programmes/${id}`, payload);
  }

  getProgramme(id: string): Observable<Programme> {
    return this.get<Programme>(`/programmes/${id}`);
  }

  getClubProgrammes(clubId: string, page = 1, pageSize = 20): Observable<PagedResult<ProgrammeSummary>> {
    return this.get<PagedResult<ProgrammeSummary>>(`/programmes/club/${clubId}`, { page, pageSize });
  }

  deleteProgramme(id: string): Observable<void> {
    return this.delete<void>(`/programmes/${id}`);
  }

  // ── Race membership ───────────────────────────────────────────────────────

  addRaceToProgramme(programmeId: string, raceId: string, scoreWeight = 1.0, sortOrder = 0): Observable<Programme> {
    return this.post<Programme>(`/programmes/${programmeId}/races`, { raceId, scoreWeight, sortOrder });
  }

  removeRaceFromProgramme(programmeId: string, raceId: string): Observable<void> {
    return this.delete<void>(`/programmes/${programmeId}/races/${raceId}`);
  }

  // ── Calculation & publishing ──────────────────────────────────────────────

  calculateResults(programmeId: string): Observable<CalculationSummary> {
    return this.post<CalculationSummary>(`/programmes/${programmeId}/calculate`);
  }

  publishProgramme(programmeId: string): Observable<Programme> {
    return this.post<Programme>(`/programmes/${programmeId}/publish`);
  }

  // ── Best Loft Results ─────────────────────────────────────────────────────

  getBestLoftResults(programmeId: string, page = 1, pageSize = 50, search?: string): Observable<PagedResult<BestLoftResult>> {
    return this.get<PagedResult<BestLoftResult>>(`/best-loft/programme/${programmeId}`, { page, pageSize, search });
  }

  // ── Ace Pigeon Results ────────────────────────────────────────────────────

  getAcePigeonResults(programmeId: string, page = 1, pageSize = 50, search?: string): Observable<PagedResult<AcePigeonResult>> {
    return this.get<PagedResult<AcePigeonResult>>(`/ace-pigeon/programme/${programmeId}`, { page, pageSize, search });
  }

  // ── Super Ace Pigeon Results ──────────────────────────────────────────────

  getSuperAcePigeonResults(programmeId: string, page = 1, pageSize = 50, search?: string): Observable<PagedResult<SuperAcePigeonResult>> {
    return this.get<PagedResult<SuperAcePigeonResult>>(`/super-ace-pigeon/programme/${programmeId}`, { page, pageSize, search });
  }
}
