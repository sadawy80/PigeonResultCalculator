import { Component, signal, OnInit, inject } from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { TranslatePipe } from '../../core/i18n';

@Component({
  selector: 'app-admin-link-requests',
  standalone: true,
  imports: [DatePipe, FormsModule, NgClass, TranslatePipe],
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    .page-title  { font-size: 1.5rem; font-weight: 700; margin: 0; }
    .toolbar     { display: flex; gap: .75rem; align-items: center; flex-wrap: wrap; margin-bottom: 1rem; }
    .toolbar label { font-size: .875rem; font-weight: 600; }
    .toolbar select { padding: .4rem .75rem; border-radius: 6px; border: 1px solid var(--border); background: var(--surface); color: var(--text); font-size: .875rem; }
    .table-wrap  { overflow-x: auto; }
    table        { width: 100%; border-collapse: collapse; font-size: .9rem; }
    th           { text-align: left; padding: .6rem 1rem; border-bottom: 2px solid var(--border); color: var(--text-muted); font-weight: 600; white-space: nowrap; }
    td           { padding: .75rem 1rem; border-bottom: 1px solid var(--border); vertical-align: top; }
    tr:last-child td { border-bottom: none; }
    .badge       { display: inline-block; padding: .2rem .55rem; border-radius: 999px; font-size: .78rem; font-weight: 600; }
    .badge-pending  { background: #fef3c7; color: #92400e; }
    .badge-approved { background: #d1fae5; color: #065f46; }
    .badge-rejected { background: #fee2e2; color: #991b1b; }
    .badge-revoked  { background: #e0e7ff; color: #3730a3; }
    .actions     { display: flex; gap: .5rem; }
    .btn         { padding: .35rem .85rem; border-radius: 6px; border: none; cursor: pointer; font-size: .85rem; font-weight: 600; }
    .btn-approve { background: #10b981; color: #fff; }
    .btn-reject  { background: #ef4444; color: #fff; }
    .btn-revoke  { background: #f59e0b; color: #fff; }
    .btn:disabled { opacity: .5; cursor: not-allowed; }
    .empty       { text-align: center; padding: 3rem; color: var(--text-muted); }
    .error-msg   { color: #ef4444; padding: .75rem; background: #fee2e2; border-radius: 8px; margin-bottom: 1rem; }
    .modal-backdrop { position: fixed; inset: 0; background: rgba(0,0,0,.4); display: flex; align-items: center; justify-content: center; z-index: 1000; }
    .modal-card  { background: var(--surface); border-radius: 12px; padding: 1.5rem; width: 100%; max-width: 480px; }
    .modal-title { font-weight: 700; font-size: 1.1rem; margin: 0 0 1rem; }
    .form-group  { margin-bottom: 1rem; }
    label        { display: block; font-size: .875rem; font-weight: 600; margin-bottom: .35rem; }
    textarea     { width: 100%; border-radius: 6px; border: 1px solid var(--border); padding: .5rem .75rem; font-size: .875rem; background: var(--surface); color: var(--text); resize: vertical; }
    .modal-footer { display: flex; justify-content: flex-end; gap: .75rem; margin-top: 1rem; }
    .btn-secondary { background: var(--border); color: var(--text); }
    .pagination  { display: flex; align-items: center; gap: .75rem; margin-top: 1rem; justify-content: flex-end; font-size: .875rem; color: var(--text-muted); }
    .page-btn    { padding: .3rem .7rem; border-radius: 6px; border: 1px solid var(--border); background: var(--surface); color: var(--text); cursor: pointer; }
    .page-btn:disabled { opacity: .4; cursor: not-allowed; }
  `],
  template: `
    <div class="page-header">
      <h1 class="page-title">{{ 'admin.links.title' | translate }}</h1>
    </div>

    @if (error()) {
      <div class="error-msg">{{ error() }}</div>
    }

    <div class="pr-card mb-4">
      <div class="flex flex-wrap gap-4 items-end">
        <div class="pr-form-group" style="flex:1;min-width:160px">
          <label class="pr-label">{{ 'admin.common.status' | translate }}</label>
          <select class="pr-select" [(ngModel)]="statusFilter" (change)="onFilterChange()">
            <option value="">{{ 'admin.common.all' | translate }}</option>
            <option value="0">Pending</option>
            <option value="1">Approved</option>
            <option value="2">Rejected</option>
            <option value="3">Revoked</option>
          </select>
        </div>
        <button class="pr-btn pr-btn--ghost pr-btn--field" (click)="statusFilter=''; onFilterChange()">Reset</button>
      </div>
    </div>

    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Loft</th>
            <th>Platform</th>
            <th>PRC User</th>
            <th>Club</th>
            <th>Status</th>
            <th>Requested</th>
            <th>Last Access</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          @if (loading()) {
            <tr><td colspan="8" class="empty">Loading…</td></tr>
          } @else if (items().length === 0) {
            <tr><td colspan="8" class="empty">No link requests found.</td></tr>
          } @else {
            @for (item of items(); track item.id) {
              <tr>
                <td>
                  <strong>{{ item.externalLoftName }}</strong><br>
                  <small style="color:var(--text-muted)">{{ item.externalLoftId }}</small>
                </td>
                <td style="font-size:.85rem">{{ item.externalPlatformName }}</td>
                <td>
                  @if (item.userId && item.userId !== '00000000-0000-0000-0000-000000000000') {
                    <span style="font-size:.85rem">{{ item.userId }}</span>
                  } @else {
                    <span style="color:var(--text-muted);font-size:.85rem">—</span>
                  }
                </td>
                <td style="font-size:.85rem">{{ item.clubId }}</td>
                <td>
                  <span class="badge" [ngClass]="badgeClass(item.statusLabel ?? item.status)">
                    {{ item.statusLabel ?? statusLabel(item.status) }}
                  </span>
                  @if (item.rejectionReason) {
                    <br><small>{{ item.rejectionReason }}</small>
                  }
                  @if (item.revokedReason) {
                    <br><small>{{ item.revokedReason }}</small>
                  }
                </td>
                <td style="font-size:.85rem">{{ item.requestedAt | date:'mediumDate' }}</td>
                <td style="font-size:.85rem">{{ item.lastDataAccessAt ? (item.lastDataAccessAt | date:'medium') : '—' }}</td>
                <td>
                  @if (isStatusPending(item)) {
                    <div class="actions">
                      <button class="btn btn-approve" (click)="approve(item)" [disabled]="busy()">Approve</button>
                      <button class="btn btn-reject"  (click)="openReject(item)" [disabled]="busy()">Reject</button>
                    </div>
                  }
                  @if (isStatusApproved(item)) {
                    <div class="actions">
                      <button class="btn btn-revoke" (click)="revoke(item)" [disabled]="busy()">Revoke</button>
                    </div>
                  }
                </td>
              </tr>
            }
          }
        </tbody>
      </table>
    </div>

    <div class="pagination-row">
      <span class="text-muted text-sm">{{ total() }} links · page {{ page }} of {{ totalPages() }}</span>
      <div class="flex gap-2 items-center">
        <select class="pr-select" style="width:auto" [(ngModel)]="pageSize" (ngModelChange)="onPageSizeChange()">
          <option [ngValue]="10">10 / page</option>
          <option [ngValue]="25">25 / page</option>
          <option [ngValue]="50">50 / page</option>
          <option [ngValue]="100">100 / page</option>
        </select>
        <button class="pr-btn pr-btn--ghost pr-btn--sm" (click)="prevPage()" [disabled]="page === 1">Prev</button>
        <button class="pr-btn pr-btn--ghost pr-btn--sm" (click)="nextPage()" [disabled]="page >= totalPages()">Next</button>
      </div>
    </div>

    @if (showRejectModal()) {
      <div class="modal-backdrop" (click)="cancelReject()">
        <div class="modal-card" (click)="$event.stopPropagation()">
          <p class="modal-title">Reject Link Request</p>
          <p>Loft: <strong>{{ rejectTarget()?.externalLoftName }}</strong></p>
          <div class="form-group">
            <label>Reason (optional)</label>
            <textarea [(ngModel)]="rejectReason" rows="3" placeholder="Explain why the link request is being rejected…"></textarea>
          </div>
          <div class="modal-footer">
            <button class="btn btn-secondary" (click)="cancelReject()">Cancel</button>
            <button class="btn btn-reject" (click)="confirmReject()" [disabled]="busy()">Confirm Reject</button>
          </div>
        </div>
      </div>
    }
  `
})
export class AdminLinkRequestsComponent implements OnInit {
  private api = inject(ApiService);

  items    = signal<any[]>([]);
  total    = signal(0);
  loading  = signal(false);
  busy     = signal(false);
  error    = signal<string | null>(null);
  page     = 1;
  pageSize = 10;
  statusFilter = '';

  showRejectModal = signal(false);
  rejectTarget    = signal<any>(null);
  rejectReason    = '';

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.error.set(null);
    const status = this.statusFilter !== '' ? +this.statusFilter : undefined;
    this.api.adminGetLinkRequests({ status, page: this.page, pageSize: this.pageSize }).subscribe({
      next: r => {
        const arr = Array.isArray(r) ? r : (r as any).items ?? [];
        this.items.set(arr);
        this.total.set((r as any).totalCount ?? arr.length);
        this.loading.set(false);
      },
      error: () => { this.error.set('Failed to load link requests.'); this.loading.set(false); }
    });
  }

  onFilterChange() { this.page = 1; this.load(); }
  prevPage()       { if (this.page > 1) { this.page--; this.load(); } }
  nextPage()       { if (this.page < this.totalPages()) { this.page++; this.load(); } }
  totalPages()     { return Math.max(1, Math.ceil(this.total() / this.pageSize)); }
  onPageSizeChange() { this.page = 1; this.load(); }

  isStatusPending(item: any): boolean  { return item.statusLabel === 'Pending'  || item.status === 0; }
  isStatusApproved(item: any): boolean { return item.statusLabel === 'Approved' || item.status === 1; }

  approve(item: any) {
    this.busy.set(true);
    this.api.adminApproveLinkRequest(item.id).subscribe({
      next: () => { this.busy.set(false); this.load(); },
      error: e => { this.error.set(e?.error?.error ?? 'Failed to approve link.'); this.busy.set(false); }
    });
  }

  revoke(item: any) {
    this.busy.set(true);
    this.api.adminRevokeLinkRequest(item.id).subscribe({
      next: () => { this.busy.set(false); this.load(); },
      error: e => { this.error.set(e?.error?.error ?? 'Failed to revoke link.'); this.busy.set(false); }
    });
  }

  openReject(item: any) {
    this.rejectTarget.set(item);
    this.rejectReason = '';
    this.showRejectModal.set(true);
  }

  cancelReject() { this.showRejectModal.set(false); this.rejectTarget.set(null); }

  confirmReject() {
    const item = this.rejectTarget();
    if (!item) return;
    this.busy.set(true);
    this.api.adminRejectLinkRequest(item.id, this.rejectReason || undefined).subscribe({
      next: () => { this.busy.set(false); this.showRejectModal.set(false); this.load(); },
      error: e => { this.error.set(e?.error?.error ?? 'Failed to reject link.'); this.busy.set(false); }
    });
  }

  statusLabel(status: number): string {
    const map: Record<number, string> = { 0: 'Pending', 1: 'Approved', 2: 'Rejected', 3: 'Revoked' };
    return map[status] ?? String(status);
  }

  badgeClass(statusOrLabel: number | string): string {
    const label = typeof statusOrLabel === 'string'
      ? statusOrLabel
      : this.statusLabel(statusOrLabel);
    const map: Record<string, string> = {
      'Pending': 'badge-pending', 'Approved': 'badge-approved',
      'Rejected': 'badge-rejected', 'Revoked': 'badge-revoked'
    };
    return map[label] ?? '';
  }
}
