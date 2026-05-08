import { Component, inject, signal, computed } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NgClass, NgIf } from '@angular/common';
import { AuthService, ThemeService } from '../../core/services/services';
import { UserRole, SiteTheme } from '../../core/models';
import { TranslationService, TranslatePipe, LanguageSwitcherComponent } from '../../core/i18n';
import { IntegrationBadgeService } from '../../core/services/integration-badge.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet, NgClass, NgIf, TranslatePipe, LanguageSwitcherComponent],
  template: `
    <div class="shell" [class.shell--collapsed]="sidebarCollapsed()" [attr.dir]="i18n.dir()">

      <!-- Sidebar -->
      <aside class="sidebar">
        <div class="sidebar__header">
          <div class="sidebar__logo">
            <span class="sidebar__logo-icon">🕊️</span>
            <span class="sidebar__logo-text">PRC Platform</span>
          </div>
          <button class="sidebar__toggle pr-btn pr-btn--ghost pr-btn--icon"
                  (click)="sidebarCollapsed.set(!sidebarCollapsed())"
                  [attr.aria-label]="sidebarCollapsed() ? 'Expand sidebar' : 'Collapse sidebar'">
            {{ toggleIcon() }}
          </button>
        </div>

        <nav class="sidebar__nav">
          <div class="sidebar__section-label">{{ 'nav.dashboard' | translate }}</div>

          @for (item of navItems(); track item.label) {
            <a class="sidebar__link"
               [routerLink]="item.path"
               routerLinkActive="sidebar__link--active">
              <span class="sidebar__link-icon">{{ item.icon }}</span>
              <span class="sidebar__link-label">{{ item.label | translate }}</span>
              @if (item.path.includes('integrations') && badge.pendingCount() > 0) {
                <span class="sidebar__badge">{{ badge.pendingCount() }}</span>
              }
            </a>
          }

          <div class="sidebar__section-label" style="margin-top: 24px">{{ 'common.select' | translate }}</div>

          <a class="sidebar__link" routerLink="settings" routerLinkActive="sidebar__link--active">
            <span class="sidebar__link-icon">⚙️</span>
            <span class="sidebar__link-label">Settings</span>
          </a>

        </nav>

        <!-- Language + Theme — outside the scrollable nav so dropdowns aren't clipped -->
        <div class="sidebar__controls">
          <div class="sidebar__lang">
            <div class="sidebar__section-label">Language / اللغة</div>
            <app-language-switcher></app-language-switcher>
          </div>
          <div class="sidebar__themes">
            <div class="sidebar__section-label">Theme</div>
            <div class="theme-swatches">
              @for (t of themes; track t.id) {
                <button
                  class="theme-swatch"
                  [title]="t.name"
                  [class.theme-swatch--active]="themeService.activeTheme() === t.id"
                  [style.background]="t.primaryColor"
                  (click)="themeService.applyTheme(t.id)">
                </button>
              }
            </div>
          </div>
        </div>

        <div class="sidebar__footer">
          <div class="sidebar__user">
            <div class="sidebar__avatar">{{ initials() }}</div>
            <div class="sidebar__user-info">
              <div class="sidebar__user-name truncate">{{ auth.currentUser()?.fullName }}</div>
              <div class="sidebar__user-role">{{ roleName() }}</div>
            </div>
          </div>
          <button class="pr-btn pr-btn--ghost pr-btn--sm" (click)="auth.logout()">Logout</button>
        </div>
      </aside>

      <!-- Main content -->
      <main class="main">
        <router-outlet />
      </main>
    </div>
  `,
  styles: [`
    :host { display: block; width: 100%; }

    .shell {
      display: grid;
      grid-template-columns: 240px 1fr;
      min-height: 100vh;
      width: 100%;
      transition: grid-template-columns var(--t-slow);
    }
    .shell--collapsed { grid-template-columns: 64px 1fr; }

    /* ── Sidebar ── */
    .sidebar {
      background: var(--pr-surface);
      /* logical property — auto-flips to border-left in RTL */
      border-inline-end: 1px solid var(--pr-border);
      display: flex; flex-direction: column;
      /* NO overflow:hidden — would clip the language-switcher dropdown */
      position: sticky; top: 0; height: 100vh;
    }
    .sidebar__header {
      display: flex; align-items: center; justify-content: space-between;
      padding: 20px 16px 16px;
      border-bottom: 1px solid var(--pr-border);
      flex-shrink: 0;
    }
    /* overflow:hidden on the logo container clips the width:0 animation */
    .sidebar__logo { display: flex; align-items: center; gap: 10px; overflow: hidden; }
    .sidebar__logo-icon { font-size: 1.4rem; flex-shrink: 0; }
    .sidebar__logo-text {
      font-family: var(--font-display);
      font-weight: 800; font-size: 1rem;
      white-space: nowrap; overflow: hidden;
      transition: opacity var(--t-base), max-width var(--t-base);
      max-width: 160px;
    }
    .shell--collapsed .sidebar__logo-text { opacity: 0; max-width: 0; }

    .sidebar__nav { flex: 1; overflow-y: auto; overflow-x: hidden; padding: 16px 8px; }
    .sidebar__section-label {
      font-size: 0.68rem; font-weight: 700; letter-spacing: 0.1em;
      text-transform: uppercase; color: var(--pr-text-muted);
      padding: 0 8px 8px;
    }

    .sidebar__link {
      display: flex; align-items: center; gap: 10px;
      padding: 10px; border-radius: var(--pr-radius);
      color: var(--pr-text-muted); font-size: 0.875rem; font-weight: 500;
      transition: all var(--t-fast); margin-bottom: 2px;
      white-space: nowrap; overflow: hidden;
    }
    .sidebar__link:hover { background: var(--pr-surface-2); color: var(--pr-text); }
    .sidebar__link--active { background: rgba(30,144,255,0.1); color: var(--pr-primary); }

    .sidebar__link-icon { font-size: 1.1rem; flex-shrink: 0; }
    .sidebar__link-label { transition: opacity var(--t-base); }
    .shell--collapsed .sidebar__link-label { opacity: 0; width: 0; overflow: hidden; }

    /* ── Controls (lang + theme) — outside scrollable nav so dropdowns are never clipped ── */
    .sidebar__controls {
      border-top: 1px solid var(--pr-border);
      padding: 8px 8px 4px;
      flex-shrink: 0;
      /* allow dropdown to overflow upward without clipping */
      overflow: visible;
      position: relative;
    }
    .sidebar__themes { padding: 0 0 4px; margin-top: 4px; }
    .theme-swatches { display: flex; gap: 8px; flex-wrap: wrap; margin-top: 8px; }
    .sidebar__lang { padding: 0; margin-bottom: 8px; }
    .sidebar__badge {
      background: #C1121F; color: #fff; font-size: 0.65rem; font-weight: 700;
      padding: 1px 6px; border-radius: 999px;
      /* logical: margin-inline-start:auto pushes badge to the end in both LTR and RTL */
      margin-inline-start: auto; flex-shrink: 0;
    }
    .shell--collapsed .sidebar__controls { display: none; }
    .theme-swatch {
      width: 22px; height: 22px; border-radius: 50%;
      border: 2px solid transparent; cursor: pointer;
      transition: transform var(--t-fast), border-color var(--t-fast);
    }
    .theme-swatch:hover { transform: scale(1.2); }
    .theme-swatch--active { border-color: var(--pr-text); transform: scale(1.15); }

    /* ── Footer ── */
    .sidebar__footer {
      padding: 16px; border-top: 1px solid var(--pr-border);
      display: flex; flex-direction: column; gap: 12px;
      flex-shrink: 0;
    }
    .sidebar__user { display: flex; align-items: center; gap: 10px; overflow: hidden; }
    .sidebar__avatar {
      width: 36px; height: 36px; border-radius: 50%;
      background: var(--pr-primary); color: #fff;
      display: flex; align-items: center; justify-content: center;
      font-family: var(--font-display); font-weight: 700; font-size: 0.8rem;
      flex-shrink: 0;
    }
    .sidebar__user-info { overflow: hidden; }
    .sidebar__user-name { font-size: 0.875rem; font-weight: 600; }
    .sidebar__user-role { font-size: 0.72rem; color: var(--pr-text-muted); text-transform: uppercase; letter-spacing: 0.06em; }
    .shell--collapsed .sidebar__user-info { display: none; }
    .shell--collapsed .sidebar__footer .pr-btn { display: none; }

    /* ── Main ── */
    .main { background: var(--pr-bg); overflow-y: auto; padding: 32px; }

    /* ── RTL — all handled by logical properties above + CSS Grid auto-reversal ── */
    /* Sidebar links: icon first then label, flip in RTL */
    :host-context([dir="rtl"]) .sidebar__link,
    .shell[dir="rtl"] .sidebar__link { flex-direction: row-reverse; text-align: right; }

    /* Logo row: reverse so icon stays on the visual-start side */
    :host-context([dir="rtl"]) .sidebar__header,
    .shell[dir="rtl"] .sidebar__header { flex-direction: row-reverse; }
    :host-context([dir="rtl"]) .sidebar__logo,
    .shell[dir="rtl"] .sidebar__logo   { flex-direction: row-reverse; }

    /* User row */
    :host-context([dir="rtl"]) .sidebar__user,
    .shell[dir="rtl"] .sidebar__user   { flex-direction: row-reverse; }

    /* Language switcher — open above trigger when inside sidebar */
    :host-context([dir="rtl"]) .sidebar__controls .ls-dropdown,
    .shell[dir="rtl"] .sidebar__controls .ls-dropdown,
    .sidebar__controls .ls-dropdown {
      top: auto;
      bottom: calc(100% + 6px);
    }

    @media (max-width: 768px) {
      .shell { grid-template-columns: 1fr; }
      .sidebar { display: none; }
    }
  `]
})
export class ShellComponent {
  auth  = inject(AuthService);
  theme = inject(ThemeService);
  i18n  = inject(TranslationService);
  badge = inject(IntegrationBadgeService);
  themeService = inject(ThemeService);
  sidebarCollapsed = signal(false);

  toggleIcon = computed(() => {
    const collapsed = this.sidebarCollapsed();
    const rtl       = this.i18n.isRtl();
    // In RTL the sidebar is on the right, so arrow directions invert
    if (rtl) return collapsed ? '←' : '→';
    return collapsed ? '→' : '←';
  });

  themes = [
    { id: SiteTheme.Skyline, name: 'Skyline', primaryColor: '#1E90FF' },
    { id: SiteTheme.Meadow,  name: 'Meadow',  primaryColor: '#2D6A4F' },
    { id: SiteTheme.Crimson, name: 'Crimson', primaryColor: '#C1121F' },
    { id: SiteTheme.Ivory,   name: 'Ivory',   primaryColor: '#B8860B' },
    { id: SiteTheme.Slate,   name: 'Slate',   primaryColor: '#4A5568' },
  ];

  initials = computed(() => {
    const u = this.auth.currentUser();
    if (!u) return '?';
    return `${u.firstName?.[0] ?? ''}${u.lastName?.[0] ?? ''}`.toUpperCase();
  });

  roleName = computed(() => {
    const roles: Record<number, string> = {
      1: 'Super Admin', 2: 'Federation Manager', 3: 'Club Manager', 4: 'Fancier'
    };
    return roles[this.auth.currentUser()?.role ?? 0] ?? '';
  });

  navItems = computed(() => {
    const role = this.auth.currentUser()?.role;
    const base: Record<number, { icon: string; label: string; path: string }[]> = {
      [UserRole.SuperAdmin]: [
        { icon: '📊', label: 'nav.dashboard',        path: '/admin/dashboard' },
        { icon: '🌍', label: 'nav.federations',      path: '/admin/federations' },
        { icon: '🏟️', label: 'nav.clubs',            path: '/admin/clubs' },
        { icon: '👥', label: 'nav.users',            path: '/admin/users' },
        { icon: '💳', label: 'nav.subscriptions',    path: '/admin/subscriptions' },
        { icon: '⬆️', label: 'nav.upgradeRequests',  path: '/admin/upgrade-requests' },
        { icon: '📋', label: 'nav.results',          path: '/admin/events' },
      ],
      [UserRole.FederationManager]: [
        { icon: '📊', label: 'nav.dashboard',        path: '/federation/dashboard' },
        { icon: '🏟️', label: 'nav.clubs',            path: '/federation/clubs' },
        { icon: '🏁', label: 'nav.races',            path: '/federation/races' },
        { icon: '🥇', label: 'nav.results',          path: '/federation/results' },
        { icon: '👥', label: 'nav.members',          path: '/federation/members' },
        { icon: '⬆️', label: 'nav.upgradeRequests',  path: '/federation/upgrade-requests' },
        { icon: '🎨', label: 'nav.clubPage',         path: '/federation/page' },
      ],
      [UserRole.ClubManager]: [
        { icon: '📊', label: 'nav.dashboard',    path: '/club/dashboard' },
        { icon: '🏁', label: 'nav.races',        path: '/club/races' },
        { icon: '🏆', label: 'nav.programmes',   path: '/club/programmes' },
        { icon: '🖨️', label: 'nav.printPdf',     path: '/club/templates' },
        { icon: '🔗', label: 'nav.integrations',  path: '/club/integrations' },
        { icon: '📥', label: 'nav.results',      path: '/club/ingest' },
        { icon: '👥', label: 'nav.members',      path: '/club/members' },
        { icon: '🎨', label: 'nav.clubPage',     path: '/club/page' },
        { icon: '🔗', label: 'nav.integrations', path: '/club/integrations' },
        { icon: '🔔', label: 'nav.notifications',path: '/club/notifications' },
      ],
      [UserRole.Fancier]: [
        { icon: '📊', label: 'nav.dashboard',    path: '/fancier/dashboard' },
        { icon: '🕊️', label: 'nav.myPigeons',    path: '/fancier/pigeons' },
        { icon: '🏁', label: 'nav.myResults',    path: '/fancier/results' },
        { icon: '🔔', label: 'nav.notifications',path: '/fancier/notifications' },
        { icon: '🔗', label: 'nav.integrations',  path: '/fancier/integrations' },
      ],
    };
    return base[role ?? -1] ?? [];
  });
}
