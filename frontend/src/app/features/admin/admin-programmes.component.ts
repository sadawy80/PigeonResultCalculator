import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { TranslationService, TranslatePipe } from '../../core/i18n';

const PROG_STATUSES: Record<number, string> = {
  0: 'admin.races.statusDraft',
  1: 'admin.common.active',
  2: 'admin.races.statusClosed',
  3: 'admin.common.inactive'
};

@Component({
  selector: 'app-admin-programmes',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe, TranslatePipe],
  template: `
    <div class="pr-page-header">
      <h1 class="pr-page-header__title">{{ 'admin.programmes.title' | translate }}</h1>
      <p class="pr-page-header__subtitle">{{ 'admin.programmes.programmesCount' | translate:{ n: total() } }}</p>
    </div>

    @if (error()) {
      <div class="pr-alert pr-alert--error mb-4">{{ error() }}</div>
    }

    <div class="pr-card mb-4">
      <div class="flex flex-wrap gap-4 items-end">
        <div class="pr-form-group" style="flex:1;min-width:200px">
          <label class="pr-label">{{ 'admin.common.search' | translate }}</label>
          <input class="pr-input" [(ngModel)]="search" [placeholder]="'admin.programmes.searchPlaceholder' | translate" (keyup.enter)="loadPage(1)">
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
                <th>{{ 'admin.common.programme' | translate }}</th>
                <th>{{ 'admin.common.club' | translate }}</th>
                <th>{{ 'admin.common.year' | translate }}</th>
                <th>{{ 'admin.common.status' | translate }}</th>
                <th>{{ 'admin.dashboard.aceResults' | translate }}</th>
                <th>{{ 'admin.dashboard.superAce' | translate }}</th>
                <th>{{ 'admin.dashboard.bestLoft' | translate }}</th>
                <th>{{ 'admin.common.createdAt' | translate }}</th>
                <th style="text-align:right">{{ 'admin.common.actions' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              @if (items().length === 0) {
                <tr><td colspan="9" class="text-center py-6 text-muted">{{ 'admin.programmes.noProgrammes' | translate }}</td></tr>
              }
              @for (p of items(); track p.id) {
                <tr>
                  <td style="font-weight:600">{{ p.name }}</td>
                  <td class="text-muted text-sm">{{ p.clubName }}</td>
                  <td>{{ p.year }}</td>
                  <td><span class="pr-badge pr-badge--info" style="font-size:0.65rem">{{ statusLabel(p.status) | translate }}</span></td>
                  <td>{{ p.aceCount }}</td>
                  <td>{{ p.superAceCount }}</td>
                  <td>{{ p.bestLoftCount }}</td>
                  <td class="text-muted text-sm">{{ p.createdAt | date:'dd/MM/yyyy' }}</td>
                  <td style="text-align:right">
                    <button class="pr-btn pr-btn--ghost pr-btn--sm" style="color:var(--pr-error,#dc2626)"
                      [disabled]="busyId() === p.id"
                      (click)="confirmDelete(p)">
                      {{ busyId() === p.id ? '…' : ('admin.common.delete' | translate) }}
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <div class="pagination-row">
          <span class="text-muted text-sm">{{ 'admin.programmes.programmesCount' | translate:{ n: total() } }} · {{ 'admin.common.page' | translate }} {{ page() }} {{ 'admin.common.of' | translate }} {{ totalPages() }}</span>
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

    @if (deleteTarget()) {
      <div class="pr-modal-backdrop" (click)="deleteTarget.set(null)">
        <div class="pr-modal pr-modal--sm" (click)="$event.stopPropagation()">
          <h3 class="pr-modal__title">{{ 'admin.programmes.deleteProgrammeTitle' | translate }}</h3>
          <p class="pr-modal__subtitle" style="margin-top:8px">
            {{ 'admin.programmes.deleteProgrammeBody' | translate:{ name: (deleteTarget()!.name + ' (' + deleteTarget()!.year + ')') } }}
          </p>
          @if (deleteError()) {
            <div class="pr-alert pr-alert--error mt-3">{{ deleteError() }}</div>
          }
          <div class="flex gap-3 justify-end mt-6">
            <button class="pr-btn pr-btn--ghost" (click)="deleteTarget.set(null)">{{ 'admin.common.cancel' | translate }}</button>
            <button class="pr-btn pr-btn--primary" style="background:var(--pr-error,#dc2626);border-color:var(--pr-error,#dc2626)"
              [disabled]="deleting()" (click)="executeDelete()">
              {{ (deleting() ? 'admin.common.deleting' : 'admin.programmes.deleteProgrammeTitle') | translate }}
            </button>
          </div>
        </div>
      </div>
    }
  `
})
export class AdminProgrammesComponent implements OnInit {
  private api  = inject(ApiService);
  private i18n = inject(TranslationService);

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
      error: () => { this.error.set(this.i18n.t('admin.ace.loadFailed')); this.loading.set(false); }
    });
  }

  reset() { this.search = ''; this.loadPage(1); }

  statusLabel(s: number) { return PROG_STATUSES[s] ?? 'admin.common.status'; }

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
        this.deleteError.set(e?.error?.message ?? this.i18n.t('admin.ace.deleteFail'));
        this.deleting.set(false);
        this.busyId.set(null);
      }
    });
  }
}
