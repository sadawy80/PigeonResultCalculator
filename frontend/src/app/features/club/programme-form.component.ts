import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgClass } from '@angular/common';
import { ProgrammeApiService } from '../../core/services/programme-api.service';
import { AuthService } from '../../core/services/services';
import { ScoringMethod, SuperAceQualification } from '../../core/models/programme.models';

@Component({
  selector: 'app-programme-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, NgClass],
  template: `
    <div class="flex items-center gap-3 mb-6">
      <a routerLink="/club/programmes" class="pr-btn pr-btn--ghost pr-btn--sm">← Programmes</a>
    </div>

    <div class="pr-page-header">
      <h1 class="pr-page-header__title">{{ isEdit() ? 'Edit Programme' : 'Create Programme' }}</h1>
      <p class="pr-page-header__subtitle">Configure the season/series, scoring rules, and qualification thresholds</p>
    </div>

    <form [formGroup]="form" (ngSubmit)="submit()">
      <div class="form-cols">

        <!-- Left: basics -->
        <div>
          <div class="pr-card mb-6">
            <h3 style="margin-bottom:20px">Programme Details</h3>
            <div style="display:flex;flex-direction:column;gap:16px">
              <div class="pr-form-group">
                <label class="pr-label">Name *</label>
                <input class="pr-input" formControlName="name" placeholder="e.g. 2025 Club Season">
              </div>
              <div class="pr-form-group">
                <label class="pr-label">Description</label>
                <textarea class="pr-textarea" formControlName="description" rows="2"
                          placeholder="Optional details..."></textarea>
              </div>
              <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
                <div class="pr-form-group">
                  <label class="pr-label">Year *</label>
                  <input class="pr-input" type="number" formControlName="year" min="2000" max="2100">
                </div>
                <div></div>
              </div>
              <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
                <div class="pr-form-group">
                  <label class="pr-label">Start Date</label>
                  <input class="pr-input" type="date" formControlName="startDate">
                </div>
                <div class="pr-form-group">
                  <label class="pr-label">End Date</label>
                  <input class="pr-input" type="date" formControlName="endDate">
                </div>
              </div>
            </div>
          </div>

          <!-- Scoring method -->
          <div class="pr-card mb-6">
            <h3 style="margin-bottom:6px">Scoring Method</h3>
            <p class="text-muted text-sm mb-4">How scores are calculated across races for all result types.</p>

            <div class="scoring-options">
              @for (opt of scoringOptions; track opt.value) {
                <label class="scoring-option" [class.scoring-option--active]="form.get('scoringMethod')?.value == opt.value">
                  <input type="radio" formControlName="scoringMethod" [value]="opt.value" style="display:none">
                  <div class="scoring-option__icon">{{ opt.icon }}</div>
                  <div>
                    <div class="scoring-option__label">{{ opt.label }}</div>
                    <div class="scoring-option__desc">{{ opt.desc }}</div>
                  </div>
                </label>
              }
            </div>

            @if (form.get('scoringMethod')?.value == ScoringMethod.PointsByRank) {
              <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-top:16px">
                <div class="pr-form-group">
                  <label class="pr-label">Points for 1st Place</label>
                  <input class="pr-input" type="number" formControlName="pointsForFirst" min="1" max="100">
                </div>
                <div class="pr-form-group">
                  <label class="pr-label">Max Paid Positions (0 = all)</label>
                  <input class="pr-input" type="number" formControlName="maxPointPositions" min="0">
                </div>
              </div>
            }
          </div>
        </div>

        <!-- Right: qualification thresholds -->
        <div>
          <div class="pr-card mb-6">
            <h3 style="margin-bottom:6px">🏠 Best Loft</h3>
            <p class="text-muted text-sm mb-4">How fancier loft performance is measured across races.</p>
            <div style="display:flex;flex-direction:column;gap:14px">
              <div class="pr-form-group">
                <label class="pr-label">Pigeons per race counted (0 = all)</label>
                <input class="pr-input" type="number" formControlName="bestLoftPigeonsPerRace" min="0">
                <span class="field-hint">e.g. 3 means only the top 3 velocities from each loft count per race</span>
              </div>
              <div class="pr-form-group">
                <label class="pr-label">Minimum races to qualify</label>
                <input class="pr-input" type="number" formControlName="bestLoftMinRaces" min="1">
              </div>
            </div>
          </div>

          <div class="pr-card mb-6">
            <h3 style="margin-bottom:6px">🕊️ Ace Pigeon</h3>
            <p class="text-muted text-sm mb-4">Qualification threshold for individual pigeons.</p>
            <div class="pr-form-group">
              <label class="pr-label">Minimum races a pigeon must enter</label>
              <input class="pr-input" type="number" formControlName="acePigeonMinRaces" min="1">
            </div>
          </div>

          <div class="pr-card mb-6">
            <h3 style="margin-bottom:6px">⭐ Super Ace Pigeon</h3>
            <p class="text-muted text-sm mb-4">Stricter qualification — elite pigeons only.</p>

            <div class="pr-form-group mb-4">
              <label class="pr-label">Qualification Rule</label>
              <select class="pr-select" formControlName="superAceQualification">
                <option [value]="SuperAceQualification.AllRacesRequired">Must enter ALL races in the programme</option>
                <option [value]="SuperAceQualification.MinimumRaceCount">Must enter at least N races</option>
                <option [value]="SuperAceQualification.MinimumRacePercentage">Must enter at least X% of races</option>
              </select>
            </div>

            @if (form.get('superAceQualification')?.value == SuperAceQualification.MinimumRaceCount) {
              <div class="pr-form-group">
                <label class="pr-label">Minimum race count</label>
                <input class="pr-input" type="number" formControlName="superAceMinRaceCount" min="1">
              </div>
            }

            @if (form.get('superAceQualification')?.value == SuperAceQualification.MinimumRacePercentage) {
              <div class="pr-form-group">
                <label class="pr-label">Minimum participation % (0–100)</label>
                <input class="pr-input" type="number" formControlName="superAceMinRacePercentage" min="0" max="100" step="5">
              </div>
            }
          </div>
        </div>
      </div>

      <!-- Submit bar -->
      <div class="submit-bar">
        @if (error()) { <div class="pr-alert pr-alert--error" style="flex:1">{{ error() }}</div> }
        <div class="flex gap-3 ml-auto">
          <a routerLink="/club/programmes" class="pr-btn pr-btn--ghost">Cancel</a>
          <button type="submit" class="pr-btn pr-btn--primary" [disabled]="saving() || form.invalid">
            @if (saving()) { <span class="pr-spinner" style="width:14px;height:14px"></span> }
            {{ isEdit() ? 'Save Changes' : 'Create Programme' }}
          </button>
        </div>
      </div>
    </form>
  `,
  styles: [`
    .form-cols { display:grid; grid-template-columns:1fr 1fr; gap:24px; }
    @media (max-width:900px) { .form-cols { grid-template-columns:1fr; } }

    .scoring-options { display:flex; flex-direction:column; gap:8px; }
    .scoring-option {
      display:flex; align-items:center; gap:12px;
      padding:12px; border-radius:var(--pr-radius); border:1px solid var(--pr-border);
      cursor:pointer; transition:all var(--t-fast);
    }
    .scoring-option:hover { border-color:var(--pr-primary); }
    .scoring-option--active { border-color:var(--pr-primary); background:rgba(30,144,255,0.06); }
    .scoring-option__icon { font-size:1.25rem; flex-shrink:0; }
    .scoring-option__label { font-weight:600; font-size:0.875rem; }
    .scoring-option__desc { font-size:0.75rem; color:var(--pr-text-muted); margin-top:2px; }

    .field-hint { font-size:0.75rem; color:var(--pr-text-muted); margin-top:4px; }

    .submit-bar {
      display:flex; align-items:center; gap:16px;
      background:var(--pr-surface); border:1px solid var(--pr-border);
      border-radius:var(--pr-radius); padding:16px 24px;
      position:sticky; bottom:16px; margin-top:24px;
      box-shadow:var(--shadow-lg);
    }
  `]
})
export class ProgrammeFormComponent implements OnInit {
  private fb      = inject(FormBuilder);
  private progApi = inject(ProgrammeApiService);
  private router  = inject(Router);
  private route   = inject(ActivatedRoute);
  private auth    = inject(AuthService);

  ScoringMethod = ScoringMethod;
  SuperAceQualification = SuperAceQualification;

  isEdit  = signal(false);
  saving  = signal(false);
  error   = signal<string | null>(null);

  scoringOptions = [
    { value: ScoringMethod.AverageSpeed,           icon: '📊', label: 'Average Speed',          desc: 'Average m/min across all races (standard)' },
    { value: ScoringMethod.PointsByRank,               icon: '🏅', label: 'Points by Rank',          desc: '1st earns N points, 2nd earns N-1, etc.' },
    { value: ScoringMethod.PointsBySpeedPercentage, icon: '📈', label: 'Speed % Points',          desc: "Score = pigeon's % of winner's speed per race" },
    { value: ScoringMethod.TotalSpeed,              icon: '⚡', label: 'Total Speed',              desc: 'Sum of all speeds across races' },
  ];

  form = this.fb.group({
    name:                    ['', Validators.required],
    description:             [''],
    year:                    [new Date().getFullYear(), Validators.required],
    startDate:               [''],
    endDate:                 [''],
    scoringMethod:           [ScoringMethod.AverageSpeed, Validators.required],
    pointsForFirst:          [10],
    maxPointPositions:       [0],
    bestLoftPigeonsPerRace:  [0],
    bestLoftMinRaces:        [1, [Validators.required, Validators.min(1)]],
    acePigeonMinRaces:       [3, [Validators.required, Validators.min(1)]],
    superAceQualification:   [SuperAceQualification.AllRacesRequired],
    superAceMinRaceCount:    [0],
    superAceMinRacePercentage:[100.0]
  });

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.isEdit.set(true);
      this.progApi.getProgramme(id).subscribe(p => this.form.patchValue(p as any));
    }
  }

  submit() {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.error.set(null);
    const v = this.form.value;
    const programmeId = this.route.snapshot.paramMap.get('id');

    const req$ = this.isEdit() && programmeId
      ? this.progApi.updateProgramme(programmeId, v as any)
      : this.progApi.createProgramme({ ...v, clubId: this.auth.clubId()! } as any);

    req$.subscribe({
      next: p => { this.saving.set(false); this.router.navigate(['/club/programmes', p.id]); },
      error: (e: any) => { this.error.set(e?.error?.message ?? 'Failed to save.'); this.saving.set(false); }
    });
  }
}
