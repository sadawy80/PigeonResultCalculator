// ── src/app/features/admin/admin.routes.ts ────────────────────────────────────
import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('../../shared/components/shell.component').then(m => m.ShellComponent),
    children: [
      { path: 'dashboard',     loadComponent: () => import('./admin-dashboard.component').then(m => m.AdminDashboardComponent) },
      { path: 'federations',        loadComponent: () => import('./admin-federations.component').then(m => m.AdminFederationsComponent) },
      { path: 'upgrade-requests',  loadComponent: () => import('./admin-upgrade-requests.component').then(m => m.AdminUpgradeRequestsComponent) },
      { path: 'clubs',         loadComponent: () => import('./admin-clubs.component').then(m => m.AdminClubsComponent) },
      { path: 'users',         loadComponent: () => import('./admin-users.component').then(m => m.AdminUsersComponent) },
      { path: 'subscriptions', loadComponent: () => import('./admin-subscriptions.component').then(m => m.AdminSubscriptionsComponent) },
      { path: 'events',        loadComponent: () => import('./admin-events.component').then(m => m.AdminEventsComponent) },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  }
];
