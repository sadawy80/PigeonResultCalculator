// ── src/app/features/fancier/fancier.routes.ts ────────────────────────────────
import { Routes } from '@angular/router';

export const FANCIER_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('../../shared/components/shell.component').then(m => m.ShellComponent),
    children: [
      { path: 'dashboard',     loadComponent: () => import('./fancier-dashboard.component').then(m => m.FancierDashboardComponent) },
      { path: 'results',       loadComponent: () => import('./fancier-dashboard.component').then(m => m.FancierResultsComponent) },
      { path: 'pigeons',       loadComponent: () => import('./fancier-dashboard.component').then(m => m.FancierPigeonsComponent) },
      { path: 'notifications', loadComponent: () => import('./notifications.component').then(m => m.NotificationsComponent) },
      { path: 'integrations',  loadComponent: () => import('./integrations/fancier-integrations.component').then(m => m.FancierIntegrationsComponent) },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  }
];
