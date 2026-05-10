import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-admin-super-ace',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="pr-page-header">
      <h1 class="pr-page-header__title">Super Ace Results</h1>
      <p class="pr-page-header__subtitle">All super ace pigeon rankings across clubs and programmes</p>
    </div>

    @if (error()) {
      <div class="pr-alert pr-alert--error mb-4">{{ error() }}</div>
    }

    <div class="pr-card mb-4">
      <div class="flex flex-wrap gap-4 items-end">
        <div class="pr-form-group" style="flex:1;min-width:200px">
          <label class="pr-label">Search</label>
          <input class="pr-input" [(ngModel)]="search" placeholder="Fancier name or ring number…" (keyup.enter)="loadPage(1)">
        </div>
        <button class="pr-btn pr-btn--primary pr-btn--field" (click)="loadPage(1)" [disabled]="loading()">Search</button>
        <button class="pr-btn pr-btn--ghost pr-btn--field" (click)="reset()">Reset</button>
      </div>
    </div>

    <div class="pr-card">
      @if (loading()) {
        <div class="text-center py-8 text-muted">Loading…</div>
      } @else {
        <div class="pr-table-wrapper">
          <table class="pr-table">
            <thead>
              <tr>
                <th>Rank</th><th>Fancier</th><th>Ring #</th><th>Pigeon</th>
                <th>Programme</th><th>Year</th><th>Club</th><th>Score</th><th>Races</th>
              </tr>
            </thead>
            <tbody>
              @for (r of items(); track r.id) {
                <tr>
                  <td><span class="pr-badge pr-badge--info">{{ r.rank }}</span></td>
                  <td class="font-bold">{{ r.fancierName }}</td>
                  <td class="text-sm text-muted">{{ r.ringNumber }}</td>
                  <td class="text-sm">{{ r.pigeonName ?? '—' }}</td>
                  <td class="text-sm">{{ r.programmeName }}</td>
                  <td class="text-sm text-muted">{{ r.programmeYear }}</td>
                  <td class="text-sm text-muted">{{ r.clubName }}</td>
                  <td class="text-sm font-bold">{{ r.totalScore | number:'1.2-2' }}</td>
                  <td class="text-sm text-muted">{{ r.racesEntered }}</td>
                </tr>
              } @empty {
                <tr><td colspan="9" class="text-center text-muted py-6">No super ace results found</td></tr>
              }
            </tbody>
          </table>
        </div>
        <div class="pagination-row">
          <span class="text-muted text-sm">{{ totalCount() }} results · page {{ page() }} of {{ totalPages() }}</span>
          <div class="flex gap-2 items-center">
            <select class="pr-select" style="width:auto" [(ngModel)]="pageSize" (ngModelChange)="onPageSizeChange()">
              <option [ngValue]="10">10 / page</option>
              <option [ngValue]="25">25 / page</option>
              <option [ngValue]="50">50 / page</option>
              <option [ngValue]="100">100 / page</option>
            </select>
            <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() === 1" (click)="loadPage(page() - 1)">Prev</button>
            <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() >= totalPages()" (click)="loadPage(page() + 1)">Next</button>
          </div>
        </div>
      }
    </div>
  `,
})
export class AdminSuperAceComponent implements OnInit {
  private api = inject(ApiService);

  loading    = signal(false);
  items      = signal<any[]>([]);
  totalCount = signal(0);
  page       = signal(1);
  pageSize = 10;
  error      = signal<string | null>(null);
  search     = '';

  totalPages = () => Math.max(1, Math.ceil(this.totalCount() / this.pageSize));
  onPageSizeChange() { this.page.set(1); this.loadPage(1); }

  ngOnInit() { this.loadPage(1); }

  loadPage(p: number) {
    this.loading.set(true);
    this.error.set(null);
    const params: any = { page: p, pageSize: this.pageSize };
    if (this.search) params['search'] = this.search;

    this.api.adminGetSuperAceResults(params).subscribe({
      next: r => {
        const data = r?.data ?? r;
        this.items.set(data?.items ?? []);
        this.totalCount.set(data?.totalCount ?? 0);
        this.page.set(p);
        this.loading.set(false);
      },
      error: () => { this.error.set('Failed to load super ace results.'); this.loading.set(false); }
    });
  }

  reset() { this.search = ''; this.loadPage(1); }
}
