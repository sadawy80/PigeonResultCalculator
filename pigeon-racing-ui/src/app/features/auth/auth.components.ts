import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NgIf, NgClass } from '@angular/common';
import { AuthService } from '../../core/services/services';

// ── Login Component ───────────────────────────────────────────────────────────

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, NgIf, NgClass],
  template: `
    <div class="auth-shell">
      <div class="auth-panel">
        <div class="auth-brand">
          <div class="auth-brand__icon">🕊️</div>
          <h1 class="auth-brand__title">Pigeon Result Calculator</h1>
          <p class="auth-brand__sub">Professional results management for racing clubs</p>
        </div>

        <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
          <h2 class="auth-form__heading">Sign In</h2>

          @if (error()) {
            <div class="pr-alert pr-alert--error">{{ error() }}</div>
          }

          <div class="pr-form-group">
            <label class="pr-label">Email</label>
            <input class="pr-input" type="email" formControlName="email" placeholder="you@example.com" autocomplete="email">
          </div>

          <div class="pr-form-group">
            <label class="pr-label">Password</label>
            <input class="pr-input" type="password" formControlName="password" placeholder="••••••••" autocomplete="current-password">
          </div>

          <button class="pr-btn pr-btn--primary w-full"
                  type="submit"
                  [disabled]="loading() || form.invalid">
            @if (loading()) { <span class="pr-spinner" style="width:16px;height:16px"></span> }
            Sign In
          </button>

          <p class="auth-form__footer">
            Have an invitation? <a routerLink="/auth/accept-invitation">Accept it here</a>
          </p>
        </form>
      </div>

      <div class="auth-visual">
        <div class="auth-visual__content">
          <blockquote class="auth-quote">
            "Speed, precision, glory — every second counts."
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
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);

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
    try {
      this.auth.login(email!, password!);
    } catch {
      this.error.set('Invalid credentials. Please try again.');
      this.loading.set(false);
    }
  }
}

// ── Accept Invitation Component ───────────────────────────────────────────────

@Component({
  selector: 'app-accept-invitation',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, NgIf],
  template: `
    <div class="auth-shell">
      <div class="auth-panel">
        <div class="auth-brand">
          <div class="auth-brand__icon">🕊️</div>
          <h1 class="auth-brand__title">Join the Club</h1>
          <p class="auth-brand__sub">Complete your registration</p>
        </div>

        <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
          @if (error()) { <div class="pr-alert pr-alert--error">{{ error() }}</div> }
          @if (success()) { <div class="pr-alert pr-alert--success">{{ success() }}</div> }

          <div class="pr-form-group">
            <label class="pr-label">Invitation Token</label>
            <input class="pr-input" formControlName="token" placeholder="Paste your invitation token">
          </div>
          <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px">
            <div class="pr-form-group">
              <label class="pr-label">First Name</label>
              <input class="pr-input" formControlName="firstName" placeholder="John">
            </div>
            <div class="pr-form-group">
              <label class="pr-label">Last Name</label>
              <input class="pr-input" formControlName="lastName" placeholder="Smith">
            </div>
          </div>
          <div class="pr-form-group">
            <label class="pr-label">Email</label>
            <input class="pr-input" type="email" formControlName="email" placeholder="you@example.com">
          </div>
          <div class="pr-form-group">
            <label class="pr-label">Password</label>
            <input class="pr-input" type="password" formControlName="password" placeholder="Min 8 characters">
          </div>

          <button class="pr-btn pr-btn--primary w-full"
                  type="submit" [disabled]="loading() || form.invalid">
            @if (loading()) { <span class="pr-spinner" style="width:16px;height:16px"></span> }
            Create Account
          </button>

          <p class="auth-form__footer">
            Already have an account? <a routerLink="/auth/login">Sign in</a>
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
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);

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
      next: () => { this.success.set('Account created! Signing you in...'); },
      error: (e: any) => { this.error.set(e?.error?.message ?? 'Registration failed.'); this.loading.set(false); }
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
