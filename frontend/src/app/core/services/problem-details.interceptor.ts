import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

// Normalizes RFC 7807 Problem Details error responses so existing code
// reading err.error.message continues to work (detail → message backfill).
export const problemDetailsInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError(err => {
      if (err instanceof HttpErrorResponse) {
        const body = err.error;
        if (body && typeof body === 'object' && 'detail' in body && !('message' in body)) {
          const normalized = new HttpErrorResponse({
            error: { ...body, message: body.detail },
            headers: err.headers,
            status: err.status,
            statusText: err.statusText,
            url: err.url ?? undefined,
          });
          return throwError(() => normalized);
        }
      }
      return throwError(() => err);
    })
  );
