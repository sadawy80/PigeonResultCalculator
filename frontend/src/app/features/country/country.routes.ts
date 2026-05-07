// ── src/app/features/country/country.routes.ts ────────────────────────────────
import { Routes } from '@angular/router';

export const COUNTRY_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('../../shared/components/shell.component').then(m => m.ShellComponent),
    children: [
      { path: 'dashboard', loadComponent: () => import('./country-dashboard.component').then(m => m.CountryDashboardComponent) },
      { path: 'results',   loadComponent: () => import('./country-results.component').then(m => m.CountryResultsComponent) },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  }
];
