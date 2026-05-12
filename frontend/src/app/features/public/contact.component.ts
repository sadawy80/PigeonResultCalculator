import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/services';
import { TranslatePipe, TranslationService, LanguageSwitcherComponent } from '../../core/i18n';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, TranslatePipe, LanguageSwitcherComponent],
  template: `
    <div class="contact-page" [attr.dir]="i18n.dir()">
      <header class="contact-header">
        <a routerLink="/" class="brand">{{ 'common.brand' | translate }}</a>
        <app-language-switcher />
      </header>

      <main class="contact-main">
        <div class="contact-card">
          <h1>{{ 'contact.title' | translate }}</h1>
          <p class="lead">{{ 'contact.lead' | translate }}</p>

          @if (submitted()) {
            <div class="success" role="status">
              <h2>{{ 'contact.thanksTitle' | translate }}</h2>
              <p>{{ 'contact.thanksBody' | translate }}</p>
              <button type="button" class="btn primary" (click)="reset()">{{ 'contact.sendAnother' | translate }}</button>
            </div>
          } @else {
            <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
              <div class="row">
                <label>
                  <span>{{ 'contact.name' | translate }} *</span>
                  <input formControlName="name" type="text" maxlength="200" autocomplete="name" />
                </label>
                <label>
                  <span>{{ 'contact.email' | translate }} *</span>
                  <input formControlName="email" type="email" maxlength="200" autocomplete="email" />
                </label>
              </div>

              <label>
                <span>{{ 'contact.phone' | translate }}</span>
                <input formControlName="phone" type="tel" maxlength="50" autocomplete="tel" />
              </label>

              <label>
                <span>{{ 'contact.subject' | translate }} *</span>
                <input formControlName="subject" type="text" maxlength="300" />
              </label>

              <label>
                <span>{{ 'contact.message' | translate }} *</span>
                <textarea formControlName="body" rows="6" maxlength="5000"></textarea>
                <small class="counter">{{ form.get('body')?.value?.length || 0 }} / 5000</small>
              </label>

              @if (error()) {
                <p class="error" role="alert">{{ error() }}</p>
              }

              <div class="actions">
                <a routerLink="/" class="btn ghost">{{ 'common.cancel' | translate }}</a>
                <button type="submit" class="btn primary" [disabled]="form.invalid || sending()">
                  {{ (sending() ? 'contact.sending' : 'contact.send') | translate }}
                </button>
              </div>
            </form>
          }
        </div>
      </main>
    </div>
  `,
  styles: [`
    :host { display: block; min-height: 100vh; background: linear-gradient(135deg, #f4f7fb 0%, #e7eefc 100%); }
    .contact-page { max-width: 100%; }
    .contact-header { display: flex; justify-content: space-between; align-items: center; padding: 1rem 1.5rem; }
    .brand { font-weight: 700; font-size: 1.25rem; color: #1e3a8a; text-decoration: none; }
    .contact-main { display: flex; justify-content: center; padding: 1rem 1.5rem 4rem; }
    .contact-card { background: #fff; border-radius: 16px; padding: 2rem; box-shadow: 0 10px 30px rgba(15,23,42,.08); max-width: 720px; width: 100%; }
    h1 { margin: 0 0 .5rem; font-size: 1.75rem; color: #0f172a; }
    .lead { color: #475569; margin: 0 0 1.5rem; }
    form { display: flex; flex-direction: column; gap: 1rem; }
    .row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    label { display: flex; flex-direction: column; gap: .35rem; font-size: .9rem; color: #334155; }
    label span { font-weight: 500; }
    input, textarea { padding: .65rem .8rem; border: 1px solid #cbd5e1; border-radius: 8px; font: inherit; }
    input:focus, textarea:focus { outline: 2px solid #6366f1; border-color: #6366f1; }
    textarea { resize: vertical; min-height: 140px; }
    .counter { color: #94a3b8; align-self: flex-end; font-size: .75rem; }
    .actions { display: flex; justify-content: flex-end; gap: .75rem; margin-top: .5rem; }
    .btn { padding: .65rem 1.4rem; border-radius: 8px; font-weight: 600; cursor: pointer; border: none; text-decoration: none; }
    .btn.primary { background: #4f46e5; color: #fff; }
    .btn.primary:disabled { opacity: .5; cursor: not-allowed; }
    .btn.ghost { background: transparent; color: #475569; border: 1px solid #cbd5e1; display: inline-flex; align-items: center; }
    .error { color: #b91c1c; background: #fee2e2; padding: .6rem .8rem; border-radius: 8px; margin: 0; }
    .success { text-align: center; padding: 1.5rem 0; }
    .success h2 { color: #166534; margin: 0 0 .5rem; }
    @media (max-width: 640px) { .row { grid-template-columns: 1fr; } .contact-card { padding: 1.25rem; } }
  `]
})
export class ContactComponent implements OnInit {
  private fb   = inject(FormBuilder);
  private api  = inject(ApiService);
  private auth = inject(AuthService);
  i18n         = inject(TranslationService);

  sending   = signal(false);
  submitted = signal(false);
  error     = signal<string | null>(null);
  /** True when an authenticated user opened the page — used to lock identity fields. */
  authenticated = signal(false);

  form: FormGroup = this.fb.group({
    name:    ['', [Validators.required, Validators.maxLength(200)]],
    email:   ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
    phone:   ['', [Validators.maxLength(50)]],
    subject: ['', [Validators.required, Validators.maxLength(300)]],
    body:    ['', [Validators.required, Validators.maxLength(5000)]]
  });

  ngOnInit() {
    const user = this.auth.currentUser();
    if (user) {
      const fullName = `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim();
      this.form.patchValue({ name: fullName, email: user.email ?? '' });
      // Submission still works even if locked — backend reads the JWT for role + UserId.
      this.form.get('name')!.disable();
      this.form.get('email')!.disable();
      this.authenticated.set(true);
    }
  }

  submit() {
    if (this.form.invalid || this.sending()) return;
    this.sending.set(true);
    this.error.set(null);

    // getRawValue() so disabled (prefilled) name + email are still sent.
    this.api.submitContactMessage(this.form.getRawValue()).subscribe({
      next: () => { this.sending.set(false); this.submitted.set(true); },
      error: err => {
        this.sending.set(false);
        this.error.set(err?.error?.detail || err?.error?.message || 'Failed to send message. Please try again.');
      }
    });
  }

  reset() {
    // Re-run ngOnInit so we re-lock identity fields if still authenticated.
    this.submitted.set(false);
    this.form.reset({ name: '', email: '', phone: '', subject: '', body: '' });
    this.ngOnInit();
  }
}
