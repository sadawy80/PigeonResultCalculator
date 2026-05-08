import { ErrorHandler, inject, Injectable } from '@angular/core';
import { LoggerService } from './logger.service';

@Injectable()
export class AppErrorHandler implements ErrorHandler {
  private logger = inject(LoggerService);

  handleError(error: unknown): void {
    const err = error as Error;
    const message = err?.message ?? String(error);
    const stack = err?.stack ?? '';

    this.logger.error(
      `Unhandled Angular error: ${message}`,
      stack,
      { errorType: err?.name ?? 'Error' },
      'Angular.ErrorHandler'
    );

    // Re-throw so the default handler still logs to the browser console
    console.error(error);
  }
}
