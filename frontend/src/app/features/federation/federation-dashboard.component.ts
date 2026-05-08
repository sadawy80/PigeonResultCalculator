import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/services';
import { FederationResult, CountryResultStatus } from '../../core/models';

// ── Country Dashboard ─────────────────────────────────────────────────────────

@Component({
  selector: 'app-federation-dashboard',
  standalone: true,
  imports: [RouterLink, DatePipe],
  template: `
    <div class="pr-page-header flex justify-between items-center">
      <div>
        <h1 class="pr-page-header__title">Country Dashboard</h1>
        <p class="pr-page-header__subtitle">Monitor all clubs and generate national results</p>
      </div>
      <a routerLink="/country/results" class="pr-btn pr-btn--primary">+ New National Result</a>
    </div>

    <div class="pr-grid-4 mb-8">
      @for (s of stats(); track s.label) {
        <div class="pr-card pr-card--flat pr-stat">
          <div class="pr-stat__value">{{ s.value }}</div>
          <div class="pr-stat__label">{{ s.label }}</div>
        </div>
      }
    </div>

    <div class="pr-card">
      <h3 style="margin-bottom:24px">Recent National Results</h3>
      @if (results().length === 0) {
        <div class="pr-empty">
          <div class="pr-empty__icon">🏆</div>
          <div class="pr-empty__title">No national results yet</div>
          <p class="pr-empty__desc">Aggregate club results to publish national standings.</p>
        </div>
      } @else {
        <div class="pr-table-wrapper">
          <table class="pr-table">
            <thead><tr><th>Name</th><th>Status</th><th>Clubs</th><th>Entries</th><th>Published</th><th></th></tr></thead>
            <tbody>
              @for (r of results(); track r.id) {
                <tr>
                  <td class="font-bold">{{ r.name }}</td>
                  <td><span [class]="r.status === CountryResultStatus.Published ? 'pr-badge pr-badge--success' : 'pr-badge pr-badge--muted'">
                    {{ r.status === CountryResultStatus.Published ? 'Published' : 'Draft' }}
                  </span></td>
                  <td>{{ r.totalClubsCount }}</td>
                  <td>{{ r.totalEntriesCount }}</td>
                  <td class="text-muted text-sm">{{ r.publishedAt | date:'dd MMM yyyy' }}</td>
                  <td><button class="pr-btn pr-btn--ghost pr-btn--sm">View</button></td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `
})
export class FederationDashboardComponent implements OnInit {
  private api  = inject(ApiService);
  private auth = inject(AuthService);
  CountryResultStatus = CountryResultStatus;
  results = signal<FederationResult[]>([]);
  stats = signal([
    { label: 'Total Clubs',     value: '—' as string | number },
    { label: 'Published Results', value: '—' as string | number },
    { label: 'Total Entries',   value: '—' as string | number },
    { label: 'This Season',     value: new Date().getFullYear() as string | number },
  ]);

  ngOnInit() {
    const FederationId = this.auth.FederationId();
    if (!FederationId) return;
    this.api.getFederationResults(FederationId, 1, 5).subscribe(p => {
      this.results.set(p.items as FederationResult[]);
      const published = (p.items as FederationResult[]).filter(r => r.status === CountryResultStatus.Published);
      const totalEntries = (p.items as FederationResult[]).reduce((s, r) => s + (r.totalEntriesCount ?? 0), 0);
      this.stats.update(arr => [
        arr[0],
        { label: 'Published Results', value: p.totalCount },
        { label: 'Total Entries',     value: totalEntries },
        arr[3],
      ]);
    });
  }
}

// ── federation results Component ─────────────────────────────────────────────────

@Component({
  selector: 'app-federation-results',
  standalone: true,
  imports: [DatePipe, NgClass, FormsModule],
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
                <span [class]="r.status === CountryResultStatus.Published ? 'pr-badge pr-badge--success' : 'pr-badge pr-badge--muted'">
                  {{ r.status === CountryResultStatus.Published ? 'Published' : 'Draft' }}
                </span>
                @if (r.status !== CountryResultStatus.Published) {
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
  CountryResultStatus = CountryResultStatus;
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
