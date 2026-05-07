import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface PlatformStats {
  totalCountries: number;
  totalClubs: number;
  totalUsers: number;
  totalRaces: number;
  activeSubscriptions: number;
  racesThisMonth: number;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [RouterLink, DatePipe, NgClass],
  template: `
    <div class="pr-page-header flex justify-between items-center">
      <div>
        <h1 class="pr-page-header__title">Platform Overview</h1>
        <p class="pr-page-header__subtitle">Super Admin · {{ today | date:'EEEE, d MMMM yyyy' }}</p>
      </div>
      <div class="flex gap-3">
        <a routerLink="/admin/countries" class="pr-btn pr-btn--outline">+ Add Country</a>
        <a routerLink="/admin/clubs"     class="pr-btn pr-btn--primary">+ Add Club</a>
      </div>
    </div>

    <!-- Platform KPIs -->
    <div class="pr-grid-3 mb-8">
      @for (s of kpis; track s.label) {
        <div class="pr-card pr-card--flat kpi-card">
          <div class="kpi-icon">{{ s.icon }}</div>
          <div class="kpi-body">
            <div class="pr-stat__value">{{ stats()[s.key] ?? '—' }}</div>
            <div class="pr-stat__label">{{ s.label }}</div>
          </div>
          <a [routerLink]="s.link" class="pr-btn pr-btn--ghost pr-btn--sm">View →</a>
        </div>
      }
    </div>

    <!-- Quick nav cards -->
    <div class="pr-grid-3 mb-8">
      @for (card of quickLinks; track card.label) {
        <a [routerLink]="card.link" class="pr-card admin-quick-card">
          <div class="admin-quick-card__icon">{{ card.icon }}</div>
          <div>
            <div class="admin-quick-card__title">{{ card.label }}</div>
            <div class="admin-quick-card__desc">{{ card.desc }}</div>
          </div>
        </a>
      }
    </div>

    <!-- Recent activity -->
    <div class="pr-card">
      <div class="flex justify-between items-center mb-6">
        <h3 style="margin:0">Recent Events</h3>
        <a routerLink="/admin/events" class="pr-btn pr-btn--ghost pr-btn--sm">Full Log →</a>
      </div>

      @if (recentEvents().length === 0) {
        <div class="pr-empty">
          <div class="pr-empty__icon">📋</div>
          <div class="pr-empty__title">No events yet</div>
        </div>
      } @else {
        <div class="event-list">
          @for (ev of recentEvents(); track ev.id) {
            <div class="event-item">
              <div class="event-item__dot" [class]="'event-item__dot--' + ev.type"></div>
              <div class="event-item__body">
                <span class="event-item__type">{{ ev.eventType }}</span>
                <span class="event-item__agg text-muted">· {{ ev.aggregateType }}:{{ ev.aggregateId.slice(0,8) }}</span>
              </div>
              <span class="event-item__time text-muted text-sm">{{ ev.createdAt | date:'HH:mm' }}</span>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .kpi-card { display:flex; align-items:center; gap:16px; }
    .kpi-icon { font-size:2rem; flex-shrink:0; }
    .kpi-body { flex:1; }

    .admin-quick-card {
      display:flex; align-items:center; gap:16px;
      cursor:pointer; text-decoration:none;
    }
    .admin-quick-card__icon { font-size:2rem; }
    .admin-quick-card__title { font-family:var(--font-display); font-weight:700; font-size:1rem; }
    .admin-quick-card__desc { font-size:0.8rem; color:var(--pr-text-muted); margin-top:2px; }

    .event-list { display:flex; flex-direction:column; gap:4px; }
    .event-item {
      display:flex; align-items:center; gap:12px;
      padding:10px 12px; border-radius:var(--pr-radius);
      transition:background var(--t-fast);
    }
    .event-item:hover { background:var(--pr-surface-2); }
    .event-item__dot { width:8px; height:8px; border-radius:50%; flex-shrink:0; background:var(--pr-primary); }
    .event-item__body { flex:1; font-size:0.875rem; }
    .event-item__type { font-weight:600; }
    .event-item__agg { font-size:0.8rem; margin-left:4px; }
    .event-item__time { flex-shrink:0; }
  `]
})
export class AdminDashboardComponent implements OnInit {
  private http = inject(HttpClient);
  today = new Date();

  stats = signal<Partial<Record<string, number>>>({});
  recentEvents = signal<any[]>([]);

  kpis = [
    { key: 'totalCountries',      label: 'Countries',          icon: '🌍', link: '/admin/countries' },
    { key: 'totalClubs',          label: 'Clubs',              icon: '🏟️', link: '/admin/clubs' },
    { key: 'totalUsers',          label: 'Registered Users',   icon: '👥', link: '/admin/users' },
    { key: 'totalRaces',          label: 'Total Races',        icon: '🏁', link: '/admin/clubs' },
    { key: 'activeSubscriptions', label: 'Active Subscriptions',icon: '💳', link: '/admin/subscriptions' },
    { key: 'racesThisMonth',      label: 'Races This Month',   icon: '📅', link: '/admin/clubs' },
  ];

  quickLinks = [
    { icon: '🌍', label: 'Countries',     desc: 'Manage country federations',      link: '/admin/countries' },
    { icon: '🏟️', label: 'Clubs',         desc: 'View and manage all clubs',       link: '/admin/clubs' },
    { icon: '👥', label: 'Users',         desc: 'Manage fanciers and managers',    link: '/admin/users' },
    { icon: '💳', label: 'Subscriptions', desc: 'Plans, billing and limits',       link: '/admin/subscriptions' },
    { icon: '📋', label: 'Event Log',     desc: 'Full immutable audit trail',      link: '/admin/events' },
    { icon: '🎨', label: 'Themes',        desc: 'Platform theme configuration',    link: '/admin/dashboard' },
  ];

  ngOnInit() {
    // In prod these come from dedicated admin endpoints
    this.stats.set({ totalCountries: 12, totalClubs: 84, totalUsers: 1320, totalRaces: 430, activeSubscriptions: 76, racesThisMonth: 18 });
  }
}
