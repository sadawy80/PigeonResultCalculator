import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { ProgrammeApiService } from '../../core/services/programme-api.service';
import { AuthService } from '../../core/services/services';
import { Race, RaceSummary, RaceStatus, Club } from '../../core/models';
import { TranslatePipe } from '../../core/i18n';

// ── Club Dashboard ────────────────────────────────────────────────────────────

@Component({
  selector: 'app-club-dashboard',
  standalone: true,
  imports: [RouterLink, DatePipe, TranslatePipe],
  template: `
    <div class="pr-page-header flex justify-between items-center">
      <div>
        <h1 class="pr-page-header__title">{{ club()?.name ?? ('user.dashboard' | translate) }}</h1>
        <p class="pr-page-header__subtitle">{{ club()?.city }} · {{ club()?.code }}</p>
      </div>
    </div>

    <!-- Activity stats -->
    <div class="kpi-section-label">{{ 'user.activity' | translate }}</div>
    <div class="kpi-grid mb-2">
      <div class="pr-card pr-card--flat kpi-card">
        <div class="kpi-icon">📋</div>
        <div class="kpi-body">
          <div class="kpi-value">{{ activityStats().totalProgrammes ?? '—' }}</div>
          <div class="kpi-label">{{ 'user.programmes' | translate }}</div>
        </div>
        <a routerLink="/club/programmes" class="pr-btn pr-btn--ghost pr-btn--sm kpi-link">→</a>
      </div>
      <div class="pr-card pr-card--flat kpi-card">
        <div class="kpi-icon">🏁</div>
        <div class="kpi-body">
          <div class="kpi-value">{{ activityStats().totalRaces ?? '—' }}</div>
          <div class="kpi-label">{{ 'user.races' | translate }}</div>
        </div>
        <a routerLink="/club/races" class="pr-btn pr-btn--ghost pr-btn--sm kpi-link">→</a>
      </div>
      <div class="pr-card pr-card--flat kpi-card">
        <div class="kpi-icon">👥</div>
        <div class="kpi-body">
          <div class="kpi-value">{{ activityStats().totalMembers ?? '—' }}</div>
          <div class="kpi-label">{{ 'user.members' | translate }}</div>
        </div>
        <a routerLink="/club/members" class="pr-btn pr-btn--ghost pr-btn--sm kpi-link">→</a>
      </div>
    </div>
    <div class="kpi-section-label kpi-section-label--sub">↳ {{ 'user.thisYear' | translate:{ year: thisYear } }}</div>
    <div class="kpi-grid mb-6">
      <div class="pr-card pr-card--flat kpi-card kpi-card--sub">
        <div class="kpi-icon">📋</div>
        <div class="kpi-body">
          <div class="kpi-value">{{ activityStats().programmesThisYear ?? '—' }}</div>
          <div class="kpi-label">{{ 'user.programmesThisYear' | translate }}</div>
        </div>
      </div>
      <div class="pr-card pr-card--flat kpi-card kpi-card--sub">
        <div class="kpi-icon">🏁</div>
        <div class="kpi-body">
          <div class="kpi-value">{{ activityStats().racesThisYear ?? '—' }}</div>
          <div class="kpi-label">{{ 'user.racesThisYear' | translate }}</div>
        </div>
      </div>
    </div>

    <!-- Live races -->
    @if (liveRaces().length > 0) {
      <div class="pr-card mb-6" style="border-color:var(--pr-success)">
        <div class="flex items-center gap-2 mb-4">
          <span style="width:8px;height:8px;border-radius:50%;background:var(--pr-success);animation:pulse 1.5s infinite;display:inline-block"></span>
          <h3 style="margin:0">{{ 'user.liveRaces' | translate }}</h3>
        </div>
        @for (race of liveRaces(); track race.id) {
          <a [routerLink]="['/club/races', race.id, 'live']" class="live-race-item">
            <span class="font-bold">{{ race.name }}</span>
            <span class="text-muted text-sm">{{ race.totalPigeonsEntered ?? 0 }} {{ 'user.entries' | translate }}</span>
            <span class="pr-badge pr-badge--success">{{ 'user.live' | translate }}</span>
          </a>
        }
      </div>
    }

    <!-- Recent races -->
    <div class="pr-card">
      <div class="flex justify-between items-center mb-6">
        <h3 style="margin:0">{{ 'user.recentRaces' | translate }}</h3>
        <a routerLink="/club/races" class="pr-btn pr-btn--ghost pr-btn--sm">{{ 'user.viewAll' | translate }}</a>
      </div>

      @if (recentRaces().length === 0) {
        <div class="pr-empty">
          <div class="pr-empty__icon">🏁</div>
          <div class="pr-empty__title">{{ 'user.noRacesYet' | translate }}</div>
          <p class="pr-empty__desc">{{ 'user.createFirstRace' | translate }}</p>
        </div>
      } @else {
        <div class="pr-table-wrapper">
          <table class="pr-table">
            <thead>
              <tr>
                <th>{{ 'user.race' | translate }}</th>
                <th>{{ 'user.status' | translate }}</th>
                <th>{{ 'user.release' | translate }}</th>
                <th>{{ 'user.entries' | translate }}</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (race of recentRaces(); track race.id) {
                <tr>
                  <td class="font-bold">{{ race.name }}</td>
                  <td><span [class]="'pr-badge ' + statusBadge(race.status)">{{ statusLabel(race.status) | translate }}</span></td>
                  <td class="text-muted text-sm">{{ (race.actualReleaseTime ?? race.scheduledReleaseTime) | date:'dd MMM, HH:mm' }}</td>
                  <td>{{ race.totalPigeonsEntered ?? '—' }}</td>
                  <td>
                    <a [routerLink]="['/club/races', race.id]" class="pr-btn pr-btn--ghost pr-btn--sm">{{ 'user.view' | translate }}</a>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
  styles: [`
    .live-race-item {
      display:flex; align-items:center; justify-content:space-between;
      padding:12px; border-radius:var(--pr-radius); margin-bottom:4px;
      background:var(--pr-surface-2); transition:background var(--t-fast);
    }
    .live-race-item:hover { background:var(--pr-border); }

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
    .kpi-grid > * {
      flex: 1 1 calc((100% - 7 * 8px) / 8);
      min-width: 120px;
    }
    .kpi-card { display:flex; align-items:center; gap:8px; padding:8px 10px !important; }
    .kpi-card--sub { opacity: 0.85; }
    .kpi-icon  { font-size:1.2rem; flex-shrink:0; }
    .kpi-body  { flex:1; min-width:0; }
    .kpi-value { font-size:1.05rem; font-weight:700; line-height:1.2; }
    .kpi-label { font-size:0.65rem; color:var(--pr-text-muted); white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
    .kpi-link  { flex-shrink:0; padding:2px 6px !important; font-size:0.75rem !important; }
    .mb-2 { margin-bottom:8px; }
  `]
})
export class ClubDashboardComponent implements OnInit {
  private api     = inject(ApiService);
  private progApi = inject(ProgrammeApiService);
  private auth    = inject(AuthService);

  club          = signal<Club | null>(null);
  liveRaces     = signal<RaceSummary[]>([]);
  recentRaces   = signal<RaceSummary[]>([]);
  activityStats = signal<{
    totalRaces: number | null; racesThisYear: number | null;
    totalProgrammes: number | null; programmesThisYear: number | null;
    totalMembers: number | null;
  }>({ totalRaces: null, racesThisYear: null, totalProgrammes: null, programmesThisYear: null, totalMembers: null });

  readonly thisYear = new Date().getFullYear();

  ngOnInit() {
    const clubId = this.auth.clubId();
    if (!clubId) return;
    this.loadDashboard(clubId);
  }

  loadDashboard(clubId: string) {
    this.api.getClub(clubId).subscribe(c => this.club.set(c));
    this.api.getLiveRaces(clubId).subscribe(r => this.liveRaces.set(r));
    this.api.getClubRaces(clubId, 1, 10).subscribe(p => this.recentRaces.set(p.items as RaceSummary[]));

    // Activity stats: total races + this-year races in parallel
    this.api.getClubRaces(clubId, 1, 1).subscribe(p =>
      this.activityStats.update(s => ({ ...s, totalRaces: p.totalCount })));

    this.api.getClubRaces(clubId, 1, 1, undefined, this.thisYear).subscribe(p =>
      this.activityStats.update(s => ({ ...s, racesThisYear: p.totalCount })));

    // Club stats endpoint (totalProgrammes, programmesThisYear, totalMembers)
    this.api.getClubStats(clubId).subscribe(s => {
      this.activityStats.update(prev => ({
        ...prev,
        totalProgrammes:    s.totalProgrammes,
        programmesThisYear: s.programmesThisYear,
        totalMembers:       s.totalMembers
      }));
    });
  }

  statusBadge(s: RaceStatus) {
    const map: Record<RaceStatus, string> = {
      [RaceStatus.Draft]:     'pr-badge--muted',
      [RaceStatus.Scheduled]: 'pr-badge--info',
      [RaceStatus.InProgress]:'pr-badge--warning',
      [RaceStatus.Completed]: 'pr-badge--info',
      [RaceStatus.Published]: 'pr-badge--success',
      [RaceStatus.Cancelled]: 'pr-badge--error',
    };
    return map[s] ?? 'pr-badge--muted';
  }

  statusLabel(s: RaceStatus) {
    // Map race status enum → admin.races.status* translation key so the badge
    // localises with the user's locale instead of showing the raw enum name.
    const map: Record<RaceStatus, string> = {
      [RaceStatus.Draft]:      'admin.races.statusDraft',
      [RaceStatus.Scheduled]:  'admin.races.statusOpen',
      [RaceStatus.InProgress]: 'race.inProgress',
      [RaceStatus.Completed]:  'race.completed',
      [RaceStatus.Published]:  'admin.races.statusPublished',
      [RaceStatus.Cancelled]:  'admin.races.statusCancelled'
    };
    return map[s] ?? (RaceStatus[s] ?? 'Unknown');
  }
}

// ── Race List Component ───────────────────────────────────────────────────────

@Component({
  selector: 'app-race-list',
  standalone: true,
  imports: [RouterLink, DatePipe, TranslatePipe],
  template: `
    <div class="pr-page-header flex justify-between items-center">
      <div>
        <h1 class="pr-page-header__title">{{ 'user.races' | translate }}</h1>
        <p class="pr-page-header__subtitle">{{ 'admin.races.subtitle' | translate }}</p>
      </div>
      <a routerLink="/club/races/new" class="pr-btn pr-btn--primary">+ {{ 'race.newRace' | translate }}</a>
    </div>

    <!-- Search & Filter -->
    <div class="flex gap-4 mb-6">
      <input class="pr-input" style="max-width:320px"
             [placeholder]="'admin.races.searchPlaceholder' | translate" (input)="search($event)" />
      <select class="pr-select" style="max-width:180px" (change)="filterStatus($event)">
        <option value="">{{ 'admin.common.allStatuses' | translate }}</option>
        <option value="1">{{ 'admin.races.statusDraft' | translate }}</option>
        <option value="2">{{ 'admin.races.statusOpen' | translate }}</option>
        <option value="3">{{ 'race.inProgress' | translate }}</option>
        <option value="4">{{ 'race.completed' | translate }}</option>
        <option value="5">{{ 'admin.races.statusPublished' | translate }}</option>
      </select>
    </div>

    <div class="pr-card">
      @if (loading()) {
        <div class="pr-empty"><div class="pr-spinner" style="margin:0 auto"></div></div>
      } @else if (races().length === 0) {
        <div class="pr-empty">
          <div class="pr-empty__icon">🏁</div>
          <div class="pr-empty__title">{{ 'admin.races.noRaces' | translate }}</div>
        </div>
      } @else {
        <div class="pr-table-wrapper">
          <table class="pr-table">
            <thead>
              <tr>
                <th>{{ 'user.name' | translate }}</th>
                <th>{{ 'user.status' | translate }}</th>
                <th>{{ 'user.release' | translate }}</th>
                <th>{{ 'admin.common.city' | translate }}</th>
                <th>{{ 'user.entries' | translate }}</th>
                <th>{{ 'admin.common.actions' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              @for (race of races(); track race.id) {
                <tr>
                  <td class="font-bold">{{ race.name }}</td>
                  <td><span [class]="'pr-badge ' + statusBadge(race.status)">{{ statusLabel(race.status) | translate }}</span></td>
                  <td class="text-muted text-sm">
                    {{ (race.actualReleaseTime ?? race.scheduledReleaseTime) | date:'dd MMM yyyy, HH:mm' }}
                  </td>
                  <td class="text-muted">—</td>
                  <td>{{ race.totalPigeonsEntered ?? '—' }}</td>
                  <td>
                    <div class="flex gap-2">
                      <a [routerLink]="['/club/races', race.id]" class="pr-btn pr-btn--ghost pr-btn--sm">{{ 'user.view' | translate }}</a>
                      @if (race.status === RaceStatus.InProgress) {
                        <a [routerLink]="['/club/races', race.id, 'live']" class="pr-btn pr-btn--sm"
                           style="background:var(--pr-success);color:#000">{{ 'user.live' | translate }}</a>
                      }
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div class="flex justify-between items-center mt-4 text-sm text-muted">
          <span>{{ 'admin.races.racesCount' | translate:{ n: totalCount() } }}</span>
          <div class="flex gap-2">
            <button class="pr-btn pr-btn--ghost pr-btn--sm"
                    [disabled]="currentPage() === 1"
                    (click)="changePage(currentPage() - 1)">← {{ 'admin.common.prev' | translate }}</button>
            <span class="flex items-center px-2">{{ currentPage() }} / {{ totalPages() }}</span>
            <button class="pr-btn pr-btn--ghost pr-btn--sm"
                    [disabled]="currentPage() >= totalPages()"
                    (click)="changePage(currentPage() + 1)">{{ 'admin.common.next' | translate }} →</button>
          </div>
        </div>
      }
    </div>
  `
})
export class RaceListComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);

  RaceStatus = RaceStatus;
  races      = signal<RaceSummary[]>([]);
  loading    = signal(true);
  currentPage = signal(1);
  totalCount  = signal(0);
  totalPages  = signal(1);
  searchTerm  = signal('');
  clubId      = '';

  ngOnInit() {
    this.clubId = this.auth.clubId() ?? '';
    this.load();
  }

  load() {
    this.loading.set(true);
    this.api.getClubRaces(this.clubId, this.currentPage(), 20, this.searchTerm() || undefined)
      .subscribe(p => {
        this.races.set(p.items as RaceSummary[]);
        this.totalCount.set(p.totalCount);
        this.totalPages.set(p.totalPages);
        this.loading.set(false);
      });
  }

  search(e: Event) {
    this.searchTerm.set((e.target as HTMLInputElement).value);
    this.currentPage.set(1);
    this.load();
  }

  filterStatus(_e: Event) { this.load(); }

  changePage(p: number) { this.currentPage.set(p); this.load(); }

  statusBadge(s: RaceStatus) {
    const map: Record<RaceStatus, string> = {
      [RaceStatus.Draft]:     'pr-badge--muted',
      [RaceStatus.Scheduled]: 'pr-badge--info',
      [RaceStatus.InProgress]:'pr-badge--warning',
      [RaceStatus.Completed]: 'pr-badge--info',
      [RaceStatus.Published]: 'pr-badge--success',
      [RaceStatus.Cancelled]: 'pr-badge--error',
    };
    return map[s];
  }
  statusLabel(s: RaceStatus) {
    const map: Record<RaceStatus, string> = {
      [RaceStatus.Draft]:      'admin.races.statusDraft',
      [RaceStatus.Scheduled]:  'admin.races.statusOpen',
      [RaceStatus.InProgress]: 'race.inProgress',
      [RaceStatus.Completed]:  'race.completed',
      [RaceStatus.Published]:  'admin.races.statusPublished',
      [RaceStatus.Cancelled]:  'admin.races.statusCancelled'
    };
    return map[s] ?? RaceStatus[s];
  }
}
