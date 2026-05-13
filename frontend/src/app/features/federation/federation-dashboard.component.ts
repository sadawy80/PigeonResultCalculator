import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/services';
import { FederationResult, FederationResultStatus } from '../../core/models';
import { TranslatePipe } from '../../core/i18n';

// ── Federation Dashboard ──────────────────────────────────────────────────────

@Component({
  selector: 'app-federation-dashboard',
  standalone: true,
  imports: [RouterLink, DatePipe, TranslatePipe],
  template: `
    <div class="pr-page-header flex justify-between items-center">
      <div>
        <h1 class="pr-page-header__title">{{ 'user.federationDashboard' | translate }}</h1>
        <p class="pr-page-header__subtitle">{{ 'user.federationOverview' | translate }}</p>
      </div>
    </div>

    <!-- Activity stats -->
    <div class="kpi-section-label">{{ 'user.activity' | translate }}</div>
    <div class="kpi-grid mb-2">
      <div class="pr-card pr-card--flat kpi-card">
        <div class="kpi-icon">🏆</div>
        <div class="kpi-body">
          <div class="kpi-value">{{ activityStats().totalResults ?? '—' }}</div>
          <div class="kpi-label">{{ 'user.totalResults' | translate }}</div>
        </div>
        <a routerLink="/federation/results" class="pr-btn pr-btn--ghost pr-btn--sm kpi-link">→</a>
      </div>
      <div class="pr-card pr-card--flat kpi-card">
        <div class="kpi-icon">📅</div>
        <div class="kpi-body">
          <div class="kpi-value">{{ thisYear }}</div>
          <div class="kpi-label">{{ 'common.season' | translate }}</div>
        </div>
      </div>
    </div>
    <div class="kpi-section-label kpi-section-label--sub">↳ {{ 'user.thisYear' | translate:{ year: thisYear } }}</div>
    <div class="kpi-grid mb-8">
      <div class="pr-card pr-card--flat kpi-card kpi-card--sub">
        <div class="kpi-icon">🏆</div>
        <div class="kpi-body">
          <div class="kpi-value">{{ activityStats().resultsThisYear ?? '—' }}</div>
          <div class="kpi-label">{{ 'user.totalResults' | translate }} {{ 'user.thisYear' | translate:{ year: thisYear } }}</div>
        </div>
      </div>
    </div>

    <div class="pr-card">
      <h3 style="margin-bottom:24px">{{ 'user.recentRaces' | translate }}</h3>
      @if (results().length === 0) {
        <div class="pr-empty">
          <div class="pr-empty__icon">🏆</div>
          <div class="pr-empty__title">{{ 'user.noMyResults' | translate }}</div>
        </div>
      } @else {
        <div class="pr-table-wrapper">
          <table class="pr-table">
            <thead>
              <tr>
                <th>{{ 'user.name' | translate }}</th>
                <th>{{ 'user.status' | translate }}</th>
                <th>{{ 'user.totalClubs' | translate }}</th>
                <th>{{ 'user.entries' | translate }}</th>
                <th>{{ 'admin.common.publishedAt' | translate }}</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (r of results(); track r.id) {
                <tr>
                  <td class="font-bold">{{ r.name }}</td>
                  <td><span [class]="r.status === FederationResultStatus.Published ? 'pr-badge pr-badge--success' : 'pr-badge pr-badge--muted'">
                    {{ (r.status === FederationResultStatus.Published ? 'admin.races.statusPublished' : 'admin.races.statusDraft') | translate }}
                  </span></td>
                  <td>{{ r.totalClubsCount }}</td>
                  <td>{{ r.totalEntriesCount }}</td>
                  <td class="text-muted text-sm">{{ r.publishedAt | date:'dd MMM yyyy' }}</td>
                  <td><button class="pr-btn pr-btn--ghost pr-btn--sm">{{ 'user.view' | translate }}</button></td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
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
export class FederationDashboardComponent implements OnInit {
  private api  = inject(ApiService);
  private auth = inject(AuthService);
  FederationResultStatus = FederationResultStatus;
  results = signal<FederationResult[]>([]);
  activityStats = signal<{ totalResults: number | null; resultsThisYear: number | null }>({
    totalResults: null, resultsThisYear: null
  });
  readonly thisYear = new Date().getFullYear();

  ngOnInit() {
    const FederationId = this.auth.FederationId();
    if (!FederationId) return;
    this.api.getFederationResults(FederationId, 1, 5).subscribe(p => {
      this.results.set(p.items as FederationResult[]);
    });
    this.api.getFederationStats().subscribe(s => {
      this.activityStats.set({ totalResults: s.totalResults, resultsThisYear: s.resultsThisYear });
    });
  }
}

// ── federation results Component ─────────────────────────────────────────────────

@Component({
  selector: 'app-federation-results',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="pr-page-header">
      <h1 class="pr-page-header__title">National Results</h1>
      <p class="pr-page-header__subtitle">Aggregate club races into national standings</p>
    </div>

    <div class="pr-grid-2" style="gap:24px">
      <!-- Create new result -->
      <div class="pr-card">
        <h3 style="margin-bottom:20px">Create National Result</h3>
        <div style="display:flex;flex-direction:column;gap:16px">
          <div class="pr-form-group">
            <label class="pr-label">Name</label>
            <input class="pr-input" [(ngModel)]="newName" placeholder="e.g. National Sprint Race 2025">
          </div>
          <div class="pr-form-group">
            <label class="pr-label">Description</label>
            <textarea class="pr-textarea" rows="3" placeholder="Optional notes..."></textarea>
          </div>
          <div class="pr-form-group">
            <label class="pr-label">Select Published Races to Include</label>
            <p class="text-muted text-sm">Select races from all clubs in your country</p>
          </div>
          <button class="pr-btn pr-btn--primary" [disabled]="creating()" (click)="create()">
            @if (creating()) { <span class="pr-spinner" style="width:16px;height:16px"></span> }
            Generate National Rankings
          </button>
        </div>
      </div>

      <!-- Existing results -->
      <div class="pr-card">
        <h3 style="margin-bottom:20px">Published Results</h3>
        @if (results().length === 0) {
          <div class="pr-empty">
            <div class="pr-empty__icon">🏆</div>
            <div class="pr-empty__title">No results yet</div>
          </div>
        } @else {
          @for (r of results(); track r.id) {
            <div class="result-item">
              <div>
                <div class="font-bold">{{ r.name }}</div>
                <div class="text-muted text-sm">{{ r.totalClubsCount }} clubs · {{ r.totalEntriesCount }} entries</div>
              </div>
              <div class="flex gap-2 items-center">
                <span [class]="r.status === FederationResultStatus.Published ? 'pr-badge pr-badge--success' : 'pr-badge pr-badge--muted'">
                  {{ r.status === FederationResultStatus.Published ? 'Published' : 'Draft' }}
                </span>
                @if (r.status !== FederationResultStatus.Published) {
                  <button class="pr-btn pr-btn--primary pr-btn--sm" (click)="publish(r.id)">Publish</button>
                }
              </div>
            </div>
          }
        }
      </div>
    </div>
  `,
  styles: [`
    .result-item {
      display:flex;justify-content:space-between;align-items:center;
      padding:12px;border-radius:var(--pr-radius);
      border:1px solid var(--pr-border);margin-bottom:8px;
    }
  `]
})
export class FederationResultsComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  FederationResultStatus = FederationResultStatus;
  results  = signal<FederationResult[]>([]);
  creating = signal(false);
  newName  = '';

  get FederationId(): string { return this.auth.FederationId() ?? ''; }

  ngOnInit() {
    if (!this.FederationId) return;
    this.api.getFederationResults(this.FederationId).subscribe(p => this.results.set(p.items as FederationResult[]));
  }

  create() {
    if (!this.FederationId) return;
    this.creating.set(true);
    this.api.createFederationResult({ FederationId: this.FederationId, name: this.newName, raceIds: [] })
      .subscribe({
        next: r => { this.results.update(arr => [r, ...arr]); this.creating.set(false); },
        error: () => this.creating.set(false)
      });
  }

  publish(id: string) {
    this.api.publishFederationResult(id).subscribe(updated => {
      this.results.update(arr => arr.map(r => r.id === id ? updated : r));
    });
  }
}
