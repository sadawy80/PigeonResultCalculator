// ── src/app/features/auth/auth.routes.ts ──────────────────────────────────────
import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./auth.components').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./auth.components').then(m => m.RegisterComponent)
  },
  {
    path: 'accept-invitation',
    loadComponent: () => import('./auth.components').then(m => m.AcceptInvitationComponent)
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./auth.components').then(m => m.ForgotPasswordComponent)
  },
  {
    path: 'reset-password',
    loadComponent: () => import('./auth.components').then(m => m.ResetPasswordComponent)
  },
  {
    path: 'verify-email',
    loadComponent: () => import('./auth.components').then(m => m.VerifyEmailComponent)
  },
  {
    path: 'resend-verification',
    loadComponent: () => import('./auth.components').then(m => m.ResendVerificationComponent)
  },
  {
    path: 'upgrade-request',
    loadComponent: () => import('./auth.components').then(m => m.UpgradeRequestComponent)
  },
  { path: '', redirectTo: 'login', pathMatch: 'full' }
];
