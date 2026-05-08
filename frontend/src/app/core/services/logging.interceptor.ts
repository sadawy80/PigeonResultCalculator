import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { tap, catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';
import { LoggerService } from './logger.service';

export const loggingInterceptor: HttpInterceptorFn = (req, next) => {
  const logger = inject(LoggerService);

  // Skip the log ingest endpoint itself to prevent infinite loops
  if (req.url.includes('/api/logs')) return next(req);

  const start = Date.now();

  return next(req).pipe(
    tap(event => {
      if (event instanceof HttpResponse) {
        const elapsed = Date.now() - start;
        logger.debug(
          `HTTP ${req.method} ${req.urlWithParams} → ${event.status} (${elapsed}ms)`,
          { method: req.method, url: req.urlWithParams, status: event.status, elapsedMs: elapsed },
          'Angular.Http'
        );
      }
    }),
    catchError(err => {
      const elapsed = Date.now() - start;
      const status = err.status ?? 0;
      logger.error(
        `HTTP ${req.method} ${req.urlWithParams} → ${status} (${elapsed}ms)`,
        err.message ?? String(err),
        { method: req.method, url: req.urlWithParams, status, elapsedMs: elapsed, body: err.error },
        'Angular.Http'
      );
      return throwError(() => err);
    })
  );
};
