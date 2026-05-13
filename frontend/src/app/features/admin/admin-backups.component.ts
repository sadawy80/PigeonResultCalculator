import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../environments/environment';
import { ToasterService } from '../../core/services/toaster.service';
import { ModalService } from '../../core/services/modal.service';
import { TranslatePipe, TranslationService } from '../../core/i18n';

interface BackupEntry {
  id:               string;
  databaseName:     string;
  objectKey:        string;
  sizeBytes:        number;
  createdAt:        string;
  completedAt:      string | null;
  status:           number;  // 1=InProgress, 2=Completed, 3=Failed
  errorMessage:     string | null;
  uploadedToMinIO:  boolean;
  uploadedToPCloud: boolean;
  triggeredBy:      string;
}

/**
 * Admin Database Backups page. Layout adopted from the reference design:
 *  - Search-by-filename + date filter at the top
 *  - Prominent "Backup Now" CTA
 *  - List of backups with per-row Browse / Restore / Delete actions
 *  - Browse opens a modal with a table picker, a search box, and a list of
 *    records each with a "Restore this record" button
 *
 * All chrome is built from the site's --pr-* tokens so it stays in theme.
 */
@Component({
  selector: 'app-admin-backups',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe, TranslatePipe],
  template: `
    <header class="bk-header">
      <h1 class="bk-header__title">{{ 'admin.backups.title' | translate }}</h1>
      <p class="bk-header__subtitle">{{ 'admin.backups.subtitle' | translate }}</p>
    </header>

    <div class="bk-toolbar">
      <label class="bk-search">
        <span class="bk-search__icon" aria-hidden="true">🔍</span>
        <input class="pr-input" type="search" [placeholder]="'admin.backups.searchFilename' | translate" [(ngModel)]="search" (ngModelChange)="onFilterChange()" />
      </label>
      <label class="bk-date">
        <input class="pr-input" type="date" [(ngModel)]="filterDate" (ngModelChange)="onFilterChange()" />
        <span class="bk-date__hint">{{ 'admin.backups.filterByDate' | translate }}</span>
      </label>
    </div>

    <button
      type="button"
      class="bk-cta"
      [disabled]="triggering()"
      (click)="triggerNow()">
      {{ (triggering() ? 'admin.backups.starting' : 'admin.backups.backupNow') | translate }}
    </button>

    @if (error()) { <div class="pr-alert pr-alert--error mt-4">{{ error() }}</div> }

    <section class="bk-card mt-6">
      <div class="bk-table">
        <div class="bk-table__head" role="row">
          <span>{{ 'admin.backups.fileHeader' | translate }}</span>
          <span>{{ 'admin.backups.dateHeader' | translate }}</span>
          <span>{{ 'admin.backups.sizeHeader' | translate }}</span>
          <span>{{ 'admin.backups.storedHeader' | translate }}</span>
          <span class="bk-actions-col">{{ 'admin.common.actions' | translate }}</span>
        </div>

        @if (loading()) {
          <div class="bk-empty"><div class="pr-spinner" style="margin:0 auto"></div></div>
        } @else if (visible().length === 0) {
          <div class="bk-empty">
            <div class="bk-empty__icon">💾</div>
            <div>{{ 'admin.backups.noBackups' | translate }}</div>
          </div>
        } @else {
          @for (b of visible(); track b.id) {
            <div class="bk-row" role="row">
              <code class="bk-row__file">{{ filename(b) }}</code>
              <div class="bk-row__date">{{ b.createdAt | date: 'medium' }}</div>
              <div class="bk-row__size">{{ formatSize(b.sizeBytes) }}</div>
              <div class="bk-row__store">
                @if (b.uploadedToMinIO) { <span class="bk-pill">MinIO</span> }
                @if (b.uploadedToPCloud) { <span class="bk-pill bk-pill--alt">pCloud</span> }
              </div>
              <div class="bk-row__actions">
                <button class="icon-btn"            [title]="'admin.backups.browse'  | translate" [attr.aria-label]="'admin.backups.browse'  | translate" (click)="openBrowse(b)">🔍</button>
                <button class="icon-btn icon-btn--success" [title]="'admin.backups.restore' | translate" [attr.aria-label]="'admin.backups.restore' | translate" (click)="confirmRestore(b)">⟲</button>
                <button class="icon-btn icon-btn--danger"  [title]="'admin.common.delete'   | translate" [attr.aria-label]="'admin.common.delete'   | translate" (click)="confirmDelete(b)">🗑</button>
              </div>
            </div>
          }
        }
      </div>

    </section>

    @if (filteredCount() > 0) {
      <div class="pagination-row">
        <span class="text-muted text-sm">{{ 'admin.backups.backupsCount' | translate:{ n: filteredCount() } }} · {{ 'admin.common.page' | translate }} {{ page() }} {{ 'admin.common.of' | translate }} {{ totalPages() }}</span>
        <div class="flex gap-2 items-center">
          <select class="pr-select" style="width:auto" [(ngModel)]="pageSize" (ngModelChange)="onPageSizeChange()">
            <option [ngValue]="10">{{ 'admin.common.perPage' | translate:{ n: 10 } }}</option>
            <option [ngValue]="25">{{ 'admin.common.perPage' | translate:{ n: 25 } }}</option>
            <option [ngValue]="50">{{ 'admin.common.perPage' | translate:{ n: 50 } }}</option>
            <option [ngValue]="100">{{ 'admin.common.perPage' | translate:{ n: 100 } }}</option>
          </select>
          <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() === 1"            (click)="goPage(page() - 1)">{{ 'admin.common.prev' | translate }}</button>
          <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() === totalPages()" (click)="goPage(page() + 1)">{{ 'admin.common.next' | translate }}</button>
        </div>
      </div>
    }

    <!-- ── Browse modal ─────────────────────────────────────────────────── -->
    @if (browseTarget(); as bt) {
      <div class="modal-backdrop" (click)="closeBrowse()">
        <div class="modal-card modal-card--wide" (click)="$event.stopPropagation()">
          <header class="modal-card__head">
            <h2>{{ 'admin.backups.browseTitle' | translate:{ name: filename(bt) } }}</h2>
            <button class="modal-card__close" [attr.aria-label]="'admin.common.cancel' | translate" (click)="closeBrowse()">×</button>
          </header>

          <div class="browse-controls">
            <select class="pr-input" [(ngModel)]="browseTable" (ngModelChange)="runBrowse()">
              @for (t of browseTables(); track t) {
                <option [value]="t">{{ t }}</option>
              }
            </select>
            <input
              class="pr-input"
              type="search"
              [placeholder]="'admin.backups.searchRecord' | translate"
              [(ngModel)]="browseSearch"
              (keyup.enter)="runBrowse()" />
            <button class="pr-btn pr-btn--primary" (click)="runBrowse()" [disabled]="browseLoading()">
              {{ (browseLoading() ? 'admin.common.loading' : 'admin.common.search') | translate }}
            </button>
          </div>

          <p class="browse-count">{{ 'admin.backups.recordsFound' | translate:{ n: browseResults().length } }}</p>

          @if (browseNotice()) {
            <div class="pr-alert pr-alert--info mb-4">{{ browseNotice() }}</div>
          }

          <div class="browse-list">
            @for (r of browseResults(); track r.id) {
              <div class="browse-row">
                <code class="browse-row__table">{{ r.table }}</code>
                <code class="browse-row__json">{{ r.preview }}</code>
                <button class="pr-btn pr-btn--outline pr-btn--sm browse-row__btn"
                        (click)="confirmRestoreRecord(bt, r)">
                  {{ 'admin.backups.restoreThisRecord' | translate }}
                </button>
              </div>
            }
            @if (browseResults().length === 0 && !browseLoading() && !browseNotice()) {
              <div class="bk-empty">{{ 'admin.backups.noMatchingRecords' | translate }}</div>
            }
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    :host { display: block; }

    /* Header */
    .bk-header__title    { margin: 0; font-size: 1.65rem; color: var(--pr-primary); }
    .bk-header__subtitle { margin: .35rem 0 1.5rem; color: var(--pr-text-muted); }

    /* Toolbar */
    .bk-toolbar {
      display: grid; grid-template-columns: 1fr 280px;
      gap: 1rem; margin-bottom: 1rem;
    }
    @media (max-width: 720px) { .bk-toolbar { grid-template-columns: 1fr; } }

    .bk-search, .bk-date {
      position: relative; display: flex; align-items: center;
    }
    .bk-search .pr-input, .bk-date .pr-input {
      width: 100%; height: 52px; padding-inline-start: 2.6rem;
      background: var(--pr-surface); border: 1px solid var(--pr-border);
      border-radius: 10px; font-size: 1rem;
    }
    .bk-search__icon {
      position: absolute; inset-inline-start: 1rem; color: var(--pr-text-muted);
      font-size: 1rem; pointer-events: none;
    }
    .bk-date .pr-input { padding-inline-start: 1rem; }
    .bk-date__hint {
      position: absolute; inset-inline-start: 1rem; top: 50%;
      transform: translateY(-50%); color: var(--pr-text-muted);
      pointer-events: none; font-size: .92rem;
    }
    .bk-date input[type="date"]:not(:placeholder-shown) + .bk-date__hint,
    .bk-date input[type="date"]:valid + .bk-date__hint { display: none; }

    /* Backup Now CTA */
    .bk-cta {
      width: 100%; padding: 1rem; border: 0; border-radius: 10px;
      background: var(--pr-primary); color: #fff;
      font: 600 1rem/1 system-ui, sans-serif; cursor: pointer;
      transition: filter .15s;
    }
    .bk-cta:hover:not(:disabled) { filter: brightness(.95); }
    .bk-cta:disabled { opacity: .6; cursor: not-allowed; }

    /* Card + table */
    .bk-card {
      background: var(--pr-surface); border: 1px solid var(--pr-border);
      border-radius: 10px; overflow: hidden;
    }
    .bk-table { display: flex; flex-direction: column; }
    .bk-table__head, .bk-row {
      display: grid;
      grid-template-columns: minmax(220px, 2fr) minmax(170px, 1.4fr) 100px 130px 150px;
      align-items: center; gap: 1rem;
      padding: .8rem 1.25rem;
    }
    .bk-table__head {
      background: var(--pr-surface-2);
      color: var(--pr-text-muted);
      font: 700 .72rem/1 system-ui, sans-serif;
      text-transform: uppercase; letter-spacing: .06em;
      border-bottom: 1px solid var(--pr-border);
    }
    .bk-row { border-bottom: 1px solid var(--pr-border); }
    .bk-row:last-child { border-bottom: 0; }
    .bk-row__file  { color: var(--pr-primary); font-family: 'JetBrains Mono', monospace; font-size: .85rem; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .bk-row__date  { color: var(--pr-text); }
    .bk-row__size  { color: var(--pr-text); }
    .bk-row__store { display: flex; gap: .35rem; flex-wrap: wrap; }
    .bk-row__actions { display: flex; gap: .35rem; justify-content: flex-end; }

    .bk-pill {
      padding: .25rem .65rem; border-radius: 999px;
      background: color-mix(in srgb, var(--pr-primary) 14%, transparent);
      color: var(--pr-primary); font: 600 .75rem/1 system-ui, sans-serif;
    }
    .bk-pill--alt {
      background: color-mix(in srgb, var(--pr-success, #16a34a) 14%, transparent);
      color: var(--pr-success, #16a34a);
    }

    .icon-btn {
      width: 36px; height: 36px; padding: 0; border: 0; border-radius: 8px;
      background: transparent; color: var(--pr-text-muted); cursor: pointer;
      font-size: 1.05rem; display: inline-flex; align-items: center; justify-content: center;
      transition: background .15s, color .15s;
    }
    .icon-btn:hover         { background: var(--pr-surface-2); color: var(--pr-text); }
    .icon-btn--success      { color: var(--pr-success, #16a34a); }
    .icon-btn--success:hover{ background: color-mix(in srgb, var(--pr-success, #16a34a) 14%, transparent); }
    .icon-btn--danger       { color: var(--pr-danger, #dc2626); }
    .icon-btn--danger:hover { background: color-mix(in srgb, var(--pr-danger, #dc2626) 14%, transparent); }

    .bk-actions-col { text-align: end; }
    .bk-empty {
      padding: 2rem 1rem; text-align: center; color: var(--pr-text-muted);
    }
    .bk-empty__icon { font-size: 2rem; margin-bottom: .35rem; }

    .bk-foot {
      display: flex; justify-content: space-between; align-items: center;
      gap: 1rem; padding: .9rem 1.25rem;
      background: var(--pr-surface-2); border-top: 1px solid var(--pr-border);
      color: var(--pr-text-muted); font-size: .9rem; flex-wrap: wrap;
    }
    .bk-foot__page { display: flex; align-items: center; gap: .5rem; }
    .bk-foot__page label { display: inline-flex; align-items: center; gap: .5rem; }
    .bk-foot__page select { padding: .35rem .5rem; height: auto; }

    /* Modal */
    .modal-backdrop {
      position: fixed; inset: 0; background: rgba(15, 23, 42, .45); z-index: 9000;
      display: flex; align-items: center; justify-content: center; padding: 1rem;
    }
    .modal-card {
      background: var(--pr-surface); border-radius: 12px;
      max-width: 760px; width: 100%; max-height: 85vh; overflow: auto;
      box-shadow: 0 12px 40px rgba(15, 23, 42, .25);
    }
    .modal-card--wide { max-width: 1100px; }
    .modal-card__head {
      display: flex; justify-content: space-between; align-items: center;
      padding: 1.1rem 1.5rem; border-bottom: 1px solid var(--pr-border);
    }
    .modal-card__head h2 { margin: 0; font-size: 1.2rem; color: var(--pr-primary); }
    .modal-card__head code { color: var(--pr-primary); font-size: .95rem; }
    .modal-card__close {
      background: transparent; border: 0; cursor: pointer; padding: .25rem;
      color: var(--pr-text-muted); font-size: 1.4rem; line-height: 1;
    }
    .modal-card__close:hover { color: var(--pr-text); }

    .browse-controls {
      display: grid; grid-template-columns: 200px 1fr auto;
      gap: .65rem; padding: 1rem 1.5rem;
    }
    @media (max-width: 640px) { .browse-controls { grid-template-columns: 1fr; } }
    .browse-count { padding: 0 1.5rem; color: var(--pr-text-muted); font-size: .9rem; margin: 0 0 .75rem; }

    .browse-list { display: flex; flex-direction: column; gap: .5rem; padding: 0 1.5rem 1.5rem; }
    .browse-row {
      display: grid; grid-template-columns: 110px 1fr auto;
      align-items: center; gap: 1rem;
      padding: .65rem .9rem;
      background: var(--pr-surface-2); border: 1px solid var(--pr-border);
      border-radius: 8px;
    }
    .browse-row__table { color: var(--pr-primary); font-family: 'JetBrains Mono', monospace; font-size: .8rem; }
    .browse-row__json  { color: var(--pr-text-muted); font-family: 'JetBrains Mono', monospace; font-size: .8rem;
                         white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .browse-row__btn   { color: var(--pr-success, #16a34a); border-color: var(--pr-success, #16a34a); }
    .browse-row__btn:hover { background: color-mix(in srgb, var(--pr-success, #16a34a) 12%, transparent); }
  `]
})
export class AdminBackupsComponent implements OnInit {
  private http    = inject(HttpClient);
  private toaster = inject(ToasterService);
  private modals  = inject(ModalService);
  private i18n    = inject(TranslationService);
  private base    = `${environment.apiUrl}/backups`;

  items      = signal<BackupEntry[]>([]);
  loading    = signal(false);
  error      = signal<string | null>(null);
  triggering = signal(false);

  search     = '';
  filterDate = '';
  pageSize   = 25;
  page       = signal(1);

  /** All items after search + date filter (pre-pagination). */
  filtered = computed(() => {
    const s = this.search.trim().toLowerCase();
    const d = this.filterDate;
    return this.items().filter(b => {
      if (s && !this.filename(b).toLowerCase().includes(s)) return false;
      if (d) {
        const day = (b.createdAt || '').slice(0, 10);
        if (day !== d) return false;
      }
      return true;
    });
  });

  filteredCount = computed(() => this.filtered().length);
  totalPages    = computed(() => Math.max(1, Math.ceil(this.filtered().length / this.pageSize)));

  /** Page-of-filtered items rendered in the table. */
  visible = computed(() => {
    const start = (this.page() - 1) * this.pageSize;
    return this.filtered().slice(start, start + this.pageSize);
  });

  // ── Browse modal state ──────────────────────────────────────────────────
  browseTarget   = signal<BackupEntry | null>(null);
  browseTables   = signal<string[]>([]);
  browseTable    = '';
  browseSearch   = '';
  browseLoading  = signal(false);
  browseNotice   = signal<string | null>(null);
  browseResults  = signal<{ id: string; table: string; preview: string }[]>([]);

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.error.set(null);
    this.http.get<any>(this.base, { params: { page: 1, pageSize: 200 } as any }).subscribe({
      next: r => {
        this.items.set(r?.items ?? []);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err?.error?.detail || err?.error?.title || this.i18n.t('admin.backups.loadFailed'));
        this.loading.set(false);
      }
    });
  }

  onFilterChange()    { this.page.set(1); }
  onPageSizeChange()  { this.page.set(1); }
  goPage(p: number)   { if (p >= 1 && p <= this.totalPages()) this.page.set(p); }

  triggerNow() {
    if (this.triggering()) return;
    this.triggering.set(true);
    this.http.post<any>(`${this.base}/trigger`, {}).subscribe({
      next: r => {
        this.triggering.set(false);
        this.toaster.success(r?.message ?? this.i18n.t('admin.backups.backupStarted'));
        setTimeout(() => this.load(), 1200);
      },
      error: err => {
        this.triggering.set(false);
        this.toaster.error(err?.error?.detail || this.i18n.t('admin.backups.backupTriggerFailed'));
      }
    });
  }

  // ── Per-row actions ─────────────────────────────────────────────────────
  openBrowse(b: BackupEntry) {
    this.browseTarget.set(b);
    this.browseSearch = '';
    this.browseResults.set([]);
    this.browseNotice.set(null);
    this.http.get<{ tables: string[] }>(`${this.base}/${b.id}/tables`).subscribe({
      next: r => {
        this.browseTables.set(r?.tables ?? []);
        this.browseTable = this.browseTables()[0] ?? '';
        this.runBrowse();
      },
      error: () => { this.browseTables.set([]); }
    });
  }
  closeBrowse() { this.browseTarget.set(null); }

  runBrowse() {
    const b = this.browseTarget();
    if (!b) return;
    this.browseLoading.set(true);
    this.browseNotice.set(null);
    this.http.get<any>(`${this.base}/${b.id}/browse`, {
      params: {
        table: this.browseTable, search: this.browseSearch, page: 1, pageSize: 50
      } as any
    }).subscribe({
      next: r => {
        this.browseResults.set(r?.items ?? []);
        this.browseLoading.set(false);
      },
      error: err => {
        this.browseLoading.set(false);
        // 501 is expected today — render the detail as an informational notice.
        if (err?.status === 501) {
          this.browseNotice.set(err?.error?.detail || this.i18n.t('admin.backups.browseFailed'));
          this.browseResults.set([]);
        } else {
          this.toaster.error(err?.error?.detail || this.i18n.t('admin.backups.browseFailed'));
        }
      }
    });
  }

  async confirmRestore(b: BackupEntry) {
    const ok = await this.modals.confirm({
      title:        this.i18n.t('admin.backups.restoreBackupTitle'),
      message:      this.i18n.t('admin.backups.restoreBackupBody', { name: this.filename(b), db: b.databaseName }),
      confirmLabel: this.i18n.t('admin.backups.restore'),
      cancelLabel:  this.i18n.t('admin.common.cancel'),
      variant: 'danger'
    });
    if (!ok) return;
    this.http.post<any>(`${this.base}/${b.id}/restore`, {}).subscribe({
      next: () => this.toaster.success(this.i18n.t('admin.backups.restoreStarted')),
      error: err => {
        if (err?.status === 501) this.toaster.info(err?.error?.detail || this.i18n.t('admin.backups.restoreFailed'));
        else this.toaster.error(err?.error?.detail || this.i18n.t('admin.backups.restoreFailed'));
      }
    });
  }

  async confirmRestoreRecord(b: BackupEntry, r: { id: string; table: string }) {
    const ok = await this.modals.confirm({
      title:        this.i18n.t('admin.backups.restoreRecordTitle'),
      message:      this.i18n.t('admin.backups.restoreRecordBody', { id: r.id, table: r.table }),
      confirmLabel: this.i18n.t('admin.backups.restore'),
      variant: 'danger'
    });
    if (!ok) return;
    this.http.post<any>(`${this.base}/${b.id}/restore`, {
      table: r.table, recordId: r.id
    }).subscribe({
      next: () => this.toaster.success(this.i18n.t('admin.backups.recordRestored')),
      error: err => {
        if (err?.status === 501) this.toaster.info(err?.error?.detail || this.i18n.t('admin.backups.restoreFailed'));
        else this.toaster.error(err?.error?.detail || this.i18n.t('admin.backups.restoreFailed'));
      }
    });
  }

  async confirmDelete(b: BackupEntry) {
    const ok = await this.modals.confirm({
      title:        this.i18n.t('admin.backups.deleteBackupTitle'),
      message:      this.i18n.t('admin.backups.deleteBackupBody', { name: this.filename(b) }),
      confirmLabel: this.i18n.t('admin.common.delete'),
      variant: 'danger'
    });
    if (!ok) return;
    this.http.delete(`${this.base}/${b.id}`).subscribe({
      next: () => { this.toaster.success(this.i18n.t('admin.backups.backupDeleted')); this.load(); },
      error: err => this.toaster.error(err?.error?.detail || this.i18n.t('admin.backups.deleteFailed'))
    });
  }

  // ── Display helpers ────────────────────────────────────────────────────
  filename(b: BackupEntry): string {
    if (b.objectKey) return b.objectKey.split('/').pop()!;
    return `${b.databaseName}_${(b.createdAt || '').replace(/[-:T.Z]/g, '').slice(0, 14)}.bak.gz`;
  }

  formatSize(bytes: number): string {
    if (!bytes) return '—';
    if (bytes < 1024)        return `${bytes} B`;
    if (bytes < 1048576)     return `${(bytes / 1024).toFixed(1)} KB`;
    if (bytes < 1073741824)  return `${(bytes / 1048576).toFixed(1)} MB`;
    return `${(bytes / 1073741824).toFixed(2)} GB`;
  }
}
