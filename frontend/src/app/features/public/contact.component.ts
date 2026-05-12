import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/services';
import { TranslatePipe, TranslationService } from '../../core/i18n';

/**
 * Contact-Us form body. Used standalone inside the shell (route /support) and
 * also embedded by ContactPublicPageComponent which adds a public landing-page
 * header for anonymous visitors at /contact.
 *
 * Styling uses the site theme tokens (--pr-*) so it looks native inside the
 * shell. No page-level chrome of its own.
 */
@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslatePipe],
  template: `
    <div class="contact-wrap" [attr.dir]="i18n.dir()">
      <header class="contact-head">
        <h1>{{ 'contact.title' | translate }}</h1>
        <p>{{ 'contact.lead' | translate }}</p>
      </header>

      <div class="pr-card contact-card">
        @if (submitted()) {
          <div class="contact-success" role="status">
            <div class="contact-success__icon">✓</div>
            <h2>{{ 'contact.thanksTitle' | translate }}</h2>
            <p>{{ 'contact.thanksBody' | translate }}</p>
            <button type="button" class="pr-btn pr-btn--primary" (click)="reset()">
              {{ 'contact.sendAnother' | translate }}
            </button>
          </div>
        } @else {
          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <div class="contact-row">
              <label class="pr-field">
                <span class="pr-label">{{ 'contact.name' | translate }} *</span>
                <input class="pr-input" formControlName="name" type="text" maxlength="200" autocomplete="name" />
              </label>
              <label class="pr-field">
                <span class="pr-label">{{ 'contact.email' | translate }} *</span>
                <input class="pr-input" formControlName="email" type="email" maxlength="200" autocomplete="email" />
              </label>
            </div>

            <label class="pr-field">
              <span class="pr-label">{{ 'contact.phone' | translate }}</span>
              <input class="pr-input" formControlName="phone" type="tel" maxlength="50" autocomplete="tel" />
            </label>

            <label class="pr-field">
              <span class="pr-label">{{ 'contact.subject' | translate }} *</span>
              <input class="pr-input" formControlName="subject" type="text" maxlength="300" />
            </label>

            <label class="pr-field">
              <span class="pr-label">{{ 'contact.message' | translate }} *</span>
              <textarea class="pr-input" formControlName="body" rows="6" maxlength="5000"></textarea>
              <small class="contact-counter">{{ form.get('body')?.value?.length || 0 }} / 5000</small>
            </label>

            @if (error()) {
              <p class="pr-alert pr-alert--error">{{ error() }}</p>
            }

            <div class="contact-actions">
              <button type="submit" class="pr-btn pr-btn--primary" [disabled]="form.invalid || sending()">
                {{ (sending() ? 'contact.sending' : 'contact.send') | translate }}
              </button>
            </div>
          </form>
        }
      </div>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .contact-wrap { max-width: 760px; margin: 0 auto; }
    .contact-head h1 { margin: 0 0 .35rem; font-size: 1.5rem; color: var(--pr-text); }
    .contact-head p  { margin: 0 0 1.25rem; color: var(--pr-text-muted); }
    .contact-card    { padding: 1.5rem; }

    form { display: flex; flex-direction: column; gap: .95rem; }
    .contact-row { display: grid; grid-template-columns: 1fr 1fr; gap: .9rem; }
    @media (max-width: 640px) { .contact-row { grid-template-columns: 1fr; } }

    .pr-field { display: flex; flex-direction: column; gap: .3rem; }
    .pr-field textarea { resize: vertical; min-height: 140px; font-family: inherit; }
    .contact-counter { align-self: flex-end; color: var(--pr-text-muted); font-size: .75rem; }
    .contact-actions { display: flex; justify-content: flex-end; gap: .5rem; }

    .contact-success { text-align: center; padding: 1.25rem .5rem; }
    .contact-success__icon {
      display: inline-flex; align-items: center; justify-content: center;
      width: 56px; height: 56px; margin: 0 auto .75rem; border-radius: 50%;
      background: rgba(22, 163, 74, .12); color: #16a34a; font-size: 1.75rem;
    }
    .contact-success h2 { margin: 0 0 .35rem; color: var(--pr-text); }
    .contact-success p  { margin: 0 0 1.2rem; color: var(--pr-text-muted); }
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
  /** True when the user opened the page while logged in — used to lock identity fields. */
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
      // Submission still works even if locked — backend reads JWT for role + UserId.
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
    this.submitted.set(false);
    this.form.reset({ name: '', email: '', phone: '', subject: '', body: '' });
    this.ngOnInit();
  }
}
