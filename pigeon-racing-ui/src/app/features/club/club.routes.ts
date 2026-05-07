// ── src/app/features/club/club.routes.ts ──────────────────────────────────────
import { Routes } from '@angular/router';

export const CLUB_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('../../shared/components/shell.component').then(m => m.ShellComponent),
    children: [
      { path: 'dashboard', loadComponent: () => import('./club-dashboard.component').then(m => m.ClubDashboardComponent) },

      // Races
      { path: 'races',         loadComponent: () => import('./club-dashboard.component').then(m => m.RaceListComponent) },
      { path: 'races/new',     loadComponent: () => import('./race-form.component').then(m => m.RaceFormComponent) },
      { path: 'races/:id',     loadComponent: () => import('./race-detail.component').then(m => m.RaceDetailComponent) },
      { path: 'races/:id/edit',loadComponent: () => import('./race-form.component').then(m => m.RaceFormComponent) },

      // Programmes — hub for all aggregate result types
      { path: 'programmes',         loadComponent: () => import('./programme-list.component').then(m => m.ProgrammeListComponent) },
      { path: 'programmes/new',     loadComponent: () => import('./programme-form.component').then(m => m.ProgrammeFormComponent) },
      { path: 'programmes/:id',         loadComponent: () => import('./programme-list.component').then(m => m.ProgrammeDetailComponent) },
      { path: 'programmes/:id/edit',    loadComponent: () => import('./programme-form.component').then(m => m.ProgrammeFormComponent) },

      // The four result type pages — each is a dedicated route under the programme
      { path: 'programmes/:id/race-results',     loadComponent: () => import('./result-pages.component').then(m => m.ProgrammeRaceResultsComponent) },
      { path: 'programmes/:id/best-loft',        loadComponent: () => import('./result-pages.component').then(m => m.BestLoftResultsComponent) },
      { path: 'programmes/:id/ace-pigeon',       loadComponent: () => import('./result-pages.component').then(m => m.AcePigeonResultsComponent) },
      { path: 'programmes/:id/super-ace-pigeon', loadComponent: () => import('./result-pages.component').then(m => m.SuperAcePigeonResultsComponent) },

      // Templates & printing
      { path: 'templates', loadComponent: () => import('./templates/templates-page.component').then(m => m.TemplatesPageComponent) },

      // External integrations
      { path: 'integrations', loadComponent: () => import('./integrations/club-integrations.component').then(m => m.ClubIntegrationsComponent) },

      // Other pages
      { path: 'members',       loadComponent: () => import('./club-members.component').then(m => m.ClubMembersComponent) },
      { path: 'page',          loadComponent: () => import('./theme-picker.component').then(m => m.ClubPageEditorComponent) },
      { path: 'notifications', loadComponent: () => import('../fancier/notifications.component').then(m => m.NotificationsComponent) },

      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  }
];
