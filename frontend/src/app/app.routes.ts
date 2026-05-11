import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/services/services';
import { UserRole } from './core/models';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/public/landing.component').then(m => m.LandingComponent),
    pathMatch: 'full'
  },

  // Auth
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES)
  },

  // Club Manager
  {
    path: 'club',
    canActivate: [authGuard, roleGuard([UserRole.ClubManager, UserRole.SuperAdmin])],
    loadChildren: () => import('./features/club/club.routes').then(m => m.CLUB_ROUTES)
  },

  // Federation Manager
  {
    path: 'federation',
    canActivate: [authGuard, roleGuard([UserRole.FederationManager, UserRole.SuperAdmin])],
    loadChildren: () => import('./features/federation/federation.routes').then(m => m.FEDERATION_ROUTES)
  },

  // Fancier
  {
    path: 'fancier',
    canActivate: [authGuard, roleGuard([UserRole.Fancier, UserRole.SuperAdmin])],
    loadChildren: () => import('./features/fancier/fancier.routes').then(m => m.FANCIER_ROUTES)
  },

  // Admin login — must be before the guarded /admin route
  {
    path: 'admin/login',
    loadComponent: () => import('./features/auth/auth.components').then(m => m.AdminLoginComponent)
  },

  // Super Admin
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard([UserRole.SuperAdmin])],
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES)
  },

  // Public pages
  {
    path: 'contact',
    loadComponent: () => import('./features/public/contact.component').then(m => m.ContactComponent)
  },
  {
    path: 'p/:slug',
    loadComponent: () => import('./features/public/public-club-page.component').then(m => m.PublicClubPageComponent)
  },
  {
    path: 'c/:slug',
    loadComponent: () => import('./features/public/public-federation-page.component').then(m => m.PublicFederationPageComponent)
  },

  // Settings + Upgrade Request (all authenticated users — wrapped in shell)
  {
    path: 'settings',
    canActivate: [authGuard],
    loadComponent: () => import('./shared/components/shell.component').then(m => m.ShellComponent),
    children: [
      { path: '', loadComponent: () => import('./features/settings/settings.component').then(m => m.SettingsComponent) }
    ]
  },
  {
    path: 'auth/upgrade-request',
    canActivate: [authGuard],
    loadComponent: () => import('./shared/components/shell.component').then(m => m.ShellComponent),
    children: [
      { path: '', loadComponent: () => import('./features/auth/auth.components').then(m => m.UpgradeRequestComponent) }
    ]
  },

  { path: 'unauthorized', loadComponent: () => import('./shared/components/unauthorized.component').then(m => m.UnauthorizedComponent) },
  { path: '**', redirectTo: '/auth/login' }
];
