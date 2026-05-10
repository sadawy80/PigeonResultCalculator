import { Injectable, inject, signal, computed, OnDestroy } from '@angular/core';
import { ApiService } from './api.service';
import { Notification, NotificationStatus } from '../models';

@Injectable({ providedIn: 'root' })
export class NotificationService implements OnDestroy {
  private api = inject(ApiService);

  private _items = signal<Notification[]>([]);
  panelOpen      = signal(false);

  items       = this._items.asReadonly();
  unreadCount = computed(() =>
    this._items().filter(n => n.status !== NotificationStatus.Read).length
  );

  private timer: ReturnType<typeof setInterval> | null = null;

  startPolling() {
    if (this.timer) return;
    this.fetch();
    this.timer = setInterval(() => this.fetch(), 60_000);
  }

  stopPolling() {
    if (this.timer) { clearInterval(this.timer); this.timer = null; }
    this._items.set([]);
  }

  ngOnDestroy() { this.stopPolling(); }

  refresh() { this.fetch(); }

  private fetch() {
    this.api.getNotifications(1, 20).subscribe({
      next: p => this._items.set(p.items as Notification[]),
      error: () => {}
    });
  }

  markRead(id: string) {
    this.api.markNotificationRead(id).subscribe({ error: () => {} });
    this._items.update(arr =>
      arr.map(n => n.id === id
        ? { ...n, status: NotificationStatus.Read, readAt: new Date().toISOString() }
        : n)
    );
  }

  markAllRead() {
    this._items().filter(n => n.status !== NotificationStatus.Read)
      .forEach(n => this.api.markNotificationRead(n.id).subscribe({ error: () => {} }));
    this._items.update(arr =>
      arr.map(n => ({ ...n, status: NotificationStatus.Read, readAt: new Date().toISOString() }))
    );
  }
}
