import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd, NavigationError } from '@angular/router';
import { ThemeService } from './core/services/services';
import { LoggerService } from './core/services/logger.service';
import { ToasterComponent } from './shared/components/toaster.component';
import { ModalHostComponent } from './shared/components/modal-host.component';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ToasterComponent, ModalHostComponent],
  template: `<router-outlet /><app-toaster /><app-modal-host />`,
  styles: [`
    :host { display: block; min-height: 100vh; background: var(--pr-bg); color: var(--pr-text); }
  `]
})
export class AppComponent implements OnInit, OnDestroy {
  private theme  = inject(ThemeService);
  private logger = inject(LoggerService);
  private router = inject(Router);
  private subs   = new Subscription();

  ngOnInit() {
    this.theme.loadSavedTheme();
    this.theme.loadThemesFromApi();

    this.logger.info('App session started', {
      referrer:   document.referrer || null,
      language:   navigator.language,
      cookiesEnabled: navigator.cookieEnabled,
      timezone:   Intl.DateTimeFormat().resolvedOptions().timeZone
    }, 'Angular.App');

    this.subs.add(
      this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe(e => {
        const nav = e as NavigationEnd;
        this.logger.info('Navigation', { url: nav.urlAfterRedirects }, 'Angular.Router');
      })
    );

    this.subs.add(
      this.router.events.pipe(filter(e => e instanceof NavigationError)).subscribe(e => {
        const err = e as NavigationError;
        this.logger.error('Navigation error', String(err.error), { url: err.url }, 'Angular.Router');
      })
    );
  }

  ngOnDestroy() {
    this.subs.unsubscribe();
  }
}
