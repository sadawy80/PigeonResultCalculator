import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-admin-pigeons',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  template: `
    <div class="pr-page-header">
      <h1 class="pr-page-header__title">Pigeons</h1>
      <p class="pr-page-header__subtitle">{{ total() | number }} registered pigeons</p>
    </div>

    @if (error()) {
      <div class="pr-alert pr-alert--error mb-4">{{ error() }}</div>
    }

    <div class="pr-card mb-4">
      <div class="flex flex-wrap gap-4 items-end">
        <div class="pr-form-group" style="flex:2;min-width:180px">
          <label class="pr-label">ring number / name</label>
          <input class="pr-input" [(ngModel)]="search" placeholder="e.g. GB24A12345…" (keyup.enter)="loadPage(1)">
        </div>
        <div class="pr-form-group" style="flex:1;min-width:160px">
          <label class="pr-label">Fancier</label>
          <input class="pr-input" [(ngModel)]="fancierSearch" placeholder="Fancier name…" (keyup.enter)="loadPage(1)">
        </div>
        <div class="pr-form-group" style="flex:1;min-width:160px">
          <label class="pr-label">Federation</label>
          <select class="pr-select" [(ngModel)]="selectedFederationId" (change)="onFederationChange()">
            <option value="">All federations</option>
            @for (f of federations(); track f.id) {
              <option [value]="f.id">{{ f.name }}</option>
            }
          </select>
        </div>
        <div class="pr-form-group" style="flex:1;min-width:160px">
          <label class="pr-label">Club</label>
          <select class="pr-select" [(ngModel)]="selectedClubId" (change)="loadPage(1)" [disabled]="!selectedFederationId">
            <option value="">All clubs</option>
            @for (c of clubs(); track c.id) {
              <option [value]="c.id">{{ c.name }}</option>
            }
          </select>
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
                <th>Ring Number</th><th>Name</th><th>Fancier</th><th>Sex</th>
                <th>Year</th><th>Color</th><th>Registered</th>
              </tr>
            </thead>
            <tbody>
              @if (items().length === 0) {
                <tr><td colspan="7" class="text-center py-6 text-muted">No pigeons found.</td></tr>
              }
              @for (p of items(); track p.id) {
                <tr>
                  <td><span style="font-family:monospace;font-weight:600">{{ p.ringNumber }}</span></td>
                  <td>{{ p.name || '—' }}</td>
                  <td class="text-muted text-sm">{{ p.fancierName || '—' }}</td>
                  <td>{{ sexLabel(p.sex) }}</td>
                  <td>{{ p.yearOfBirth || '—' }}</td>
                  <td>{{ p.color || '—' }}</td>
                  <td>{{ p.createdAt | date:'dd/MM/yyyy' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <div class="pagination-row">
          <span class="text-muted text-sm">{{ total() }} pigeons · page {{ page() }} of {{ totalPages() }}</span>
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
  `
})
export class AdminPigeonsComponent implements OnInit {
  private api = inject(ApiService);

  search             = '';
  fancierSearch      = '';
  selectedFederationId = '';
  selectedClubId       = '';

  loading     = signal(false);
  error       = signal<string | null>(null);
  items       = signal<any[]>([]);
  total       = signal(0);
  page        = signal(1);
  pageSize    = 10;
  federations = signal<any[]>([]);
  clubs       = signal<any[]>([]);

  totalPages = () => Math.max(1, Math.ceil(this.total() / this.pageSize));
  onPageSizeChange() { this.page.set(1); this.loadPage(1); }

  ngOnInit() {
    this.api.getPublicFederations().subscribe({
      next: f => this.federations.set(f),
      error: () => {}
    });
    this.loadPage(1);
  }

  onFederationChange() {
    this.selectedClubId = '';
    this.clubs.set([]);
    if (this.selectedFederationId) {
      this.api.adminGetClubs({ FederationId: this.selectedFederationId, page: 1, pageSize: 200 }).subscribe({
        next: r => this.clubs.set(r?.items ?? []),
        error: () => {}
      });
    }
    this.loadPage(1);
  }

  loadPage(p: number) {
    this.page.set(p);
    this.loading.set(true);
    this.error.set(null);
    this.api.adminGetPigeons({
      search:        this.search || undefined,
      fancierSearch: this.fancierSearch || undefined,
      federationId:  this.selectedFederationId || undefined,
      clubId:        this.selectedClubId || undefined,
      page: p, pageSize: this.pageSize
    }).subscribe({
      next: r => { this.items.set(r?.items ?? []); this.total.set(r?.totalCount ?? 0); this.loading.set(false); },
      error: () => { this.error.set('Failed to load pigeons.'); this.loading.set(false); }
    });
  }

  reset() {
    this.search = '';
    this.fancierSearch = '';
    this.selectedFederationId = '';
    this.selectedClubId = '';
    this.clubs.set([]);
    this.loadPage(1);
  }

  sexLabel(sex: string | null | undefined): string {
    if (!sex) return 'Unknown';
    const s = sex.toUpperCase();
    if (s === 'M' || s === 'C') return 'Cock';
    if (s === 'F' || s === 'H') return 'Hen';
    return 'Unknown';
  }
}
