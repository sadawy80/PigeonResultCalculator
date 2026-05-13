import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { TranslationService, TranslatePipe } from '../../core/i18n';

const ROLE_LABELS: Record<number, string> = {
  0: 'Pending', 1: 'admin.users.superAdmin', 2: 'admin.users.federationManager', 3: 'admin.users.clubManager', 4: 'admin.users.fancier'
};

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [RouterLink, DatePipe, NgClass, FormsModule, TranslatePipe],
  template: `
    <div class="pr-page-header flex justify-between items-center">
      <div>
        <h1 class="pr-page-header__title">{{ 'admin.dashboard.title' | translate }}</h1>
        <p class="pr-page-header__subtitle">{{ 'admin.dashboard.subtitle' | translate:{ date: ((today | date:'EEEE, d MMMM yyyy') ?? '') } }}</p>
      </div>
    </div>

    <!-- KPI Section 1 — People & Platform -->
    <div class="kpi-section-label">{{ 'admin.dashboard.peoplePlatform' | translate }}</div>
    <div class="kpi-grid mb-2">
      @for (s of kpiPeople; track s.label) {
        <div class="pr-card pr-card--flat kpi-card">
          <div class="kpi-icon">{{ s.icon }}</div>
          <div class="kpi-body">
            <div class="kpi-value">{{ stats()[s.key] ?? '—' }}</div>
            <div class="kpi-label">{{ s.label | translate }}</div>
          </div>
          <a [routerLink]="s.link" class="pr-btn pr-btn--ghost pr-btn--sm kpi-link">→</a>
        </div>
      }
    </div>

    <!-- KPI Section 2 — People This Year -->
    <div class="kpi-section-label kpi-section-label--sub">{{ 'admin.dashboard.thisYearArrow' | translate:{ year: today.getFullYear() } }}</div>
    <div class="kpi-grid mb-4">
      @for (s of kpiPeopleThisYear; track s.label) {
        <div class="pr-card pr-card--flat kpi-card kpi-card--sub">
          <div class="kpi-icon">{{ s.icon }}</div>
          <div class="kpi-body">
            <div class="kpi-value">{{ stats()[s.key] ?? '—' }}</div>
            <div class="kpi-label">{{ s.label | translate }}</div>
          </div>
          <a [routerLink]="s.link" class="pr-btn pr-btn--ghost pr-btn--sm kpi-link">→</a>
        </div>
      }
    </div>

    <!-- KPI Section 3 — Activity Totals -->
    <div class="kpi-section-label">{{ 'admin.dashboard.activityTotals' | translate }}</div>
    <div class="kpi-grid mb-2">
      @for (s of kpiActivity; track s.label) {
        <div class="pr-card pr-card--flat kpi-card">
          <div class="kpi-icon">{{ s.icon }}</div>
          <div class="kpi-body">
            <div class="kpi-value">{{ stats()[s.key] ?? '—' }}</div>
            <div class="kpi-label">{{ s.label | translate }}</div>
          </div>
          <a [routerLink]="s.link" class="pr-btn pr-btn--ghost pr-btn--sm kpi-link">→</a>
        </div>
      }
    </div>

    <!-- KPI Section 4 — Activity This Year -->
    <div class="kpi-section-label kpi-section-label--sub">{{ 'admin.dashboard.thisYearArrow' | translate:{ year: today.getFullYear() } }}</div>
    <div class="kpi-grid mb-8">
      @for (s of kpiThisYear; track s.label) {
        <div class="pr-card pr-card--flat kpi-card kpi-card--sub">
          <div class="kpi-icon">{{ s.icon }}</div>
          <div class="kpi-body">
            <div class="kpi-value">{{ stats()[s.key] ?? '—' }}</div>
            <div class="kpi-label">{{ s.label | translate }}</div>
          </div>
          <a [routerLink]="s.link" class="pr-btn pr-btn--ghost pr-btn--sm kpi-link">→</a>
        </div>
      }
    </div>

    <!-- Two-column bottom section -->
    <div class="dash-bottom">

      <!-- Pending Upgrade Requests -->
      <div class="pr-card">
        <div class="flex justify-between items-center mb-4">
          <h3 style="margin:0">⬆️ {{ 'admin.dashboard.pendingUpgrades' | translate }}
            @if (pendingUpgrades().length) {
              <span class="upgrade-badge">{{ pendingUpgrades().length }}</span>
            }
          </h3>
          <a routerLink="/admin/upgrade-requests" class="pr-btn pr-btn--ghost pr-btn--sm">{{ 'admin.dashboard.allRequests' | translate }} →</a>
        </div>

        @if (upgradesLoading()) {
          <div class="text-center py-4 text-muted text-sm">{{ 'admin.common.loading' | translate }}</div>
        } @else if (upgradesError()) {
          <div class="pr-alert pr-alert--error" style="padding:12px 16px;font-size:0.875rem">
            {{ 'admin.dashboard.couldNotLoadUpgrades' | translate }}
          </div>
        } @else if (pendingUpgrades().length === 0) {
          <div class="pr-empty" style="padding:24px 0">
            <div class="pr-empty__icon">✅</div>
            <div class="pr-empty__title" style="font-size:0.9rem">{{ 'admin.dashboard.noPendingRequests' | translate }}</div>
          </div>
        } @else {
          <div class="upgrade-list">
            @for (req of pendingUpgrades(); track req.id) {
              <div class="upgrade-item">
                <div class="upgrade-item__info">
                  <div class="upgrade-item__name">{{ req.userFullName }}</div>
                  <div class="upgrade-item__meta text-muted text-sm">
                    {{ req.userEmail }} · {{ 'admin.dashboard.requesting' | translate }}
                    <span class="pr-badge pr-badge--info" style="font-size:0.65rem;padding:1px 6px">{{ roleLabel(req.requestedRole) | translate }}</span>
                    @if (req.federationName) {
                      · {{ req.federationName }}
                    } @else if (req.federationId) {
                      · Fed: <span style="font-family:monospace">{{ req.federationId.slice(0,8) }}…</span>
                    }
                  </div>
                  @if (req.notes) {
                    <div class="upgrade-item__notes text-muted text-sm">"{{ req.notes }}"</div>
                  }
                </div>
                <div class="upgrade-item__actions">
                  <button class="pr-btn pr-btn--sm pr-btn--primary"
                    [disabled]="busyId() === req.id"
                    (click)="approve(req)">
                    {{ busyId() === req.id ? '…' : ('admin.dashboard.approve' | translate) }}
                  </button>
                  <button class="pr-btn pr-btn--sm pr-btn--ghost" style="color:var(--pr-error,#dc2626)"
                    [disabled]="busyId() === req.id"
                    (click)="openReject(req)">
                    ✕ {{ 'admin.dashboard.reject' | translate }}
                  </button>
                </div>
              </div>
            }
          </div>
          <div class="pagination-row" style="margin-top:12px">
            <span class="text-muted text-sm">{{ 'admin.dashboard.pendingCount' | translate:{ n: upgradesTotalCount() } }} · {{ 'admin.common.page' | translate }} {{ upgradesPage() }} {{ 'admin.common.of' | translate }} {{ upgradesTotalPages() }}</span>
            <div class="flex gap-2 items-center">
              <select class="pr-select" style="width:auto" [ngModel]="upgradesPageSize()" (ngModelChange)="upgradesChangeSize($event)">
                <option [ngValue]="10">{{ 'admin.common.perPage' | translate:{ n: 10 } }}</option>
                <option [ngValue]="25">{{ 'admin.common.perPage' | translate:{ n: 25 } }}</option>
                <option [ngValue]="50">{{ 'admin.common.perPage' | translate:{ n: 50 } }}</option>
              </select>
              <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="upgradesPage() === 1" (click)="upgradesChangePage(upgradesPage() - 1)">{{ 'admin.common.prev' | translate }}</button>
              <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="upgradesPage() >= upgradesTotalPages()" (click)="upgradesChangePage(upgradesPage() + 1)">{{ 'admin.common.next' | translate }}</button>
            </div>
          </div>
        }
      </div>

      <!-- Recent Events -->
      <div class="pr-card">
        <div class="flex justify-between items-center mb-4">
          <h3 style="margin:0">📋 {{ 'admin.dashboard.recentEvents' | translate }}</h3>
          <a routerLink="/admin/events" class="pr-btn pr-btn--ghost pr-btn--sm">{{ 'admin.dashboard.fullLog' | translate }} →</a>
        </div>

        @if (recentEvents().length === 0) {
          <div class="pr-empty" style="padding:24px 0">
            <div class="pr-empty__icon">📋</div>
            <div class="pr-empty__title" style="font-size:0.9rem">{{ 'admin.dashboard.noEventsYet' | translate }}</div>
          </div>
        } @else {
          <div class="event-list">
            @for (ev of recentEvents(); track ev.id) {
              <div class="event-item">
                <div class="event-item__dot" [ngClass]="severityDotClass(ev.severity)"></div>
                <div class="event-item__body">
                  <div class="event-item__desc">{{ actionLabel(ev.action) }}</div>
                  <div class="event-item__meta text-muted text-sm">
                    @if (ev.triggeredByName) { <span style="font-weight:600;color:var(--pr-text)">{{ ev.triggeredByName }}</span> · }
                    @if (ev.entityType) { {{ ev.entityType }} · }
                    @if (ev.country || ev.ipAddress) {
                      <span class="event-item__geo">
                        @if (ev.country) { {{ ev.country }} }
                        @if (ev.ipAddress) { <span class="event-item__ip">({{ ev.ipAddress }})</span> }
                      </span>
                    }
                  </div>
                </div>
                <span class="event-item__time text-muted text-sm">{{ ev.createdAt | date:'HH:mm' }}</span>
              </div>
            }
          </div>
          <div class="pagination-row" style="margin-top:12px">
            <span class="text-muted text-sm">{{ 'admin.dashboard.eventsCount' | translate:{ n: eventsTotal() } }} · {{ 'admin.common.page' | translate }} {{ eventsPage() }} {{ 'admin.common.of' | translate }} {{ eventsTotalPages() }}</span>
            <div class="flex gap-2 items-center">
              <select class="pr-select" style="width:auto" [ngModel]="eventsPageSize()" (ngModelChange)="eventsChangeSize($event)">
                <option [ngValue]="10">{{ 'admin.common.perPage' | translate:{ n: 10 } }}</option>
                <option [ngValue]="25">{{ 'admin.common.perPage' | translate:{ n: 25 } }}</option>
                <option [ngValue]="50">{{ 'admin.common.perPage' | translate:{ n: 50 } }}</option>
              </select>
              <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="eventsPage() === 1" (click)="eventsChangePage(eventsPage() - 1)">{{ 'admin.common.prev' | translate }}</button>
              <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="eventsPage() >= eventsTotalPages()" (click)="eventsChangePage(eventsPage() + 1)">{{ 'admin.common.next' | translate }}</button>
            </div>
          </div>
        }
      </div>
    </div>

    <!-- Reject modal -->
    @if (rejectTarget()) {
      <div class="pr-modal-backdrop" (click)="rejectTarget.set(null)">
        <div class="pr-modal pr-modal--sm" (click)="$event.stopPropagation()">
          <h3 class="pr-modal__title">{{ 'admin.dashboard.rejectTitle' | translate }}</h3>
          <p class="pr-modal__subtitle" style="margin-top:6px">
            <strong>{{ rejectTarget()!.userFullName }}</strong> — {{ roleLabel(rejectTarget()!.requestedRole) | translate }}
          </p>
          <hr class="pr-modal__divider">
          <div class="pr-form-group">
            <label class="pr-label">{{ 'admin.dashboard.reason' | translate }} <span style="font-weight:400;color:var(--pr-text-muted)">{{ 'admin.common.optional' | translate }}</span></label>
            <input class="pr-input" [(ngModel)]="rejectReason" [placeholder]="'admin.dashboard.reasonPlaceholder' | translate">
          </div>
          @if (rejectError()) {
            <div class="pr-alert pr-alert--error mt-3">{{ rejectError() }}</div>
          }
          <div class="flex gap-3 justify-end mt-4">
            <button class="pr-btn pr-btn--ghost" (click)="rejectTarget.set(null)">{{ 'admin.common.cancel' | translate }}</button>
            <button class="pr-btn pr-btn--primary"
              style="background:var(--pr-error,#dc2626);border-color:var(--pr-error,#dc2626)"
              [disabled]="busyId() !== null"
              (click)="confirmReject()">
              {{ (busyId() !== null ? 'admin.dashboard.rejecting' : 'admin.dashboard.reject') | translate }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .kpi-section-label {
      font-size: 0.68rem; font-weight: 700; letter-spacing: 0.08em;
      text-transform: uppercase; color: var(--pr-text-muted);
      margin-bottom: 6px;
    }
    .kpi-section-label--sub {
      font-size: 0.63rem; margin-top: 4px; margin-left: 8px;
      letter-spacing: 0.06em; opacity: 0.75;
    }

    .kpi-grid {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
    }
    /* basis = 1/8 of row (minus 7 gaps); flex-grow fills any remaining space
       so 7 cards expand to fill what 8 would occupy.
       min-width triggers wrapping at small viewports.                       */
    .kpi-grid > * {
      flex: 1 1 calc((100% - 7 * 8px) / 8);
      min-width: 120px;
    }

    .kpi-card { display:flex; align-items:center; gap:8px; padding:8px 10px !important; }
    .kpi-card--sub { opacity: 0.85; }
    .kpi-icon { font-size:1.2rem; flex-shrink:0; }
    .kpi-body { flex:1; min-width:0; }
    .kpi-value { font-size:1.05rem; font-weight:700; line-height:1.2; }
    .kpi-label { font-size:0.65rem; color:var(--pr-text-muted); white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
    .kpi-link  { flex-shrink:0; padding:2px 6px !important; font-size:0.75rem !important; }

    .dash-bottom {
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .upgrade-badge {
      display: inline-flex; align-items: center; justify-content: center;
      min-width: 20px; height: 20px; padding: 0 5px;
      border-radius: 10px; font-size: 0.7rem; font-weight: 700;
      background: var(--pr-warning, #f59e0b); color: #fff;
      margin-left: 8px; vertical-align: middle;
    }

    .upgrade-list { display: flex; flex-direction: column; gap: 2px; }
    .upgrade-item {
      display: flex; align-items: flex-start; justify-content: space-between;
      gap: 12px; padding: 12px; border-radius: var(--pr-radius);
      transition: background var(--t-fast);
    }
    .upgrade-item:hover { background: var(--pr-surface-2); }
    .upgrade-item__info { flex: 1; min-width: 0; }
    .upgrade-item__name { font-weight: 600; font-size: 0.9rem; }
    .upgrade-item__meta { margin-top: 2px; }
    .upgrade-item__notes { margin-top: 4px; font-style: italic; }
    .upgrade-item__actions { display: flex; gap: 6px; flex-shrink: 0; }

    .event-list { display:flex; flex-direction:column; gap:4px; }
    .event-item {
      display:flex; align-items:center; gap:12px;
      padding:10px 12px; border-radius:var(--pr-radius);
      transition:background var(--t-fast);
    }
    .event-item:hover { background:var(--pr-surface-2); }
    .event-item__dot { width:8px; height:8px; border-radius:50%; flex-shrink:0; background:var(--pr-primary); }
    .event-item__dot--warning { background:#f59e0b; }
    .event-item__dot--critical { background:var(--pr-error,#dc2626); }
    .event-item__dot--info, .event-item__dot--default { background:var(--pr-primary); }
    .event-item__body { flex:1; font-size:0.875rem; }
    .event-item__body { flex:1; min-width:0; }
    .event-item__desc { font-size:0.875rem; font-weight:500; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
    .event-item__meta { font-size:0.75rem; margin-top:1px; }
    .event-item__time { flex-shrink:0; }
    .event-item__geo  { color:var(--pr-text-muted); }
    .event-item__ip   { font-family:monospace; font-size:0.7rem; opacity:0.75; }
  `]
})
export class AdminDashboardComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  today = new Date();
  private _pollTimer: ReturnType<typeof setInterval> | null = null;

  stats           = signal<Partial<Record<string, number>>>({});
  recentEvents    = signal<any[]>([]);
  eventsTotal     = signal(0);
  eventsPage      = signal(1);
  eventsPageSize  = signal(10);
  eventsTotalPages = () => Math.max(1, Math.ceil(this.eventsTotal() / this.eventsPageSize()));

  pendingUpgrades    = signal<any[]>([]);
  upgradesTotalCount = signal(0);
  upgradesPage       = signal(1);
  upgradesPageSize   = signal(10);
  upgradesTotalPages = () => Math.max(1, Math.ceil(this.upgradesTotalCount() / this.upgradesPageSize()));
  upgradesLoading = signal(false);
  upgradesError   = signal(false);
  busyId          = signal<string | null>(null);
  rejectTarget    = signal<any>(null);
  rejectReason    = '';
  rejectError     = signal<string | null>(null);

  kpiPeopleThisYear = [
    { key: 'federationsThisYear',    label: 'admin.dashboard.federations',    icon: '🌍', link: '/admin/federations' },
    { key: 'clubsThisYear',          label: 'admin.dashboard.clubs',          icon: '🏟️', link: '/admin/clubs' },
    { key: 'usersThisYear',          label: 'admin.dashboard.users',          icon: '👥', link: '/admin/users' },
    { key: 'fanciersThisYear',       label: 'admin.dashboard.fanciers',       icon: '🕊️', link: '/admin/fanciers' },
    { key: 'pigeonsThisYear',        label: 'admin.dashboard.pigeons',        icon: '🐦', link: '/admin/pigeons' },
    { key: 'federationSubsThisYear', label: 'admin.dashboard.federationSubs', icon: '💳', link: '/admin/subscriptions' },
    { key: 'clubSubsThisYear',       label: 'admin.dashboard.clubSubs',       icon: '🏷️', link: '/admin/subscriptions' },
  ];

  kpiPeople = [
    { key: 'totalFederations',        label: 'admin.dashboard.federations',    icon: '🌍', link: '/admin/federations' },
    { key: 'totalClubs',              label: 'admin.dashboard.clubs',          icon: '🏟️', link: '/admin/clubs' },
    { key: 'totalUsers',              label: 'admin.dashboard.users',          icon: '👥', link: '/admin/users' },
    { key: 'totalFanciers',           label: 'admin.dashboard.fanciers',       icon: '🕊️', link: '/admin/fanciers' },
    { key: 'totalPigeons',            label: 'admin.dashboard.pigeons',        icon: '🐦', link: '/admin/pigeons' },
    { key: 'federationSubscriptions', label: 'admin.dashboard.federationSubs', icon: '💳', link: '/admin/subscriptions' },
    { key: 'clubSubscriptions',       label: 'admin.dashboard.clubSubs',       icon: '🏷️', link: '/admin/subscriptions' },
  ];

  kpiActivity = [
    { key: 'totalProgrammes',      label: 'admin.dashboard.programmes', icon: '📋', link: '/admin/programmes' },
    { key: 'totalRaces',           label: 'admin.dashboard.races',      icon: '🏁', link: '/admin/races' },
    { key: 'totalResults',         label: 'admin.dashboard.raceResults',icon: '📊', link: '/admin/races' },
    { key: 'totalAceResults',      label: 'admin.dashboard.aceResults', icon: '🥇', link: '/admin/results/ace' },
    { key: 'totalSuperAceResults', label: 'admin.dashboard.superAce',   icon: '🏆', link: '/admin/results/super-ace' },
    { key: 'totalBestLoftResults', label: 'admin.dashboard.bestLoft',   icon: '🎖️', link: '/admin/results/best-loft' },
  ];

  kpiThisYear = [
    { key: 'programmesThisYear',       label: 'admin.dashboard.programmes', icon: '📋', link: '/admin/programmes' },
    { key: 'racesThisYear',            label: 'admin.dashboard.races',      icon: '🏁', link: '/admin/races' },
    { key: 'resultsThisYear',          label: 'admin.dashboard.raceResults',icon: '📊', link: '/admin/races' },
    { key: 'aceResultsThisYear',       label: 'admin.dashboard.aceResults', icon: '🥇', link: '/admin/results/ace' },
    { key: 'superAceResultsThisYear',  label: 'admin.dashboard.superAce',   icon: '🏆', link: '/admin/results/super-ace' },
    { key: 'bestLoftResultsThisYear',  label: 'admin.dashboard.bestLoft',   icon: '🎖️', link: '/admin/results/best-loft' },
  ];

  ngOnInit() {
    this.loadStats();
    this.loadEvents();
    this.loadUpgrades();
    this._pollTimer = setInterval(() => {
      this.loadStats();
      this.loadEvents();
      this.loadUpgrades();
    }, 30_000);
  }

  ngOnDestroy() {
    if (this._pollTimer) clearInterval(this._pollTimer);
  }

  private loadStats() {
    this.api.adminGetStats().subscribe({ next: s => this.stats.set(s), error: () => {} });
  }

  private loadEvents() {
    this.api.adminGetEvents({ page: this.eventsPage(), pageSize: this.eventsPageSize() }).subscribe({
      next: p => { this.recentEvents.set(p.items ?? []); this.eventsTotal.set(p.totalCount ?? (p.items ?? []).length); },
      error: () => {}
    });
  }

  eventsChangePage(p: number) { this.eventsPage.set(p); this.loadEvents(); }
  eventsChangeSize(size: number) { this.eventsPageSize.set(size); this.eventsPage.set(1); this.loadEvents(); }

  private loadUpgrades() {
    this.upgradesLoading.set(true);
    this.upgradesError.set(false);
    this.api.getAdminUpgradeRequests({ status: 0, page: this.upgradesPage(), pageSize: this.upgradesPageSize() }).subscribe({
      next: r => {
        this.pendingUpgrades.set(r?.items ?? r ?? []);
        this.upgradesTotalCount.set(r?.totalCount ?? (r?.items ?? r ?? []).length);
        this.upgradesLoading.set(false);
      },
      error: () => { this.upgradesLoading.set(false); this.upgradesError.set(true); }
    });
  }

  upgradesChangePage(p: number) { this.upgradesPage.set(p); this.loadUpgrades(); }
  upgradesChangeSize(size: number) { this.upgradesPageSize.set(size); this.upgradesPage.set(1); this.loadUpgrades(); }

  roleLabel(role: number) { return ROLE_LABELS[role] ?? `Role ${role}`; }

  actionLabel(action: string): string {
    const map: Record<string, string> = {
      LOGIN: 'Logged In', LOGIN_FAILED: 'Login Failed',
      FEDERATION_CREATED: 'Federation Created', FEDERATION_DELETED: 'Federation Deleted',
      FEDERATION_TOGGLED: 'Federation Toggled', FEDERATION_MANAGER_ASSIGNED: 'Manager Assigned',
      CLUB_CREATED: 'Club Created', CLUB_DELETED: 'Club Deleted',
      CLUB_SUSPENDED: 'Club Toggled', CLUB_MANAGER_ASSIGNED: 'Club Manager Assigned',
      USER_TOGGLED: 'User Toggled', USER_DELETED: 'User Deleted',
      ROLE_ASSIGNED: 'Role Assigned', LIMITS_CHANGED: 'Limits Changed',
      UPGRADE_REQUEST_APPROVED: 'Upgrade Approved', UPGRADE_REQUEST_REJECTED: 'Upgrade Rejected',
      SUBSCRIPTION_CREATED: 'Subscription Created', SUBSCRIPTION_PLAN_UPDATED: 'Plan Updated',
      IMPERSONATION: 'Impersonation',
    };
    return map[action] ?? action.replace(/_/g, ' ').toLowerCase().replace(/\b\w/g, c => c.toUpperCase());
  }

  severityDotClass(severity: string) {
    const s = (severity ?? '').toLowerCase();
    return { 'event-item__dot--warning': s === 'warning', 'event-item__dot--critical': s === 'critical' };
  }

  approve(req: any) {
    this.busyId.set(req.id);
    this.api.approveAdminUpgradeRequest(req.id).subscribe({
      next: () => {
        this.busyId.set(null);
        this.pendingUpgrades.update(list => list.filter(r => r.id !== req.id));
      },
      error: () => this.busyId.set(null)
    });
  }

  openReject(req: any) {
    this.rejectReason = '';
    this.rejectError.set(null);
    this.rejectTarget.set(req);
  }

  confirmReject() {
    const req = this.rejectTarget();
    if (!req) return;
    this.busyId.set(req.id);
    this.rejectError.set(null);
    this.api.rejectAdminUpgradeRequest(req.id, this.rejectReason || undefined).subscribe({
      next: () => {
        this.busyId.set(null);
        this.rejectTarget.set(null);
        this.pendingUpgrades.update(list => list.filter(r => r.id !== req.id));
      },
      error: (e) => {
        this.rejectError.set(e?.error?.message ?? 'Failed to reject request.');
        this.busyId.set(null);
      }
    });
  }
}
