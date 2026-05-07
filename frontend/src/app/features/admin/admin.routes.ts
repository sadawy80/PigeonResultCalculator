// ── src/app/features/admin/admin.routes.ts ────────────────────────────────────
import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('../../shared/components/shell.component').then(m => m.ShellComponent),
    children: [
      { path: 'dashboard',     loadComponent: () => import('./admin-dashboard.component').then(m => m.AdminDashboardComponent) },
      { path: 'countries',     loadComponent: () => import('./admin-countries.component').then(m => m.AdminCountriesComponent) },
      { path: 'clubs',         loadComponent: () => import('./admin-clubs.component').then(m => m.AdminClubsComponent) },
      { path: 'users',         loadComponent: () => import('./admin-users.component').then(m => m.AdminUsersComponent) },
      { path: 'subscriptions', loadComponent: () => import('./admin-subscriptions.component').then(m => m.AdminSubscriptionsComponent) },
      { path: 'events',        loadComponent: () => import('./admin-events.component').then(m => m.AdminEventsComponent) },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  }
];
