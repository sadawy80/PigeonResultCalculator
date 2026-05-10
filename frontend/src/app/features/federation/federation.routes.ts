// ── src/app/features/federation/federation.routes.ts ────────────────────────────────
import { Routes } from '@angular/router';

export const FEDERATION_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('../../shared/components/shell.component').then(m => m.ShellComponent),
    children: [
      { path: 'dashboard', loadComponent: () => import('./federation-dashboard.component').then(m => m.FederationDashboardComponent) },
      { path: 'results',   loadComponent: () => import('./federation-results.component').then(m => m.FederationResultsComponent) },
      { path: 'page',             loadComponent: () => import('./federation-page-editor.component').then(m => m.FederationPageEditorComponent) },
      { path: 'upgrade-requests', loadComponent: () => import('./federation-upgrade-requests.component').then(m => m.FederationUpgradeRequestsComponent) },
      { path: 'notifications',   loadComponent: () => import('../fancier/notifications.component').then(m => m.NotificationsComponent) },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  }
];
