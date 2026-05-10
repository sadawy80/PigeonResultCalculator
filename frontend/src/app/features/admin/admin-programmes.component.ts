import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

const PROG_STATUSES: Record<number, string> = { 0: 'Draft', 1: 'Active', 2: 'Closed', 3: 'Archived' };

@Component({
  selector: 'app-admin-programmes',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  template: `
    <div class="pr-page-header">
      <h1 class="pr-page-header__title">Programmes</h1>
      <p class="pr-page-header__subtitle">{{ total() | number }} total programmes</p>
    </div>

    @if (error()) {
      <div class="pr-alert pr-alert--error mb-4">{{ error() }}</div>
    }

    <div class="pr-card mb-4">
      <div class="flex flex-wrap gap-4 items-end">
        <div class="pr-form-group" style="flex:1;min-width:200px">
          <label class="pr-label">programme / club name</label>
          <input class="pr-input" [(ngModel)]="search" placeholder="Programme or club name…" (keyup.enter)="loadPage(1)">
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
                <th>Programme</th><th>Club</th><th>Year</th><th>Status</th>
                <th>Ace</th><th>Super Ace</th><th>Best Loft</th>
                <th>Created</th><th style="text-align:right">Actions</th>
              </tr>
            </thead>
            <tbody>
              @if (items().length === 0) {
                <tr><td colspan="9" class="text-center py-6 text-muted">No programmes found.</td></tr>
              }
              @for (p of items(); track p.id) {
                <tr>
                  <td style="font-weight:600">{{ p.name }}</td>
                  <td class="text-muted text-sm">{{ p.clubName }}</td>
                  <td>{{ p.year }}</td>
                  <td><span class="pr-badge pr-badge--info" style="font-size:0.65rem">{{ statusLabel(p.status) }}</span></td>
                  <td>{{ p.aceCount }}</td>
                  <td>{{ p.superAceCount }}</td>
                  <td>{{ p.bestLoftCount }}</td>
                  <td class="text-muted text-sm">{{ p.createdAt | date:'dd/MM/yyyy' }}</td>
                  <td style="text-align:right">
                    <button class="pr-btn pr-btn--ghost pr-btn--sm" style="color:var(--pr-error,#dc2626)"
                      [disabled]="busyId() === p.id"
                      (click)="confirmDelete(p)">
                      {{ busyId() === p.id ? '…' : 'Delete' }}
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <div class="pagination-row">
          <span class="text-muted text-sm">{{ total() }} programmes · page {{ page() }} of {{ totalPages() }}</span>
          <div class="flex gap-2 items-center">
            <select class="pr-select" style="width:auto" [(ngModel)]="pageSize" (ngModelChange)="onPageSizeChange()">
              <option [ngValue]="10">10 / page</option>
              <option [ngValue]="25">25 / page</option>
              <option [ngValue]="50">50 / page</option>
              <option [ngValue]="100">100 / page</option>
            </select>
            <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() === 1" (click)="loadPage(page() - 1)">Prev</button>
            <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() === totalPages()" (click)="loadPage(page() + 1)">Next</button>
          </div>
        </div>
      }
    </div>

    @if (deleteTarget()) {
      <div class="pr-modal-backdrop" (click)="deleteTarget.set(null)">
        <div class="pr-modal pr-modal--sm" (click)="$event.stopPropagation()">
          <h3 class="pr-modal__title">Delete Programme</h3>
          <p class="pr-modal__subtitle" style="margin-top:8px">
            Are you sure you want to delete <strong>{{ deleteTarget()!.name }}</strong> ({{ deleteTarget()!.year }})?
          </p>
          <p style="margin-top:.5rem;font-size:.875rem;color:var(--pr-error,#dc2626);font-weight:600">
            This will permanently delete all related results including ace pigeon, super ace and best loft records.
          </p>
          @if (deleteError()) {
            <div class="pr-alert pr-alert--error mt-3">{{ deleteError() }}</div>
          }
          <div class="flex gap-3 justify-end mt-6">
            <button class="pr-btn pr-btn--ghost" (click)="deleteTarget.set(null)">Cancel</button>
            <button class="pr-btn pr-btn--primary" style="background:var(--pr-error,#dc2626);border-color:var(--pr-error,#dc2626)"
              [disabled]="deleting()" (click)="executeDelete()">
              {{ deleting() ? 'Deleting…' : 'Delete Programme' }}
            </button>
          </div>
        </div>
      </div>
    }
  `
})
export class AdminProgrammesComponent implements OnInit {
  private api = inject(ApiService);

  search   = '';
  loading  = signal(false);
  error    = signal<string | null>(null);
  items    = signal<any[]>([]);
  total    = signal(0);
  page     = signal(1);
  pageSize = 10;
  busyId   = signal<string | null>(null);

  deleteTarget = signal<any>(null);
  deleting     = signal(false);
  deleteError  = signal<string | null>(null);

  totalPages = () => Math.max(1, Math.ceil(this.total() / this.pageSize));
  onPageSizeChange() { this.page.set(1); this.loadPage(1); }

  ngOnInit() { this.loadPage(1); }

  loadPage(p: number) {
    this.page.set(p);
    this.loading.set(true);
    this.error.set(null);
    this.api.adminGetProgrammes({
      search: this.search || undefined,
      page: p, pageSize: this.pageSize
    }).subscribe({
      next: r => { this.items.set(r?.items ?? []); this.total.set(r?.totalCount ?? 0); this.loading.set(false); },
      error: () => { this.error.set('Failed to load programmes.'); this.loading.set(false); }
    });
  }

  reset() { this.search = ''; this.loadPage(1); }

  statusLabel(s: number) { return PROG_STATUSES[s] ?? `Status ${s}`; }

  confirmDelete(p: any) {
    this.deleteError.set(null);
    this.deleteTarget.set(p);
  }

  executeDelete() {
    const p = this.deleteTarget();
    if (!p) return;
    this.deleting.set(true);
    this.deleteError.set(null);
    this.busyId.set(p.id);
    this.api.adminDeleteProgramme(p.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.busyId.set(null);
        this.deleteTarget.set(null);
        this.items.update(list => list.filter(x => x.id !== p.id));
        this.total.update(n => n - 1);
      },
      error: (e) => {
        this.deleteError.set(e?.error?.message ?? 'Failed to delete programme.');
        this.deleting.set(false);
        this.busyId.set(null);
      }
    });
  }
}
