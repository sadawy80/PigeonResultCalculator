import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { PrintApiService } from '../../core/services/print-api.service';
import { TranslationService, TranslatePipe } from '../../core/i18n';

@Component({
  selector: 'app-admin-ace-results',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  template: `
    <div class="pr-page-header">
      <h1 class="pr-page-header__title">{{ 'admin.ace.title' | translate }}</h1>
      <p class="pr-page-header__subtitle">{{ 'admin.ace.subtitle' | translate }}</p>
    </div>

    @if (error()) {
      <div class="pr-alert pr-alert--error mb-4">{{ error() }}</div>
    }

    <div class="pr-card mb-4">
      <div class="flex flex-wrap gap-4 items-end">
        <div class="pr-form-group" style="flex:1;min-width:200px">
          <label class="pr-label">{{ 'admin.common.search' | translate }}</label>
          <input class="pr-input" [(ngModel)]="search" [placeholder]="'admin.ace.searchPlaceholder' | translate" (keyup.enter)="loadPage(1)">
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
                <th>{{ 'admin.common.rank' | translate }}</th>
                <th>{{ 'admin.common.fancier' | translate }}</th>
                <th>{{ 'admin.common.ringNumber' | translate }}</th>
                <th>{{ 'admin.common.pigeon' | translate }}</th>
                <th>{{ 'admin.common.programme' | translate }}</th>
                <th>{{ 'admin.common.year' | translate }}</th>
                <th>{{ 'admin.common.club' | translate }}</th>
                <th>{{ 'admin.common.score' | translate }}</th>
                <th>{{ 'admin.common.racesCount' | translate }}</th>
                <th>{{ 'admin.common.actions' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              @for (r of items(); track r.id) {
                <tr>
                  <td><span class="pr-badge pr-badge--success">{{ r.rank }}</span></td>
                  <td class="font-bold">{{ r.fancierName }}</td>
                  <td class="text-sm text-muted">{{ r.ringNumber }}</td>
                  <td class="text-sm">{{ r.pigeonName ?? '—' }}</td>
                  <td class="text-sm">{{ r.programmeName }}</td>
                  <td class="text-sm text-muted">{{ r.programmeYear }}</td>
                  <td class="text-sm text-muted">{{ r.clubName }}</td>
                  <td class="text-sm font-bold">{{ r.totalScore | number:'1.2-2' }}</td>
                  <td class="text-sm text-muted">{{ r.racesEntered }}</td>
                  <td>
                    <div class="flex gap-1 flex-wrap">
                      <button class="pr-btn pr-btn--ghost pr-btn--sm" (click)="viewResult(r)" [title]="'admin.ace.viewProgramme' | translate">👁 {{ 'admin.common.view' | translate }}</button>
                      <button class="pr-btn pr-btn--ghost pr-btn--sm"
                        [disabled]="pdfBusy() === r.programmeId" (click)="downloadPdf(r)" [title]="'admin.ace.downloadProgrammePdf' | translate">
                        {{ pdfBusy() === r.programmeId ? '…' : ('admin.common.downloadPdf' | translate) }}
                      </button>
                      <button class="pr-btn pr-btn--ghost pr-btn--sm" style="color:var(--pr-error,#dc2626)"
                        (click)="confirmDelete(r)" [title]="'admin.ace.deleteProgramme' | translate">🗑️ {{ 'admin.common.delete' | translate }}</button>
                    </div>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="10" class="text-center text-muted py-6">{{ 'admin.ace.noResults' | translate }}</td></tr>
              }
            </tbody>
          </table>
        </div>
        <div class="pagination-row">
          <span class="text-muted text-sm">{{ 'admin.ace.resultsCount' | translate:{ n: totalCount() } }} · {{ 'admin.common.page' | translate }} {{ page() }} {{ 'admin.common.of' | translate }} {{ totalPages() }}</span>
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
          <h3 class="pr-modal__title">{{ 'admin.ace.deleteProgramme' | translate }}</h3>
          <p class="pr-modal__subtitle" style="margin-top:8px">
            {{ 'admin.ace.deleteProgrammeBody' | translate:{ name: deleteTarget()!.programmeName, year: deleteTarget()!.programmeYear } }}
          </p>
          @if (deleteError()) {
            <div class="pr-alert pr-alert--error mt-3">{{ deleteError() }}</div>
          }
          <div class="flex gap-3 justify-end mt-6">
            <button class="pr-btn pr-btn--ghost" (click)="deleteTarget.set(null)">{{ 'admin.common.cancel' | translate }}</button>
            <button class="pr-btn pr-btn--primary" style="background:var(--pr-error,#dc2626);border-color:var(--pr-error,#dc2626)"
              [disabled]="deleting()" (click)="executeDelete()">
              {{ (deleting() ? 'admin.common.deleting' : 'admin.ace.deleteProgramme') | translate }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class AdminAceResultsComponent implements OnInit {
  private api    = inject(ApiService);
  private print  = inject(PrintApiService);
  private router = inject(Router);
  private i18n   = inject(TranslationService);

  loading    = signal(false);
  items      = signal<any[]>([]);
  totalCount = signal(0);
  page       = signal(1);
  pageSize = 10;
  error      = signal<string | null>(null);
  search     = '';

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
    if (this.search) params['search'] = this.search;

    this.api.adminGetAceResults(params).subscribe({
      next: r => {
        const data = r?.data ?? r;
        this.items.set(data?.items ?? []);
        this.totalCount.set(data?.totalCount ?? 0);
        this.page.set(p);
        this.loading.set(false);
      },
      error: () => { this.error.set(this.i18n.t('admin.ace.loadFailed')); this.loading.set(false); }
    });
  }

  reset() { this.search = ''; this.loadPage(1); }

  viewResult(r: any) {
    this.router.navigate(['/club/programmes', r.programmeId, 'ace-pigeon']);
  }

  downloadPdf(r: any) {
    if (this.pdfBusy()) return;
    this.pdfBusy.set(r.programmeId);
    this.error.set(null);
    this.print.renderAceResultsPdf({ programmeId: r.programmeId, designId: 'A1', language: this.i18n.locale() }).subscribe({
      next: blob => {
        const safe = (r.programmeName ?? 'programme').replace(/[^a-z0-9-]+/gi, '_');
        this.print.download(blob, `ace-${safe}-${r.programmeYear}.pdf`);
        this.pdfBusy.set(null);
      },
      error: () => {
        this.pdfBusy.set(null);
        this.error.set(this.i18n.t('admin.ace.renderPdfFail'));
      }
    });
  }

  confirmDelete(r: any) {
    this.deleteError.set(null);
    this.deleteTarget.set(r);
  }

  executeDelete() {
    const row = this.deleteTarget();
    if (!row) return;
    this.deleting.set(true);
    this.deleteError.set(null);
    this.api.adminDeleteProgramme(row.programmeId).subscribe({
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
}
