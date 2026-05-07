import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  PrintTemplate, PrintJob, RenderTemplateRequest,
  TemplateCategory, TemplateStyle
} from '../models/template.models';
import { TranslationService } from '../i18n/translation.service';

interface ApiResponse<T> { success: boolean; data: T; message?: string; }
interface PagedResult<T> { items: T[]; totalCount: number; page: number; pageSize: number; totalPages: number; }

@Injectable({ providedIn: 'root' })
export class TemplateApiService {
  private http = inject(HttpClient);
  private i18n = inject(TranslationService);
  private base = `${environment.apiUrl}/templates`;

  /** Get all templates, optionally filtered by category/style */
  getTemplates(category?: TemplateCategory, style?: TemplateStyle): Observable<PrintTemplate[]> {
    let params = new HttpParams();
    if (category != null) params = params.set('category', category.toString());
    if (style != null)    params = params.set('style', style.toString());
    return this.http.get<ApiResponse<PrintTemplate[]>>(this.base, { params })
      .pipe(map(r => r.data));
  }

  /** Get a single template's metadata */
  getTemplate(id: string): Observable<PrintTemplate> {
    return this.http.get<ApiResponse<PrintTemplate>>(`${this.base}/${id}`)
      .pipe(map(r => r.data));
  }

  /** Render a template and return the substituted HTML string */
  renderTemplate(templateId: string, req: Omit<RenderTemplateRequest, 'templateId'>): Observable<string> {
    return this.http.post(
      `${this.base}/${templateId}/render`,
      { ...req, templateId },
      { responseType: 'text' }
    );
  }

  /** Build a URL for the browser-print endpoint (opens in new tab, auto-prints) */
  buildPrintUrl(templateId: string, req: Omit<RenderTemplateRequest, 'templateId'>): string {
    const params = new URLSearchParams();
    params.set('category', req.category.toString());
    params.set('locale', this.i18n.locale());        // inject current UI locale
    if (req.raceId)       params.set('raceId', req.raceId);
    if (req.programmeId)  params.set('programmeId', req.programmeId);
    if (req.raceResultId) params.set('raceResultId', req.raceResultId);
    if (req.certificateRecipientName) params.set('recipientName', req.certificateRecipientName);
    if (req.certificateRank)          params.set('rank', req.certificateRank);
    return `${environment.apiUrl}/templates/${templateId}/print?${params.toString()}`;
  }

  /** Create a print job audit record */
  createPrintJob(req: RenderTemplateRequest): Observable<PrintJob> {
    return this.http.post<ApiResponse<PrintJob>>(`${this.base}/jobs`, req)
      .pipe(map(r => r.data));
  }

  /** Get print job history for a club */
  getPrintJobs(clubId: string, page = 1, pageSize = 20): Observable<PagedResult<PrintJob>> {
    return this.http.get<ApiResponse<PagedResult<PrintJob>>>(
      `${this.base}/jobs/club/${clubId}`,
      { params: new HttpParams().set('page', page).set('pageSize', pageSize) }
    ).pipe(map(r => r.data));
  }
}
