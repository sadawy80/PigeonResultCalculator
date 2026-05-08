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
    loadChildren: () => import('./features/federation/federation.routes').then(m => m.COUNTRY_ROUTES)
  },

  // Fancier
  {
    path: 'fancier',
    canActivate: [authGuard, roleGuard([UserRole.Fancier, UserRole.SuperAdmin])],
    loadChildren: () => import('./features/fancier/fancier.routes').then(m => m.FANCIER_ROUTES)
  },

  // Super Admin
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard([UserRole.SuperAdmin])],
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES)
  },

  // Public pages
  {
    path: 'p/:slug',
    loadComponent: () => import('./features/public/public-club-page.component').then(m => m.PublicClubPageComponent)
  },
  {
    path: 'c/:slug',
    loadComponent: () => import('./features/public/public-federation-page.component').then(m => m.PublicFederationPageComponent)
  },

  { path: 'unauthorized', loadComponent: () => import('./shared/components/unauthorized.component').then(m => m.UnauthorizedComponent) },
  { path: '**', redirectTo: '/auth/login' }
];
