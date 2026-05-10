// ── src/app/features/admin/admin.routes.ts ────────────────────────────────────
import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('../../shared/components/shell.component').then(m => m.ShellComponent),
    children: [
      { path: 'dashboard',      loadComponent: () => import('./admin-dashboard.component').then(m => m.AdminDashboardComponent) },
      { path: 'federations',    loadComponent: () => import('./admin-federations.component').then(m => m.AdminFederationsComponent) },
      { path: 'upgrade-requests', loadComponent: () => import('./admin-upgrade-requests.component').then(m => m.AdminUpgradeRequestsComponent) },
      { path: 'clubs',          loadComponent: () => import('./admin-clubs.component').then(m => m.AdminClubsComponent) },
      { path: 'users',          loadComponent: () => import('./admin-users.component').then(m => m.AdminUsersComponent) },
      { path: 'plans',          loadComponent: () => import('./admin-plans.component').then(m => m.AdminPlansComponent) },
      { path: 'subscriptions',  loadComponent: () => import('./admin-subscriptions.component').then(m => m.AdminSubscriptionsComponent) },
      { path: 'events',         loadComponent: () => import('./admin-events.component').then(m => m.AdminEventsComponent) },
      { path: 'races',          loadComponent: () => import('./admin-race-results.component').then(m => m.AdminRaceResultsComponent) },
      { path: 'results/ace',        loadComponent: () => import('./admin-ace-results.component').then(m => m.AdminAceResultsComponent) },
      { path: 'results/super-ace',  loadComponent: () => import('./admin-super-ace.component').then(m => m.AdminSuperAceComponent) },
      { path: 'results/best-loft',  loadComponent: () => import('./admin-best-loft.component').then(m => m.AdminBestLoftComponent) },
      { path: 'pigeons',        loadComponent: () => import('./admin-pigeons.component').then(m => m.AdminPigeonsComponent) },
      { path: 'fanciers',       loadComponent: () => import('./admin-fanciers.component').then(m => m.AdminFanciersComponent) },
      { path: 'link-requests',  loadComponent: () => import('./admin-link-requests.component').then(m => m.AdminLinkRequestsComponent) },
      { path: 'programmes',     loadComponent: () => import('./admin-programmes.component').then(m => m.AdminProgrammesComponent) },
      { path: 'notifications',  loadComponent: () => import('./admin-notifications.component').then(m => m.AdminNotificationsComponent) },
      { path: 'backups',        loadComponent: () => import('./admin-backups.component').then(m => m.AdminBackupsComponent) },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  }
];
