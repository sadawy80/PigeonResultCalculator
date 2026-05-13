import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/services';
import { TranslatePipe, TranslationService, LanguageSwitcherComponent } from '../../core/i18n';

/**
 * Full-page Contact Us for anonymous visitors who clicked through from the
 * landing page. Mirrors the landing nav and the dark-navy / gold visual
 * direction so it reads as a continuation of the marketing site. The form
 * is self-contained — we do NOT reuse the in-shell ContactComponent here
 * because that one is themed against the authenticated app's --pr-* tokens.
 * Authenticated users still go through /support (inside the app shell).
 */
@Component({
  selector: 'app-contact-public-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, TranslatePipe, LanguageSwitcherComponent],
  template: `
    <!-- Nav (mirrors landing.component.html) -->
    <nav class="nav">
      <div class="nav__inner">
        <a class="nav__brand" routerLink="/">
          <span class="nav__brand-bird">🕊️</span>
          <span class="nav__brand-text">
            Pigeon<span class="nav__brand-accent">Result</span>Calculator
          </span>
        </a>
        <div class="nav__links">
          <a routerLink="/" fragment="features">{{ 'landing.navFeatures' | translate }}</a>
          <a routerLink="/" fragment="integrations">{{ 'landing.navIntegrations' | translate }}</a>
          <a routerLink="/" fragment="season">{{ 'landing.navSeason' | translate }}</a>
          <a routerLink="/" fragment="pricing">Pricing</a>
        </div>
        <app-language-switcher />
        <div class="nav__actions">
          <a routerLink="/auth/login"    class="nav__login">{{ 'landing.navLogin' | translate }}</a>
          <a routerLink="/auth/register" class="nav__cta">{{ 'landing.navGetStarted' | translate }}</a>
        </div>
      </div>
    </nav>

    <main class="page" [attr.dir]="i18n.dir()">
      <header class="page__head">
        <h1 class="page__title">{{ 'contact.title' | translate }}</h1>
        <p  class="page__lead">{{ 'contact.lead' | translate }}</p>
      </header>

      <section class="form-card">
        @if (submitted()) {
          <div class="form-success">
            <div class="form-success__icon">✓</div>
            <h2>{{ 'contact.thanksTitle' | translate }}</h2>
            <p>{{ 'contact.thanksBody' | translate }}</p>
            <button type="button" class="btn btn--primary" (click)="reset()">
              {{ 'contact.sendAnother' | translate }}
            </button>
          </div>
        } @else {
          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <div class="row">
              <label class="field">
                <span>{{ 'contact.name' | translate }} *</span>
                <input formControlName="name" type="text" maxlength="200" autocomplete="name" />
              </label>
              <label class="field">
                <span>{{ 'contact.email' | translate }} *</span>
                <input formControlName="email" type="email" maxlength="200" autocomplete="email" />
              </label>
            </div>

            <label class="field">
              <span>{{ 'contact.phone' | translate }}</span>
              <input formControlName="phone" type="tel" maxlength="50" autocomplete="tel" />
            </label>

            <label class="field">
              <span>{{ 'contact.subject' | translate }} *</span>
              <input formControlName="subject" type="text" maxlength="300" />
            </label>

            <label class="field">
              <span>{{ 'contact.message' | translate }} *</span>
              <textarea formControlName="body" rows="6" maxlength="5000"></textarea>
              <small class="counter">{{ form.get('body')?.value?.length || 0 }} / 5000</small>
            </label>

            @if (error()) { <p class="alert">{{ error() }}</p> }

            <div class="actions">
              <button type="submit" class="btn btn--primary" [disabled]="form.invalid || sending()">
                {{ (sending() ? 'contact.sending' : 'contact.send') | translate }}
              </button>
            </div>
          </form>
        }
      </section>
    </main>

    <footer class="footer">
      <div class="footer__inner">
        <div class="footer__brand">
          <span class="footer__bird">🕊️</span>
          <span class="footer__name">PigeonResultCalculator</span>
        </div>
        <div class="footer__links">
          <a routerLink="/auth/login">{{ 'landing.footerLogin' | translate }}</a>
          <a routerLink="/auth/register">{{ 'landing.footerRegister' | translate }}</a>
          <a routerLink="/contact">{{ 'contact.title' | translate }}</a>
        </div>
        <div class="footer__copy">{{ 'landing.footerCopy' | translate:{ year: currentYear } }}</div>
      </div>
    </footer>
  `,
  styles: [`
    /* Landing-style tokens (kept local; landing.component.scss is scoped). */
    :host {
      --navy:       #0C1929;
      --navy-2:     #111F32;
      --gold:       #C9A84C;
      --gold-light: #E8C87A;
      --gold-dim:   rgba(201,168,76,.15);
      --white:      #FFFFFF;
      --off-white:  #F0EDE8;
      --muted:      rgba(240,237,232,.55);
      --border:     rgba(255,255,255,.08);
      --r-sm: 6px;

      display: block;
      min-height: 100vh;
      background: var(--navy);
      color: var(--off-white);
      font-family: 'DM Sans', system-ui, sans-serif;
    }
    * { box-sizing: border-box; }
    a { color: inherit; text-decoration: none; }

    /* Nav */
    .nav { position: sticky; top: 0; z-index: 100; background: rgba(12,25,41,.85); backdrop-filter: blur(16px); border-bottom: 1px solid var(--border); }
    .nav__inner { max-width: 1280px; margin: 0 auto; padding: 0 40px; height: 68px; display: flex; align-items: center; gap: 40px; }
    .nav__brand { display: flex; align-items: center; gap: 10px; font-weight: 700; font-size: 1.05rem; flex-shrink: 0; }
    .nav__brand-bird   { font-size: 1.35rem; }
    .nav__brand-accent { color: var(--gold); }
    .nav__links { display: flex; gap: 32px; flex: 1; }
    .nav__links a { font-size: .875rem; color: var(--muted); transition: color .2s; }
    .nav__links a:hover { color: var(--white); }
    .nav__actions { display: flex; align-items: center; gap: 12px; }
    .nav__login { font-size: .875rem; color: var(--muted); transition: color .2s; }
    .nav__login:hover { color: var(--white); }
    .nav__cta { padding: 8px 18px; background: var(--gold); color: var(--navy); border-radius: var(--r-sm); font-size: .875rem; font-weight: 700; transition: all .2s; }
    .nav__cta:hover { background: var(--gold-light); transform: translateY(-1px); }
    app-language-switcher { --ls-trigger-color: var(--muted); --ls-trigger-hover: var(--white); }
    @media (max-width: 900px) { .nav__links { display: none; } .nav__inner { padding: 0 20px; gap: 16px; } }

    /* Body */
    .page { max-width: 720px; margin: 0 auto; padding: 64px 24px 96px; }
    .page__head { text-align: center; margin-bottom: 32px; }
    .page__title {
      font-family: 'Instrument Serif', Georgia, serif;
      font-weight: 400; font-size: clamp(2rem, 4vw, 2.6rem);
      color: var(--white); margin: 0 0 .5rem; letter-spacing: -.01em;
    }
    .page__lead { color: var(--muted); font-size: 1.02rem; line-height: 1.55; margin: 0 auto; max-width: 540px; }

    /* Form card */
    .form-card {
      background: var(--navy-2);
      border: 1px solid var(--border);
      border-radius: 16px;
      padding: 28px 28px 24px;
      box-shadow: 0 24px 60px rgba(0,0,0,.35);
    }
    form { display: flex; flex-direction: column; gap: 1rem; }
    .row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    @media (max-width: 600px) { .row { grid-template-columns: 1fr; } }
    .field { display: flex; flex-direction: column; gap: .35rem; font-size: .9rem; color: var(--off-white); }
    .field span { font-weight: 500; }
    .field input, .field textarea {
      width: 100%; padding: .7rem .85rem;
      background: rgba(255,255,255,.04);
      border: 1px solid var(--border); border-radius: 8px;
      color: var(--off-white); font: inherit;
      transition: border-color .2s, background .2s;
    }
    .field input:focus, .field textarea:focus {
      outline: 0; border-color: var(--gold); background: rgba(201,168,76,.06);
    }
    .field textarea { resize: vertical; min-height: 160px; font-family: inherit; }
    .counter { color: var(--muted); font-size: .75rem; align-self: flex-end; }
    .alert {
      margin: 0; padding: .65rem .85rem;
      background: rgba(220, 38, 38, .14);
      border: 1px solid rgba(220, 38, 38, .35);
      border-radius: 8px; color: #fca5a5; font-size: .9rem;
    }
    .actions { display: flex; justify-content: flex-end; }
    .btn { padding: .7rem 1.4rem; border-radius: 8px; font-weight: 700; cursor: pointer; border: 0; font: inherit; transition: all .2s; }
    .btn--primary { background: var(--gold); color: var(--navy); }
    .btn--primary:hover:not(:disabled) { background: var(--gold-light); transform: translateY(-1px); }
    .btn--primary:disabled { opacity: .5; cursor: not-allowed; }

    /* Success */
    .form-success { text-align: center; padding: 1.5rem 0 .5rem; }
    .form-success__icon {
      display: inline-flex; align-items: center; justify-content: center;
      width: 64px; height: 64px; margin-bottom: .85rem; border-radius: 50%;
      background: var(--gold-dim); color: var(--gold); font-size: 1.85rem;
    }
    .form-success h2 { font-family: 'Instrument Serif', serif; font-weight: 400; color: var(--white); margin: 0 0 .35rem; font-size: 1.6rem; }
    .form-success p  { margin: 0 0 1.25rem; color: var(--muted); }

    /* Footer */
    .footer { border-top: 1px solid var(--border); padding: 32px 24px; background: rgba(0,0,0,.18); }
    .footer__inner { max-width: 1280px; margin: 0 auto; display: flex; align-items: center; gap: 32px; flex-wrap: wrap; }
    .footer__brand { display: flex; align-items: center; gap: 8px; font-weight: 600; color: var(--white); }
    .footer__bird  { font-size: 1.15rem; }
    .footer__links { display: flex; gap: 24px; flex: 1; flex-wrap: wrap; }
    .footer__links a { color: var(--muted); font-size: .875rem; transition: color .2s; }
    .footer__links a:hover { color: var(--white); }
    .footer__copy { color: var(--muted); font-size: .8rem; }
  `]
})
export class ContactPublicPageComponent implements OnInit {
  private fb   = inject(FormBuilder);
  private api  = inject(ApiService);
  private auth = inject(AuthService);
  i18n         = inject(TranslationService);

  sending   = signal(false);
  submitted = signal(false);
  error     = signal<string | null>(null);

  readonly currentYear = new Date().getFullYear();

  form: FormGroup = this.fb.group({
    name:    ['', [Validators.required, Validators.maxLength(200)]],
    email:   ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
    phone:   ['', [Validators.maxLength(50)]],
    subject: ['', [Validators.required, Validators.maxLength(300)]],
    body:    ['', [Validators.required, Validators.maxLength(5000)]]
  });

  ngOnInit() {
    // Prefill if the visitor happens to be logged in — costs nothing.
    const user = this.auth.currentUser();
    if (user) {
      const fullName = `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim();
      this.form.patchValue({ name: fullName, email: user.email ?? '' });
    }
  }

  submit() {
    if (this.form.invalid || this.sending()) return;
    this.sending.set(true);
    this.error.set(null);

    this.api.submitContactMessage(this.form.getRawValue()).subscribe({
      next: () => { this.sending.set(false); this.submitted.set(true); },
      error: err => {
        this.sending.set(false);
        this.error.set(err?.error?.detail || err?.error?.message || 'Failed to send message. Please try again.');
      }
    });
  }

  reset() {
    this.submitted.set(false);
    this.form.reset({ name: '', email: '', phone: '', subject: '', body: '' });
    this.ngOnInit();
  }
}
