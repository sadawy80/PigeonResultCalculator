import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { NgIf, NgClass, NgFor } from '@angular/common';
import { AuthService } from '../../core/services/services';
import { ApiService } from '../../core/services/api.service';
import { TranslatePipe, TranslationService } from '../../core/i18n';

// ── Login Component ───────────────────────────────────────────────────────────

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  template: `
    <div class="auth-shell">
      <div class="auth-panel">
        <div class="auth-brand">
          <div class="auth-brand__icon">🕊️</div>
          <h1 class="auth-brand__title">Pigeon Result Calculator</h1>
          <p class="auth-brand__sub">{{ 'auth.professionalMgmt' | translate }}</p>
        </div>

        <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
          <h2 class="auth-form__heading">{{ 'auth.signIn' | translate }}</h2>

          @if (error()) {
            <div class="pr-alert pr-alert--error">{{ error() }}</div>
          }

          <div class="pr-form-group">
            <label class="pr-label">{{ 'auth.email' | translate }}</label>
            <input class="pr-input" type="email" formControlName="email" placeholder="you@example.com" autocomplete="email">
          </div>

          <div class="pr-form-group">
            <label class="pr-label">{{ 'auth.password' | translate }}</label>
            <input class="pr-input" type="password" formControlName="password" placeholder="••••••••" autocomplete="current-password">
          </div>

          <div style="display:flex;justify-content:flex-end">
            <a routerLink="/auth/forgot-password" class="pr-link" style="font-size:0.875rem">{{ 'auth.forgotPassword' | translate }}</a>
          </div>

          <button class="pr-btn pr-btn--primary w-full"
                  type="submit"
                  [disabled]="loading() || form.invalid">
            @if (loading()) { <span class="pr-spinner" style="width:16px;height:16px"></span> }
            {{ 'auth.signIn' | translate }}
          </button>

          <p class="auth-form__footer">
            {{ 'auth.invitationPrompt' | translate }} <a routerLink="/auth/accept-invitation">{{ 'auth.acceptHere' | translate }}</a>
          </p>
        </form>
      </div>

      <div class="auth-visual">
        <div class="auth-visual__content">
          <blockquote class="auth-quote">
            {{ 'auth.signInQuote' | translate }}
          </blockquote>
          <div class="auth-visual__dots">
            @for (d of [1,2,3,4,5]; track d) {
              <span class="dot" [style.animation-delay]="d * 0.15 + 's'"></span>
            }
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-shell {
      display: grid;
      grid-template-columns: 1fr 1fr;
      min-height: 100vh;
    }
    @media (max-width: 768px) { .auth-shell { grid-template-columns: 1fr; } .auth-visual { display: none; } }

    .auth-panel {
      display: flex; flex-direction: column; justify-content: center;
      align-items: center; padding: 48px 32px;
      background: var(--pr-surface);
    }
    .auth-brand { text-align: center; margin-bottom: 40px; }
    .auth-brand__icon { font-size: 3rem; margin-bottom: 8px; }
    .auth-brand__title {
      font-family: var(--font-display); font-weight: 800;
      font-size: 1.75rem; color: var(--pr-text);
    }
    .auth-brand__sub { color: var(--pr-text-muted); font-size: 0.875rem; margin-top: 4px; }

    .auth-form { width: 100%; max-width: 380px; display: flex; flex-direction: column; gap: 20px; }
    .auth-form__heading { font-family: var(--font-display); font-size: 1.5rem; font-weight: 700; }
    .auth-form__footer { text-align: center; font-size: 0.875rem; color: var(--pr-text-muted); }

    .auth-visual {
      background: var(--pr-bg);
      display: flex; align-items: center; justify-content: center;
      position: relative; overflow: hidden;
    }
    .auth-visual::before {
      content: '';
      position: absolute; inset: 0;
      background: radial-gradient(ellipse at 60% 40%, rgba(30,144,255,0.15) 0%, transparent 60%);
    }
    .auth-visual__content { position: relative; text-align: center; padding: 48px; }
    .auth-quote {
      font-family: var(--font-display); font-size: 1.5rem; font-weight: 700;
      color: var(--pr-text); line-height: 1.4;
      border: none; max-width: 360px;
    }
    .auth-visual__dots { display: flex; gap: 12px; justify-content: center; margin-top: 32px; }
    .dot {
      width: 8px; height: 8px; border-radius: 50%;
      background: var(--pr-primary); opacity: 0.4;
      animation: pulse 2s ease-in-out infinite;
    }
    @keyframes pulse { 0%,100% { opacity: 0.2; transform: scale(0.8); } 50% { opacity: 1; transform: scale(1.2); } }
  `]
})
export class LoginComponent {
  private fb   = inject(FormBuilder);
  private auth = inject(AuthService);
  private i18n = inject(TranslationService);

  form = this.fb.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  loading = signal(false);
  error   = signal<string | null>(null);

  submit() {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);
    const { email, password } = this.form.value;
    this.auth.login(email!, password!).subscribe({
      next: () => this.loading.set(false),
      error: (err) => {
        this.error.set(err?.error?.message ?? this.i18n.t('auth.loginError'));
        this.loading.set(false);
      }
    });
  }
}

// ── Register Component ────────────────────────────────────────────────────────

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  template: `
    <div class="auth-shell">
      <div class="auth-panel">
        <div class="auth-brand">
          <div class="auth-brand__icon">🕊️</div>
          <h1 class="auth-brand__title">Pigeon Result Calculator</h1>
          <p class="auth-brand__sub">{{ 'auth.createYourAccount' | translate }}</p>
        </div>

        @if (submitted()) {
          <div class="pending-card">
            <div class="pending-card__icon">✅</div>
            <h2 class="pending-card__title">{{ 'auth.accountCreated' | translate }}</h2>
            <p class="pending-card__body">
              {{ 'auth.accountCreatedBody' | translate }}
            </p>
            <a routerLink="/auth/login" class="pr-btn pr-btn--primary" style="margin-top:8px">
              {{ 'auth.signIn' | translate }}
            </a>
          </div>
        } @else {
          <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
            <h2 class="auth-form__heading">{{ 'auth.getStarted' | translate }}</h2>

            @if (error()) {
              <div class="pr-alert pr-alert--error">{{ error() }}</div>
            }

            <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px">
              <div class="pr-form-group">
                <label class="pr-label">{{ 'auth.firstName' | translate }}</label>
                <input class="pr-input" formControlName="firstName" autocomplete="given-name">
              </div>
              <div class="pr-form-group">
                <label class="pr-label">{{ 'auth.lastName' | translate }}</label>
                <input class="pr-input" formControlName="lastName" autocomplete="family-name">
              </div>
            </div>

            <div class="pr-form-group">
              <label class="pr-label">{{ 'auth.email' | translate }}</label>
              <input class="pr-input" type="email" formControlName="email" placeholder="you@example.com" autocomplete="email">
            </div>

            <div class="pr-form-group">
              <label class="pr-label">{{ 'auth.password' | translate }}</label>
              <input class="pr-input" type="password" formControlName="password" autocomplete="new-password">
            </div>

            <button class="pr-btn pr-btn--primary w-full"
                    type="submit"
                    [disabled]="loading() || form.invalid">
              @if (loading()) { <span class="pr-spinner" style="width:16px;height:16px"></span> }
              {{ 'auth.registerSubmit' | translate }}
            </button>

            <p class="auth-form__footer">
              {{ 'auth.haveAccount' | translate }} <a routerLink="/auth/login">{{ 'auth.signIn' | translate }}</a>
            </p>
            <p class="auth-form__footer">
              {{ 'auth.invitationPrompt' | translate }} <a routerLink="/auth/accept-invitation">{{ 'auth.acceptHere' | translate }}</a>
            </p>
          </form>
        }
      </div>

      <div class="auth-visual">
        <div class="auth-visual__content">
          <blockquote class="auth-quote">
            {{ 'auth.registerQuote' | translate }}
          </blockquote>
          <div class="auth-visual__dots">
            @for (d of [1,2,3,4,5]; track d) {
              <span class="dot" [style.animation-delay]="d * 0.15 + 's'"></span>
            }
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-shell { display: grid; grid-template-columns: 1fr 1fr; min-height: 100vh; }
    @media (max-width: 768px) { .auth-shell { grid-template-columns: 1fr; } .auth-visual { display: none; } }
    .auth-panel {
      display: flex; flex-direction: column; justify-content: center;
      align-items: center; padding: 48px 32px; background: var(--pr-surface);
    }
    .auth-brand { text-align: center; margin-bottom: 40px; }
    .auth-brand__icon { font-size: 3rem; margin-bottom: 8px; }
    .auth-brand__title { font-family: var(--font-display); font-weight: 800; font-size: 1.75rem; color: var(--pr-text); }
    .auth-brand__sub { color: var(--pr-text-muted); font-size: 0.875rem; margin-top: 4px; }
    .auth-form { width: 100%; max-width: 400px; display: flex; flex-direction: column; gap: 20px; }
    .auth-form__heading { font-family: var(--font-display); font-size: 1.5rem; font-weight: 700; }
    .auth-form__footer { text-align: center; font-size: 0.875rem; color: var(--pr-text-muted); }
    .pending-card {
      width: 100%; max-width: 400px; display: flex; flex-direction: column;
      align-items: center; gap: 12px; text-align: center;
      background: var(--pr-bg); border: 1px solid var(--pr-border);
      border-radius: var(--pr-radius); padding: 40px 32px;
    }
    .pending-card__icon { font-size: 3rem; }
    .pending-card__title { font-family: var(--font-display); font-size: 1.25rem; font-weight: 700; }
    .pending-card__body { color: var(--pr-text-muted); font-size: 0.9rem; line-height: 1.6; }
    .auth-visual {
      background: var(--pr-bg); display: flex; align-items: center;
      justify-content: center; position: relative; overflow: hidden;
    }
    .auth-visual::before {
      content: ''; position: absolute; inset: 0;
      background: radial-gradient(ellipse at 60% 40%, rgba(30,144,255,0.15) 0%, transparent 60%);
    }
    .auth-visual__content { position: relative; text-align: center; padding: 48px; }
    .auth-quote {
      font-family: var(--font-display); font-size: 1.5rem; font-weight: 700;
      color: var(--pr-text); line-height: 1.4; border: none; max-width: 360px;
    }
    .auth-visual__dots { display: flex; gap: 12px; justify-content: center; margin-top: 32px; }
    .dot {
      width: 8px; height: 8px; border-radius: 50%; background: var(--pr-primary);
      opacity: 0.4; animation: pulse 2s ease-in-out infinite;
    }
    @keyframes pulse { 0%,100% { opacity: 0.2; transform: scale(0.8); } 50% { opacity: 1; transform: scale(1.2); } }
  `]
})
export class RegisterComponent {
  private fb   = inject(FormBuilder);
  private api  = inject(ApiService);
  private i18n = inject(TranslationService);

  form = this.fb.group({
    firstName: ['', Validators.required],
    lastName:  ['', Validators.required],
    email:     ['', [Validators.required, Validators.email]],
    password:  ['', [Validators.required, Validators.minLength(8)]]
  });

  loading   = signal(false);
  error     = signal<string | null>(null);
  submitted = signal(false);

  submit() {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);
    const v = this.form.value;
    this.api.register({
      email: v.email!, password: v.password!,
      firstName: v.firstName!, lastName: v.lastName!,
      role: 4  // Fancier — active immediately, can request upgrade later
    }).subscribe({
      next: () => { this.submitted.set(true); },
      error: (e: any) => {
        this.error.set(e?.error?.message ?? this.i18n.t('errors.serverError'));
        this.loading.set(false);
      }
    });
  }
}

// ── Accept Invitation Component ───────────────────────────────────────────────

@Component({
  selector: 'app-accept-invitation',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  template: `
    <div class="auth-shell">
      <div class="auth-panel">
        <div class="auth-brand">
          <div class="auth-brand__icon">🕊️</div>
          <h1 class="auth-brand__title">{{ 'auth.acceptInviteTitle' | translate }}</h1>
          <p class="auth-brand__sub">{{ 'auth.acceptInviteSub' | translate }}</p>
        </div>

        <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
          @if (error()) { <div class="pr-alert pr-alert--error">{{ error() }}</div> }
          @if (success()) { <div class="pr-alert pr-alert--success">{{ success() }}</div> }

          <div class="pr-form-group">
            <label class="pr-label">{{ 'auth.acceptHere' | translate }}</label>
            <input class="pr-input" formControlName="token">
          </div>
          <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px">
            <div class="pr-form-group">
              <label class="pr-label">{{ 'auth.firstName' | translate }}</label>
              <input class="pr-input" formControlName="firstName">
            </div>
            <div class="pr-form-group">
              <label class="pr-label">{{ 'auth.lastName' | translate }}</label>
              <input class="pr-input" formControlName="lastName">
            </div>
          </div>
          <div class="pr-form-group">
            <label class="pr-label">{{ 'auth.email' | translate }}</label>
            <input class="pr-input" type="email" formControlName="email" placeholder="you@example.com">
          </div>
          <div class="pr-form-group">
            <label class="pr-label">{{ 'auth.password' | translate }}</label>
            <input class="pr-input" type="password" formControlName="password">
          </div>

          <button class="pr-btn pr-btn--primary w-full"
                  type="submit" [disabled]="loading() || form.invalid">
            @if (loading()) { <span class="pr-spinner" style="width:16px;height:16px"></span> }
            {{ 'auth.acceptInviteSubmit' | translate }}
          </button>

          <p class="auth-form__footer">
            {{ 'auth.haveAccount' | translate }} <a routerLink="/auth/login">{{ 'auth.signIn' | translate }}</a>
          </p>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .auth-shell { display:flex; justify-content:center; align-items:center; min-height:100vh; background:var(--pr-bg); }
    .auth-panel { background:var(--pr-surface); border-radius:var(--pr-radius); padding:48px; width:100%; max-width:480px; border:1px solid var(--pr-border); }
    .auth-brand { text-align:center; margin-bottom:32px; }
    .auth-brand__icon { font-size:2.5rem; margin-bottom:8px; }
    .auth-brand__title { font-family:var(--font-display); font-size:1.5rem; font-weight:800; }
    .auth-brand__sub { color:var(--pr-text-muted); font-size:0.875rem; }
    .auth-form { display:flex; flex-direction:column; gap:18px; }
    .auth-form__footer { text-align:center; font-size:0.875rem; color:var(--pr-text-muted); }
  `]
})
export class AcceptInvitationComponent {
  private fb   = inject(FormBuilder);
  private auth = inject(AuthService);
  private i18n = inject(TranslationService);

  form = this.fb.group({
    token:     ['', Validators.required],
    firstName: ['', Validators.required],
    lastName:  ['', Validators.required],
    email:     ['', [Validators.required, Validators.email]],
    password:  ['', [Validators.required, Validators.minLength(8)]]
  });

  loading = signal(false);
  error   = signal<string | null>(null);
  success = signal<string | null>(null);

  submit() {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);
    const v = this.form.value;
    this.auth['api'].register({
      email: v.email!, password: v.password!,
      firstName: v.firstName!, lastName: v.lastName!,
      role: 4, // Fancier
      invitationToken: v.token!
    }).subscribe({
      next: () => { this.success.set(this.i18n.t('auth.accountCreated')); },
      error: (e: any) => { this.error.set(e?.error?.message ?? this.i18n.t('auth.acceptInviteFailed')); this.loading.set(false); }
    });
  }
}

// ── Forgot Password Component ─────────────────────────────────────────────────

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  template: `
    <div class="auth-shell-center">
      <div class="auth-card">
        <div class="auth-brand">
          <div class="auth-brand__icon">🔑</div>
          <h1 class="auth-brand__title">Pigeon Result Calculator</h1>
        </div>

        @if (sent()) {
          <div class="auth-success">
            <div class="success-icon">✉️</div>
            <h2>{{ 'auth.resetSent' | translate }}</h2>
            <p>{{ 'auth.resetSent' | translate }} — <strong>{{ emailSent() }}</strong></p>
            <a routerLink="/auth/login" class="pr-btn pr-btn--secondary" style="margin-top:16px">{{ 'auth.backToLogin' | translate }}</a>
          </div>
        } @else {
          <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
            <h2 class="auth-form__heading">{{ 'auth.forgotTitle' | translate }}</h2>
            <p class="auth-form__sub">{{ 'auth.forgotSub' | translate }}</p>

            @if (error()) { <div class="pr-alert pr-alert--error">{{ error() }}</div> }

            <div class="pr-form-group">
              <label class="pr-label">{{ 'auth.email' | translate }}</label>
              <input class="pr-input" type="email" formControlName="email"
                     placeholder="you@example.com" autocomplete="email">
            </div>

            <button class="pr-btn pr-btn--primary w-full"
                    type="submit" [disabled]="loading() || form.invalid">
              @if (loading()) { <span class="pr-spinner" style="width:16px;height:16px"></span> }
              {{ 'auth.sendResetLink' | translate }}
            </button>

            <p class="auth-form__footer">
              <a routerLink="/auth/login">{{ 'auth.backToLogin' | translate }}</a>
            </p>
          </form>
        }
      </div>
    </div>
  `,
  styles: [`
    .auth-shell-center {
      min-height: 100vh; display: flex; align-items: center; justify-content: center;
      background: var(--pr-bg); padding: 24px;
    }
    .auth-card {
      background: var(--pr-surface); border: 1px solid var(--pr-border);
      border-radius: var(--pr-radius); padding: 48px 40px;
      width: 100%; max-width: 420px;
    }
    .auth-brand { text-align: center; margin-bottom: 32px; }
    .auth-brand__icon { font-size: 2.5rem; margin-bottom: 8px; }
    .auth-brand__title { font-family: var(--font-display); font-weight: 800; font-size: 1.4rem; color: var(--pr-text); }
    .auth-form { display: flex; flex-direction: column; gap: 18px; }
    .auth-form__heading { font-family: var(--font-display); font-size: 1.4rem; font-weight: 700; margin-bottom: 4px; }
    .auth-form__sub { color: var(--pr-text-muted); font-size: 0.875rem; margin-top: -8px; }
    .auth-form__footer { text-align: center; font-size: 0.875rem; color: var(--pr-text-muted); }
    .auth-success { text-align: center; display: flex; flex-direction: column; align-items: center; gap: 12px; }
    .success-icon { font-size: 3rem; }
    .auth-success h2 { font-family: var(--font-display); font-size: 1.3rem; font-weight: 700; }
    .auth-success p { color: var(--pr-text-muted); font-size: 0.9rem; line-height: 1.6; }
  `]
})
export class ForgotPasswordComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  loading  = signal(false);
  error    = signal<string | null>(null);
  sent     = signal(false);
  emailSent = signal('');

  submit() {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);
    const email = this.form.value.email!;
    this.api.forgotPassword(email).subscribe({
      next: () => {
        this.emailSent.set(email);
        this.sent.set(true);
        this.loading.set(false);
      },
      error: () => {
        // Show success anyway to avoid enumeration
        this.emailSent.set(email);
        this.sent.set(true);
        this.loading.set(false);
      }
    });
  }
}

// ── Reset Password Component ──────────────────────────────────────────────────

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  template: `
    <div class="auth-shell-center">
      <div class="auth-card">
        <div class="auth-brand">
          <div class="auth-brand__icon">🔐</div>
          <h1 class="auth-brand__title">Pigeon Result Calculator</h1>
        </div>

        @if (done()) {
          <div class="auth-success">
            <div class="success-icon">✅</div>
            <h2>{{ 'auth.resetSuccess' | translate }}</h2>
            <a routerLink="/auth/login" class="pr-btn pr-btn--primary" style="margin-top:16px">{{ 'auth.signIn' | translate }}</a>
          </div>
        } @else {
          <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
            <h2 class="auth-form__heading">{{ 'auth.resetTitle' | translate }}</h2>

            @if (error()) { <div class="pr-alert pr-alert--error">{{ error() }}</div> }

            <div class="pr-form-group">
              <label class="pr-label">{{ 'auth.newPassword' | translate }}</label>
              <input class="pr-input" type="password" formControlName="password" autocomplete="new-password">
            </div>

            <div class="pr-form-group">
              <label class="pr-label">{{ 'auth.confirmPassword' | translate }}</label>
              <input class="pr-input" type="password" formControlName="confirmPassword" autocomplete="new-password">
              @if (form.hasError('mismatch') && form.get('confirmPassword')?.touched) {
                <span class="pr-field-error">{{ 'errors.passwordMismatch' | translate }}</span>
              }
            </div>

            <button class="pr-btn pr-btn--primary w-full"
                    type="submit" [disabled]="loading() || form.invalid">
              @if (loading()) { <span class="pr-spinner" style="width:16px;height:16px"></span> }
              {{ 'auth.resetSubmit' | translate }}
            </button>
          </form>
        }
      </div>
    </div>
  `,
  styles: [`
    .auth-shell-center {
      min-height: 100vh; display: flex; align-items: center; justify-content: center;
      background: var(--pr-bg); padding: 24px;
    }
    .auth-card {
      background: var(--pr-surface); border: 1px solid var(--pr-border);
      border-radius: var(--pr-radius); padding: 48px 40px;
      width: 100%; max-width: 420px;
    }
    .auth-brand { text-align: center; margin-bottom: 32px; }
    .auth-brand__icon { font-size: 2.5rem; margin-bottom: 8px; }
    .auth-brand__title { font-family: var(--font-display); font-weight: 800; font-size: 1.4rem; color: var(--pr-text); }
    .auth-form { display: flex; flex-direction: column; gap: 18px; }
    .auth-form__heading { font-family: var(--font-display); font-size: 1.4rem; font-weight: 700; }
    .pr-field-error { color: var(--pr-error, #ef4444); font-size: 0.8rem; margin-top: 4px; display: block; }
    .auth-success { text-align: center; display: flex; flex-direction: column; align-items: center; gap: 12px; }
    .success-icon { font-size: 3rem; }
    .auth-success h2 { font-family: var(--font-display); font-size: 1.3rem; font-weight: 700; }
    .auth-success p { color: var(--pr-text-muted); font-size: 0.9rem; line-height: 1.6; }
  `]
})
export class ResetPasswordComponent implements OnInit {
  private fb    = inject(FormBuilder);
  private api   = inject(ApiService);
  private route = inject(ActivatedRoute);

  private email = '';
  private token = '';

  form = this.fb.group({
    password:        ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required]
  }, { validators: passwordMatchValidator });

  loading = signal(false);
  error   = signal<string | null>(null);
  done    = signal(false);

  ngOnInit() {
    this.email = this.route.snapshot.queryParamMap.get('email') ?? '';
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
  }

  submit() {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);
    this.api.resetPassword(this.email, this.token, this.form.value.password!).subscribe({
      next: () => { this.done.set(true); this.loading.set(false); },
      error: (e: any) => {
        this.error.set(e?.error?.message ?? 'Reset failed. The link may have expired.');
        this.loading.set(false);
      }
    });
  }
}

function passwordMatchValidator(group: import('@angular/forms').AbstractControl) {
  const pw  = group.get('password')?.value;
  const cpw = group.get('confirmPassword')?.value;
  return pw && cpw && pw !== cpw ? { mismatch: true } : null;
}

// ── Verify Email Component ────────────────────────────────────────────────────

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="auth-shell-center">
      <div class="auth-card" style="text-align:center">
        @if (loading()) {
          <div class="auth-brand__icon">⏳</div>
          <h2 style="font-family:var(--font-display);font-size:1.3rem;font-weight:700;margin-top:16px">Verifying your email…</h2>
        } @else if (success()) {
          <div class="auth-brand__icon">✅</div>
          <h2 style="font-family:var(--font-display);font-size:1.3rem;font-weight:700;margin:16px 0 8px">Email verified!</h2>
          <p style="color:var(--pr-text-muted);font-size:0.9rem;margin-bottom:24px">Your email address has been confirmed. You can now sign in.</p>
          <a routerLink="/auth/login" class="pr-btn pr-btn--primary">Sign In</a>
        } @else {
          <div class="auth-brand__icon">❌</div>
          <h2 style="font-family:var(--font-display);font-size:1.3rem;font-weight:700;margin:16px 0 8px">Verification failed</h2>
          <p style="color:var(--pr-text-muted);font-size:0.9rem;margin-bottom:8px">{{ error() }}</p>
          <p style="color:var(--pr-text-muted);font-size:0.875rem;margin-bottom:24px">
            The link may have expired.
            <a routerLink="/auth/resend-verification">Request a new link</a>
          </p>
          <a routerLink="/auth/login" class="pr-btn pr-btn--secondary">Back to Sign In</a>
        }
      </div>
    </div>
  `,
  styles: [`
    .auth-shell-center {
      min-height: 100vh; display: flex; align-items: center; justify-content: center;
      background: var(--pr-bg); padding: 24px;
    }
    .auth-card {
      background: var(--pr-surface); border: 1px solid var(--pr-border);
      border-radius: var(--pr-radius); padding: 48px 40px;
      width: 100%; max-width: 420px;
    }
    .auth-brand__icon { font-size: 3rem; }
  `]
})
export class VerifyEmailComponent implements OnInit {
  private api   = inject(ApiService);
  private route = inject(ActivatedRoute);

  loading = signal(true);
  success = signal(false);
  error   = signal<string | null>(null);

  ngOnInit() {
    const userId = this.route.snapshot.queryParamMap.get('userId') ?? '';
    const token  = this.route.snapshot.queryParamMap.get('token') ?? '';

    if (!userId || !token) {
      this.loading.set(false);
      this.error.set('Invalid verification link.');
      return;
    }

    this.api.verifyEmail(userId, token).subscribe({
      next: () => { this.success.set(true); this.loading.set(false); },
      error: (e: any) => {
        this.error.set(e?.error?.message ?? 'Verification failed.');
        this.loading.set(false);
      }
    });
  }
}

// ── Resend Verification Component ─────────────────────────────────────────────

@Component({
  selector: 'app-resend-verification',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-shell-center">
      <div class="auth-card">
        <div class="auth-brand">
          <div class="auth-brand__icon">📧</div>
          <h1 class="auth-brand__title">Pigeon Result Calculator</h1>
        </div>

        @if (sent()) {
          <div class="auth-success">
            <div class="success-icon">✉️</div>
            <h2>Verification sent</h2>
            <p>A new verification link has been sent to <strong>{{ emailSent() }}</strong>. Please check your inbox.</p>
            <a routerLink="/auth/login" class="pr-btn pr-btn--secondary" style="margin-top:16px">Back to Sign In</a>
          </div>
        } @else {
          <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
            <h2 class="auth-form__heading">Resend Verification</h2>
            <p class="auth-form__sub">Enter your email address to receive a new verification link.</p>

            @if (error()) { <div class="pr-alert pr-alert--error">{{ error() }}</div> }

            <div class="pr-form-group">
              <label class="pr-label">Email Address</label>
              <input class="pr-input" type="email" formControlName="email"
                     placeholder="you@example.com" autocomplete="email">
            </div>

            <button class="pr-btn pr-btn--primary w-full"
                    type="submit" [disabled]="loading() || form.invalid">
              @if (loading()) { <span class="pr-spinner" style="width:16px;height:16px"></span> }
              Resend Link
            </button>

            <p class="auth-form__footer">
              <a routerLink="/auth/login">Back to Sign In</a>
            </p>
          </form>
        }
      </div>
    </div>
  `,
  styles: [`
    .auth-shell-center {
      min-height: 100vh; display: flex; align-items: center; justify-content: center;
      background: var(--pr-bg); padding: 24px;
    }
    .auth-card {
      background: var(--pr-surface); border: 1px solid var(--pr-border);
      border-radius: var(--pr-radius); padding: 48px 40px;
      width: 100%; max-width: 420px;
    }
    .auth-brand { text-align: center; margin-bottom: 32px; }
    .auth-brand__icon { font-size: 2.5rem; margin-bottom: 8px; }
    .auth-brand__title { font-family: var(--font-display); font-weight: 800; font-size: 1.4rem; color: var(--pr-text); }
    .auth-form { display: flex; flex-direction: column; gap: 18px; }
    .auth-form__heading { font-family: var(--font-display); font-size: 1.4rem; font-weight: 700; margin-bottom: 4px; }
    .auth-form__sub { color: var(--pr-text-muted); font-size: 0.875rem; margin-top: -8px; }
    .auth-form__footer { text-align: center; font-size: 0.875rem; color: var(--pr-text-muted); }
    .auth-success { text-align: center; display: flex; flex-direction: column; align-items: center; gap: 12px; }
    .success-icon { font-size: 3rem; }
    .auth-success h2 { font-family: var(--font-display); font-size: 1.3rem; font-weight: 700; }
    .auth-success p { color: var(--pr-text-muted); font-size: 0.9rem; line-height: 1.6; }
  `]
})
export class ResendVerificationComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  loading   = signal(false);
  error     = signal<string | null>(null);
  sent      = signal(false);
  emailSent = signal('');

  submit() {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);
    const email = this.form.value.email!;
    this.api.resendVerification(email).subscribe({
      next: () => {
        this.emailSent.set(email);
        this.sent.set(true);
        this.loading.set(false);
      },
      error: () => {
        // Show success anyway to avoid enumeration
        this.emailSent.set(email);
        this.sent.set(true);
        this.loading.set(false);
      }
    });
  }
}

// ── Role Upgrade Request Component ───────────────────────────────────────────

import { WORLD_COUNTRIES, flagEmoji } from '../../core/constants/countries';

const REMINDER_COOLDOWN_MS = 24 * 60 * 60 * 1000; // 24 hours


@Component({
  selector: 'app-upgrade-request',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="upg-page">

      <!-- ── Loading ─────────────────────────────────────────── -->
      @if (checkingExisting()) {
        <div class="upg-card" style="text-align:center;padding:40px">
          <div class="pr-spinner" style="width:32px;height:32px;margin:0 auto"></div>
        </div>
      }

      <!-- ── PENDING STATE ────────────────────────────────────── -->
      @else if (pendingRequest()) {
        <div class="upg-card upg-card--pending">
          <div class="upg-card__hd">
            <span class="upg-card__icon">⏳</span>
            <div>
              <div class="upg-card__title">Request Pending Review</div>
              <div class="upg-card__sub">Submitted {{ formatDate(pendingRequest().createdAt) }}</div>
            </div>
          </div>

          <div class="upg-detail-grid">
            <div class="upg-detail">
              <span class="upg-detail__label">Requested Role</span>
              <span class="upg-detail__val">{{ roleName(pendingRequest().requestedRole) }}</span>
            </div>
            @if (pendingRequest().federationId) {
              <div class="upg-detail">
                <span class="upg-detail__label">Federation</span>
                <span class="upg-detail__val">{{ federationName(pendingRequest().federationId) }}</span>
              </div>
            }
            @if (pendingRequest().clubName) {
              <div class="upg-detail">
                <span class="upg-detail__label">Club Name</span>
                <span class="upg-detail__val">{{ pendingRequest().clubName }}</span>
              </div>
            }
            @if (pendingRequest().notes) {
              <div class="upg-detail upg-detail--full">
                <span class="upg-detail__label">Notes</span>
                <span class="upg-detail__val">{{ pendingRequest().notes }}</span>
              </div>
            }
          </div>

          <!-- Reminder -->
          <div class="upg-reminder">
            @if (reminderSent()) {
              <span class="upg-reminder__ok">✅ Reminder sent — the reviewer has been notified again.</span>
            } @else {
              <span class="upg-reminder__desc">
                Not heard back?
                @if (reminderCooldownMs() > 0) {
                  Next reminder available in <strong>{{ cooldownLabel() }}</strong>.
                } @else {
                  Send a reminder to nudge the reviewer.
                }
              </span>
              <button class="pr-btn pr-btn--outline pr-btn--sm"
                      [disabled]="reminderLoading() || reminderCooldownMs() > 0"
                      (click)="sendReminder()">
                @if (reminderLoading()) { <span class="pr-spinner" style="width:12px;height:12px"></span> }
                🔔 Send Reminder
              </button>
            }
            @if (reminderError()) {
              <span class="upg-reminder__err">{{ reminderError() }}</span>
            }
          </div>
        </div>
      }

      <!-- ── SUBMITTED SUCCESS ─────────────────────────────────── -->
      @else if (submitted()) {
        <div class="upg-card upg-card--success">
          <div class="upg-card__hd">
            <span class="upg-card__icon">✅</span>
            <div>
              <div class="upg-card__title">Request Submitted!</div>
              <div class="upg-card__sub">
                @if (routedTo() === 'federation') {
                  The federation manager will review your request.
                } @else {
                  The admin will review your request and assign your role.
                }
              </div>
            </div>
          </div>
        </div>
      }

      <!-- ── FORM ──────────────────────────────────────────────── -->
      @else {
        <div class="upg-card">
          <div class="upg-header">
            <div class="upg-header__icon">⬆️</div>
            <h1 class="upg-header__title">Request a Role Upgrade</h1>
            <p class="upg-header__sub">Tell us what role you need and we'll route your request to the right reviewer.</p>
          </div>

          @if (lastRejected()) {
            <div class="upg-rejected-notice">
              <strong>⚠️ Previous request was rejected</strong>
              @if (lastRejected().rejectionReason) {
                <div class="upg-rejected-notice__reason">{{ lastRejected().rejectionReason }}</div>
              }
              <div style="font-size:0.8rem;margin-top:4px;color:var(--pr-text-muted)">You may submit a new request below.</div>
            </div>
          }

          @if (error()) {
            <div class="pr-alert pr-alert--error" style="margin-bottom:16px">{{ error() }}</div>
          }

          <form [formGroup]="form" (ngSubmit)="submit()" class="upg-form">
            <div class="pr-form-group">
              <label class="pr-label">I want to become a…</label>
              <div class="role-options">
                @if (currentUserRole() !== 3) {
                  <label class="role-option" [class.selected]="form.value.role === 3">
                    <input type="radio" formControlName="role" [value]="3" style="display:none">
                    <div class="role-option__icon">🏟️</div>
                    <div class="role-option__title">Club Manager</div>
                    <div class="role-option__desc">Manage a racing club, enter results, invite members</div>
                  </label>
                }
                <label class="role-option" [class.selected]="form.value.role === 2">
                  <input type="radio" formControlName="role" [value]="2" style="display:none">
                  <div class="role-option__icon">🌍</div>
                  <div class="role-option__title">Federation Manager</div>
                  <div class="role-option__desc">Manage a national federation and its affiliated clubs</div>
                </label>
              </div>
            </div>

            @if (form.value.role === 3) {
              <div class="pr-form-group">
                <label class="pr-label">Country <span style="color:var(--pr-text-muted)">*</span></label>
                <div class="fed-combo">
                  <input class="pr-input"
                         [value]="fedSearchDisplay()"
                         (input)="onFedSearch($any($event.target).value)"
                         (focus)="fedDropdownOpen.set(true)"
                         (blur)="onFedBlur()"
                         placeholder="Search country…"
                         autocomplete="off">
                  @if (fedDropdownOpen()) {
                    <div class="fed-dropdown">
                      @if (filteredFederations().length === 0) {
                        <div class="fed-option fed-option--empty">No countries found</div>
                      }
                      @for (f of filteredFederations(); track f.code) {
                        <button type="button" class="fed-option"
                                [class.fed-option--active]="selectedCountry()?.code === f.code"
                                (mousedown)="selectFederation(f)">
                          <span style="margin-right:6px">{{ f.flag }}</span>{{ f.name }}
                        </button>
                      }
                    </div>
                  }
                </div>
                <span class="form-hint">Your request will be routed to the federation manager for this federation.</span>
              </div>
              <div class="pr-form-group">
                <label class="pr-label">Club Name <span style="color:var(--pr-text-muted)">*</span></label>
                <input class="pr-input" formControlName="clubName" placeholder="Name of the club you intend to manage">
              </div>
            }

            @if (form.value.role === 2) {
              <div class="pr-form-group">
                <label class="pr-label">Country <span style="color:var(--pr-text-muted)">*</span></label>
                <div class="fed-combo">
                  <input class="pr-input"
                         [value]="fedSearchDisplay()"
                         (input)="onFedSearch($any($event.target).value)"
                         (focus)="fedDropdownOpen.set(true)"
                         (blur)="onFedBlur()"
                         placeholder="Search country…"
                         autocomplete="off">
                  @if (fedDropdownOpen()) {
                    <div class="fed-dropdown">
                      @if (filteredFederations().length === 0) {
                        <div class="fed-option fed-option--empty">No countries found</div>
                      }
                      @for (f of filteredFederations(); track f.code) {
                        <button type="button" class="fed-option"
                                [class.fed-option--active]="selectedCountry()?.code === f.code"
                                (mousedown)="selectFederation(f)">
                          <span style="margin-right:6px">{{ f.flag }}</span>{{ f.name }}
                        </button>
                      }
                    </div>
                  }
                </div>
                <span class="form-hint">The federation you want to manage.</span>
              </div>
              <div class="upg-info-box">
                🛡️ Federation manager requests are reviewed by the admin.
              </div>
            }

            <div class="pr-form-group">
              <label class="pr-label">Additional notes <span style="color:var(--pr-text-muted)">(optional)</span></label>
              <textarea class="pr-input" formControlName="notes" rows="3"
                placeholder="Tell us a bit about yourself or your club…"
                style="resize:vertical"></textarea>
            </div>

            <button class="pr-btn pr-btn--primary w-full" type="submit"
                    [disabled]="loading() || form.invalid || ((form.value.role === 3 || form.value.role === 2) && !selectedCountry())">
              @if (loading()) { <span class="pr-spinner" style="width:16px;height:16px"></span> }
              Submit Request
            </button>
          </form>
        </div>
      }

      <!-- ── REQUEST HISTORY (always shown when requests exist) ── -->
      @if (!checkingExisting() && allRequests().length > 0) {
        <div class="upg-history">
          <div class="upg-history__title">Request History</div>
          @for (r of allRequests(); track r.id) {
            <div class="upg-history-item">
              <div class="upg-history-item__left">
                <span class="upg-history-item__role">{{ roleName(r.requestedRole) }}</span>
                @if (r.federationId) {
                  <span class="upg-history-item__meta">{{ federationName(r.federationId) }}</span>
                }
                @if (r.clubName) {
                  <span class="upg-history-item__meta">{{ r.clubName }}</span>
                }
              </div>
              <div class="upg-history-item__right">
                <span [class]="'pr-badge ' + statusBadge(r.status)">{{ statusLabel(r.status) }}</span>
                <span class="upg-history-item__date">{{ formatDate(r.createdAt) }}</span>
              </div>
              @if (r.status === 3 && r.rejectionReason) {
                <div class="upg-history-item__reason">{{ r.rejectionReason }}</div>
              }
            </div>
          }
        </div>
      }

    </div>
  `,
  styles: [`
    .upg-page { max-width: 580px; margin: 0 auto; padding: 32px 0; display: flex; flex-direction: column; gap: 16px; }

    /* Card */
    .upg-card {
      background: var(--pr-surface); border: 1px solid var(--pr-border);
      border-radius: var(--pr-radius); padding: 32px 28px;
    }
    .upg-card--pending { border-left: 4px solid var(--pr-warning, #F59E0B); }
    .upg-card--success { border-left: 4px solid var(--pr-success); }

    /* Card header row */
    .upg-card__hd { display: flex; align-items: center; gap: 14px; margin-bottom: 20px; }
    .upg-card__icon { font-size: 2rem; flex-shrink: 0; }
    .upg-card__title { font-weight: 700; font-size: 1.05rem; color: var(--pr-text); }
    .upg-card__sub { font-size: 0.82rem; color: var(--pr-text-muted); margin-top: 2px; }

    /* Form header */
    .upg-header { text-align: center; margin-bottom: 28px; }
    .upg-header__icon { font-size: 2.5rem; margin-bottom: 8px; }
    .upg-header__title { font-family: var(--font-display); font-weight: 800; font-size: 1.3rem; color: var(--pr-text); margin: 0 0 6px; }
    .upg-header__sub { color: var(--pr-text-muted); font-size: 0.875rem; line-height: 1.5; margin: 0; }

    /* Detail grid */
    .upg-detail-grid {
      display: grid; grid-template-columns: 1fr 1fr; gap: 12px;
      background: var(--pr-surface-2); border: 1px solid var(--pr-border);
      border-radius: var(--pr-radius); padding: 14px; margin-bottom: 16px;
    }
    .upg-detail { display: flex; flex-direction: column; gap: 2px; }
    .upg-detail--full { grid-column: 1 / -1; }
    .upg-detail__label { font-size: 0.7rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.06em; color: var(--pr-text-muted); }
    .upg-detail__val { font-size: 0.9rem; color: var(--pr-text); }

    /* Reminder */
    .upg-reminder {
      display: flex; align-items: center; gap: 12px; flex-wrap: wrap;
      background: var(--pr-bg); border: 1px solid var(--pr-border);
      border-radius: var(--pr-radius); padding: 12px 14px;
    }
    .upg-reminder__desc { font-size: 0.85rem; color: var(--pr-text-muted); flex: 1; }
    .upg-reminder__ok   { font-size: 0.85rem; color: var(--pr-success); }
    .upg-reminder__err  { font-size: 0.8rem; color: var(--pr-error); width: 100%; }

    /* Rejected notice */
    .upg-rejected-notice {
      background: rgba(255,82,82,0.07); border: 1px solid var(--pr-error);
      border-radius: var(--pr-radius); padding: 12px 14px;
      font-size: 0.875rem; margin-bottom: 20px;
    }
    .upg-rejected-notice__reason { color: var(--pr-text-muted); font-size: 0.8rem; margin-top: 4px; }

    /* Form */
    .upg-form { display: flex; flex-direction: column; gap: 18px; }
    .role-options { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 8px; }
    .role-option {
      display: flex; flex-direction: column; align-items: center; gap: 6px;
      padding: 18px 10px; border: 2px solid var(--pr-border);
      border-radius: var(--pr-radius); cursor: pointer; text-align: center;
      transition: border-color 0.15s, background 0.15s;
    }
    .role-option:hover { border-color: var(--pr-primary); }
    .role-option.selected { border-color: var(--pr-primary); background: color-mix(in srgb, var(--pr-primary) 8%, transparent); }
    .role-option__icon  { font-size: 1.6rem; }
    .role-option__title { font-weight: 600; font-size: 0.875rem; color: var(--pr-text); }
    .role-option__desc  { font-size: 0.72rem; color: var(--pr-text-muted); line-height: 1.4; }
    .form-hint { font-size: 0.78rem; color: var(--pr-text-muted); margin-top: 4px; display: block; }
    .upg-info-box {
      background: var(--pr-surface-2); border: 1px solid var(--pr-border);
      border-radius: var(--pr-radius); padding: 10px 14px;
      font-size: 0.875rem; color: var(--pr-text-muted);
    }

    /* Federation combobox */
    .fed-combo { position: relative; }
    .fed-dropdown {
      position: absolute; top: calc(100% + 4px); left: 0; right: 0; z-index: 200;
      background: var(--pr-surface); border: 1px solid var(--pr-border);
      border-radius: var(--pr-radius); box-shadow: 0 8px 24px rgba(0,0,0,.15);
      max-height: 220px; overflow-y: auto;
    }
    .fed-option {
      display: block; width: 100%; padding: 9px 14px; border: none; background: none;
      cursor: pointer; text-align: left; font-size: 0.875rem; color: var(--pr-text);
      transition: background var(--t-fast);
    }
    .fed-option:hover { background: var(--pr-surface-2); }
    .fed-option--active { background: rgba(30,144,255,0.08); font-weight: 600; }
    .fed-option--empty { cursor: default; color: var(--pr-text-muted); font-style: italic; }

    /* History */
    .upg-history {
      background: var(--pr-surface); border: 1px solid var(--pr-border);
      border-radius: var(--pr-radius); padding: 20px 24px;
    }
    .upg-history__title {
      font-size: 0.72rem; font-weight: 700; text-transform: uppercase;
      letter-spacing: 0.08em; color: var(--pr-text-muted); margin-bottom: 12px;
    }
    .upg-history-item {
      display: flex; align-items: center; gap: 10px; flex-wrap: wrap;
      padding: 10px 0; border-top: 1px solid var(--pr-border);
    }
    .upg-history-item:first-of-type { border-top: none; padding-top: 0; }
    .upg-history-item__left { flex: 1; display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
    .upg-history-item__right { display: flex; align-items: center; gap: 8px; }
    .upg-history-item__role { font-weight: 600; font-size: 0.875rem; }
    .upg-history-item__meta { font-size: 0.78rem; color: var(--pr-text-muted); }
    .upg-history-item__meta::before { content: '·'; margin-right: 6px; }
    .upg-history-item__date { font-size: 0.75rem; color: var(--pr-text-muted); }
    .upg-history-item__reason {
      width: 100%; font-size: 0.78rem; color: var(--pr-error);
      padding: 6px 10px; background: rgba(255,82,82,0.06);
      border-radius: 4px; margin-top: 4px;
    }
  `]
})
export class UpgradeRequestComponent implements OnInit {
  private fb   = inject(FormBuilder);
  private api  = inject(ApiService);
  private auth = inject(AuthService);

  currentUserRole = computed(() => this.auth.currentUser()?.role ?? null);

  form = this.fb.group({
    role:         [null as number | null, Validators.required],
    federationId: [''],
    clubName:     [''],
    notes:        ['']
  });

  checkingExisting   = signal(true);
  loading            = signal(false);
  error              = signal<string | null>(null);
  submitted          = signal(false);
  routedTo           = signal<'federation' | 'admin'>('admin');
  countries = signal<{ name: string; code: string; flag: string; federationId?: string }[]>([]);

  fedSearch        = signal('');
  fedDropdownOpen  = signal(false);
  selectedCountry  = signal<{ name: string; code: string; flag: string; federationId?: string } | null>(null);

  filteredFederations = computed(() => {
    const q = this.fedSearch().toLowerCase().trim();
    const all = this.countries();
    return q ? all.filter(c => c.name.toLowerCase().includes(q)) : all;
  });

  fedSearchDisplay = computed(() => {
    const sel = this.selectedCountry();
    if (sel) return sel.name;
    return this.fedSearch();
  });

  noFedWarning = computed(() => {
    const sel = this.selectedCountry();
    return sel && !sel.federationId && this.form.value.role === 3;
  });

  allRequests        = signal<any[]>([]);
  pendingRequest     = signal<any>(null);
  lastRejected       = signal<any>(null);

  reminderLoading    = signal(false);
  reminderSent       = signal(false);
  reminderError      = signal<string | null>(null);
  reminderCooldownMs = signal(0);

  ngOnInit() {
    const base = WORLD_COUNTRIES.map(c => ({ ...c, flag: flagEmoji(c.code) }));
    this.countries.set(base);

    this.api.getPublicFederations().subscribe({
      next: (list: any[]) => {
        const feds = (list ?? []).map((f: any) => ({ id: String(f.id ?? f.Id), name: String(f.name ?? f.Name) }));
        const fedMap = new Map(feds.map(f => [f.name.toLowerCase(), f.id]));
        const enriched = base.map(c => ({ ...c, federationId: fedMap.get(c.name.toLowerCase()) }));
        this.countries.set(enriched);

        const userFedId = this.auth.FederationId();
        if (userFedId) {
          const fed = feds.find(f => f.id === userFedId);
          if (fed) {
            const country = enriched.find(c => c.name.toLowerCase() === fed.name.toLowerCase());
            if (country) { this.selectedCountry.set(country); this.form.controls.federationId.setValue(country.federationId ?? ''); }
          }
        }
      },
      error: () => {}
    });

    this.form.controls.role.valueChanges.subscribe(() => {
      this.selectedCountry.set(null);
      this.fedSearch.set('');
      this.form.controls.federationId.setValue('');
      this.fedDropdownOpen.set(false);
    });

    this.api.getMyUpgradeRequests().subscribe({
      next: (requests: any[]) => {
        const safe = requests ?? [];
        this.allRequests.set(safe);

        const pending  = safe.find(r => r.status === 0);
        const rejected = safe.filter(r => r.status === 2)
          .sort((a: any, b: any) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())[0];

        if (pending) {
          this.pendingRequest.set(pending);
          this.reminderCooldownMs.set(this.calcCooldown(pending.id));
        } else if (rejected) {
          this.lastRejected.set(rejected);
        }
        this.checkingExisting.set(false);
      },
      error: () => this.checkingExisting.set(false)
    });
  }

  submit() {
    if (this.form.invalid) return;
    const { role, federationId, clubName, notes } = this.form.value;

    this.loading.set(true);
    this.error.set(null);

    const country = this.selectedCountry();
    const finalNotes = role === 2 && country && !federationId
      ? `Country: ${country.name}${notes ? '\n' + notes : ''}`
      : (notes || undefined);

    this.api.submitUpgradeRequest({
      requestedRole: role!,
      federationId: federationId || undefined,
      clubName: clubName || undefined,
      notes: finalNotes
    }).subscribe({
      next: (req: any) => {
        this.routedTo.set((role === 3 && country?.federationId) ? 'federation' : 'admin');
        this.allRequests.update(list => [req, ...list]);
        this.pendingRequest.set(req);
        this.reminderCooldownMs.set(0);
        this.submitted.set(true);
        this.loading.set(false);
      },
      error: (e: any) => {
        this.error.set(e?.error?.message ?? 'Failed to submit request. Please try again.');
        this.loading.set(false);
      }
    });
  }

  sendReminder() {
    const req = this.pendingRequest();
    if (!req) return;
    this.reminderLoading.set(true);
    this.reminderError.set(null);
    this.api.sendUpgradeReminder(req.id).subscribe({
      next: () => {
        localStorage.setItem(`upg_reminder_${req.id}`, Date.now().toString());
        this.reminderSent.set(true);
        this.reminderLoading.set(false);
        this.reminderCooldownMs.set(REMINDER_COOLDOWN_MS);
      },
      error: (e: any) => {
        this.reminderError.set(e?.error?.message ?? 'Failed to send reminder.');
        this.reminderLoading.set(false);
      }
    });
  }

  onFedSearch(value: string) {
    this.fedSearch.set(value);
    this.selectedCountry.set(null);
    this.form.controls.federationId.setValue('');
    this.fedDropdownOpen.set(true);
  }

  selectFederation(f: { name: string; code: string; flag: string; federationId?: string }) {
    this.selectedCountry.set(f);
    this.form.controls.federationId.setValue(f.federationId ?? '');
    this.fedSearch.set(f.name);
    this.fedDropdownOpen.set(false);
  }

  onFedBlur() {
    setTimeout(() => this.fedDropdownOpen.set(false), 150);
  }

  roleName(role: number): string {
    return { 2: 'Federation Manager', 3: 'Club Manager' }[role as 2 | 3] ?? `Role ${role}`;
  }

  federationName(id: string): string {
    return this.countries().find(c => c.federationId === id)?.name ?? '—';
  }

  statusLabel(status: number): string {
    return { 0: 'Pending', 1: 'Approved', 2: 'Rejected', 3: 'Revoked', 4: 'Admin Revoked' }[status] ?? 'Unknown';
  }

  statusBadge(status: number): string {
    return { 1: 'pr-badge--warning', 2: 'pr-badge--success', 3: 'pr-badge--error' }[status] ?? '';
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  cooldownLabel(): string {
    const ms = this.reminderCooldownMs();
    if (ms <= 0) return '';
    const h = Math.ceil(ms / 3600000);
    return h === 1 ? '1 hour' : `${h} hours`;
  }

  private calcCooldown(requestId: string): number {
    const stored = localStorage.getItem(`upg_reminder_${requestId}`);
    if (!stored) return 0;
    return Math.max(0, REMINDER_COOLDOWN_MS - (Date.now() - parseInt(stored, 10)));
  }
}

// ── Admin Login Component ─────────────────────────────────────────────────────

@Component({
  selector: 'app-admin-login',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe],
  template: `
    <div class="adm-shell">
      <div class="adm-card">
        <div class="adm-brand">
          <div class="adm-brand__icon">🛡️</div>
          <div class="adm-brand__title">{{ 'auth.adminPortal' | translate }}</div>
          <div class="adm-brand__sub">Pigeon Result Calculator</div>
        </div>

        <form [formGroup]="form" (ngSubmit)="submit()" class="adm-form">
          @if (error()) {
            <div class="adm-error">{{ error() }}</div>
          }
          <div class="adm-field">
            <label class="adm-label">{{ 'auth.email' | translate }}</label>
            <input class="adm-input" type="email" formControlName="email"
                   placeholder="admin@prc.local" autocomplete="username">
          </div>
          <div class="adm-field">
            <label class="adm-label">{{ 'auth.password' | translate }}</label>
            <input class="adm-input" type="password" formControlName="password"
                   placeholder="••••••••" autocomplete="current-password">
          </div>
          <button class="adm-btn" type="submit" [disabled]="loading() || form.invalid">
            @if (loading()) {
              <span class="adm-spinner"></span>
            } @else {
              {{ 'auth.signIn' | translate }}
            }
          </button>
        </form>

        <div class="adm-footer">{{ 'auth.adminSignInSub' | translate }}</div>
      </div>
    </div>
  `,
  styles: [`
    .adm-shell {
      min-height: 100vh;
      background: #0d1117;
      display: flex; align-items: center; justify-content: center;
      padding: 24px;
    }

    .adm-card {
      width: 100%; max-width: 380px;
      background: #161b22;
      border: 1px solid #30363d;
      border-radius: 12px;
      padding: 40px 36px;
      display: flex; flex-direction: column; gap: 28px;
    }

    .adm-brand { text-align: center; }
    .adm-brand__icon { font-size: 2.4rem; margin-bottom: 10px; }
    .adm-brand__title {
      font-size: 1.15rem; font-weight: 700; color: #e6edf3;
      letter-spacing: 0.02em;
    }
    .adm-brand__sub { font-size: 0.78rem; color: #8b949e; margin-top: 4px; }

    .adm-form { display: flex; flex-direction: column; gap: 16px; }

    .adm-error {
      background: rgba(248,81,73,0.1); border: 1px solid rgba(248,81,73,0.4);
      color: #f85149; border-radius: 6px;
      padding: 10px 14px; font-size: 0.85rem;
    }

    .adm-field { display: flex; flex-direction: column; gap: 6px; }
    .adm-label { font-size: 0.78rem; font-weight: 600; color: #8b949e; text-transform: uppercase; letter-spacing: 0.05em; }
    .adm-input {
      background: #0d1117; border: 1px solid #30363d; border-radius: 6px;
      color: #e6edf3; font-size: 0.9rem; padding: 10px 12px;
      outline: none; transition: border-color 0.15s;
    }
    .adm-input::placeholder { color: #484f58; }
    .adm-input:focus { border-color: #388bfd; }

    .adm-btn {
      background: #238636; border: 1px solid #2ea043; border-radius: 6px;
      color: #fff; font-size: 0.9rem; font-weight: 600;
      padding: 10px; cursor: pointer; margin-top: 4px;
      display: flex; align-items: center; justify-content: center; gap: 8px;
      transition: background 0.15s;
    }
    .adm-btn:hover:not(:disabled) { background: #2ea043; }
    .adm-btn:disabled { opacity: 0.5; cursor: not-allowed; }

    .adm-spinner {
      width: 16px; height: 16px; border-radius: 50%;
      border: 2px solid rgba(255,255,255,0.3);
      border-top-color: #fff;
      animation: spin 0.7s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    .adm-footer {
      text-align: center; font-size: 0.72rem;
      color: #484f58; border-top: 1px solid #21262d; padding-top: 20px;
    }
  `]
})
export class AdminLoginComponent {
  private fb   = inject(FormBuilder);
  private api  = inject(ApiService);
  private auth = inject(AuthService);
  private router = inject(Router);

  form = this.fb.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  loading = signal(false);
  error   = signal<string | null>(null);

  submit() {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);
    const { email, password } = this.form.value;
    this.api.adminLogin(email!, password!).subscribe({
      next: (res) => {
        this.auth.setAdminSession(res.token, res.userId, res.fullName);
        this.router.navigate(['/admin/dashboard']);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message ?? 'Invalid credentials or insufficient permissions.');
        this.loading.set(false);
      }
    });
  }
}

// ── Unauthorized Component ────────────────────────────────────────────────────

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="pr-empty" style="min-height:100vh;display:flex;flex-direction:column;justify-content:center">
      <div class="pr-empty__icon">🚫</div>
      <div class="pr-empty__title">Access Denied</div>
      <p class="pr-empty__desc">You don't have permission to view this page.</p>
      <a routerLink="/auth/login" class="pr-btn pr-btn--primary" style="margin:24px auto 0">Go Home</a>
    </div>
  `
})
export class UnauthorizedComponent {}
