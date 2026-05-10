import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-admin-fanciers',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="pr-page-header">
      <h1 class="pr-page-header__title">Fanciers</h1>
      <p class="pr-page-header__subtitle">{{ total() }} fanciers from race results</p>
    </div>

    @if (error()) {
      <div class="pr-alert pr-alert--error mb-4">{{ error() }}</div>
    }

    <div class="pr-card mb-4">
      <div class="flex flex-wrap gap-4 items-end">
        <div class="pr-form-group" style="flex:2;min-width:180px">
          <label class="pr-label">Search name</label>
          <input class="pr-input" [(ngModel)]="search" placeholder="Fancier name…" (keyup.enter)="loadPage(1)">
        </div>
        <div class="pr-form-group" style="flex:1;min-width:140px">
          <label class="pr-label">Linked</label>
          <select class="pr-select" [(ngModel)]="linkedFilter" (change)="loadPage(1)">
            <option value="">All</option>
            <option value="true">Linked</option>
            <option value="false">Unlinked</option>
          </select>
        </div>
        <button class="pr-btn pr-btn--primary pr-btn--field" (click)="loadPage(1)" [disabled]="loading()">Search</button>
        <button class="pr-btn pr-btn--ghost pr-btn--field" (click)="reset()">Reset</button>
      </div>
    </div>

    <div class="pr-card">
      @if (loading()) {
        <div class="text-center py-8 text-muted">Loading…</div>
      } @else {
        <div class="pr-table-wrapper">
          <table class="pr-table">
            <thead>
              <tr>
                <th>Name</th><th>Club</th><th>Federation</th><th>Country</th>
                <th>Linked To</th><th style="text-align:right">Actions</th>
              </tr>
            </thead>
            <tbody>
              @if (items().length === 0) {
                <tr><td colspan="6" class="text-center py-6 text-muted">No fanciers found.</td></tr>
              }
              @for (f of items(); track f.id) {
                <tr>
                  <td style="font-weight:600">{{ f.name }}</td>
                  <td class="text-muted text-sm">{{ f.clubName }}</td>
                  <td class="text-muted text-sm">{{ f.federationName || '—' }}</td>
                  <td class="text-muted text-sm">{{ f.country || '—' }}</td>
                  <td>
                    @if (f.isLinked) {
                      <div style="font-size:0.85rem">
                        <div style="font-weight:600">{{ f.linkedUserName }}</div>
                        <div class="text-muted">{{ f.linkedUserEmail }}</div>
                      </div>
                    } @else {
                      <span class="pr-badge pr-badge--warning">Unlinked</span>
                    }
                  </td>
                  <td style="text-align:right">
                    @if (!f.isLinked) {
                      <button class="pr-btn pr-btn--primary pr-btn--sm" (click)="openLinkModal(f)">Link</button>
                    } @else {
                      <button class="pr-btn pr-btn--ghost pr-btn--sm" style="color:var(--pr-danger)" (click)="unlink(f)">Unlink</button>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <div class="pagination-row">
          <span class="text-muted text-sm">{{ total() }} fanciers · page {{ page() }} of {{ totalPages() }}</span>
          <div class="flex gap-2 items-center">
            <select class="pr-select" style="width:auto" [(ngModel)]="pageSize" (ngModelChange)="onPageSizeChange()">
              <option [ngValue]="10">10 / page</option>
              <option [ngValue]="25">25 / page</option>
              <option [ngValue]="50">50 / page</option>
              <option [ngValue]="100">100 / page</option>
            </select>
            <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() === 1" (click)="loadPage(page() - 1)">Prev</button>
            <button class="pr-btn pr-btn--ghost pr-btn--sm" [disabled]="page() === totalPages()" (click)="loadPage(page() + 1)">Next</button>
          </div>
        </div>
      }
    </div>

    <!-- Link modal -->
    @if (showLinkModal()) {
      <div class="pr-modal-backdrop" (click)="showLinkModal.set(false)">
        <div class="pr-modal" (click)="$event.stopPropagation()">
          <h3 class="pr-modal__title">Link Fancier — {{ linkTarget()?.name }}</h3>
          <p class="text-muted text-sm mt-1">Search for a registered user to link this fancier record to.</p>
          <div class="flex gap-2 mt-4">
            <input class="pr-input" style="flex:1" [(ngModel)]="userSearch" placeholder="Search by name or email…" (keyup.enter)="searchUsers()">
            <button class="pr-btn pr-btn--primary pr-btn--sm" (click)="searchUsers()" [disabled]="searchingUsers()">Search</button>
          </div>
          @if (userResults().length > 0) {
            <div class="mt-3" style="max-height:200px;overflow-y:auto;border:1px solid var(--pr-border);border-radius:var(--pr-radius)">
              @for (u of userResults(); track u.id) {
                <div class="flex justify-between items-center p-3" style="border-bottom:1px solid var(--pr-border);cursor:pointer"
                  [style.background]="selectedUser()?.id === u.id ? 'var(--pr-primary-10)' : ''"
                  (click)="selectedUser.set(u)">
                  <div>
                    <div style="font-weight:600;font-size:0.875rem">{{ u.firstName }} {{ u.lastName }}</div>
                    <div class="text-muted text-sm">{{ u.email }}</div>
                  </div>
                  @if (selectedUser()?.id === u.id) {
                    <span class="pr-badge pr-badge--success">Selected</span>
                  }
                </div>
              }
            </div>
          }
          <div class="flex gap-3 justify-end mt-6">
            <button class="pr-btn pr-btn--ghost" (click)="showLinkModal.set(false)">Cancel</button>
            <button class="pr-btn pr-btn--primary" [disabled]="!selectedUser() || linking()" (click)="confirmLink()">
              {{ linking() ? 'Linking…' : 'Link' }}
            </button>
          </div>
        </div>
      </div>
    }
  `
})
export class AdminFanciersComponent implements OnInit {
  private api = inject(ApiService);

  search       = '';
  linkedFilter = '';
  loading      = signal(false);
  error        = signal<string | null>(null);
  items        = signal<any[]>([]);
  total        = signal(0);
  page         = signal(1);
  pageSize = 10;

  showLinkModal  = signal(false);
  linkTarget     = signal<any>(null);
  userSearch     = '';
  userResults    = signal<any[]>([]);
  searchingUsers = signal(false);
  selectedUser   = signal<any>(null);
  linking        = signal(false);

  totalPages = () => Math.max(1, Math.ceil(this.total() / this.pageSize));
  onPageSizeChange() { this.page.set(1); this.loadPage(1); }

  ngOnInit() { this.loadPage(1); }

  loadPage(p: number) {
    this.page.set(p);
    this.loading.set(true);
    this.error.set(null);
    const isLinked = this.linkedFilter === 'true' ? true : this.linkedFilter === 'false' ? false : undefined;
    this.api.adminGetFanciers({ search: this.search || undefined, isLinked, page: p, pageSize: this.pageSize }).subscribe({
      next: r => { this.items.set(r?.items ?? []); this.total.set(r?.totalCount ?? 0); this.loading.set(false); },
      error: () => { this.error.set('Failed to load fanciers.'); this.loading.set(false); }
    });
  }

  reset() { this.search = ''; this.linkedFilter = ''; this.loadPage(1); }

  openLinkModal(f: any) {
    this.linkTarget.set(f);
    this.userSearch = f.name;
    this.userResults.set([]);
    this.selectedUser.set(null);
    this.showLinkModal.set(true);
    this.searchUsers();
  }

  searchUsers() {
    if (!this.userSearch.trim()) return;
    this.searchingUsers.set(true);
    this.api.adminGetUsers({ search: this.userSearch, role: '4', page: 1, pageSize: 10 }).subscribe({
      next: r => { this.userResults.set((r as any).users ?? r?.items ?? []); this.searchingUsers.set(false); },
      error: () => this.searchingUsers.set(false)
    });
  }

  confirmLink() {
    const f = this.linkTarget();
    const u = this.selectedUser();
    if (!f || !u) return;
    this.linking.set(true);
    this.api.adminLinkFancier(f.id, u.id, `${u.firstName} ${u.lastName}`.trim(), u.email).subscribe({
      next: () => {
        this.items.update(list => list.map(x => x.id === f.id
          ? { ...x, isLinked: true, linkedUserId: u.id, linkedUserName: `${u.firstName} ${u.lastName}`.trim(), linkedUserEmail: u.email }
          : x));
        this.showLinkModal.set(false);
        this.linking.set(false);
      },
      error: () => { this.error.set('Failed to link fancier.'); this.linking.set(false); }
    });
  }

  unlink(f: any) {
    this.api.adminUnlinkFancier(f.id).subscribe({
      next: () => this.items.update(list => list.map(x => x.id === f.id
        ? { ...x, isLinked: false, linkedUserId: null, linkedUserName: null, linkedUserEmail: null }
        : x)),
      error: () => this.error.set('Failed to unlink fancier.')
    });
  }
}
