import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

const RACE_STATUSES: Record<number, string> = { 1: 'Draft', 2: 'Open', 3: 'Closed', 4: 'Published', 5: 'Cancelled' };

@Component({
  selector: 'app-admin-race-results',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  template: `
    <div class="pr-page-header">
      <h1 class="pr-page-header__title">Race Results</h1>
      <p class="pr-page-header__subtitle">View and manage all club races</p>
    </div>

    @if (error()) {
      <div class="pr-alert pr-alert--error mb-4">{{ error() }}</div>
    }

    <div class="pr-card mb-4">
      <div class="flex flex-wrap gap-4 items-end">
        <div class="pr-form-group" style="flex:1;min-width:200px">
          <label class="pr-label">Search</label>
          <input class="pr-input" [(ngModel)]="search" placeholder="Race or club name…" (keyup.enter)="loadPage(1)">
        </div>
        <div class="pr-form-group" style="min-width:140px">
          <label class="pr-label">Status</label>
          <select class="pr-select" [(ngModel)]="statusFilter">
            <option [value]="null">All statuses</option>
            <option [value]="1">Draft</option>
            <option [value]="2">Open</option>
            <option [value]="3">Closed</option>
            <option [value]="4">Published</option>
            <option [value]="5">Cancelled</option>
          </select>
        </div>
        <div class="pr-form-group" style="min-width:180px">
          <label class="pr-label">Period</label>
          <select class="pr-select" [(ngModel)]="periodPreset" (ngModelChange)="onPeriodChange($event)">
            <option value="">All time</option>
            <option value="today">Today</option>
            <option value="week">This week</option>
            <option value="month">This month</option>
            <option value="year">This year</option>
            <option value="custom">Custom range</option>
          </select>
        </div>
        @if (periodPreset === 'custom') {
          <div class="pr-form-group" style="min-width:150px">
            <label class="pr-label">From</label>
            <input class="pr-input" type="date" [(ngModel)]="dateFrom">
          </div>
          <div class="pr-form-group" style="min-width:150px">
            <label class="pr-label">To</label>
            <input class="pr-input" type="date" [(ngModel)]="dateTo">
          </div>
        }
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
                <th>Race</th><th>Club</th><th>Status</th>
                <th>Scheduled</th><th>Published</th><th>Results</th><th>Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (r of items(); track r.id) {
                <tr>
                  <td class="font-bold">{{ r.name }}</td>
                  <td class="text-muted text-sm">{{ r.clubName }}</td>
                  <td><span [class]="statusBadgeClass(r.status)">{{ statusLabel(r.status) }}</span></td>
                  <td class="text-muted text-sm">{{ r.scheduledAt | date:'dd MMM yyyy' }}</td>
                  <td class="text-muted text-sm">{{ r.publishedAt | date:'dd MMM yyyy' }}</td>
                  <td class="text-sm">{{ r.resultCount }}</td>
                  <td>
                    <button class="pr-btn pr-btn--ghost pr-btn--sm" style="color:var(--pr-error,#dc2626)"
                      (click)="confirmDelete(r)">🗑️ Delete</button>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="7" class="text-center text-muted py-6">No races found</td></tr>
              }
            </tbody>
          </table>
        </div>
        <div class="pagination-row">
          <span class="text-muted text-sm">{{ totalCount() }} races · page {{ page() }} of {{ totalPages() }}</span>
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

    @if (deleteTarget()) {
      <div class="pr-modal-backdrop" (click)="deleteTarget.set(null)">
        <div class="pr-modal pr-modal--sm" (click)="$event.stopPropagation()">
          <h3 class="pr-modal__title">Delete Race</h3>
          <p class="pr-modal__subtitle" style="margin-top:8px">
            Are you sure you want to delete <strong>{{ deleteTarget()!.name }}</strong>?
            All results for this race will also be removed and the club managers will be notified.
          </p>
          @if (deleteError()) {
            <div class="pr-alert pr-alert--error mt-3">{{ deleteError() }}</div>
          }
          <div class="flex gap-3 justify-end mt-6">
            <button class="pr-btn pr-btn--ghost" (click)="deleteTarget.set(null)">Cancel</button>
            <button class="pr-btn pr-btn--primary" style="background:var(--pr-error,#dc2626);border-color:var(--pr-error,#dc2626)"
              [disabled]="deleting()" (click)="executeDelete()">
              {{ deleting() ? 'Deleting…' : 'Delete Race' }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class AdminRaceResultsComponent implements OnInit {
  private api = inject(ApiService);

  loading   = signal(false);
  items     = signal<any[]>([]);
  totalCount= signal(0);
  page      = signal(1);
  pageSize = 10;
  error     = signal<string | null>(null);

  search        = '';
  statusFilter: number | null = null;
  periodPreset  = '';
  dateFrom      = '';
  dateTo        = '';

  deleteTarget = signal<any>(null);
  deleting     = signal(false);
  deleteError  = signal<string | null>(null);

  totalPages = () => Math.max(1, Math.ceil(this.totalCount() / this.pageSize));
  onPageSizeChange() { this.page.set(1); this.loadPage(1); }

  ngOnInit() { this.loadPage(1); }

  loadPage(p: number) {
    this.loading.set(true);
    this.error.set(null);
    const params: any = { page: p, pageSize: this.pageSize };
    if (this.search)       params['search']   = this.search;
    if (this.statusFilter) params['status']   = this.statusFilter;
    if (this.dateFrom)     params['dateFrom'] = this.dateFrom;
    if (this.dateTo)       params['dateTo']   = this.dateTo;

    this.api.adminGetRaces(params).subscribe({
      next: r => {
        const data = r?.data ?? r;
        this.items.set(data?.items ?? []);
        this.totalCount.set(data?.totalCount ?? 0);
        this.page.set(p);
        this.loading.set(false);
      },
      error: () => { this.error.set('Failed to load races.'); this.loading.set(false); }
    });
  }

  onPeriodChange(preset: string) {
    const today = new Date();
    const fmt = (d: Date) => d.toISOString().slice(0, 10);
    if (preset === 'today') {
      this.dateFrom = this.dateTo = fmt(today);
    } else if (preset === 'week') {
      const mon = new Date(today); mon.setDate(today.getDate() - today.getDay() + 1);
      this.dateFrom = fmt(mon); this.dateTo = fmt(today);
    } else if (preset === 'month') {
      this.dateFrom = fmt(new Date(today.getFullYear(), today.getMonth(), 1));
      this.dateTo   = fmt(today);
    } else if (preset === 'year') {
      this.dateFrom = `${today.getFullYear()}-01-01`;
      this.dateTo   = fmt(today);
    } else if (preset === '') {
      this.dateFrom = this.dateTo = '';
    }
  }

  reset() {
    this.search = '';
    this.statusFilter = null;
    this.periodPreset = '';
    this.dateFrom = '';
    this.dateTo = '';
    this.loadPage(1);
  }

  confirmDelete(race: any) {
    this.deleteError.set(null);
    this.deleteTarget.set(race);
  }

  executeDelete() {
    const race = this.deleteTarget();
    if (!race) return;
    this.deleting.set(true);
    this.deleteError.set(null);
    this.api.adminDeleteRace(race.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.deleteTarget.set(null);
        this.loadPage(this.page());
      },
      error: () => {
        this.deleteError.set('Failed to delete race. Please try again.');
        this.deleting.set(false);
      }
    });
  }

  statusLabel(s: number) { return RACE_STATUSES[s] ?? 'Unknown'; }

  statusBadgeClass(s: number) {
    const map: Record<number, string> = {
      1: 'pr-badge',
      2: 'pr-badge pr-badge--info',
      3: 'pr-badge',
      4: 'pr-badge pr-badge--success',
      5: 'pr-badge pr-badge--error',
    };
    return map[s] ?? 'pr-badge';
  }
}
