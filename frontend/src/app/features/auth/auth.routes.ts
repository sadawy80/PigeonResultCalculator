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
  { path: '', redirectTo: 'login', pathMatch: 'full' }
];
