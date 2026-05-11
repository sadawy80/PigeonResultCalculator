import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

interface ContactListItem {
  id: string;
  senderRole: string;
  senderName: string;
  senderEmail: string;
  senderPhone?: string;
  subject: string;
  status: string;
  hasReply: boolean;
  createdAt: string;
}

interface ContactDetail extends ContactListItem {
  body: string;
  adminReply?: string;
  repliedAt?: string;
  repliedByAdminId?: string;
  updatedAt?: string;
}

@Component({
  selector: 'app-admin-contact',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  template: `
    <div class="pr-page-header">
      <div>
        <h1 class="pr-page-header__title">Contact inbox</h1>
        <p class="pr-page-header__subtitle">{{ total() }} message(s) · {{ openCount() }} open</p>
      </div>
      <div class="filters">
        <select [(ngModel)]="statusFilter" (change)="load()">
          <option value="">All statuses</option>
          <option value="Open">Open</option>
          <option value="Replied">Replied</option>
          <option value="Closed">Closed</option>
        </select>
        <input type="search" placeholder="Search subject / name / email" [(ngModel)]="search" (keyup.enter)="load()" />
      </div>
    </div>

    @if (error()) { <div class="pr-alert pr-alert--error mb-4">{{ error() }}</div> }

    <div class="contact-layout">
      <div class="contact-list">
        @if (loading()) {
          <div class="pr-empty"><div class="pr-spinner" style="margin:0 auto"></div></div>
        } @else if (items().length === 0) {
          <div class="pr-empty"><div class="pr-empty__icon">📭</div><div>No messages.</div></div>
        } @else {
          @for (m of items(); track m.id) {
            <button class="contact-row" [class.active]="selected()?.id === m.id" (click)="select(m.id)">
              <div class="row-main">
                <span class="status" [attr.data-status]="m.status">{{ m.status }}</span>
                <strong>{{ m.subject }}</strong>
              </div>
              <div class="row-meta">
                <span>{{ m.senderName }} · {{ m.senderEmail }}</span>
                <span>{{ m.createdAt | date: 'short' }}</span>
              </div>
            </button>
          }
        }
      </div>

      <div class="contact-detail">
        @if (selected(); as d) {
          <header>
            <h2>{{ d.subject }}</h2>
            <span class="status" [attr.data-status]="d.status">{{ d.status }}</span>
          </header>
          <dl class="meta">
            <div><dt>From</dt><dd>{{ d.senderName }} &lt;{{ d.senderEmail }}&gt;</dd></div>
            @if (d.senderPhone) { <div><dt>Phone</dt><dd>{{ d.senderPhone }}</dd></div> }
            <div><dt>Role</dt><dd>{{ d.senderRole }}</dd></div>
            <div><dt>Received</dt><dd>{{ d.createdAt | date: 'medium' }}</dd></div>
          </dl>

          <section class="body">
            <h3>Message</h3>
            <pre>{{ d.body }}</pre>
          </section>

          @if (d.adminReply) {
            <section class="body reply">
              <h3>Your reply ({{ d.repliedAt | date: 'medium' }})</h3>
              <pre>{{ d.adminReply }}</pre>
            </section>
          }

          @if (d.status !== 'Closed') {
            <section class="reply-form">
              <h3>{{ d.adminReply ? 'Send follow-up' : 'Reply' }}</h3>
              <textarea [(ngModel)]="replyText" rows="6" maxlength="5000" placeholder="Type your reply..."></textarea>
              <div class="actions">
                <button class="pr-btn pr-btn--ghost" (click)="closeTicket(d.id)" [disabled]="busy()">Close ticket</button>
                <button class="pr-btn pr-btn--primary" (click)="sendReply(d.id)" [disabled]="busy() || !replyText.trim()">
                  {{ busy() ? 'Sending…' : 'Send reply' }}
                </button>
              </div>
            </section>
          }
        } @else {
          <div class="pr-empty"><div>Select a message to view.</div></div>
        }
      </div>
    </div>
  `,
  styles: [`
    .filters { display: flex; gap: .5rem; }
    .filters select, .filters input { padding: .5rem .75rem; border: 1px solid #cbd5e1; border-radius: 6px; }
    .contact-layout { display: grid; grid-template-columns: 360px 1fr; gap: 1rem; height: calc(100vh - 200px); }
    .contact-list { background: #fff; border-radius: 8px; overflow-y: auto; border: 1px solid #e2e8f0; }
    .contact-row { width: 100%; text-align: left; padding: .75rem 1rem; border: 0; background: transparent; border-bottom: 1px solid #f1f5f9; cursor: pointer; display: flex; flex-direction: column; gap: .25rem; }
    .contact-row:hover { background: #f8fafc; }
    .contact-row.active { background: #eef2ff; }
    .row-main { display: flex; gap: .5rem; align-items: center; }
    .row-meta { display: flex; justify-content: space-between; font-size: .8rem; color: #64748b; }
    .status { font-size: .7rem; font-weight: 600; padding: .15rem .5rem; border-radius: 999px; background: #e2e8f0; color: #475569; text-transform: uppercase; }
    .status[data-status="Open"]    { background: #dbeafe; color: #1d4ed8; }
    .status[data-status="Replied"] { background: #dcfce7; color: #166534; }
    .status[data-status="Closed"]  { background: #f1f5f9; color: #64748b; }
    .contact-detail { background: #fff; border-radius: 8px; padding: 1.5rem; border: 1px solid #e2e8f0; overflow-y: auto; }
    .contact-detail header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .contact-detail h2 { margin: 0; }
    .meta { display: grid; grid-template-columns: 1fr 1fr; gap: .5rem 1.5rem; margin: 1rem 0; font-size: .9rem; }
    .meta dt { color: #64748b; font-weight: 500; }
    .meta dd { margin: 0; }
    .body { margin-top: 1rem; }
    .body h3 { font-size: 1rem; color: #334155; margin: 0 0 .5rem; }
    .body pre { background: #f8fafc; padding: 1rem; border-radius: 6px; white-space: pre-wrap; font-family: inherit; margin: 0; }
    .body.reply pre { background: #f0fdf4; }
    .reply-form { margin-top: 1.5rem; }
    .reply-form textarea { width: 100%; padding: .75rem; border: 1px solid #cbd5e1; border-radius: 6px; font: inherit; resize: vertical; }
    .reply-form .actions { display: flex; justify-content: flex-end; gap: .5rem; margin-top: .5rem; }
    @media (max-width: 900px) { .contact-layout { grid-template-columns: 1fr; height: auto; } }
  `]
})
export class AdminContactComponent implements OnInit {
  private api = inject(ApiService);

  items     = signal<ContactListItem[]>([]);
  total     = signal(0);
  openCount = signal(0);
  selected  = signal<ContactDetail | null>(null);
  loading   = signal(false);
  busy      = signal(false);
  error     = signal<string | null>(null);

  search = '';
  statusFilter = '';
  replyText = '';

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.error.set(null);
    this.api.adminListContactMessages({
      status: this.statusFilter || undefined,
      search: this.search || undefined,
      page: 1, pageSize: 100
    }).subscribe({
      next: r => {
        this.items.set(r.items || []);
        this.total.set(r.total || 0);
        this.openCount.set(this.items().filter(i => i.status === 'Open').length);
        this.loading.set(false);
      },
      error: err => {
        this.loading.set(false);
        this.error.set(err?.error?.detail || 'Failed to load messages.');
      }
    });
  }

  select(id: string) {
    this.replyText = '';
    this.api.adminGetContactMessage(id).subscribe({
      next: d => this.selected.set(d),
      error: err => this.error.set(err?.error?.detail || 'Failed to load message.')
    });
  }

  sendReply(id: string) {
    if (!this.replyText.trim() || this.busy()) return;
    this.busy.set(true);
    this.api.adminReplyContactMessage(id, this.replyText.trim()).subscribe({
      next: () => {
        this.busy.set(false);
        this.replyText = '';
        this.select(id);
        this.load();
      },
      error: err => {
        this.busy.set(false);
        this.error.set(err?.error?.detail || 'Failed to send reply.');
      }
    });
  }

  closeTicket(id: string) {
    this.busy.set(true);
    this.api.adminCloseContactMessage(id).subscribe({
      next: () => {
        this.busy.set(false);
        this.select(id);
        this.load();
      },
      error: err => {
        this.busy.set(false);
        this.error.set(err?.error?.detail || 'Failed to close ticket.');
      }
    });
  }
}
