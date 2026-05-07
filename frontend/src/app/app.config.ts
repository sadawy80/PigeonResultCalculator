import { ApplicationConfig, provideZoneChangeDetection, APP_INITIALIZER } from '@angular/core';
import { provideRouter, withViewTransitions } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { jwtInterceptor } from './core/services/services';
import { TranslationService } from './core/i18n/translation.service';

// ── i18n initialiser — loads the saved/default locale before the app renders ──
function initI18n(i18n: TranslationService) {
  return () => i18n.init();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withViewTransitions()),
    provideHttpClient(withInterceptors([jwtInterceptor])),

    // Boot i18n before the first render
    {
      provide: APP_INITIALIZER,
      useFactory: initI18n,
      deps: [TranslationService],
      multi: true
    },
  ]
};
