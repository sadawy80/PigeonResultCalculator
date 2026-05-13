import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { PrintApiService } from '../../core/services/print-api.service';
import { TranslationService, TranslatePipe } from '../../core/i18n';

const RACE_STATUSES: Record<number, string> = {
  1: 'admin.races.statusDraft',
  2: 'admin.races.statusOpen',
  3: 'admin.races.statusClosed',
  4: 'admin.races.statusPublished',
  5: 'admin.races.statusCancelled'
};

@Component({
  selector: 'app-admin-race-results',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe, TranslatePipe],
  template: `
    <div class="pr-page-header">
      <h1 class="pr-page-header__title">{{ 'admin.races.title' | translate }}</h1>
      <p class="pr-page-header__subtitle">{{ 'admin.races.subtitle' | translate }}</p>
    </div>

    @if (error()) {
      <div class="pr-alert pr-alert--error mb-4">{{ error() }}</div>
    }

    <div class="pr-card mb-4">
      <div class="flex flex-wrap gap-4 items-end">
        <div class="pr-form-group" style="flex:1;min-width:200px">
          <label class="pr-label">{{ 'admin.common.search' | translate }}</label>
          <input class="pr-input" [(ngModel)]="search" [placeholder]="'admin.races.searchPlaceholder' | translate" (keyup.enter)="loadPage(1)">
        </div>
        <div class="pr-form-group" style="min-width:140px">
          <label class="pr-label">{{ 'admin.common.status' | translate }}</label>
          <select class="pr-select" [(ngModel)]="statusFilter">
            <option [value]="null">{{ 'admin.common.allStatuses' | translate }}</option>
            <option [value]="1">{{ 'admin.races.statusDraft' | translate }}</option>
            <option [value]="2">{{ 'admin.races.statusOpen' | translate }}</option>
            <option [value]="3">{{ 'admin.races.statusClosed' | translate }}</option>
            <option [value]="4">{{ 'admin.races.statusPublished' | translate }}</option>
            <option [value]="5">{{ 'admin.races.statusCancelled' | translate }}</option>
          </select>
        </div>
        <div class="pr-form-group" style="min-width:180px">
          <label class="pr-label">{{ 'admin.common.filter' | translate }}</label>
          <select class="pr-select" [(ngModel)]="periodPreset" (ngModelChange)="onPeriodChange($event)">
            <option value="">{{ 'admin.common.allTime' | translate }}</option>
            <option value="today">{{ 'admin.common.today' | translate }}</option>
            <option value="week">{{ 'admin.common.thisWeek' | translate }}</option>
            <option value="month">{{ 'admin.common.thisMonth' | translate }}</option>
            <option value="year">{{ 'admin.common.thisYear' | translate }}</option>
            <option value="custom">{{ 'admin.common.customRange' | translate }}</option>
          </select>
        </div>
        @if (periodPreset === 'custom') {
          <div class="pr-form-group" style="min-width:150px">
            <label class="pr-label">{{ 'admin.common.from' | translate }}</label>
            <input class="pr-input" type="date" [(ngModel)]="dateFrom">
          </div>
          <div class="pr-form-group" style="min-width:150px">
            <label class="pr-label">{{ 'admin.common.to' | translate }}</label>
            <input class="pr-input" type="date" [(ngModel)]="dateTo">
          </div>
        }
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
                <th>{{ 'admin.common.race' | translate }}</th>
                <th>{{ 'admin.common.club' | translate }}</th>
                <th>{{ 'admin.common.status' | translate }}</th>
                <th>{{ 'admin.common.scheduledAt' | translate }}</th>
                <th>{{ 'admin.common.publishedAt' | translate }}</th>
                <th>{{ 'admin.common.results' | translate }}</th>
                <th>{{ 'admin.common.actions' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              @for (r of items(); track r.id) {
                <tr>
                  <td class="font-bold">{{ r.name }}</td>
                  <td class="text-muted text-sm">{{ r.clubName }}</td>
                  <td><span [class]="statusBadgeClass(r.status)">{{ statusLabel(r.status) | translate }}</span></td>
                  <td class="text-muted text-sm">{{ r.scheduledAt | date:'dd MMM yyyy' }}</td>
                  <td class="text-muted text-sm">{{ r.publishedAt | date:'dd MMM yyyy' }}</td>
                  <td class="text-sm">{{ r.resultCount }}</td>
                  <td>
                    <div class="flex gap-1 flex-wrap">
                      <button class="pr-btn pr-btn--ghost pr-btn--sm" (click)="viewResult(r)" [title]="'admin.races.viewRace' | translate">👁 {{ 'admin.common.view' | translate }}</button>
                      <button class="pr-btn pr-btn--ghost pr-btn--sm"
                        [disabled]="pdfBusy() === r.id" (click)="downloadPdf(r)" [title]="'admin.races.downloadRacePdf' | translate">
                        {{ pdfBusy() === r.id ? '…' : ('admin.common.downloadPdf' | translate) }}
                      </button>
                      <button class="pr-btn pr-btn--ghost pr-btn--sm" style="color:var(--pr-error,#dc2626)"
                        (click)="confirmDelete(r)">🗑️ {{ 'admin.common.delete' | translate }}</button>
                    </div>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="7" class="text-center text-muted py-6">{{ 'admin.races.noRaces' | translate }}</td></tr>
              }
            </tbody>
          </table>
        </div>
        <div class="pagination-row">
          <span class="text-muted text-sm">{{ 'admin.races.racesCount' | translate:{ n: totalCount() } }} · {{ 'admin.common.page' | translate }} {{ page() }} {{ 'admin.common.of' | translate }} {{ totalPages() }}</span>
          <div class="flex gap-2 items-center">
            <select class="pr-select" style="width:auto" [(ngModel)]="pageSize" (ngModelChange)="onPageSizeChange()">
              <option [ngValue]="10">{{ 'admin.common.perPage' | translate:{ n: 10 } }}</option>
              <option [ngValue]="25">{{ 'admin.common.perPage' | translate:{ n: 25 } }}</option>
              <option [ngValue]="50">{{ 'admin.common.perPage' | translate:{ n: 50 } }}</option>
              <option [ngValue]="100">{{ 'admin.common.perPage' | translate:{ n: 100 } }}</option>
            </select>
            <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() === 1" (click)="loadPage(page() - 1)">{{ 'admin.common.prev' | translate }}</button>
            <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() >= totalPages()" (click)="loadPage(page() + 1)">{{ 'admin.common.next' | translate }}</button>
          </div>
        </div>
      }
    </div>

    @if (deleteTarget()) {
      <div class="pr-modal-backdrop" (click)="deleteTarget.set(null)">
        <div class="pr-modal pr-modal--sm" (click)="$event.stopPropagation()">
          <h3 class="pr-modal__title">{{ 'admin.races.deleteRaceTitle' | translate }}</h3>
          <p class="pr-modal__subtitle" style="margin-top:8px">
            {{ 'admin.races.deleteRaceBody' | translate:{ name: deleteTarget()!.name } }}
          </p>
          @if (deleteError()) {
            <div class="pr-alert pr-alert--error mt-3">{{ deleteError() }}</div>
          }
          <div class="flex gap-3 justify-end mt-6">
            <button class="pr-btn pr-btn--ghost" (click)="deleteTarget.set(null)">{{ 'admin.common.cancel' | translate }}</button>
            <button class="pr-btn pr-btn--primary" style="background:var(--pr-error,#dc2626);border-color:var(--pr-error,#dc2626)"
              [disabled]="deleting()" (click)="executeDelete()">
              {{ (deleting() ? 'admin.common.deleting' : 'admin.races.deleteRace') | translate }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class AdminRaceResultsComponent implements OnInit {
  private api    = inject(ApiService);
  private print  = inject(PrintApiService);
  private router = inject(Router);
  private i18n   = inject(TranslationService);

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

  pdfBusy = signal<string | null>(null);

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
      error: () => { this.error.set(this.i18n.t('admin.races.loadFailed')); this.loading.set(false); }
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
        this.deleteError.set(this.i18n.t('admin.ace.deleteFail'));
        this.deleting.set(false);
      }
    });
  }

  viewResult(race: any) {
    // Race detail lives inside the club shell; SuperAdmin satisfies the route guard.
    this.router.navigate(['/club/races', race.id]);
  }

  downloadPdf(race: any) {
    if (this.pdfBusy()) return;
    this.pdfBusy.set(race.id);
    this.error.set(null);
    this.print.renderRaceResultsPdf({ raceId: race.id, designId: 'T1', language: this.i18n.locale() }).subscribe({
      next: blob => {
        const safe = (race.name ?? 'race').replace(/[^a-z0-9-]+/gi, '_');
        this.print.download(blob, `race-${safe}.pdf`);
        this.pdfBusy.set(null);
      },
      error: () => {
        this.pdfBusy.set(null);
        this.error.set(this.i18n.t('admin.races.renderPdfFail'));
      }
    });
  }

  statusLabel(s: number) { return RACE_STATUSES[s] ?? 'admin.races.statusDraft'; }

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
