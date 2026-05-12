import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { ToasterService } from '../../core/services/toaster.service';
import { ModalService } from '../../core/services/modal.service';

interface ContactItem {
  id: string;
  senderRole: string;
  senderName: string;
  senderEmail: string;
  senderPhone?: string;
  subject: string;
  body: string;
  status: 'Open' | 'Replied' | 'Closed' | string;
  adminReply?: string | null;
  repliedAt?: string | null;
  createdAt: string;
}

/**
 * Admin Contact inbox. Single-column card layout: each card shows the sender
 * header, status badge, the body, the admin reply (if any), and an inline
 * reply form when the ticket isn't closed. Filters at the top scope the list
 * to All / New / Replied. Styled with the site theme tokens.
 */
@Component({
  selector: 'app-admin-contact',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  template: `
    <div class="inbox-header">
      <h1 class="inbox-title">Contact Inbox</h1>
      <div class="inbox-count">{{ total() }} message(s)</div>
    </div>

    <div class="inbox-controls">
      <div class="filter-pills" role="tablist">
        <button
          type="button"
          class="filter-pill"
          [class.active]="activeFilter() === 'all'"
          (click)="setFilter('all')">All</button>
        <button
          type="button"
          class="filter-pill"
          [class.active]="activeFilter() === 'new'"
          (click)="setFilter('new')">New</button>
        <button
          type="button"
          class="filter-pill"
          [class.active]="activeFilter() === 'replied'"
          (click)="setFilter('replied')">Replied</button>
      </div>

      <input
        type="search"
        class="pr-input inbox-search"
        placeholder="Search subject, name or email…"
        [(ngModel)]="search"
        (keyup.enter)="load()" />
    </div>

    @if (error()) { <div class="pr-alert pr-alert--error mb-4">{{ error() }}</div> }

    @if (loading()) {
      <div class="inbox-empty"><div class="pr-spinner" style="margin:0 auto"></div></div>
    } @else if (visible().length === 0) {
      <div class="inbox-empty">
        <div class="inbox-empty__icon">📭</div>
        <div>No messages match this filter.</div>
      </div>
    } @else {
      <div class="card-list">
        @for (m of visible(); track m.id) {
          <article class="msg-card" [attr.data-status]="m.status">
            <header class="msg-card__head">
              <div class="msg-card__from">
                <div class="msg-card__name">{{ m.senderName }}</div>
                <div class="msg-card__email">{{ m.senderEmail }}</div>
                <div class="msg-card__date">{{ m.createdAt | date: 'medium' }}</div>
              </div>
              <span class="status-badge" [attr.data-status]="m.status">{{ statusLabel(m.status) }}</span>
            </header>

            <div class="msg-card__body">
              <h2 class="msg-card__subject">{{ m.subject }}</h2>
              <p class="msg-card__text">{{ m.body }}</p>
            </div>

            @if (m.adminReply) {
              <section class="reply-block">
                <div class="reply-block__label">Admin reply:</div>
                <p class="reply-block__text">{{ m.adminReply }}</p>
                @if (m.repliedAt) {
                  <time class="reply-block__date">{{ m.repliedAt | date: 'short' }}</time>
                }
              </section>
            }

            @if (m.status !== 'Closed') {
              <footer class="reply-form">
                <textarea
                  class="pr-input reply-form__input"
                  rows="3"
                  maxlength="5000"
                  [(ngModel)]="replyDraft[m.id]"
                  placeholder="Type your reply…"></textarea>
                <div class="reply-form__actions">
                  <button
                    type="button"
                    class="pr-btn pr-btn--ghost pr-btn--sm"
                    [disabled]="busyId() === m.id"
                    (click)="closeTicket(m)">Close</button>
                  <button
                    type="button"
                    class="pr-btn pr-btn--primary pr-btn--sm"
                    [disabled]="busyId() === m.id || !(replyDraft[m.id] || '').trim()"
                    (click)="sendReply(m)">
                    {{ busyId() === m.id ? 'Sending…' : (m.adminReply ? 'Send follow-up' : 'Send reply') }}
                  </button>
                </div>
              </footer>
            }
          </article>
        }
      </div>
    }
  `,
  styles: [`
    :host { display: block; }

    /* Header */
    .inbox-header {
      display: flex; justify-content: space-between; align-items: baseline;
      margin-bottom: 1.25rem;
    }
    .inbox-title { margin: 0; font-size: 1.65rem; font-weight: 700; color: var(--pr-primary); }
    .inbox-count { color: var(--pr-text-muted); font-size: .9rem; }

    /* Filter row */
    .inbox-controls {
      display: flex; justify-content: space-between; align-items: center;
      gap: 1rem; flex-wrap: wrap; margin-bottom: 1.25rem;
    }
    .filter-pills { display: flex; gap: .5rem; flex-wrap: wrap; }
    .filter-pill {
      padding: .4rem 1rem;
      border-radius: 999px;
      border: 1px solid var(--pr-border);
      background: transparent;
      color: var(--pr-text-muted);
      font: 500 .9rem/1 system-ui, sans-serif;
      cursor: pointer;
      transition: background .15s, color .15s, border-color .15s;
    }
    .filter-pill:hover { color: var(--pr-text); border-color: color-mix(in srgb, var(--pr-primary) 40%, var(--pr-border)); }
    .filter-pill.active {
      background: color-mix(in srgb, var(--pr-primary) 15%, transparent);
      color: var(--pr-primary);
      border-color: var(--pr-primary);
    }
    .inbox-search { min-width: 260px; flex: 0 1 320px; }

    /* Empty / loading */
    .inbox-empty {
      padding: 3rem 1rem; text-align: center;
      color: var(--pr-text-muted);
      background: var(--pr-surface); border: 1px dashed var(--pr-border);
      border-radius: 10px;
    }
    .inbox-empty__icon { font-size: 2rem; margin-bottom: .5rem; }

    /* Card stack */
    .card-list { display: flex; flex-direction: column; gap: 1.25rem; }

    .msg-card {
      background: var(--pr-surface);
      border: 1px solid var(--pr-border);
      border-radius: 10px;
      padding: 1.1rem 1.25rem 1rem;
      position: relative;
      border-inline-start: 4px solid var(--pr-border);
    }
    .msg-card[data-status="Open"]    { border-inline-start-color: var(--pr-warning, #f59e0b); }
    .msg-card[data-status="Replied"] { border-inline-start-color: var(--pr-success, #16a34a); }
    .msg-card[data-status="Closed"]  { border-inline-start-color: var(--pr-text-muted); opacity: .85; }

    .msg-card__head {
      display: flex; justify-content: space-between; align-items: flex-start;
      gap: 1rem; margin-bottom: .9rem;
    }
    .msg-card__name  { font-weight: 700; color: var(--pr-text); font-size: 1.05rem; }
    .msg-card__email { color: var(--pr-text-muted); font-size: .88rem; margin-top: .15rem; }
    .msg-card__date  { color: var(--pr-text-muted); font-size: .82rem; margin-top: .15rem; }

    .status-badge {
      flex-shrink: 0;
      padding: .25rem .65rem;
      border-radius: 999px;
      font: 700 .7rem/1 system-ui, sans-serif;
      text-transform: uppercase;
      letter-spacing: .04em;
      background: var(--pr-surface-2);
      color: var(--pr-text-muted);
    }
    .status-badge[data-status="Open"] {
      background: color-mix(in srgb, var(--pr-warning, #f59e0b) 18%, transparent);
      color: var(--pr-warning, #b45309);
    }
    .status-badge[data-status="Replied"] {
      background: color-mix(in srgb, var(--pr-success, #16a34a) 18%, transparent);
      color: var(--pr-success, #15803d);
    }

    .msg-card__subject { margin: 0 0 .35rem; font-size: 1rem; font-weight: 700; color: var(--pr-text); }
    .msg-card__text {
      margin: 0; color: var(--pr-text); white-space: pre-wrap; line-height: 1.55;
    }

    /* Inline admin reply */
    .reply-block {
      margin-top: 1rem;
      padding: .8rem 1rem;
      background: color-mix(in srgb, var(--pr-success, #16a34a) 8%, var(--pr-surface));
      border: 1px solid color-mix(in srgb, var(--pr-success, #16a34a) 30%, transparent);
      border-radius: 8px;
    }
    .reply-block__label { color: var(--pr-success, #16a34a); font-weight: 600; font-size: .85rem; margin-bottom: .35rem; }
    .reply-block__text  { margin: 0; color: var(--pr-text); white-space: pre-wrap; line-height: 1.5; }
    .reply-block__date  { display: block; margin-top: .5rem; color: var(--pr-text-muted); font-size: .78rem; }

    /* Inline reply form */
    .reply-form { margin-top: 1rem; display: flex; flex-direction: column; gap: .6rem; }
    .reply-form__input { resize: vertical; min-height: 76px; font-family: inherit; }
    .reply-form__actions { display: flex; justify-content: flex-end; gap: .5rem; }
  `]
})
export class AdminContactComponent implements OnInit {
  private api     = inject(ApiService);
  private toaster = inject(ToasterService);
  private modals  = inject(ModalService);

  items   = signal<ContactItem[]>([]);
  loading = signal(false);
  busyId  = signal<string | null>(null);
  error   = signal<string | null>(null);
  total   = computed(() => this.visible().length);

  search = '';
  activeFilter = signal<'all' | 'new' | 'replied'>('all');
  replyDraft: Record<string, string> = {};

  visible = computed(() => {
    const f = this.activeFilter();
    const s = this.search.trim().toLowerCase();
    return this.items().filter(m => {
      if (f === 'new'     && m.status !== 'Open')    return false;
      if (f === 'replied' && m.status !== 'Replied') return false;
      if (!s) return true;
      return [m.subject, m.senderName, m.senderEmail].some(v => v?.toLowerCase().includes(s));
    });
  });

  ngOnInit() { this.load(); }

  setFilter(f: 'all' | 'new' | 'replied') { this.activeFilter.set(f); }

  load() {
    this.loading.set(true);
    this.error.set(null);
    // The list endpoint now returns the full message (body + reply inline) so
    // a single request fills the cards — no per-item GETs to fan out.
    this.api.adminListContactMessages({ page: 1, pageSize: 100 }).subscribe({
      next: r => {
        this.items.set((r?.items ?? []) as ContactItem[]);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err?.error?.detail || err?.error?.title || 'Failed to load messages.');
        this.loading.set(false);
      }
    });
  }

  statusLabel(status: string): string {
    return status === 'Open' ? 'New' : status;
  }

  sendReply(m: ContactItem) {
    const text = (this.replyDraft[m.id] || '').trim();
    if (!text || this.busyId()) return;
    this.busyId.set(m.id);
    this.api.adminReplyContactMessage(m.id, text).subscribe({
      next: () => {
        this.replyDraft[m.id] = '';
        this.busyId.set(null);
        this.toaster.success('Reply sent.');
        this.refreshOne(m.id);
      },
      error: err => {
        this.busyId.set(null);
        this.toaster.error(err?.error?.detail || 'Failed to send reply.');
      }
    });
  }

  async closeTicket(m: ContactItem) {
    const ok = await this.modals.confirm({
      title: 'Close ticket',
      message: `Close the conversation with ${m.senderName}?`,
      confirmLabel: 'Close',
      cancelLabel: 'Cancel'
    });
    if (!ok) return;
    this.busyId.set(m.id);
    this.api.adminCloseContactMessage(m.id).subscribe({
      next: () => {
        this.busyId.set(null);
        this.toaster.info('Ticket closed.');
        this.refreshOne(m.id);
      },
      error: err => {
        this.busyId.set(null);
        this.toaster.error(err?.error?.detail || 'Failed to close ticket.');
      }
    });
  }

  private refreshOne(id: string) {
    this.api.adminGetContactMessage(id).subscribe({
      next: d => {
        if (!d) return;
        this.items.update(list => list.map(x => x.id === id ? { ...x, ...d } as ContactItem : x));
      }
    });
  }
}
