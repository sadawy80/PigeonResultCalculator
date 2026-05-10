import { Component, signal, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../core/services/api.service';

type Severity = 'success' | 'info' | 'warning' | 'alert';

function adminSeverity(type: string): Severity {
  if (type === 'System') return 'warning';
  return 'info';
}

const SEV_ICON:  Record<Severity, string> = { success: '✓', info: 'ℹ', warning: '⚠', alert: '🔔' };
const SEV_LABEL: Record<Severity, string> = { success: 'Success', info: 'Info', warning: 'Warning', alert: 'Alert' };

@Component({
  selector: 'app-admin-notifications',
  standalone: true,
  imports: [DatePipe, FormsModule],
  template: `
    <div class="pr-page-header notif-page-header">
      <div>
        <h1 class="pr-page-header__title">Notifications</h1>
        <p class="pr-page-header__subtitle">{{ unreadCount() }} unread · {{ total() }} total</p>
      </div>
      <div class="notif-header-actions">
        <label class="notif-filter-check">
          <input type="checkbox" [(ngModel)]="unreadOnlyValue" (change)="onUnreadOnlyChange()">
          <span>Unread only</span>
        </label>
        @if (unreadCount() > 0) {
          <button class="pr-btn pr-btn--ghost pr-btn--sm" (click)="markAllRead()" [disabled]="busy()">Mark all read</button>
        }
      </div>
    </div>

    @if (error()) {
      <div class="pr-alert pr-alert--error mb-4">{{ error() }}</div>
    }

    <div class="notif-list">
      @if (loading()) {
        <div class="pr-empty"><div class="pr-spinner" style="margin:0 auto"></div></div>
      } @else if (items().length === 0) {
        <div class="pr-empty">
          <div class="pr-empty__icon">🔔</div>
          <div class="pr-empty__title">{{ unreadOnlyValue ? 'No unread notifications' : 'No notifications' }}</div>
          @if (!unreadOnlyValue) {
            <p class="pr-empty__desc">You'll be notified here when users submit upgrade requests, link requests, and other platform events.</p>
          }
        </div>
      } @else {
        @for (n of items(); track n.id) {
          @let sev = severity(n);
          <div class="notif-item" [class.notif-item--unread]="!n.isRead" (click)="open(n)">

            <div class="notif-icon-wrap">
              @if (!n.isRead) { <div class="notif-unread-dot"></div> }
              <div class="notif-icon notif-icon--{{ sev }}">{{ SEV_ICON[sev] }}</div>
            </div>

            <div class="notif-body">
              <div class="notif-title">{{ n.title }}</div>
              <div class="notif-text">{{ n.body }}</div>
              <div class="notif-time">{{ n.createdAt | date:'dd MMM yyyy HH:mm' }}</div>
            </div>

            <div class="notif-right">
              <span class="notif-badge notif-badge--{{ sev }}">{{ SEV_LABEL[sev] }}</span>
              <button class="notif-dismiss" title="Dismiss" (click)="dismiss(n, $event)">✕</button>
            </div>
          </div>
        }

        @if (hasMore()) {
          <div class="notif-load-more">
            <button class="pr-btn pr-btn--ghost" [disabled]="loadingMore()" (click)="loadMore()">
              @if (loadingMore()) { <span class="pr-spinner" style="width:14px;height:14px"></span> }
              Load more
            </button>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .notif-page-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      flex-wrap: wrap;
      gap: 12px;
    }

    .notif-header-actions {
      display: flex;
      align-items: center;
      gap: 10px;
      flex-wrap: wrap;
    }

    .notif-filter-check {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 0.875rem;
      color: var(--pr-text-muted);
      cursor: pointer;
      input { accent-color: var(--pr-primary); cursor: pointer; }
    }

    .notif-list { display: flex; flex-direction: column; gap: 6px; }

    .notif-item {
      display: flex;
      align-items: flex-start;
      gap: 14px;
      padding: 14px 16px;
      border-radius: var(--pr-radius);
      border: 1px solid var(--pr-border);
      background: var(--pr-surface);
      cursor: pointer;
      transition: all var(--t-fast);
    }
    .notif-item:hover { border-color: var(--pr-primary); background: var(--pr-surface-2); }
    .notif-item--unread { border-left: 3px solid var(--pr-primary); }

    .notif-icon-wrap { position: relative; flex-shrink: 0; margin-top: 2px; }

    .notif-unread-dot {
      position: absolute;
      top: -4px;
      right: -4px;
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: var(--pr-primary);
      border: 2px solid var(--pr-surface);
    }

    .notif-icon {
      width: 36px;
      height: 36px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 0.9rem;
      font-weight: 700;
    }
    .notif-icon--success { background: #d1fae5; color: #059669; }
    .notif-icon--info    { background: #dbeafe; color: #2563eb; }
    .notif-icon--warning { background: #fef3c7; color: #d97706; }
    .notif-icon--alert   { background: #fee2e2; color: #dc2626; }

    .notif-body { flex: 1; min-width: 0; }
    .notif-title { font-weight: 600; font-size: 0.9rem; margin-bottom: 3px; }
    .notif-text  { font-size: 0.83rem; color: var(--pr-text-muted); margin-bottom: 6px; line-height: 1.4; }
    .notif-time  { font-size: 0.75rem; color: var(--pr-text-muted); }

    .notif-right {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: 8px;
      flex-shrink: 0;
    }

    .notif-badge {
      font-size: 0.68rem;
      font-weight: 700;
      padding: 2px 8px;
      border-radius: 10px;
      white-space: nowrap;
    }
    .notif-badge--success { background: #d1fae5; color: #059669; }
    .notif-badge--info    { background: #dbeafe; color: #2563eb; }
    .notif-badge--warning { background: #fef3c7; color: #d97706; }
    .notif-badge--alert   { background: #fee2e2; color: #dc2626; }

    .notif-dismiss {
      background: none;
      border: none;
      cursor: pointer;
      color: var(--pr-text-muted);
      font-size: 0.8rem;
      padding: 2px 4px;
      border-radius: 4px;
      line-height: 1;
      transition: all var(--t-fast);
    }
    .notif-dismiss:hover { background: var(--pr-surface-3, var(--pr-border)); color: var(--pr-text); }

    .notif-load-more { display: flex; justify-content: center; margin-top: 24px; }
  `]
})
export class AdminNotificationsComponent implements OnInit {
  private api    = inject(ApiService);
  private router = inject(Router);

  SEV_ICON  = SEV_ICON;
  SEV_LABEL = SEV_LABEL;

  items       = signal<any[]>([]);
  total       = signal(0);
  unreadCount = signal(0);
  loading     = signal(false);
  loadingMore = signal(false);
  busy        = signal(false);
  error       = signal<string | null>(null);
  hasMore     = signal(false);
  unreadOnlyValue = false;
  page        = 1;
  readonly pageSize = 20;

  severity(n: any): Severity { return adminSeverity(n.type ?? ''); }

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.error.set(null);
    this.api.adminGetNotifications({ page: 1, pageSize: this.pageSize, unreadOnly: this.unreadOnlyValue || undefined }).subscribe({
      next: r => {
        const data = r?.data ?? r;
        this.items.set(data?.items ?? []);
        this.total.set(data?.totalCount ?? 0);
        this.unreadCount.set(data?.unreadCount ?? 0);
        this.hasMore.set((data?.totalCount ?? 0) > this.pageSize);
        this.page = 1;
        this.loading.set(false);
      },
      error: () => { this.error.set('Failed to load notifications.'); this.loading.set(false); }
    });
  }

  onUnreadOnlyChange() { this.load(); }

  loadMore() {
    this.loadingMore.set(true);
    const next = this.page + 1;
    this.api.adminGetNotifications({ page: next, pageSize: this.pageSize, unreadOnly: this.unreadOnlyValue || undefined }).subscribe({
      next: r => {
        const data = r?.data ?? r;
        this.items.update(arr => [...arr, ...(data?.items ?? [])]);
        this.page = next;
        this.hasMore.set(this.items().length < (data?.totalCount ?? 0));
        this.loadingMore.set(false);
      },
      error: () => this.loadingMore.set(false)
    });
  }

  open(n: any) {
    if (!n.isRead) {
      this.api.adminMarkNotificationRead(n.id).subscribe({
        next: () => {
          this.items.update(arr => arr.map(x => x.id === n.id ? { ...x, isRead: true } : x));
          this.unreadCount.update(c => Math.max(0, c - 1));
        }
      });
    }
    if (n.actionUrl) this.router.navigateByUrl(n.actionUrl);
  }

  dismiss(n: any, e: Event) {
    e.stopPropagation();
    this.api.adminDismissNotification(n.id).subscribe({
      next: () => {
        const wasUnread = !n.isRead;
        this.items.update(arr => arr.filter(x => x.id !== n.id));
        this.total.update(c => Math.max(0, c - 1));
        if (wasUnread) this.unreadCount.update(c => Math.max(0, c - 1));
      }
    });
  }

  markAllRead() {
    this.busy.set(true);
    this.api.adminMarkAllNotificationsRead().subscribe({
      next: () => {
        this.items.update(arr => arr.map(n => ({ ...n, isRead: true })));
        this.unreadCount.set(0);
        this.busy.set(false);
      },
      error: () => this.busy.set(false)
    });
  }

  dismissAll() {
    this.busy.set(true);
    this.api.adminDismissAllNotifications().subscribe({
      next: () => {
        this.items.set([]);
        this.total.set(0);
        this.unreadCount.set(0);
        this.hasMore.set(false);
        this.busy.set(false);
      },
      error: () => this.busy.set(false)
    });
  }
}
