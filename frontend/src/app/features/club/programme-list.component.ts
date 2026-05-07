import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe, DecimalPipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProgrammeApiService } from '../../core/services/programme-api.service';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/services';
import {
  Programme, ProgrammeSummary, ProgrammeStatus,
  ScoringMethod, SuperAceQualification, CalculationSummary
} from '../../core/models/programme.models';
import { RaceSummary } from '../../core/models';

// ── Programme List Page ────────────────────────────────────────────────────────

@Component({
  selector: 'app-programme-list',
  standalone: true,
  imports: [RouterLink, DatePipe, NgClass],
  template: `
    <div class="pr-page-header flex justify-between items-center">
      <div>
        <h1 class="pr-page-header__title">Programmes</h1>
        <p class="pr-page-header__subtitle">Seasons and series — Best Loft, Ace Pigeon, Super Ace</p>
      </div>
      <a routerLink="/club/programmes/new" class="pr-btn pr-btn--primary">+ New Programme</a>
    </div>

    @if (loading()) {
      <div class="pr-empty"><div class="pr-spinner" style="margin:0 auto"></div></div>
    } @else if (programmes().length === 0) {
      <div class="pr-empty">
        <div class="pr-empty__icon">🏆</div>
        <div class="pr-empty__title">No programmes yet</div>
        <p class="pr-empty__desc">Create a programme to start calculating Best Loft, Ace Pigeon and Super Ace results.</p>
        <a routerLink="/club/programmes/new" class="pr-btn pr-btn--primary" style="margin:20px auto 0">Create Programme</a>
      </div>
    } @else {
      <div class="prog-grid">
        @for (p of programmes(); track p.id) {
          <a [routerLink]="['/club/programmes', p.id]" class="prog-card pr-card">
            <div class="prog-card__header">
              <div class="prog-card__year">{{ p.year }}</div>
              <span [class]="'pr-badge ' + statusBadge(p.status)">{{ statusLabel(p.status) }}</span>
            </div>
            <div class="prog-card__name">{{ p.name }}</div>
            <div class="prog-card__meta flex gap-4 mt-4">
              <div class="pr-stat" style="text-align:left">
                <div class="pr-stat__value">{{ p.raceCount }}</div>
                <div class="pr-stat__label">Races</div>
              </div>
              <div class="pr-stat" style="text-align:left">
                <div class="pr-stat__value">{{ scoringLabel(p.scoringMethod) }}</div>
                <div class="pr-stat__label">Scoring</div>
              </div>
            </div>
          </a>
        }
      </div>
    }
  `,
  styles: [`
    .prog-grid { display:grid; grid-template-columns:repeat(auto-fill,minmax(280px,1fr)); gap:16px; }
    .prog-card { display:flex; flex-direction:column; cursor:pointer; text-decoration:none; }
    .prog-card__header { display:flex; justify-content:space-between; align-items:center; margin-bottom:8px; }
    .prog-card__year { font-size:0.75rem; font-weight:700; letter-spacing:0.08em; color:var(--pr-text-muted); }
    .prog-card__name { font-family:var(--font-display); font-weight:700; font-size:1.125rem; }
  `]
})
export class ProgrammeListComponent implements OnInit {
  private progApi = inject(ProgrammeApiService);
  private auth    = inject(AuthService);

  ProgrammeStatus = ProgrammeStatus;
  programmes = signal<ProgrammeSummary[]>([]);
  loading    = signal(true);

  ngOnInit() {
    const clubId = this.auth.clubId();
    if (!clubId) { this.loading.set(false); return; }
    this.progApi.getClubProgrammes(clubId).subscribe(p => {
      this.programmes.set(p.items as ProgrammeSummary[]);
      this.loading.set(false);
    });
  }

  statusBadge(s: ProgrammeStatus) {
    const m: Record<number, string> = {
      1:'pr-badge--muted', 2:'pr-badge--info', 3:'pr-badge--warning', 4:'pr-badge--success', 5:'pr-badge--error'
    };
    return m[s] ?? 'pr-badge--muted';
  }

  statusLabel(s: ProgrammeStatus) { return ProgrammeStatus[s] ?? 'Unknown'; }

  scoringLabel(s: ScoringMethod) {
    const m: Record<number, string> = { 1:'Avg Velocity', 2:'Points/Rank', 3:'% Velocity', 4:'Total Velocity' };
    return m[s] ?? 'Unknown';
  }
}

// ── Programme Detail / Dashboard Page ─────────────────────────────────────────

@Component({
  selector: 'app-programme-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, NgClass, FormsModule],
  template: `
    <div class="flex items-center gap-3 mb-6">
      <a routerLink="/club/programmes" class="pr-btn pr-btn--ghost pr-btn--sm">← Programmes</a>
      <span class="text-muted">/</span>
      <span class="text-sm">{{ programme()?.name }}</span>
    </div>

    @if (!programme()) {
      <div class="pr-empty"><div class="pr-spinner" style="margin:0 auto"></div></div>
    } @else {
      <div class="pr-page-header flex justify-between items-center">
        <div>
          <div class="flex items-center gap-3 mb-2">
            <h1 class="pr-page-header__title" style="margin:0">{{ programme()!.name }}</h1>
            <span [class]="'pr-badge ' + statusBadge(programme()!.status)">{{ ProgrammeStatus[programme()!.status] }}</span>
          </div>
          <p class="pr-page-header__subtitle">
            {{ programme()!.year }} ·
            {{ scoringLabel(programme()!.scoringMethod) }} ·
            {{ programme()!.races.length }} races
          </p>
        </div>
        <div class="flex gap-3">
          @if (programme()!.status !== ProgrammeStatus.Published) {
            <button class="pr-btn pr-btn--outline" [disabled]="calculating()" (click)="calculate()">
              @if (calculating()) { <span class="pr-spinner" style="width:14px;height:14px"></span> }
              ⚙ Calculate Results
            </button>
            <button class="pr-btn pr-btn--primary"
                    [disabled]="programme()!.status === ProgrammeStatus.Draft"
                    (click)="publish()">
              📢 Publish
            </button>
          }
        </div>
      </div>

      @if (calcSummary()) {
        <div class="pr-alert pr-alert--success mb-6">
          ✓ Calculated: {{ calcSummary()!.bestLoftEntriesCalculated }} lofts ·
          {{ calcSummary()!.acePigeonEntriesCalculated }} ace pigeons ·
          {{ calcSummary()!.superAcePigeonEntriesCalculated }} super ace ·
          {{ calcSummary()!.racesIncluded }} races
          @if (calcSummary()!.warnings) {
            <br><span style="opacity:0.8">⚠ {{ calcSummary()!.warnings }}</span>
          }
        </div>
      }

      <!-- Result type navigation -->
      <div class="result-type-nav mb-8">
        @for (tab of resultTabs; track tab.id) {
          <a [routerLink]="['/club/programmes', programme()!.id, tab.id]"
             class="result-type-card pr-card"
             [class.result-type-card--active]="false">
            <div class="result-type-card__icon">{{ tab.icon }}</div>
            <div class="result-type-card__label">{{ tab.label }}</div>
            <div class="result-type-card__desc">{{ tab.desc }}</div>
          </a>
        }
      </div>

      <!-- Programme races -->
      <div class="pr-grid-2" style="gap:24px">
        <div class="pr-card">
          <div class="flex justify-between items-center mb-4">
            <h3 style="margin:0">Programme Races ({{ programme()!.races.length }})</h3>
          </div>

          @if (programme()!.races.length === 0) {
            <div class="pr-empty">
              <div class="pr-empty__icon">🏁</div>
              <div class="pr-empty__title">No races assigned</div>
              <p class="pr-empty__desc">Add published races from your club to include them in calculations.</p>
            </div>
          } @else {
            @for (r of programme()!.races; track r.raceId) {
              <div class="race-row">
                <div class="flex-1">
                  <div class="font-bold text-sm">{{ r.raceName }}</div>
                  <div class="text-muted text-sm">{{ r.actualReleaseTime | date:'dd MMM yyyy' }} · {{ r.totalEntries }} entries · ×{{ r.scoreWeight }}</div>
                </div>
                @if (programme()!.status !== ProgrammeStatus.Published) {
                  <button class="pr-btn pr-btn--danger pr-btn--sm"
                          (click)="removeRace(r.raceId)">✕</button>
                }
              </div>
            }
          }

          <!-- Add race form -->
          @if (programme()!.status !== ProgrammeStatus.Published) {
            <div class="add-race-form mt-4">
              <select class="pr-select" [(ngModel)]="selectedRaceId" style="flex:1">
                <option value="">Select a race to add...</option>
                @for (r of availableRaces(); track r.id) {
                  <option [value]="r.id">{{ r.name }}</option>
                }
              </select>
              <button class="pr-btn pr-btn--outline"
                      [disabled]="!selectedRaceId"
                      (click)="addRace()">Add Race</button>
            </div>
          }
        </div>

        <!-- Scoring config summary -->
        <div class="pr-card">
          <h3 style="margin-bottom:20px">Configuration</h3>
          <div class="config-grid">
            <div class="config-row">
              <span class="config-label">Scoring Method</span>
              <span class="config-value">{{ scoringLabel(programme()!.scoringMethod) }}</span>
            </div>
            @if (programme()!.scoringMethod === ScoringMethod.PointsByRank) {
              <div class="config-row">
                <span class="config-label">Points for 1st</span>
                <span class="config-value">{{ programme()!.pointsForFirst }}</span>
              </div>
            }
            <div class="config-row">
              <span class="config-label">Best Loft: Pigeons/Race</span>
              <span class="config-value">{{ programme()!.bestLoftPigeonsPerRace === 0 ? 'All' : programme()!.bestLoftPigeonsPerRace }}</span>
            </div>
            <div class="config-row">
              <span class="config-label">Best Loft: Min Races</span>
              <span class="config-value">{{ programme()!.bestLoftMinRaces }}</span>
            </div>
            <div class="config-row">
              <span class="config-label">Ace Pigeon: Min Races</span>
              <span class="config-value">{{ programme()!.acePigeonMinRaces }}</span>
            </div>
            <div class="config-row">
              <span class="config-label">Super Ace: Qualification</span>
              <span class="config-value">{{ superAceLabel(programme()!.superAceQualification) }}</span>
            </div>
            @if (programme()!.superAceQualification === SuperAceQualification.MinimumRaceCount) {
              <div class="config-row">
                <span class="config-label">Super Ace: Min Races</span>
                <span class="config-value">{{ programme()!.superAceMinRaceCount }}</span>
              </div>
            }
            @if (programme()!.superAceQualification === SuperAceQualification.MinimumRacePercentage) {
              <div class="config-row">
                <span class="config-label">Super Ace: Min %</span>
                <span class="config-value">{{ programme()!.superAceMinRacePercentage }}%</span>
              </div>
            }
          </div>

          @if (programme()!.status !== ProgrammeStatus.Published) {
            <a [routerLink]="['/club/programmes', programme()!.id, 'edit']"
               class="pr-btn pr-btn--ghost pr-btn--sm" style="margin-top:16px">Edit Configuration</a>
          }
        </div>
      </div>
    }
  `,
  styles: [`
    .result-type-nav {
      display:grid; grid-template-columns:repeat(4,1fr); gap:16px;
    }
    @media (max-width:900px) { .result-type-nav { grid-template-columns:repeat(2,1fr); } }

    .result-type-card {
      display:flex; flex-direction:column; align-items:center; text-align:center;
      padding:24px 16px; cursor:pointer; text-decoration:none; transition:all var(--t-base);
    }
    .result-type-card:hover { border-color:var(--pr-primary); transform:translateY(-2px); }
    .result-type-card__icon { font-size:2rem; margin-bottom:8px; }
    .result-type-card__label { font-family:var(--font-display); font-weight:700; font-size:0.95rem; }
    .result-type-card__desc { font-size:0.75rem; color:var(--pr-text-muted); margin-top:4px; }

    .race-row {
      display:flex; align-items:center; gap:12px;
      padding:10px; border-radius:var(--pr-radius);
      border:1px solid var(--pr-border); margin-bottom:8px;
      background:var(--pr-surface-2);
    }
    .add-race-form { display:flex; gap:10px; align-items:center; }

    .config-grid { display:flex; flex-direction:column; gap:0; }
    .config-row {
      display:flex; justify-content:space-between;
      padding:8px 0; border-bottom:1px solid var(--pr-border); font-size:0.875rem;
    }
    .config-row:last-child { border-bottom:none; }
    .config-label { color:var(--pr-text-muted); }
    .config-value { font-weight:600; }
  `]
})
export class ProgrammeDetailComponent implements OnInit {
  private progApi = inject(ProgrammeApiService);
  private api     = inject(ApiService);
  private route   = inject(ActivatedRoute);
  private auth    = inject(AuthService);

  ProgrammeStatus = ProgrammeStatus;
  ScoringMethod   = ScoringMethod;
  SuperAceQualification = SuperAceQualification;

  programme      = signal<Programme | null>(null);
  availableRaces = signal<RaceSummary[]>([]);
  calcSummary    = signal<CalculationSummary | null>(null);
  calculating    = signal(false);
  selectedRaceId = '';

  resultTabs = [
    { id: 'race-results',      icon: '🏁', label: 'Race Results',      desc: 'Per-race velocity rankings' },
    { id: 'best-loft',         icon: '🏠', label: 'Best Loft',         desc: 'Top fanciers across all races' },
    { id: 'ace-pigeon',        icon: '🕊️', label: 'Ace Pigeon',        desc: 'Top individual birds' },
    { id: 'super-ace-pigeon',  icon: '⭐', label: 'Super Ace Pigeon',  desc: 'Elite qualifying pigeons' },
  ];

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.progApi.getProgramme(id).subscribe(p => {
      this.programme.set(p);
      this.loadAvailableRaces(p.clubId);
    });
  }

  loadAvailableRaces(clubId: string) {
    this.api.getClubRaces(clubId, 1, 100).subscribe(p => {
      const assigned = new Set(this.programme()?.races.map(r => r.raceId) ?? []);
      this.availableRaces.set((p.items as RaceSummary[]).filter(r => !assigned.has(r.id)));
    });
  }

  addRace() {
    if (!this.selectedRaceId || !this.programme()) return;
    const nextSort = this.programme()!.races.length;
    this.progApi.addRaceToProgramme(this.programme()!.id, this.selectedRaceId, 1.0, nextSort)
      .subscribe(p => { this.programme.set(p); this.selectedRaceId = ''; });
  }

  removeRace(raceId: string) {
    this.progApi.removeRaceFromProgramme(this.programme()!.id, raceId)
      .subscribe(() => {
        this.programme.update(p => p ? { ...p, races: p.races.filter(r => r.raceId !== raceId) } : p);
      });
  }

  calculate() {
    this.calculating.set(true);
    this.progApi.calculateResults(this.programme()!.id).subscribe({
      next: s => { this.calcSummary.set(s); this.calculating.set(false); },
      error: () => this.calculating.set(false)
    });
  }

  publish() {
    this.progApi.publishProgramme(this.programme()!.id).subscribe(p => this.programme.set(p));
  }

  statusBadge(s: ProgrammeStatus) {
    const m: Record<number, string> = { 1:'pr-badge--muted', 2:'pr-badge--info', 3:'pr-badge--warning', 4:'pr-badge--success', 5:'pr-badge--error' };
    return m[s] ?? 'pr-badge--muted';
  }

  scoringLabel(s: ScoringMethod) {
    const m: Record<number, string> = { 1:'Average Velocity', 2:'Points by Rank', 3:'Velocity %', 4:'Total Velocity' };
    return m[s] ?? 'Unknown';
  }

  superAceLabel(s: SuperAceQualification) {
    const m: Record<number, string> = { 1:'All Races Required', 2:'Minimum Race Count', 3:'Minimum Race %' };
    return m[s] ?? 'Unknown';
  }
}
