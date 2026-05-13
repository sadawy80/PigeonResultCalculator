import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { TranslatePipe } from '../../core/i18n';

@Component({
  selector: 'app-admin-pigeons',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe, TranslatePipe],
  template: `
    <div class="pr-page-header">
      <h1 class="pr-page-header__title">{{ 'admin.pigeons.title' | translate }}</h1>
      <p class="pr-page-header__subtitle">{{ 'admin.pigeons.subtitle' | translate:{ count: (total() | number) ?? 0 } }}</p>
    </div>

    @if (error()) {
      <div class="pr-alert pr-alert--error mb-4">{{ error() }}</div>
    }

    <div class="pr-card mb-4">
      <div class="flex flex-wrap gap-4 items-end">
        <div class="pr-form-group" style="flex:2;min-width:180px">
          <label class="pr-label">{{ 'admin.pigeons.ringNameSearch' | translate }}</label>
          <input class="pr-input" [(ngModel)]="search" [placeholder]="'admin.pigeons.ringSearchPlaceholder' | translate" (keyup.enter)="loadPage(1)">
        </div>
        <div class="pr-form-group" style="flex:1;min-width:160px">
          <label class="pr-label">{{ 'admin.pigeons.fancierFilter' | translate }}</label>
          <input class="pr-input" [(ngModel)]="fancierSearch" [placeholder]="'admin.fanciers.fancierSearchPlaceholder' | translate" (keyup.enter)="loadPage(1)">
        </div>
        <div class="pr-form-group" style="flex:1;min-width:160px">
          <label class="pr-label">{{ 'admin.common.federation' | translate }}</label>
          <select class="pr-select" [(ngModel)]="selectedFederationId" (change)="onFederationChange()">
            <option value="">{{ 'admin.common.allFederations' | translate }}</option>
            @for (f of federations(); track f.id) {
              <option [value]="f.id">{{ f.name }}</option>
            }
          </select>
        </div>
        <div class="pr-form-group" style="flex:1;min-width:160px">
          <label class="pr-label">{{ 'admin.common.club' | translate }}</label>
          <select class="pr-select" [(ngModel)]="selectedClubId" (change)="loadPage(1)" [disabled]="!selectedFederationId">
            <option value="">{{ 'admin.common.allClubs' | translate }}</option>
            @for (c of clubs(); track c.id) {
              <option [value]="c.id">{{ c.name }}</option>
            }
          </select>
        </div>
        <button class="pr-btn pr-btn--primary pr-btn--field" (click)="loadPage(1)" [disabled]="loading()">{{ 'admin.common.search' | translate }}</button>
        <button class="pr-btn pr-btn--ghost pr-btn--field" (click)="reset()">{{ 'admin.common.reset' | translate }}</button>
      </div>
    </div>

    <div class="pr-card">
      @if (loading()) {
        <div class="text-center py-8 text-muted">{{ 'admin.common.loading' | translate }}</div>
      } @else {
        <div class="pr-table-wrapper">
          <table class="pr-table">
            <thead>
              <tr>
                <th>{{ 'admin.common.ringNumber' | translate }}</th>
                <th>{{ 'admin.common.name' | translate }}</th>
                <th>{{ 'admin.common.fancier' | translate }}</th>
                <th>{{ 'admin.pigeons.sex' | translate }}</th>
                <th>{{ 'admin.common.year' | translate }}</th>
                <th>{{ 'admin.pigeons.color' | translate }}</th>
                <th>{{ 'admin.pigeons.registered' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              @if (items().length === 0) {
                <tr><td colspan="7" class="text-center py-6 text-muted">{{ 'admin.pigeons.noPigeons' | translate }}</td></tr>
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
          <span class="text-muted text-sm">{{ 'admin.pigeons.pigeonsCount' | translate:{ n: total() } }} · {{ 'admin.common.page' | translate }} {{ page() }} {{ 'admin.common.of' | translate }} {{ totalPages() }}</span>
          <div class="flex gap-2 items-center">
            <select class="pr-select" style="width:auto" [(ngModel)]="pageSize" (ngModelChange)="onPageSizeChange()">
              <option [ngValue]="10">{{ 'admin.common.perPage' | translate:{ n: 10 } }}</option>
              <option [ngValue]="25">{{ 'admin.common.perPage' | translate:{ n: 25 } }}</option>
              <option [ngValue]="50">{{ 'admin.common.perPage' | translate:{ n: 50 } }}</option>
              <option [ngValue]="100">{{ 'admin.common.perPage' | translate:{ n: 100 } }}</option>
            </select>
            <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() === 1" (click)="loadPage(page() - 1)">{{ 'admin.common.prev' | translate }}</button>
            <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() === totalPages()" (click)="loadPage(page() + 1)">{{ 'admin.common.next' | translate }}</button>
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
