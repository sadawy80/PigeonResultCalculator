import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/services';
import { Notification, NotificationStatus, NotificationType, UserRole } from '../../core/models';
import { TranslatePipe } from '../../core/i18n';

type Severity = 'success' | 'info' | 'warning' | 'alert';

function notifSeverity(type: NotificationType): Severity {
  switch (type) {
    case NotificationType.RaceResult: return 'success';
    case NotificationType.ErrorAlert: return 'warning';
    default:                          return 'info';
  }
}

const SEV_ICON:  Record<Severity, string> = { success: '✓', info: 'ℹ', warning: '⚠', alert: '🔔' };
const SEV_LABEL: Record<Severity, string> = { success: 'Success', info: 'Info', warning: 'Warning', alert: 'Alert' };

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [DatePipe, FormsModule, TranslatePipe],
  template: `
    <div class="pr-page-header notif-page-header">
      <div>
        <h1 class="pr-page-header__title">{{ 'admin.notifications.title' | translate }}</h1>
        <p class="pr-page-header__subtitle">{{ unreadCount() }} · {{ totalCount() }}</p>
      </div>
      <div class="notif-header-actions">
        <label class="notif-filter-check">
          <input type="checkbox" [(ngModel)]="unreadOnlyValue" (change)="onUnreadOnlyChange()">
          <span>{{ 'admin.upgrades.pending' | translate }}</span>
        </label>
        @if (unreadCount() > 0) {
          <button class="pr-btn pr-btn--ghost pr-btn--sm" (click)="markAllRead()" [disabled]="busy()">{{ 'admin.notifications.markAllRead' | translate }}</button>
        }
      </div>
    </div>

    <div class="notif-list">
      @if (loading()) {
        <div class="pr-empty"><div class="pr-spinner" style="margin:0 auto"></div></div>
      } @else if (notifications().length === 0) {
        <div class="pr-empty">
          <div class="pr-empty__icon">🔔</div>
          <div class="pr-empty__title">{{ 'admin.notifications.noNotifications' | translate }}</div>
        </div>
      } @else {
        @for (n of notifications(); track n.id) {
          @let sev = severity(n);
          @let isUnread = n.status !== NotificationStatus.Read;
          <div class="notif-item" [class.notif-item--unread]="isUnread" (click)="markRead(n)">

            <div class="notif-icon-wrap">
              @if (isUnread) { <div class="notif-unread-dot"></div> }
              <div class="notif-icon notif-icon--{{ sev }}">{{ SEV_ICON[sev] }}</div>
            </div>

            <div class="notif-body">
              <div class="notif-title">{{ n.title }}</div>
              <div class="notif-text">{{ n.body }}</div>
              <div class="notif-time">{{ n.createdAt | date:'dd MMM yyyy HH:mm' }}</div>
            </div>

            <div class="notif-right">
              <span class="notif-badge notif-badge--{{ sev }}">{{ SEV_LABEL[sev] }}</span>
              <button class="notif-dismiss" [title]="'admin.notifications.delete' | translate" (click)="dismiss(n, $event)">✕</button>
            </div>
          </div>
        }

        @if (hasMore()) {
          <div class="notif-load-more">
            <button class="pr-btn pr-btn--ghost" [disabled]="loadingMore()" (click)="loadMore()">
              @if (loadingMore()) { <span class="pr-spinner" style="width:14px;height:14px"></span> }
              {{ 'admin.common.next' | translate }}
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
export class NotificationsComponent implements OnInit {
  private api = inject(ApiService);
  auth = inject(AuthService);

  NotificationStatus = NotificationStatus;
  SEV_ICON  = SEV_ICON;
  SEV_LABEL = SEV_LABEL;

  notifications  = signal<Notification[]>([]);
  loading        = signal(true);
  loadingMore    = signal(false);
  busy           = signal(false);
  page           = signal(1);
  hasMore        = signal(false);
  totalCount     = signal(0);
  serverUnread   = signal(0);
  unreadOnlyValue = false;

  unreadCount = computed(() =>
    this.unreadOnlyValue ? this.serverUnread() : this.serverUnread()
  );

  severity(n: Notification): Severity { return notifSeverity(n.type); }

  ngOnInit() {
    if (this.auth.currentUser()?.role !== UserRole.SuperAdmin) this.load();
  }

  load() {
    this.loading.set(true);
    this.api.getNotifications(1, 20, this.unreadOnlyValue).subscribe(p => {
      this.notifications.set(p.items ?? []);
      this.totalCount.set(p.totalCount ?? 0);
      this.serverUnread.set(p.unreadCount ?? 0);
      this.hasMore.set((p.totalPages ?? 1) > 1);
      this.page.set(1);
      this.loading.set(false);
    });
  }

  onUnreadOnlyChange() { this.load(); }

  loadMore() {
    this.loadingMore.set(true);
    const next = this.page() + 1;
    this.api.getNotifications(next, 20, this.unreadOnlyValue).subscribe(p => {
      this.notifications.update(arr => [...arr, ...(p.items ?? [])]);
      this.serverUnread.set(p.unreadCount ?? 0);
      this.page.set(next);
      this.hasMore.set(next < (p.totalPages ?? 1));
      this.loadingMore.set(false);
    });
  }

  markRead(n: Notification) {
    if (n.status === NotificationStatus.Read) return;
    this.api.markNotificationRead(n.id).subscribe({
      next: () => {
        this.notifications.update(arr =>
          arr.map(x => x.id === n.id ? { ...x, status: NotificationStatus.Read } : x)
        );
        this.serverUnread.update(c => Math.max(0, c - 1));
      }
    });
    if (n.actionUrl) window.open(n.actionUrl, '_blank');
  }

  markAllRead() {
    this.busy.set(true);
    this.api.markAllNotificationsRead().subscribe({
      next: () => {
        this.notifications.update(arr =>
          arr.map(n => ({ ...n, status: NotificationStatus.Read }))
        );
        this.serverUnread.set(0);
        this.busy.set(false);
      },
      error: () => this.busy.set(false)
    });
  }

  dismiss(n: Notification, e: Event) {
    e.stopPropagation();
    this.api.dismissNotification(n.id).subscribe({
      next: () => {
        const wasUnread = n.status !== NotificationStatus.Read;
        this.notifications.update(arr => arr.filter(x => x.id !== n.id));
        this.totalCount.update(c => Math.max(0, c - 1));
        if (wasUnread) this.serverUnread.update(c => Math.max(0, c - 1));
      }
    });
  }

  dismissAll() {
    this.busy.set(true);
    this.api.dismissAllNotifications().subscribe({
      next: () => {
        this.notifications.set([]);
        this.totalCount.set(0);
        this.serverUnread.set(0);
        this.hasMore.set(false);
        this.busy.set(false);
      },
      error: () => this.busy.set(false)
    });
  }
}
