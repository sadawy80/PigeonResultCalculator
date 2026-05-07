import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, NgClass } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/services';
import { Race, RaceSummary, RaceStatus, Club } from '../../core/models';

// ── Club Dashboard ────────────────────────────────────────────────────────────

@Component({
  selector: 'app-club-dashboard',
  standalone: true,
  imports: [RouterLink, DatePipe, NgClass],
  template: `
    <div class="pr-page-header flex justify-between items-center">
      <div>
        <h1 class="pr-page-header__title">{{ club()?.name ?? 'Club Dashboard' }}</h1>
        <p class="pr-page-header__subtitle">{{ club()?.city }} · {{ club()?.code }}</p>
      </div>
      <a routerLink="/club/races/new" class="pr-btn pr-btn--primary">+ New Race</a>
    </div>

    <!-- Stats -->
    <div class="pr-grid-4 mb-6">
      @for (s of stats(); track s.label) {
        <div class="pr-card pr-card--flat pr-stat">
          <div class="pr-stat__value">{{ s.value }}</div>
          <div class="pr-stat__label">{{ s.label }}</div>
        </div>
      }
    </div>

    <!-- Live races -->
    @if (liveRaces().length > 0) {
      <div class="pr-card mb-6" style="border-color:var(--pr-success)">
        <div class="flex items-center gap-2 mb-4">
          <span style="width:8px;height:8px;border-radius:50%;background:var(--pr-success);animation:pulse 1.5s infinite;display:inline-block"></span>
          <h3 style="margin:0">Live Races</h3>
        </div>
        @for (race of liveRaces(); track race.id) {
          <a [routerLink]="['/club/races', race.id, 'live']" class="live-race-item">
            <span class="font-bold">{{ race.name }}</span>
            <span class="text-muted text-sm">{{ race.totalPigeonsEntered ?? 0 }} entries</span>
            <span class="pr-badge pr-badge--success">Live ↗</span>
          </a>
        }
      </div>
    }

    <!-- Recent races -->
    <div class="pr-card">
      <div class="flex justify-between items-center mb-6">
        <h3 style="margin:0">Recent Races</h3>
        <a routerLink="/club/races" class="pr-btn pr-btn--ghost pr-btn--sm">View All</a>
      </div>

      @if (recentRaces().length === 0) {
        <div class="pr-empty">
          <div class="pr-empty__icon">🏁</div>
          <div class="pr-empty__title">No races yet</div>
          <p class="pr-empty__desc">Create your first race to get started.</p>
        </div>
      } @else {
        <div class="pr-table-wrapper">
          <table class="pr-table">
            <thead>
              <tr>
                <th>Race</th>
                <th>Status</th>
                <th>Release</th>
                <th>Entries</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (race of recentRaces(); track race.id) {
                <tr>
                  <td class="font-bold">{{ race.name }}</td>
                  <td><span [class]="'pr-badge ' + statusBadge(race.status)">{{ statusLabel(race.status) }}</span></td>
                  <td class="text-muted text-sm">{{ (race.actualReleaseTime ?? race.scheduledReleaseTime) | date:'dd MMM, HH:mm' }}</td>
                  <td>{{ race.totalPigeonsEntered ?? '—' }}</td>
                  <td>
                    <a [routerLink]="['/club/races', race.id]" class="pr-btn pr-btn--ghost pr-btn--sm">View</a>
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
  `]
})
export class ClubDashboardComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);

  club         = signal<Club | null>(null);
  liveRaces    = signal<RaceSummary[]>([]);
  recentRaces  = signal<RaceSummary[]>([]);
  stats        = signal<{ label: string; value: string | number }[]>([]);

  ngOnInit() {
    const clubId = this.auth.clubId();
    if (!clubId) return;
    this.loadDashboard(clubId);
  }

  loadDashboard(clubId: string) {
    this.api.getClub(clubId).subscribe(c => {
      this.club.set(c);
      this.stats.set([
        { label: 'Members',      value: c.memberCount },
        { label: 'Total Races',  value: '—' },
        { label: 'Active Season',value: new Date().getFullYear() },
        { label: 'Subscription', value: 'Active' },
      ]);
    });

    this.api.getLiveRaces(clubId).subscribe(r => this.liveRaces.set(r));

    this.api.getClubRaces(clubId, 1, 10).subscribe(p => this.recentRaces.set(p.items as RaceSummary[]));
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
    return RaceStatus[s] ?? 'Unknown';
  }
}

// ── Race List Component ───────────────────────────────────────────────────────

@Component({
  selector: 'app-race-list',
  standalone: true,
  imports: [RouterLink, DatePipe, NgClass],
  template: `
    <div class="pr-page-header flex justify-between items-center">
      <div>
        <h1 class="pr-page-header__title">Races</h1>
        <p class="pr-page-header__subtitle">Manage your club's races</p>
      </div>
      <a routerLink="/club/races/new" class="pr-btn pr-btn--primary">+ New Race</a>
    </div>

    <!-- Search & Filter -->
    <div class="flex gap-4 mb-6">
      <input class="pr-input" style="max-width:320px"
             placeholder="Search races..." (input)="search($event)" />
      <select class="pr-select" style="max-width:180px" (change)="filterStatus($event)">
        <option value="">All Statuses</option>
        <option value="1">Draft</option>
        <option value="2">Scheduled</option>
        <option value="3">In Progress</option>
        <option value="4">Completed</option>
        <option value="5">Published</option>
      </select>
    </div>

    <div class="pr-card">
      @if (loading()) {
        <div class="pr-empty"><div class="pr-spinner" style="margin:0 auto"></div></div>
      } @else if (races().length === 0) {
        <div class="pr-empty">
          <div class="pr-empty__icon">🏁</div>
          <div class="pr-empty__title">No races found</div>
        </div>
      } @else {
        <div class="pr-table-wrapper">
          <table class="pr-table">
            <thead>
              <tr>
                <th>Name</th><th>Status</th><th>Release Time</th>
                <th>Location</th><th>Entries</th><th>Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (race of races(); track race.id) {
                <tr>
                  <td class="font-bold">{{ race.name }}</td>
                  <td><span [class]="'pr-badge ' + statusBadge(race.status)">{{ statusLabel(race.status) }}</span></td>
                  <td class="text-muted text-sm">
                    {{ (race.actualReleaseTime ?? race.scheduledReleaseTime) | date:'dd MMM yyyy, HH:mm' }}
                  </td>
                  <td class="text-muted">—</td>
                  <td>{{ race.totalPigeonsEntered ?? '—' }}</td>
                  <td>
                    <div class="flex gap-2">
                      <a [routerLink]="['/club/races', race.id]" class="pr-btn pr-btn--ghost pr-btn--sm">View</a>
                      @if (race.status === RaceStatus.InProgress) {
                        <a [routerLink]="['/club/races', race.id, 'live']" class="pr-btn pr-btn--sm"
                           style="background:var(--pr-success);color:#000">Live</a>
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
          <span>{{ totalCount() }} total races</span>
          <div class="flex gap-2">
            <button class="pr-btn pr-btn--ghost pr-btn--sm"
                    [disabled]="currentPage() === 1"
                    (click)="changePage(currentPage() - 1)">← Prev</button>
            <span class="flex items-center px-2">{{ currentPage() }} / {{ totalPages() }}</span>
            <button class="pr-btn pr-btn--ghost pr-btn--sm"
                    [disabled]="currentPage() >= totalPages()"
                    (click)="changePage(currentPage() + 1)">Next →</button>
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
  statusLabel(s: RaceStatus) { return RaceStatus[s]; }
}
