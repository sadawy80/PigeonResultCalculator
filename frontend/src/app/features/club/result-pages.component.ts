import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe, NgClass, PercentPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProgrammeApiService } from '../../core/services/programme-api.service';
import { ApiService } from '../../core/services/api.service';
import {
  Programme, BestLoftResult, AcePigeonResult, SuperAcePigeonResult,
  RaceBreakdownItem, ProgrammeStatus
} from '../../core/models/programme.models';
import { RaceResult, RaceSummary } from '../../core/models';
import { PrintButtonComponent } from './templates/print-button.component';
import { CertificatePickerComponent } from './templates/certificate-picker.component';
import { TemplateCategory } from '../../core/models/template.models';

// ── Shared breakdown expansion panel ─────────────────────────────────────────

function breakdownPanel(items: RaceBreakdownItem[]): string {
  return items.map(i =>
    `<div class="bd-row ${i.dnf ? 'bd-row--dnf' : ''}">
      <span class="bd-race">${i.raceName}</span>
      <span class="bd-rank">${i.dnf ? 'DNF' : '#' + i.clubRank}</span>
      <span class="bd-vel">${i.dnf ? '—' : i.speed.toFixed(1) + ' m/min'}</span>
      <span class="bd-score">${i.dnf ? '0' : i.score.toFixed(2)} pts</span>
    </div>`
  ).join('');
}

// ─────────────────────────────────────────────────────────────────────────────
//  Race Results Page — per-programme view of all individual race results
// ─────────────────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-programme-race-results',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, NgClass, FormsModule, PrintButtonComponent, CertificatePickerComponent],
  template: `
    <div class="flex items-center gap-3 mb-6">
      <a [routerLink]="['/club/programmes', programmeId]" class="pr-btn pr-btn--ghost pr-btn--sm">← Programme</a>
      <span class="text-muted">/</span>
      <span class="text-sm">Race Results</span>
    </div>

    <div class="pr-page-header flex justify-between items-center">
      <div>
        <h1 class="pr-page-header__title">🏁 Race Results</h1>
        <p class="pr-page-header__subtitle">Per-race speed rankings for all races in this programme</p>
      </div>
      <app-print-button
        [category]="TemplateCategory.RaceResults"
        [raceId]="selectedRaceId || undefined">
      </app-print-button>
    </div>

    <!-- Race selector -->
    <div class="flex gap-4 mb-6">
      <select class="pr-select" style="max-width:320px" [(ngModel)]="selectedRaceId" (ngModelChange)="loadRaceResults()">
        <option value="">Select a race...</option>
        @for (r of races(); track r.id) {
          <option [value]="r.id">{{ r.name }}</option>
        }
      </select>
      @if (selectedRaceId) {
        <input class="pr-input" style="max-width:240px"
               placeholder="Search ring #, fancier..."
               [(ngModel)]="search" (ngModelChange)="loadRaceResults()">
      }
    </div>

    @if (selectedRaceId) {
      <div class="pr-card">
        @if (loadingResults()) {
          <div class="pr-empty"><div class="pr-spinner" style="margin:0 auto"></div></div>
        } @else if (results().length === 0) {
          <div class="pr-empty">
            <div class="pr-empty__icon">📋</div>
            <div class="pr-empty__title">No published results for this race</div>
          </div>
        } @else {
          <div class="pr-table-wrapper">
            <table class="pr-table">
              <thead>
                <tr>
                  <th>Rank</th><th>Ring #</th><th>Pigeon</th><th>Fancier</th>
                  <th>Category</th><th style="text-align:right">Speed (m/min)</th>
                  <th style="text-align:right">Speed (km/h)</th>
                  <th style="text-align:right">Distance</th><th>Arrival</th>
                </tr>
              </thead>
              <tbody>
                @for (r of results(); track r.id) {
                  <tr>
                    <td><span [class]="'pr-rank ' + rankClass(r.clubRank)">{{ r.clubRank ?? '—' }}</span></td>
                    <td><code class="ring-code">{{ r.ringNumber }}</code></td>
                    <td>{{ r.pigeonName ?? '—' }} <span class="text-muted text-sm">{{ r.pigeonSex }}</span></td>
                    <td>{{ r.fancierName ?? '—' }}</td>
                    <td class="text-muted text-sm">{{ r.categoryName ?? 'Open' }}</td>
                    <td style="text-align:right" class="font-bold">{{ r.speedMperMin | number:'1.4-4' }}</td>
                    <td style="text-align:right" class="text-muted">{{ r.speedKmH | number:'1.3-3' }}</td>
                    <td style="text-align:right" class="text-muted text-sm">{{ r.distanceKm | number:'1.3-3' }} km</td>
                    <td class="text-muted text-sm">{{ r.arrivalTime | date:'HH:mm:ss' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <div class="flex justify-between items-center mt-4 text-sm text-muted">
            <span>{{ totalCount() }} entries</span>
            <div class="flex gap-2">
              <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() === 1" (click)="changePage(page()-1)">← Prev</button>
              <span class="flex items-center px-2">{{ page() }} / {{ totalPages() }}</span>
              <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() >= totalPages()" (click)="changePage(page()+1)">Next →</button>
            </div>
          </div>
        }
      </div>
    } @else {
      <div class="pr-empty">
        <div class="pr-empty__icon">🏁</div>
        <div class="pr-empty__title">Select a race above to view results</div>
      </div>
    }
  `,
  styles: [`.ring-code{font-size:0.8rem;background:var(--pr-surface-2);padding:2px 6px;border-radius:4px}`]
})
export class ProgrammeRaceResultsComponent implements OnInit {
  private route  = inject(ActivatedRoute);
  private api    = inject(ApiService);
  private progApi = inject(ProgrammeApiService);

  TemplateCategory = TemplateCategory;

  programmeId    = '';
  races          = signal<any[]>([]);
  results        = signal<RaceResult[]>([]);
  loadingResults = signal(false);
  selectedRaceId = '';
  search         = '';
  page           = signal(1);
  totalCount     = signal(0);
  totalPages     = signal(1);

  ngOnInit() {
    this.programmeId = this.route.snapshot.paramMap.get('id')!;
    this.progApi.getProgramme(this.programmeId).subscribe(p => {
      this.races.set(p.races);
    });
  }

  loadRaceResults() {
    if (!this.selectedRaceId) return;
    this.loadingResults.set(true);
    this.api.getRaceResults(this.selectedRaceId, this.page(), 50, undefined, this.search || undefined)
      .subscribe(p => {
        this.results.set(p.items as RaceResult[]);
        this.totalCount.set(p.totalCount);
        this.totalPages.set(p.totalPages);
        this.loadingResults.set(false);
      });
  }

  changePage(p: number) { this.page.set(p); this.loadRaceResults(); }

  rankClass(rank?: number | null) {
    if (!rank) return 'pr-rank--other';
    return rank <= 3 ? `pr-rank--${rank}` : 'pr-rank--other';
  }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Best Loft Results Page
// ─────────────────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-best-loft-results',
  standalone: true,
  imports: [RouterLink, DecimalPipe, NgClass, FormsModule],
  template: `
    <div class="flex items-center gap-3 mb-6">
      <a [routerLink]="['/club/programmes', programmeId]" class="pr-btn pr-btn--ghost pr-btn--sm">← Programme</a>
      <span class="text-muted">/</span>
      <span class="text-sm">Best Loft Results</span>
    </div>

    <div class="pr-page-header flex justify-between items-center">
      <div>
        <h1 class="pr-page-header__title">🏠 Best Loft Results</h1>
        <p class="pr-page-header__subtitle">{{ programmeName() }} · Fanciers ranked by overall loft performance across all programme races</p>
      </div>
    </div>

    <div class="flex gap-4 mb-6">
      <input class="pr-input" style="max-width:280px" placeholder="Search fancier..."
             [(ngModel)]="search" (ngModelChange)="load()">
    </div>

    <div class="pr-card">
      @if (loading()) {
        <div class="pr-empty"><div class="pr-spinner" style="margin:0 auto"></div></div>
      } @else if (results().length === 0) {
        <div class="pr-empty">
          <div class="pr-empty__icon">🏠</div>
          <div class="pr-empty__title">No Best Loft results calculated</div>
          <p class="pr-empty__desc">Run the calculation from the Programme page first.</p>
        </div>
      } @else {
        <div class="pr-table-wrapper">
          <table class="pr-table">
            <thead>
              <tr>
                <th>Rank</th>
                <th>Fancier</th>
                <th style="text-align:right">Total Score</th>
                <th style="text-align:right">Avg Score</th>
                <th style="text-align:right">Races</th>
                <th style="text-align:right">Pigeons</th>
                <th style="text-align:right">Best Speed</th>
                <th style="text-align:right">Avg Speed</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (r of results(); track r.id) {
                <tr>
                  <td><span [class]="'pr-rank ' + rankClass(r.loftRank)">{{ r.loftRank }}</span></td>
                  <td class="font-bold">{{ r.fancierName }}</td>
                  <td style="text-align:right" class="font-bold">{{ r.totalScore | number:'1.2-2' }}</td>
                  <td style="text-align:right">{{ r.averageScore | number:'1.2-2' }}</td>
                  <td style="text-align:right">{{ r.racesEntered }}</td>
                  <td style="text-align:right">{{ r.pigeonsEntered }}</td>
                  <td style="text-align:right">{{ r.bestSingleSpeedMperMin | number:'1.0-1' }}</td>
                  <td style="text-align:right">{{ r.averageSpeedMperMin | number:'1.0-1' }}</td>
                  <td>
                    <button class="pr-btn pr-btn--ghost pr-btn--sm"
                            (click)="toggleBreakdown(r.id)">
                      {{ expandedId() === r.id ? '▲' : '▼' }} Races
                    </button>
                  </td>
                </tr>
                @if (expandedId() === r.id) {
                  <tr class="breakdown-row">
                    <td colspan="9">
                      <div class="breakdown-panel">
                        <div class="bd-header">
                          <span>Race</span><span>Rank</span><span>Speed</span><span>Score</span>
                        </div>
                        @for (b of r.raceBreakdown; track b.raceId) {
                          <div class="bd-row" [class.bd-row--dnf]="b.dnf">
                            <span class="bd-race">{{ b.raceName }}</span>
                            <span class="bd-rank">{{ b.dnf ? 'DNF' : '#' + b.clubRank }}</span>
                            <span class="bd-vel">{{ b.dnf ? '—' : (b.speed | number:'1.0-1') + ' m/min' }}</span>
                            <span class="bd-score">{{ b.score | number:'1.2-2' }}</span>
                          </div>
                        }
                      </div>
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>
        <div class="flex justify-between items-center mt-4 text-sm text-muted">
          <span>{{ totalCount() }} fanciers</span>
          <div class="flex gap-2">
            <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() === 1" (click)="changePage(page()-1)">← Prev</button>
            <span class="flex items-center px-2">{{ page() }} / {{ totalPages() }}</span>
            <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() >= totalPages()" (click)="changePage(page()+1)">Next →</button>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .breakdown-row td { padding:0; }
    .breakdown-panel { background:var(--pr-surface-2); border-top:1px solid var(--pr-border); padding:12px 16px; }
    .bd-header { display:grid; grid-template-columns:1fr 80px 140px 100px; gap:8px; padding:4px 0 8px; font-size:0.72rem; font-weight:700; text-transform:uppercase; letter-spacing:0.06em; color:var(--pr-text-muted); border-bottom:1px solid var(--pr-border); }
    .bd-row { display:grid; grid-template-columns:1fr 80px 140px 100px; gap:8px; padding:6px 0; font-size:0.85rem; border-bottom:1px solid var(--pr-border); }
    .bd-row:last-child { border-bottom:none; }
    .bd-row--dnf { opacity:0.5; }
    .bd-rank { font-weight:600; }
    .bd-score { font-weight:700; color:var(--pr-primary); }
  `]
})
export class BestLoftResultsComponent implements OnInit {
  private route   = inject(ActivatedRoute);
  private progApi = inject(ProgrammeApiService);

  TemplateCategory = TemplateCategory;

  programmeId   = '';
  programmeName = signal('');
  results       = signal<BestLoftResult[]>([]);
  loading       = signal(true);
  expandedId    = signal<string | null>(null);
  search        = '';
  page          = signal(1);
  totalCount    = signal(0);
  totalPages    = signal(1);

  ngOnInit() {
    this.programmeId = this.route.snapshot.paramMap.get('id')!;
    this.progApi.getProgramme(this.programmeId).subscribe(p => this.programmeName.set(p.name));
    this.load();
  }

  load() {
    this.loading.set(true);
    this.progApi.getBestLoftResults(this.programmeId, this.page(), 50, this.search || undefined)
      .subscribe(p => {
        this.results.set(p.items as BestLoftResult[]);
        this.totalCount.set(p.totalCount);
        this.totalPages.set(p.totalPages);
        this.loading.set(false);
      });
  }

  toggleBreakdown(id: string) {
    this.expandedId.set(this.expandedId() === id ? null : id);
  }

  changePage(p: number) { this.page.set(p); this.load(); }

  rankClass(rank: number) {
    return rank <= 3 ? `pr-rank--${rank}` : 'pr-rank--other';
  }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Ace Pigeon Results Page
// ─────────────────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-ace-pigeon-results',
  standalone: true,
  imports: [RouterLink, DecimalPipe, NgClass, FormsModule],
  template: `
    <div class="flex items-center gap-3 mb-6">
      <a [routerLink]="['/club/programmes', programmeId]" class="pr-btn pr-btn--ghost pr-btn--sm">← Programme</a>
      <span class="text-muted">/</span>
      <span class="text-sm">Ace Pigeon Results</span>
    </div>

    <div class="pr-page-header">
      <h1 class="pr-page-header__title">🕊️ Ace Pigeon Results</h1>
      <p class="pr-page-header__subtitle">{{ programmeName() }} · Top individual pigeons ranked across all programme races</p>
    </div>

    <div class="flex gap-4 mb-6">
      <input class="pr-input" style="max-width:280px" placeholder="Search ring #, pigeon, fancier..."
             [(ngModel)]="search" (ngModelChange)="load()">
    </div>

    <div class="pr-card">
      @if (loading()) {
        <div class="pr-empty"><div class="pr-spinner" style="margin:0 auto"></div></div>
      } @else if (results().length === 0) {
        <div class="pr-empty">
          <div class="pr-empty__icon">🕊️</div>
          <div class="pr-empty__title">No Ace Pigeon results calculated</div>
          <p class="pr-empty__desc">Run the calculation from the Programme page first.</p>
        </div>
      } @else {
        <div class="pr-table-wrapper">
          <table class="pr-table">
            <thead>
              <tr>
                <th>Rank</th><th>Ring #</th><th>Pigeon</th><th>Fancier</th>
                <th style="text-align:right">Score</th>
                <th style="text-align:right">Avg Score</th>
                <th style="text-align:right">Races</th>
                <th style="text-align:right">Participation</th>
                <th style="text-align:right">Best Speed</th>
                <th style="text-align:right">Best Rank</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (r of results(); track r.id) {
                <tr>
                  <td><span [class]="'pr-rank ' + rankClass(r.aceRank)">{{ r.aceRank }}</span></td>
                  <td><code class="ring-code">{{ r.ringNumber }}</code></td>
                  <td>
                    <span class="font-bold">{{ r.pigeonName ?? '—' }}</span>
                    @if (r.pigeonSex) { <span class="sex-badge" [class]="r.pigeonSex === 'M' ? 'sex-badge--male' : 'sex-badge--female'">{{ r.pigeonSex === 'M' ? 'Cock' : 'Hen' }}</span> }
                    @if (r.pigeonYearOfBirth) { <span class="text-muted text-sm"> · {{ r.pigeonYearOfBirth }}</span> }
                  </td>
                  <td>{{ r.fancierName }}</td>
                  <td style="text-align:right" class="font-bold">{{ r.totalScore | number:'1.2-2' }}</td>
                  <td style="text-align:right">{{ r.averageScore | number:'1.2-2' }}</td>
                  <td style="text-align:right">{{ r.racesEntered }} / {{ r.racesInProgramme }}</td>
                  <td style="text-align:right">
                    <span [class]="r.participationRate < 50 ? 'text-muted' : r.participationRate === 100 ? '' : ''">
                      {{ r.participationRate | number:'1.0-0' }}%
                    </span>
                  </td>
                  <td style="text-align:right">{{ r.bestSpeedMperMin | number:'1.0-1' }}</td>
                  <td style="text-align:right">
                    <span [class]="'pr-rank pr-rank--' + (r.bestClubRank <= 3 ? r.bestClubRank : 'other')" style="width:auto;border-radius:4px;padding:2px 8px">
                      #{{ r.bestClubRank }}
                    </span>
                  </td>
                  <td>
                    <button class="pr-btn pr-btn--ghost pr-btn--sm" (click)="toggleBreakdown(r.id)">
                      {{ expandedId() === r.id ? '▲' : '▼' }}
                    </button>
                  </td>
                </tr>
                @if (expandedId() === r.id) {
                  <tr class="breakdown-row">
                    <td colspan="11">
                      <div class="breakdown-panel">
                        <div class="bd-header">
                          <span>Race</span><span>Club Rank</span><span>Speed (m/min)</span><span>Score</span>
                        </div>
                        @for (b of r.raceBreakdown; track b.raceId) {
                          <div class="bd-row" [class.bd-row--dnf]="b.dnf">
                            <span>{{ b.raceName }}</span>
                            <span class="font-bold">{{ b.dnf ? 'DNF' : '#' + b.clubRank }}</span>
                            <span>{{ b.dnf ? '—' : (b.speed | number:'1.4-4') }}</span>
                            <span class="font-bold" style="color:var(--pr-primary)">{{ b.score | number:'1.2-2' }}</span>
                          </div>
                        }
                      </div>
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>
        <div class="flex justify-between items-center mt-4 text-sm text-muted">
          <span>{{ totalCount() }} pigeons</span>
          <div class="flex gap-2">
            <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() === 1" (click)="changePage(page()-1)">← Prev</button>
            <span class="flex items-center px-2">{{ page() }} / {{ totalPages() }}</span>
            <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() >= totalPages()" (click)="changePage(page()+1)">Next →</button>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .ring-code{font-size:0.8rem;background:var(--pr-surface-2);padding:2px 6px;border-radius:4px}
    .sex-badge{font-size:0.68rem;font-weight:700;padding:1px 6px;border-radius:999px;margin-left:6px}
    .sex-badge--male{background:rgba(30,144,255,0.15);color:var(--pr-primary)}
    .sex-badge--female{background:rgba(255,82,82,0.15);color:var(--pr-error)}
    .breakdown-row td{padding:0}
    .breakdown-panel{background:var(--pr-surface-2);border-top:1px solid var(--pr-border);padding:12px 16px}
    .bd-header{display:grid;grid-template-columns:1fr 100px 160px 100px;gap:8px;padding:4px 0 8px;font-size:0.72rem;font-weight:700;text-transform:uppercase;letter-spacing:0.06em;color:var(--pr-text-muted);border-bottom:1px solid var(--pr-border)}
    .bd-row{display:grid;grid-template-columns:1fr 100px 160px 100px;gap:8px;padding:6px 0;font-size:0.85rem;border-bottom:1px solid var(--pr-border)}
    .bd-row:last-child{border-bottom:none}
    .bd-row--dnf{opacity:0.5}
  `]
})
export class AcePigeonResultsComponent implements OnInit {
  private route   = inject(ActivatedRoute);
  private progApi = inject(ProgrammeApiService);

  TemplateCategory = TemplateCategory;

  programmeId   = '';
  programmeName = signal('');
  results       = signal<AcePigeonResult[]>([]);
  loading       = signal(true);
  expandedId    = signal<string | null>(null);
  search        = '';
  page          = signal(1);
  totalCount    = signal(0);
  totalPages    = signal(1);

  ngOnInit() {
    this.programmeId = this.route.snapshot.paramMap.get('id')!;
    this.progApi.getProgramme(this.programmeId).subscribe(p => this.programmeName.set(p.name));
    this.load();
  }

  load() {
    this.loading.set(true);
    this.progApi.getAcePigeonResults(this.programmeId, this.page(), 50, this.search || undefined)
      .subscribe(p => {
        this.results.set(p.items as AcePigeonResult[]);
        this.totalCount.set(p.totalCount);
        this.totalPages.set(p.totalPages);
        this.loading.set(false);
      });
  }

  toggleBreakdown(id: string) { this.expandedId.set(this.expandedId() === id ? null : id); }
  changePage(p: number) { this.page.set(p); this.load(); }
  rankClass(rank: number) { return rank <= 3 ? `pr-rank--${rank}` : 'pr-rank--other'; }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Super Ace Pigeon Results Page
// ─────────────────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-super-ace-pigeon-results',
  standalone: true,
  imports: [RouterLink, DecimalPipe, NgClass, FormsModule],
  template: `
    <div class="flex items-center gap-3 mb-6">
      <a [routerLink]="['/club/programmes', programmeId]" class="pr-btn pr-btn--ghost pr-btn--sm">← Programme</a>
      <span class="text-muted">/</span>
      <span class="text-sm">Super Ace Pigeon</span>
    </div>

    <div class="pr-page-header">
      <h1 class="pr-page-header__title">⭐ Super Ace Pigeon</h1>
      <p class="pr-page-header__subtitle">{{ programmeName() }} · Elite qualifying pigeons meeting strict participation criteria</p>
    </div>

    <!-- Qualification info banner -->
    @if (qualificationInfo()) {
      <div class="pr-alert pr-alert--info mb-6">
        <span>⭐</span>
        <span>{{ qualificationInfo() }}</span>
      </div>
    }

    <div class="flex gap-4 mb-6">
      <input class="pr-input" style="max-width:280px" placeholder="Search ring #, pigeon, fancier..."
             [(ngModel)]="search" (ngModelChange)="load()">
    </div>

    <div class="pr-card">
      @if (loading()) {
        <div class="pr-empty"><div class="pr-spinner" style="margin:0 auto"></div></div>
      } @else if (results().length === 0) {
        <div class="pr-empty">
          <div class="pr-empty__icon">⭐</div>
          <div class="pr-empty__title">No Super Ace Pigeon qualifiers</div>
          <p class="pr-empty__desc">Either the calculation hasn't been run yet, or no pigeons met the Super Ace qualification criteria.</p>
        </div>
      } @else {
        <!-- Top 3 podium cards -->
        @if (page() === 1) {
          <div class="podium mb-8">
            @for (r of results().slice(0,3); track r.id) {
              <div class="podium-card" [class]="'podium-card--' + r.superAceRank">
                <div class="podium-card__rank">{{ r.superAceRank === 1 ? '🥇' : r.superAceRank === 2 ? '🥈' : '🥉' }}</div>
                <code class="podium-card__ring">{{ r.ringNumber }}</code>
                <div class="podium-card__name">{{ r.pigeonName ?? 'Unnamed' }}</div>
                <div class="podium-card__fancier">{{ r.fancierName }}</div>
                <div class="podium-card__score">{{ r.totalScore | number:'1.2-2' }}</div>
                <div class="podium-card__sub">{{ r.averageSpeedMperMin | number:'1.0-1' }} m/min avg</div>
                <div class="podium-card__races">{{ r.racesEntered }}/{{ r.racesInProgramme }} races</div>
              </div>
            }
          </div>
        }

        <div class="pr-table-wrapper">
          <table class="pr-table">
            <thead>
              <tr>
                <th>Rank</th><th>Ring #</th><th>Pigeon</th><th>Fancier</th>
                <th style="text-align:right">Total Score</th>
                <th style="text-align:right">Avg Score</th>
                <th style="text-align:right">Races</th>
                <th style="text-align:right">Best Vel.</th>
                <th style="text-align:right">Avg Vel.</th>
                <th style="text-align:right">Best Rank</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (r of results(); track r.id) {
                <tr [class.podium-row]="r.superAceRank <= 3">
                  <td>
                    <span [class]="'pr-rank ' + rankClass(r.superAceRank)">
                      {{ r.superAceRank <= 3 ? ['🥇','🥈','🥉'][r.superAceRank-1] : r.superAceRank }}
                    </span>
                  </td>
                  <td><code class="ring-code">{{ r.ringNumber }}</code></td>
                  <td>
                    <span class="font-bold">{{ r.pigeonName ?? '—' }}</span>
                    @if (r.pigeonSex) { <span class="sex-badge" [class]="r.pigeonSex === 'M' ? 'sex-badge--male' : 'sex-badge--female'">{{ r.pigeonSex === 'M' ? '♂' : '♀' }}</span> }
                  </td>
                  <td>{{ r.fancierName }}</td>
                  <td style="text-align:right" class="font-bold">{{ r.totalScore | number:'1.2-2' }}</td>
                  <td style="text-align:right">{{ r.averageScore | number:'1.2-2' }}</td>
                  <td style="text-align:right">{{ r.racesEntered }}/{{ r.racesInProgramme }}</td>
                  <td style="text-align:right">{{ r.bestSpeedMperMin | number:'1.0-1' }}</td>
                  <td style="text-align:right">{{ r.averageSpeedMperMin | number:'1.0-1' }}</td>
                  <td style="text-align:right">
                    <span [class]="'pr-rank pr-rank--' + (r.bestClubRank <= 3 ? r.bestClubRank : 'other')" style="width:auto;border-radius:4px;padding:2px 8px">
                      #{{ r.bestClubRank }}
                    </span>
                  </td>
                  <td>
                    <button class="pr-btn pr-btn--ghost pr-btn--sm" (click)="toggleBreakdown(r.id)">
                      {{ expandedId() === r.id ? '▲' : '▼' }}
                    </button>
                  </td>
                </tr>
                @if (expandedId() === r.id) {
                  <tr class="breakdown-row">
                    <td colspan="11">
                      <div class="breakdown-panel">
                        <div class="bd-header"><span>Race</span><span>Club Rank</span><span>Speed</span><span>Score</span></div>
                        @for (b of r.raceBreakdown; track b.raceId) {
                          <div class="bd-row" [class.bd-row--dnf]="b.dnf">
                            <span>{{ b.raceName }}</span>
                            <span class="font-bold">{{ b.dnf ? 'DNF' : '#' + b.clubRank }}</span>
                            <span>{{ b.dnf ? '—' : (b.speed | number:'1.4-4') + ' m/min' }}</span>
                            <span class="font-bold" style="color:var(--pr-primary)">{{ b.score | number:'1.2-2' }}</span>
                          </div>
                        }
                      </div>
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>
        <div class="flex justify-between items-center mt-4 text-sm text-muted">
          <span>{{ totalCount() }} Super Ace qualifiers</span>
          <div class="flex gap-2">
            <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() === 1" (click)="changePage(page()-1)">← Prev</button>
            <span class="flex items-center px-2">{{ page() }} / {{ totalPages() }}</span>
            <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() >= totalPages()" (click)="changePage(page()+1)">Next →</button>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .podium { display:grid; grid-template-columns:repeat(3,1fr); gap:16px; margin-bottom:32px; }
    @media (max-width:700px) { .podium { grid-template-columns:1fr; } }

    .podium-card {
      background:var(--pr-surface); border:2px solid var(--pr-border);
      border-radius:calc(var(--pr-radius)*1.5); padding:24px 16px; text-align:center;
    }
    .podium-card--1 { border-color:#FFD700; box-shadow:0 0 20px rgba(255,215,0,0.2); }
    .podium-card--2 { border-color:#C0C0C0; box-shadow:0 0 16px rgba(192,192,192,0.15); }
    .podium-card--3 { border-color:#CD7F32; box-shadow:0 0 16px rgba(205,127,50,0.15); }
    .podium-card__rank { font-size:2.5rem; margin-bottom:8px; }
    .podium-card__ring { font-size:0.8rem; background:var(--pr-surface-2); padding:2px 8px; border-radius:4px; }
    .podium-card__name { font-family:var(--font-display); font-weight:700; font-size:1rem; margin-top:10px; }
    .podium-card__fancier { font-size:0.8rem; color:var(--pr-text-muted); margin-top:2px; }
    .podium-card__score { font-family:var(--font-display); font-size:1.5rem; font-weight:800; color:var(--pr-primary); margin-top:8px; }
    .podium-card__sub { font-size:0.75rem; color:var(--pr-text-muted); }
    .podium-card__races { font-size:0.72rem; color:var(--pr-text-muted); margin-top:4px; }

    .podium-row { background:rgba(30,144,255,0.03); }
    .ring-code { font-size:0.8rem; background:var(--pr-surface-2); padding:2px 6px; border-radius:4px; }
    .sex-badge { font-size:0.8rem; margin-left:4px; }
    .sex-badge--male { color:var(--pr-primary); }
    .sex-badge--female { color:var(--pr-error); }

    .breakdown-row td { padding:0; }
    .breakdown-panel { background:var(--pr-surface-2); border-top:1px solid var(--pr-border); padding:12px 16px; }
    .bd-header { display:grid; grid-template-columns:1fr 100px 160px 100px; gap:8px; padding:4px 0 8px; font-size:0.72rem; font-weight:700; text-transform:uppercase; letter-spacing:0.06em; color:var(--pr-text-muted); border-bottom:1px solid var(--pr-border); }
    .bd-row { display:grid; grid-template-columns:1fr 100px 160px 100px; gap:8px; padding:6px 0; font-size:0.85rem; border-bottom:1px solid var(--pr-border); }
    .bd-row:last-child { border-bottom:none; }
    .bd-row--dnf { opacity:0.5; }
  `]
})
export class SuperAcePigeonResultsComponent implements OnInit {
  private route   = inject(ActivatedRoute);
  private progApi = inject(ProgrammeApiService);

  TemplateCategory = TemplateCategory;

  programmeId       = '';
  programmeName     = signal('');
  qualificationInfo = signal('');
  results           = signal<SuperAcePigeonResult[]>([]);
  loading           = signal(true);
  expandedId        = signal<string | null>(null);
  search            = '';
  page              = signal(1);
  totalCount        = signal(0);
  totalPages        = signal(1);

  ngOnInit() {
    this.programmeId = this.route.snapshot.paramMap.get('id')!;
    this.progApi.getProgramme(this.programmeId).subscribe(p => {
      this.programmeName.set(p.name);
      const qi = p.superAceQualification === 1
        ? `Pigeons must have entered all ${p.races.length} races`
        : p.superAceQualification === 2
          ? `Pigeons must have entered at least ${p.superAceMinRaceCount} races`
          : `Pigeons must have entered at least ${p.superAceMinRacePercentage}% of races`;
      this.qualificationInfo.set(qi);
    });
    this.load();
  }

  load() {
    this.loading.set(true);
    this.progApi.getSuperAcePigeonResults(this.programmeId, this.page(), 50, this.search || undefined)
      .subscribe(p => {
        this.results.set(p.items as SuperAcePigeonResult[]);
        this.totalCount.set(p.totalCount);
        this.totalPages.set(p.totalPages);
        this.loading.set(false);
      });
  }

  toggleBreakdown(id: string) { this.expandedId.set(this.expandedId() === id ? null : id); }
  changePage(p: number) { this.page.set(p); this.load(); }
  rankClass(rank: number) { return rank <= 3 ? `pr-rank--${rank}` : 'pr-rank--other'; }
}
