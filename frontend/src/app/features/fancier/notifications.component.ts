import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/services';
import { Notification, NotificationStatus } from '../../core/models';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [DatePipe, NgClass],
  template: `
    <div class="pr-page-header flex justify-between items-center">
      <div>
        <h1 class="pr-page-header__title">Notifications</h1>
        <p class="pr-page-header__subtitle">{{ unreadCount() }} unread</p>
      </div>
      @if (unreadCount() > 0) {
        <button class="pr-btn pr-btn--ghost pr-btn--sm" (click)="markAllRead()">Mark all read</button>
      }
    </div>

    <div class="notif-list">
      @if (loading()) {
        <div class="pr-empty"><div class="pr-spinner" style="margin:0 auto"></div></div>
      } @else if (notifications().length === 0) {
        <div class="pr-empty">
          <div class="pr-empty__icon">🔔</div>
          <div class="pr-empty__title">No notifications yet</div>
          <p class="pr-empty__desc">Race result publications and club updates will appear here.</p>
        </div>
      } @else {
        @for (n of notifications(); track n.id) {
          <div class="notif-item" [class.notif-item--unread]="n.status !== NotificationStatus.Read"
               (click)="markRead(n)">
            <div class="notif-item__dot" [class.notif-item__dot--unread]="n.status !== NotificationStatus.Read"></div>
            <div class="notif-item__body">
              <div class="notif-item__title">{{ n.title }}</div>
              <div class="notif-item__body-text">{{ n.body }}</div>
              <div class="notif-item__time text-muted text-sm">{{ n.createdAt | date:'dd MMM yyyy HH:mm' }}</div>
            </div>
            <span [class]="typeIcon(n.type)" class="notif-item__icon"></span>
          </div>
        }

        <!-- Load more -->
        @if (hasMore()) {
          <div class="flex justify-center mt-6">
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
    .notif-list { display:flex; flex-direction:column; gap:4px; }

    .notif-item {
      display:flex; align-items:flex-start; gap:14px;
      padding:16px; border-radius:var(--pr-radius);
      border:1px solid var(--pr-border);
      background:var(--pr-surface);
      cursor:pointer;
      transition:all var(--t-fast);
    }
    .notif-item:hover { border-color:var(--pr-primary); background:var(--pr-surface-2); }
    .notif-item--unread { border-left:3px solid var(--pr-primary); background:var(--pr-surface); }

    .notif-item__dot {
      width:8px; height:8px; border-radius:50%; flex-shrink:0; margin-top:6px;
      background:var(--pr-border);
    }
    .notif-item__dot--unread { background:var(--pr-primary); }

    .notif-item__body { flex:1; }
    .notif-item__title { font-weight:600; font-size:0.9rem; margin-bottom:3px; }
    .notif-item__body-text { font-size:0.85rem; color:var(--pr-text-muted); margin-bottom:6px; }
    .notif-item__time { font-size:0.75rem; }
    .notif-item__icon { font-size:1.4rem; flex-shrink:0; }
  `]
})
export class NotificationsComponent implements OnInit {
  private api = inject(ApiService);
  auth = inject(AuthService);
  NotificationStatus = NotificationStatus;

  notifications = signal<Notification[]>([]);
  loading       = signal(true);
  loadingMore   = signal(false);
  page          = signal(1);
  hasMore       = signal(false);
  unreadCount   = () => this.notifications().filter(n => n.status !== NotificationStatus.Read).length;

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.api.getNotifications(1, 20).subscribe(p => {
      this.notifications.set(p.items as Notification[]);
      this.hasMore.set(p.totalPages > 1);
      this.loading.set(false);
    });
  }

  loadMore() {
    this.loadingMore.set(true);
    const next = this.page() + 1;
    this.api.getNotifications(next, 20).subscribe(p => {
      this.notifications.update(arr => [...arr, ...(p.items as Notification[])]);
      this.page.set(next);
      this.hasMore.set(next < p.totalPages);
      this.loadingMore.set(false);
    });
  }

  markRead(n: Notification) {
    if (n.status === NotificationStatus.Read) return;
    this.api.markNotificationRead(n.id).subscribe(() => {
      this.notifications.update(arr =>
        arr.map(x => x.id === n.id ? { ...x, status: NotificationStatus.Read, readAt: new Date().toISOString() } : x)
      );
    });
    if (n.actionUrl) window.open(n.actionUrl, '_blank');
  }

  markAllRead() {
    const unread = this.notifications().filter(n => n.status !== NotificationStatus.Read);
    unread.forEach(n => this.api.markNotificationRead(n.id).subscribe());
    this.notifications.update(arr =>
      arr.map(n => ({ ...n, status: NotificationStatus.Read, readAt: new Date().toISOString() }))
    );
  }

  typeIcon(type: number): string {
    const icons: Record<number, string> = {
      1: '🏁', 2: '🏟️', 3: '📢', 4: '⚙️', 5: '📧', 6: '⚠️'
    };
    return icons[type] ?? '🔔';
  }
}
