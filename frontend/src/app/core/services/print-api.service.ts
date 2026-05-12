import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DesignInfo { id: string; name: string; arabic: boolean; }
export interface CertDesignCatalogue { portrait: DesignInfo[]; landscape: DesignInfo[]; }

export type CertType   = 'race' | 'ace' | 'super-ace' | 'best-loft';
export type ResultType = 'race' | 'ace' | 'super-ace' | 'best-loft';

@Injectable({ providedIn: 'root' })
export class PrintApiService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/print`;

  // ── Design catalogue ────────────────────────────────────────────────────
  getCertDesigns(type: CertType): Observable<CertDesignCatalogue> {
    return this.http.get<CertDesignCatalogue>(`${this.base}/designs/cert/${type}`);
  }
  getResultDesigns(type: ResultType): Observable<DesignInfo[]> {
    return this.http.get<DesignInfo[]>(`${this.base}/designs/result/${type}`);
  }

  // ── Certificates → PDF blob ─────────────────────────────────────────────
  renderRaceCert(body: { raceResultId: string; designId: string; language: string }): Observable<Blob> {
    return this.http.post(`${this.base}/cert/race`, body, { responseType: 'blob' });
  }
  renderAceCert(body: { programmeId: string; ringNumber: string; designId: string; language: string }): Observable<Blob> {
    return this.http.post(`${this.base}/cert/ace`, body, { responseType: 'blob' });
  }
  renderSuperAceCert(body: { programmeId: string; ringNumber: string; designId: string; language: string }): Observable<Blob> {
    return this.http.post(`${this.base}/cert/super-ace`, body, { responseType: 'blob' });
  }
  renderBestLoftCert(body: { programmeId: string; fancierUserId: string; designId: string; language: string }): Observable<Blob> {
    return this.http.post(`${this.base}/cert/best-loft`, body, { responseType: 'blob' });
  }

  // ── Result tables → PDF blob ────────────────────────────────────────────
  renderRaceResultsPdf(body: { raceId: string; designId: string; language: string }): Observable<Blob> {
    return this.http.post(`${this.base}/result/race/pdf`, body, { responseType: 'blob' });
  }
  renderAceResultsPdf(body: { programmeId: string; designId: string; language: string }): Observable<Blob> {
    return this.http.post(`${this.base}/result/ace/pdf`, body, { responseType: 'blob' });
  }
  renderSuperAceResultsPdf(body: { programmeId: string; designId: string; language: string }): Observable<Blob> {
    return this.http.post(`${this.base}/result/super-ace/pdf`, body, { responseType: 'blob' });
  }
  renderBestLoftResultsPdf(body: { programmeId: string; designId: string; language: string }): Observable<Blob> {
    return this.http.post(`${this.base}/result/best-loft/pdf`, body, { responseType: 'blob' });
  }

  // ── Result tables → Excel blob ──────────────────────────────────────────
  renderRaceResultsExcel(body: { raceId: string; language: string }): Observable<Blob> {
    return this.http.post(`${this.base}/result/race/excel`, body, { responseType: 'blob' });
  }
  renderAceResultsExcel(body: { programmeId: string; language: string }): Observable<Blob> {
    return this.http.post(`${this.base}/result/ace/excel`, body, { responseType: 'blob' });
  }
  renderSuperAceResultsExcel(body: { programmeId: string; language: string }): Observable<Blob> {
    return this.http.post(`${this.base}/result/super-ace/excel`, body, { responseType: 'blob' });
  }
  renderBestLoftResultsExcel(body: { programmeId: string; language: string }): Observable<Blob> {
    return this.http.post(`${this.base}/result/best-loft/excel`, body, { responseType: 'blob' });
  }

  /** Trigger a browser download from a Blob. */
  download(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
  }
}
