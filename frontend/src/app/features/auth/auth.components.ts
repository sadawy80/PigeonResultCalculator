import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { NgIf, NgClass, NgFor } from '@angular/common';
import { AuthService } from '../../core/services/services';
import { ApiService } from '../../core/services/api.service';

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

          <div style="display:flex;justify-content:flex-end">
            <a routerLink="/auth/forgot-password" class="pr-link" style="font-size:0.875rem">Forgot password?</a>
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

// ── Register Component ────────────────────────────────────────────────────────

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-shell">
      <div class="auth-panel">
        <div class="auth-brand">
          <div class="auth-brand__icon">🕊️</div>
          <h1 class="auth-brand__title">Pigeon Result Calculator</h1>
          <p class="auth-brand__sub">Create your account</p>
        </div>

        @if (submitted()) {
          <div class="pending-card">
            <div class="pending-card__icon">✅</div>
            <h2 class="pending-card__title">Account created!</h2>
            <p class="pending-card__body">
              You're signed up as a viewer — you can browse and view race results right away.
              Want to manage a club or federation? Submit a role upgrade request after signing in.
            </p>
            <a routerLink="/auth/login" class="pr-btn pr-btn--primary" style="margin-top:8px">
              Sign In
            </a>
          </div>
        } @else {
          <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
            <h2 class="auth-form__heading">Get Started</h2>

            @if (error()) {
              <div class="pr-alert pr-alert--error">{{ error() }}</div>
            }

            <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px">
              <div class="pr-form-group">
                <label class="pr-label">First Name</label>
                <input class="pr-input" formControlName="firstName" placeholder="John" autocomplete="given-name">
              </div>
              <div class="pr-form-group">
                <label class="pr-label">Last Name</label>
                <input class="pr-input" formControlName="lastName" placeholder="Smith" autocomplete="family-name">
              </div>
            </div>

            <div class="pr-form-group">
              <label class="pr-label">Email</label>
              <input class="pr-input" type="email" formControlName="email" placeholder="you@example.com" autocomplete="email">
            </div>

            <div class="pr-form-group">
              <label class="pr-label">Password</label>
              <input class="pr-input" type="password" formControlName="password" placeholder="Min 8 characters" autocomplete="new-password">
            </div>

            <button class="pr-btn pr-btn--primary w-full"
                    type="submit"
                    [disabled]="loading() || form.invalid">
              @if (loading()) { <span class="pr-spinner" style="width:16px;height:16px"></span> }
              Create Account
            </button>

            <p class="auth-form__footer">
              Already have an account? <a routerLink="/auth/login">Sign in</a>
            </p>
            <p class="auth-form__footer">
              Have an invitation? <a routerLink="/auth/accept-invitation">Accept it here</a>
            </p>
          </form>
        }
      </div>

      <div class="auth-visual">
        <div class="auth-visual__content">
          <blockquote class="auth-quote">
            "Manage your club, publish results, celebrate champions."
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
  private fb  = inject(FormBuilder);
  private api = inject(ApiService);

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
        this.error.set(e?.error?.message ?? 'Registration failed. Please try again.');
        this.loading.set(false);
      }
    });
  }
}

// ── Accept Invitation Component ───────────────────────────────────────────────

@Component({
  selector: 'app-accept-invitation',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
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

// ── Forgot Password Component ─────────────────────────────────────────────────

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
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
            <h2>Check your email</h2>
            <p>If an account exists for <strong>{{ emailSent() }}</strong>, you will receive a password reset link shortly.</p>
            <a routerLink="/auth/login" class="pr-btn pr-btn--secondary" style="margin-top:16px">Back to Sign In</a>
          </div>
        } @else {
          <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
            <h2 class="auth-form__heading">Forgot Password</h2>
            <p class="auth-form__sub">Enter your email address and we'll send you a reset link.</p>

            @if (error()) { <div class="pr-alert pr-alert--error">{{ error() }}</div> }

            <div class="pr-form-group">
              <label class="pr-label">Email Address</label>
              <input class="pr-input" type="email" formControlName="email"
                     placeholder="you@example.com" autocomplete="email">
            </div>

            <button class="pr-btn pr-btn--primary w-full"
                    type="submit" [disabled]="loading() || form.invalid">
              @if (loading()) { <span class="pr-spinner" style="width:16px;height:16px"></span> }
              Send Reset Link
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
  imports: [ReactiveFormsModule, RouterLink],
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
            <h2>Password updated</h2>
            <p>Your password has been changed successfully. You can now sign in with your new password.</p>
            <a routerLink="/auth/login" class="pr-btn pr-btn--primary" style="margin-top:16px">Sign In</a>
          </div>
        } @else {
          <form [formGroup]="form" (ngSubmit)="submit()" class="auth-form">
            <h2 class="auth-form__heading">Set New Password</h2>

            @if (error()) { <div class="pr-alert pr-alert--error">{{ error() }}</div> }

            <div class="pr-form-group">
              <label class="pr-label">New Password</label>
              <input class="pr-input" type="password" formControlName="password"
                     placeholder="Min 8 characters" autocomplete="new-password">
            </div>

            <div class="pr-form-group">
              <label class="pr-label">Confirm Password</label>
              <input class="pr-input" type="password" formControlName="confirmPassword"
                     placeholder="Repeat your password" autocomplete="new-password">
              @if (form.hasError('mismatch') && form.get('confirmPassword')?.touched) {
                <span class="pr-field-error">Passwords do not match</span>
              }
            </div>

            <button class="pr-btn pr-btn--primary w-full"
                    type="submit" [disabled]="loading() || form.invalid">
              @if (loading()) { <span class="pr-spinner" style="width:16px;height:16px"></span> }
              Update Password
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

@Component({
  selector: 'app-upgrade-request',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, NgFor],
  template: `
    <div class="auth-shell-center">
      <div class="auth-card" style="max-width:520px">
        <div class="auth-brand">
          <div class="auth-brand__icon">📋</div>
          <h1 class="auth-brand__title">Request a Role Upgrade</h1>
          <p style="color:var(--pr-text-muted);font-size:0.875rem;margin-top:4px;text-align:center">
            Tell us what you'd like to do and we'll route your request to the right approver.
          </p>
        </div>

        @if (submitted()) {
          <div style="text-align:center;display:flex;flex-direction:column;align-items:center;gap:12px">
            <div style="font-size:3rem">✅</div>
            <h2 style="font-family:var(--font-display);font-size:1.2rem;font-weight:700">Request submitted!</h2>
            <p style="color:var(--pr-text-muted);font-size:0.9rem;line-height:1.6">
              Your upgrade request has been sent for review.
              @if (routedTo() === 'federation') {
                The federation manager will be notified and will review your request.
              } @else {
                The super admin will review your request and assign your role.
              }
            </p>
            <a routerLink="/" class="pr-btn pr-btn--primary" style="margin-top:8px">Go to Dashboard</a>
          </div>
        } @else {
          @if (error()) { <div class="pr-alert pr-alert--error" style="margin-bottom:16px">{{ error() }}</div> }

          <form [formGroup]="form" (ngSubmit)="submit()" style="display:flex;flex-direction:column;gap:20px">

            <div class="pr-form-group">
              <label class="pr-label">I want to become a…</label>
              <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-top:8px">
                <label class="role-option" [class.selected]="form.value.role === 3">
                  <input type="radio" formControlName="role" [value]="3" style="display:none">
                  <div class="role-option__icon">🏟️</div>
                  <div class="role-option__title">Club Manager</div>
                  <div class="role-option__desc">Manage a racing club, enter results, invite members</div>
                </label>
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
                <label class="pr-label">Select your Federation</label>
                <select class="pr-input" formControlName="federationId">
                  <option value="">— choose a federation —</option>
                  @for (f of federations(); track f.id) {
                    <option [value]="f.id">{{ f.name }} ({{ f.code }})</option>
                  }
                </select>
                <span style="font-size:0.8rem;color:var(--pr-text-muted);margin-top:4px;display:block">
                  Your request will go to the federation manager for approval.
                  If the federation has no manager yet, it will go to the super admin.
                </span>
              </div>
            }

            @if (form.value.role === 2) {
              <div class="pr-alert" style="background:var(--pr-bg);border:1px solid var(--pr-border);border-radius:var(--pr-radius);padding:12px 16px;font-size:0.875rem;color:var(--pr-text-muted)">
                Federation manager requests are reviewed directly by the super admin.
              </div>
            }

            <div class="pr-form-group">
              <label class="pr-label">Additional notes <span style="color:var(--pr-text-muted)">(optional)</span></label>
              <textarea class="pr-input" formControlName="notes" rows="3"
                placeholder="Tell us a bit about yourself or your club…"
                style="resize:vertical"></textarea>
            </div>

            <button class="pr-btn pr-btn--primary w-full"
                    type="submit"
                    [disabled]="loading() || form.invalid || (form.value.role === 3 && !form.value.federationId)">
              @if (loading()) { <span class="pr-spinner" style="width:16px;height:16px"></span> }
              Submit Request
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
      width: 100%;
    }
    .auth-brand { text-align: center; margin-bottom: 32px; }
    .auth-brand__icon { font-size: 2.5rem; margin-bottom: 8px; }
    .auth-brand__title { font-family: var(--font-display); font-weight: 800; font-size: 1.4rem; color: var(--pr-text); }
    .role-option {
      display: flex; flex-direction: column; align-items: center; gap: 6px;
      padding: 20px 12px; border: 2px solid var(--pr-border);
      border-radius: var(--pr-radius); cursor: pointer; text-align: center;
      transition: border-color 0.15s, background 0.15s;
    }
    .role-option:hover { border-color: var(--pr-primary); }
    .role-option.selected { border-color: var(--pr-primary); background: color-mix(in srgb, var(--pr-primary) 8%, transparent); }
    .role-option__icon { font-size: 1.75rem; }
    .role-option__title { font-weight: 600; font-size: 0.9rem; color: var(--pr-text); }
    .role-option__desc { font-size: 0.75rem; color: var(--pr-text-muted); line-height: 1.4; }
  `]
})
export class UpgradeRequestComponent implements OnInit {
  private fb     = inject(FormBuilder);
  private api    = inject(ApiService);
  private router = inject(Router);

  form = this.fb.group({
    role:         [null as number | null, Validators.required],
    federationId: [''],
    notes:        ['']
  });

  loading    = signal(false);
  error      = signal<string | null>(null);
  submitted  = signal(false);
  routedTo   = signal<'federation' | 'admin'>('admin');
  federations = signal<{ id: string; name: string; code: string }[]>([]);

  ngOnInit() {
    this.api.getPublicFederations().subscribe({
      next: (list: any[]) => {
        // API returns objects; map to id/name/code
        this.federations.set(list.map((f: any) => ({
          id: f.id ?? f.Id,
          name: f.name ?? f.Name,
          code: f.code ?? f.Code
        })));
      },
      error: () => {}
    });
  }

  submit() {
    if (this.form.invalid) return;
    const { role, federationId, notes } = this.form.value;
    if (role === 3 && !federationId) return;

    this.loading.set(true);
    this.error.set(null);

    this.api.submitUpgradeRequest({
      requestedRole: role!,
      federationId: federationId || undefined,
      notes: notes || undefined
    }).subscribe({
      next: () => {
        this.routedTo.set(role === 3 ? 'federation' : 'admin');
        this.submitted.set(true);
        this.loading.set(false);
      },
      error: (e: any) => {
        this.error.set(e?.error?.message ?? 'Failed to submit request. Please try again.');
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
