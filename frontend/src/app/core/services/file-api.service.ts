import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

interface ApiResponse<T> { success: boolean; data: T; message?: string; }
export interface UploadResult { url: string; }

/**
 * Client for the FileService user-facing upload endpoint.
 *
 *  - `uploadImage(file)` → POST /api/files/upload → public MinIO URL
 *  - `deleteImage(objectKey)` → DELETE /api/files?objectKey=…
 *
 * Backend handles the bucket/MIME/size validation. Returned URL is suitable
 * for `<img [src]>` directly — the public bucket allows anonymous reads.
 */
@Injectable({ providedIn: 'root' })
export class FileApiService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/files`;

  uploadImage(file: File): Observable<UploadResult> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<ApiResponse<UploadResult>>(`${this.base}/upload`, form)
      .pipe(map(r => r.data));
  }

  deleteImage(objectKey: string): Observable<void> {
    const params = new URLSearchParams({ objectKey });
    return this.http.delete<ApiResponse<unknown>>(`${this.base}?${params}`).pipe(map(() => undefined));
  }
}
