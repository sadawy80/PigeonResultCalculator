import { Component, signal, computed, OnInit, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { DatePipe, DecimalPipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../environments/environment';

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

@Component({
  selector: 'app-admin-backups',
  standalone: true,
  imports: [DatePipe, DecimalPipe, NgClass, FormsModule],
  template: `
    <!-- Page header -->
    <div class="pr-page-header">
      <div>
        <h1 class="pr-page-header__title">Database Backups</h1>
        <p class="pr-page-header__subtitle">{{ total() }} backup(s) · MinIO object storage · optional pCloud offsite</p>
      </div>
      <div style="display:flex;gap:8px;flex-wrap:wrap">
        <button class="pr-btn pr-btn--outline pr-btn--sm"
                (click)="openTrigger(null)"
                [disabled]="triggering()">
          Full Backup
        </button>
      </div>
    </div>

    @if (error()) {
      <div class="pr-alert pr-alert--error mb-4">{{ error() }}</div>
    }

    @if (triggerMsg()) {
      <div class="pr-alert pr-alert--success mb-4">{{ triggerMsg() }}</div>
    }

    <!-- Filters -->
    <div class="sub-filters">
      <div class="sub-filter-group">
        <label>Database</label>
        <select class="pr-input" [(ngModel)]="filterDb" (ngModelChange)="onFilterChange()">
          <option value="">All databases</option>
          @for (db of knownDbs; track db) {
            <option [value]="db">{{ db }}</option>
          }
        </select>
      </div>
      <div class="sub-filter-group">
        <label>Status</label>
        <select class="pr-input" [(ngModel)]="filterStatus" (ngModelChange)="onFilterChange()">
          <option value="">All statuses</option>
          <option value="Completed">Completed</option>
          <option value="Failed">Failed</option>
          <option value="InProgress">In progress</option>
        </select>
      </div>
      @if (filterDb || filterStatus) {
        <button class="pr-btn pr-btn--ghost pr-btn--sm" style="align-self:flex-end" (click)="clearFilters()">
          Clear
        </button>
      }
    </div>

    <!-- Table -->
    <div class="pr-table-wrap">
      <table class="pr-table">
        <thead>
          <tr>
            <th>Database</th>
            <th>Started</th>
            <th>Duration</th>
            <th>Size</th>
            <th>Status</th>
            <th>MinIO</th>
            <th>pCloud</th>
            <th>Triggered by</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          @if (loading()) {
            <tr><td colspan="9" style="text-align:center;padding:32px">
              <div class="pr-spinner" style="margin:0 auto"></div>
            </td></tr>
          } @else if (items().length === 0) {
            <tr><td colspan="9">
              <div class="pr-empty">
                <div class="pr-empty__icon">💾</div>
                <div class="pr-empty__title">No backups yet</div>
                <p class="pr-empty__desc">Backups run automatically at 2 AM UTC. You can also trigger one manually.</p>
              </div>
            </td></tr>
          } @else {
            @for (b of items(); track b.id) {
              <tr>
                <td><code style="font-size:0.8rem">{{ b.databaseName }}</code></td>
                <td>{{ b.createdAt | date:'dd MMM yyyy HH:mm' }}</td>
                <td>{{ durationLabel(b) }}</td>
                <td>{{ b.sizeBytes > 0 ? formatSize(b.sizeBytes) : '—' }}</td>
                <td>
                  <span class="pr-badge"
                    [ngClass]="{
                      'pr-badge--success': b.status === 2,
                      'pr-badge--error':   b.status === 3,
                      'pr-badge--warning': b.status === 1
                    }">
                    {{ statusLabel(b.status) }}
                  </span>
                </td>
                <td>
                  @if (b.uploadedToMinIO) {
                    <span class="pr-badge pr-badge--success">✓</span>
                  } @else {
                    <span style="color:var(--pr-text-muted)">—</span>
                  }
                </td>
                <td>
                  @if (b.uploadedToPCloud) {
                    <span class="pr-badge pr-badge--success">✓</span>
                  } @else {
                    <span style="color:var(--pr-text-muted)">—</span>
                  }
                </td>
                <td style="font-size:0.8rem;color:var(--pr-text-muted)">
                  {{ b.triggeredBy === 'schedule' ? '🕑 Schedule' : '👤 ' + b.triggeredBy.replace('manual:', '') }}
                </td>
                <td>
                  <div class="row-actions">
                    @if (b.status === 1) {
                      <button class="pr-btn pr-btn--outline pr-btn--sm"
                              (click)="openTrigger(b.databaseName)"
                              [disabled]="triggering()">
                        Re-run
                      </button>
                    } @else {
                      <button class="pr-btn pr-btn--outline pr-btn--sm"
                              (click)="openTrigger(b.databaseName)"
                              [disabled]="triggering()">
                        Back up now
                      </button>
                    }
                    @if (b.status === 2 && b.uploadedToMinIO) {
                      <button class="pr-btn pr-btn--primary pr-btn--sm"
                              [disabled]="downloadingId() === b.id"
                              (click)="download(b)">
                        {{ downloadingId() === b.id ? '…' : 'Download' }}
                      </button>
                    }
                    <button class="pr-btn pr-btn--ghost pr-btn--sm"
                            style="color:var(--pr-danger)"
                            [disabled]="deletingId() === b.id"
                            (click)="confirmDelete(b)">
                      {{ deletingId() === b.id ? '…' : 'Delete' }}
                    </button>
                  </div>
                </td>
              </tr>
              @if (b.errorMessage) {
                <tr>
                  <td colspan="9" style="padding:0 12px 8px;background:var(--pr-surface-2)">
                    <div style="font-size:0.78rem;color:var(--pr-danger);font-family:monospace;white-space:pre-wrap;word-break:break-all">
                      {{ b.errorMessage }}
                    </div>
                  </td>
                </tr>
              }
            }
          }
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    @if (totalPages() > 1) {
      <div class="pagination-row">
        <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page === 1" (click)="goPage(page - 1)">← Prev</button>
        <span style="font-size:0.85rem;color:var(--pr-text-muted)">Page {{ page }} of {{ totalPages() }}</span>
        <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page === totalPages()" (click)="goPage(page + 1)">Next →</button>
      </div>
    }

    <!-- Trigger confirmation modal -->
    @if (showTriggerModal()) {
      <div class="modal-backdrop" (click)="closeTrigger()">
        <div class="modal-card" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h2 class="modal-title">Trigger Backup</h2>
            <button class="modal-close" (click)="closeTrigger()">✕</button>
          </div>
          <div class="modal-body">
            @if (triggerTarget()) {
              <p>Back up <strong>{{ triggerTarget() }}</strong> right now?</p>
              <p class="pr-hint">The backup will compress and upload to MinIO. This may take several minutes.</p>
            } @else {
              <p>Run a full backup of <strong>all 8 databases</strong> right now?</p>
              <p class="pr-hint">This runs in the background. Each database is compressed and uploaded to MinIO.</p>
            }
          </div>
          <div class="modal-footer">
            <button class="pr-btn pr-btn--ghost" (click)="closeTrigger()">Cancel</button>
            <button class="pr-btn pr-btn--primary" [disabled]="triggering()" (click)="runTrigger()">
              {{ triggering() ? 'Starting…' : 'Start Backup' }}
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Delete confirmation modal -->
    @if (deleteTarget()) {
      <div class="modal-backdrop" (click)="deleteTarget.set(null)">
        <div class="modal-card" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h2 class="modal-title">Delete Backup</h2>
            <button class="modal-close" (click)="deleteTarget.set(null)">✕</button>
          </div>
          <div class="modal-body">
            <div class="pr-alert pr-alert--warning mb-4">This cannot be undone.</div>
            <p>Delete the <strong>{{ deleteTarget()!.databaseName }}</strong> backup from
               <strong>{{ deleteTarget()!.createdAt | date:'dd MMM yyyy HH:mm' }}</strong>?</p>
            <p class="pr-hint">The MinIO object will also be removed.</p>
          </div>
          <div class="modal-footer">
            <button class="pr-btn pr-btn--ghost" (click)="deleteTarget.set(null)">Cancel</button>
            <button class="pr-btn pr-btn--primary" style="background:var(--pr-danger);border-color:var(--pr-danger)"
                    [disabled]="deletingId() === deleteTarget()!.id"
                    (click)="doDelete()">
              Delete
            </button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .sub-filters {
      display: flex; gap: 10px; align-items: flex-end; flex-wrap: wrap;
      padding: 16px; border-bottom: 1px solid var(--pr-border);
      background: var(--pr-surface-2);
      border-radius: var(--pr-radius) var(--pr-radius) 0 0;
    }
    .sub-filter-group {
      display: flex; flex-direction: column; gap: 4px; min-width: 160px;
      label { font-size: 0.7rem; font-weight: 600; color: var(--pr-text-muted); text-transform: uppercase; letter-spacing: 0.06em; }
    }
    .row-actions { display: flex; gap: 6px; flex-wrap: wrap; }
    .pagination-row {
      display: flex; align-items: center; gap: 12px; justify-content: center;
      padding: 16px;
    }
    @media (max-width: 767px) {
      .sub-filters { flex-direction: column; gap: 8px; }
      .sub-filter-group { min-width: 100%; }
    }
  `]
})
export class AdminBackupsComponent implements OnInit {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/backups`;

  items        = signal<BackupEntry[]>([]);
  total        = signal(0);
  loading      = signal(false);
  error        = signal<string | null>(null);
  triggerMsg   = signal<string | null>(null);

  page     = 1;
  pageSize = 20;
  filterDb     = '';
  filterStatus = '';

  showTriggerModal = signal(false);
  triggerTarget    = signal<string | null>(null);
  triggering       = signal(false);

  deleteTarget  = signal<BackupEntry | null>(null);
  deletingId    = signal<string | null>(null);
  downloadingId = signal<string | null>(null);

  readonly knownDbs = [
    'PRC_Identity', 'PRC_Club', 'PRC_Race', 'PRC_Federation',
    'PRC_Rendering', 'PRC_Integration', 'PRC_Admin', 'PRC_Subscription'
  ];

  totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.error.set(null);

    let params = new HttpParams()
      .set('page', this.page)
      .set('pageSize', this.pageSize);
    if (this.filterDb)     params = params.set('database', this.filterDb);
    if (this.filterStatus) params = params.set('status', this.filterStatus);

    this.http.get<any>(this.base, { params }).subscribe({
      next: r => {
        this.items.set(r.items ?? []);
        this.total.set(r.total ?? 0);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load backups.');
        this.loading.set(false);
      }
    });
  }

  onFilterChange() { this.page = 1; this.load(); }

  clearFilters() {
    this.filterDb = '';
    this.filterStatus = '';
    this.page = 1;
    this.load();
  }

  goPage(p: number) { this.page = p; this.load(); }

  openTrigger(db: string | null) {
    this.triggerTarget.set(db);
    this.showTriggerModal.set(true);
  }

  closeTrigger() { this.showTriggerModal.set(false); }

  runTrigger() {
    this.triggering.set(true);
    const body = this.triggerTarget() ? { databaseName: this.triggerTarget() } : {};
    this.http.post<any>(`${this.base}/trigger`, body).subscribe({
      next: r => {
        this.triggering.set(false);
        this.showTriggerModal.set(false);
        this.triggerMsg.set(r.message ?? 'Backup started.');
        setTimeout(() => this.triggerMsg.set(null), 5000);
        this.load();
      },
      error: () => {
        this.triggering.set(false);
        this.error.set('Failed to trigger backup.');
      }
    });
  }

  download(b: BackupEntry) {
    this.downloadingId.set(b.id);
    this.http.get<{ url: string }>(`${this.base}/${b.id}/download-url`).subscribe({
      next: r => {
        this.downloadingId.set(null);
        window.open(r.url, '_blank');
      },
      error: () => {
        this.downloadingId.set(null);
        this.error.set('Failed to get download URL.');
      }
    });
  }

  confirmDelete(b: BackupEntry) { this.deleteTarget.set(b); }

  doDelete() {
    const b = this.deleteTarget();
    if (!b) return;
    this.deletingId.set(b.id);
    this.http.delete(`${this.base}/${b.id}`).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.deleteTarget.set(null);
        this.load();
      },
      error: () => {
        this.deletingId.set(null);
        this.error.set('Failed to delete backup.');
      }
    });
  }

  statusLabel(status: number): string {
    return { 1: 'In Progress', 2: 'Completed', 3: 'Failed' }[status] ?? 'Unknown';
  }

  durationLabel(b: BackupEntry): string {
    if (!b.completedAt) return '—';
    const ms = new Date(b.completedAt).getTime() - new Date(b.createdAt).getTime();
    if (ms < 1000)   return `${ms}ms`;
    if (ms < 60000)  return `${(ms / 1000).toFixed(1)}s`;
    return `${Math.floor(ms / 60000)}m ${Math.floor((ms % 60000) / 1000)}s`;
  }

  formatSize(bytes: number): string {
    if (bytes < 1024)        return `${bytes} B`;
    if (bytes < 1048576)     return `${(bytes / 1024).toFixed(1)} KB`;
    if (bytes < 1073741824)  return `${(bytes / 1048576).toFixed(1)} MB`;
    return `${(bytes / 1073741824).toFixed(2)} GB`;
  }
}
