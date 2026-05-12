import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ContactComponent } from './contact.component';
import { TranslatePipe, TranslationService, LanguageSwitcherComponent } from '../../core/i18n';

/**
 * Public landing-page wrapper for anonymous visitors at /contact. Adds the
 * brand bar + language switcher above the bare <app-contact> form body.
 * Authenticated users hit /support inside the shell instead.
 */
@Component({
  selector: 'app-contact-public-page',
  standalone: true,
  imports: [RouterLink, ContactComponent, TranslatePipe, LanguageSwitcherComponent],
  template: `
    <div class="page" [attr.dir]="i18n.dir()">
      <header class="page__header">
        <a routerLink="/" class="brand">{{ 'common.brand' | translate }}</a>
        <app-language-switcher />
      </header>
      <main class="page__main">
        <app-contact />
      </main>
    </div>
  `,
  styles: [`
    :host { display: block; min-height: 100vh; background: var(--pr-bg); color: var(--pr-text); }
    .page__header {
      display: flex; align-items: center; justify-content: space-between;
      padding: 1rem 1.5rem;
      border-bottom: 1px solid var(--pr-border);
      background: var(--pr-surface);
    }
    .brand { font-weight: 700; font-size: 1.2rem; color: var(--pr-primary); text-decoration: none; }
    .page__main { padding: 2rem 1.25rem 3rem; }
  `]
})
export class ContactPublicPageComponent {
  i18n = inject(TranslationService);
}
