import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/services';
import { RaceResult, ResultStatus } from '../../core/models';
import { TranslatePipe, TranslationService } from '../../core/i18n';

// ── Fancier Dashboard ─────────────────────────────────────────────────────────

@Component({
  selector: 'app-fancier-dashboard',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, TranslatePipe],
  template: `
    <div class="pr-page-header">
      <h1 class="pr-page-header__title">{{ 'user.welcome' | translate:{ name: (auth.currentUser()?.firstName ?? '') } }}</h1>
      <p class="pr-page-header__subtitle">{{ 'user.fancierProfile' | translate }}</p>
    </div>

    <div class="pr-grid-4 mb-8">
      @for (s of stats(); track s.labelKey) {
        <div class="pr-card pr-card--flat pr-stat">
          <div class="pr-stat__value">{{ s.value }}</div>
          <div class="pr-stat__label">{{ s.labelKey | translate }}</div>
        </div>
      }
    </div>

    <!-- Recent results -->
    <div class="pr-card">
      <div class="flex justify-between items-center mb-6">
        <h3 style="margin:0">{{ 'user.recentRaces' | translate }}</h3>
        <a routerLink="/fancier/results" class="pr-btn pr-btn--ghost pr-btn--sm">{{ 'user.viewAll' | translate }}</a>
      </div>

      @if (recentResults().length === 0) {
        <div class="pr-empty">
          <div class="pr-empty__icon">🕊️</div>
          <div class="pr-empty__title">{{ 'user.noMyResults' | translate }}</div>
        </div>
      } @else {
        <div class="pr-table-wrapper">
          <table class="pr-table">
            <thead>
              <tr>
                <th>{{ 'user.race' | translate }}</th>
                <th>{{ 'admin.common.pigeon' | translate }}</th>
                <th>{{ 'user.rank' | translate }}</th>
                <th>{{ 'user.speed' | translate }}</th>
                <th>{{ 'result.distanceKm' | translate }}</th>
                <th>{{ 'common.date' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              @for (r of recentResults(); track r.id) {
                <tr>
                  <td class="font-bold">{{ r.raceName }}</td>
                  <td>{{ r.ringNumber }}</td>
                  <td>
                    <span [class]="'pr-rank ' + rankClass(r.clubRank)">{{ r.clubRank ?? '—' }}</span>
                  </td>
                  <td>{{ r.speedMperMin | number:'1.0-1' }} m/min</td>
                  <td>{{ r.distanceKm | number:'1.1-1' }} km</td>
                  <td class="text-muted text-sm">{{ r.arrivalTime | date:'dd MMM yyyy' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `
})
export class FancierDashboardComponent implements OnInit {
  private api = inject(ApiService);
  auth = inject(AuthService);

  recentResults = signal<RaceResult[]>([]);
  stats = signal<{ labelKey: string; value: string | number }[]>([
    { labelKey: 'user.totalResults',    value: '—' },
    { labelKey: 'user.rank',            value: '—' },
    { labelKey: 'user.speed',           value: '—' },
    { labelKey: 'user.registeredPigeons', value: '—' },
  ]);

  ngOnInit() {
    const userId = this.auth.currentUser()?.id;
    if (!userId) return;
    this.api.getFancierResults(userId, 1, 10).subscribe(p => {
      const items = p.items as RaceResult[];
      this.recentResults.set(items);
      if (items.length > 0) {
        const bestRank = Math.min(...items.filter(r => r.clubRank).map(r => r.clubRank!));
        const avgVel = items.reduce((a, b) => a + b.speedMperMin, 0) / items.length;
        this.stats.set([
          { labelKey: 'user.totalResults',      value: p.totalCount },
          { labelKey: 'user.rank',              value: `#${bestRank}` },
          { labelKey: 'user.speed',             value: `${avgVel.toFixed(0)} m/min` },
          { labelKey: 'user.registeredPigeons', value: new Set(items.map(r => r.ringNumber)).size },
        ]);
      }
    });
  }

  rankClass(rank?: number | null) {
    if (!rank) return 'pr-rank--other';
    if (rank === 1) return 'pr-rank--1';
    if (rank === 2) return 'pr-rank--2';
    if (rank === 3) return 'pr-rank--3';
    return 'pr-rank--other';
  }
}

// ── Fancier Results Component ─────────────────────────────────────────────────

@Component({
  selector: 'app-fancier-results',
  standalone: true,
  imports: [DatePipe, DecimalPipe, TranslatePipe],
  template: `
    <div class="pr-page-header">
      <h1 class="pr-page-header__title">{{ 'user.myResults' | translate }}</h1>
      <p class="pr-page-header__subtitle">{{ 'user.fancierProfile' | translate }}</p>
    </div>

    <div class="pr-card">
      @if (loading()) {
        <div class="pr-empty"><div class="pr-spinner" style="margin:0 auto"></div></div>
      } @else if (results().length === 0) {
        <div class="pr-empty">
          <div class="pr-empty__icon">📋</div>
          <div class="pr-empty__title">{{ 'user.noMyResults' | translate }}</div>
        </div>
      } @else {
        <div class="pr-table-wrapper">
          <table class="pr-table">
            <thead>
              <tr>
                <th>{{ 'user.race' | translate }}</th>
                <th>{{ 'user.ringNumber' | translate }}</th>
                <th>{{ 'admin.common.pigeon' | translate }}</th>
                <th>{{ 'user.rank' | translate }}</th>
                <th>{{ 'user.speed' | translate }}</th>
                <th>{{ 'result.distanceKm' | translate }}</th>
                <th>{{ 'result.arrival' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              @for (r of results(); track r.id) {
                <tr>
                  <td class="font-bold">{{ r.raceName }}</td>
                  <td><code style="font-size:0.8rem;background:var(--pr-surface-2);padding:2px 6px;border-radius:4px">{{ r.ringNumber }}</code></td>
                  <td>{{ r.pigeonName ?? '—' }}</td>
                  <td><span [class]="'pr-rank ' + rankClass(r.clubRank)">{{ r.clubRank ?? '—' }}</span></td>
                  <td>{{ r.speedMperMin | number:'1.2-2' }} m/min</td>
                  <td>{{ r.distanceKm | number:'1.3-3' }} km</td>
                  <td class="text-muted text-sm">{{ r.arrivalTime | date:'dd MMM yyyy HH:mm' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `
})
export class FancierResultsComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  results = signal<RaceResult[]>([]);
  loading = signal(true);

  ngOnInit() {
    const id = this.auth.currentUser()?.id;
    if (!id) return;
    this.api.getFancierResults(id, 1, 50).subscribe(p => {
      this.results.set(p.items as RaceResult[]);
      this.loading.set(false);
    });
  }

  rankClass(rank?: number | null) {
    if (!rank) return 'pr-rank--other';
    return rank <= 3 ? `pr-rank--${rank}` : 'pr-rank--other';
  }
}

// ── Fancier Pigeons Component ─────────────────────────────────────────────────

@Component({
  selector: 'app-fancier-pigeons',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    <div class="pr-page-header">
      <h1 class="pr-page-header__title">{{ 'user.myPigeons' | translate }}</h1>
      <p class="pr-page-header__subtitle">{{ 'user.fancierProfile' | translate }}</p>
    </div>

    <div class="pr-empty">
      <div class="pr-empty__icon">🕊️</div>
      <div class="pr-empty__title">{{ 'user.noPigeons' | translate }}</div>
    </div>
  `
})
export class FancierPigeonsComponent {}
