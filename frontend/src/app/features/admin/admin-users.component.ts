import { Component, signal, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { TranslatePipe } from '../../core/i18n';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [DatePipe, FormsModule, TranslatePipe],
  templateUrl: './admin-users.component.html',
  styleUrls: ['./admin-users.component.scss']
})
export class AdminUsersComponent implements OnInit {
  private api = inject(ApiService);

  search     = '';
  roleFilter = '';
  page       = 1;
  pageSize = 10;
  total      = signal(0);
  users      = signal<any[]>([]);
  loading    = signal(false);
  error      = signal<string | null>(null);

  // Role assignment modal
  showRoleModal  = signal(false);
  selectedUser   = signal<any>(null);
  assignRoleVal  = '';
  assignFederation  = '';

  // Limits modal
  showLimitsModal = signal(false);
  limitsUser      = signal<any>(null);
  limitMaxResults: number | null = null;
  limitMaxClubs: number | null = null;

  // Delete modal
  showDeleteModal = signal(false);
  deleteUser      = signal<any>(null);
  deleting        = signal(false);

  readonly roles = ['SuperAdmin', 'FederationManager', 'ClubManager', 'Fancier'];

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.error.set(null);
    this.api.adminGetUsers({
      search: this.search || undefined,
      role: this.roleFilter || undefined,
      page: this.page,
      pageSize: this.pageSize
    }).subscribe({
      next: r => { this.users.set((r as any).users ?? r.items ?? []); this.total.set(r.totalCount); this.loading.set(false); },
      error: () => { this.error.set('Failed to load users.'); this.loading.set(false); }
    });
  }

  onSearch() { this.page = 1; this.load(); }

  toggleUser(id: string) {
    this.api.adminToggleUser(id).subscribe({
      next: r => this.users.update(arr => arr.map(u => u.id === id ? { ...u, isActive: r.isActive } : u)),
      error: () => this.error.set('Failed to toggle user.')
    });
  }

  openRoleModal(user: any) {
    this.selectedUser.set(user);
    this.assignRoleVal = user.role ?? '';
    this.assignFederation = user.FederationId ?? '';
    this.showRoleModal.set(true);
  }

  saveRole() {
    const user = this.selectedUser();
    if (!user) return;
    const roleMap: Record<string, number> = { SuperAdmin: 1, FederationManager: 2, ClubManager: 3, Fancier: 4 };
    this.api.adminAssignRole(user.id, roleMap[this.assignRoleVal], this.assignFederation || undefined).subscribe({
      next: r => {
        this.users.update(arr => arr.map(u => u.id === user.id ? { ...u, role: this.assignRoleVal, isActive: r.isActive } : u));
        this.showRoleModal.set(false);
      },
      error: () => this.error.set('Failed to assign role.')
    });
  }

  openLimitsModal(user: any) {
    this.limitsUser.set(user);
    this.limitMaxResults = user.maxResultsOverride ?? null;
    this.limitMaxClubs   = user.maxClubsOverride ?? null;
    this.showLimitsModal.set(true);
  }

  openDeleteModal(user: any) {
    this.deleteUser.set(user);
    this.showDeleteModal.set(true);
  }

  confirmDelete() {
    const user = this.deleteUser();
    if (!user) return;
    this.deleting.set(true);
    this.api.adminDeleteUser(user.id).subscribe({
      next: () => {
        this.users.update(arr => arr.filter(u => u.id !== user.id));
        this.total.update(t => t - 1);
        this.showDeleteModal.set(false);
        this.deleting.set(false);
      },
      error: () => { this.error.set('Failed to delete user.'); this.deleting.set(false); }
    });
  }

  saveLimits() {
    const user = this.limitsUser();
    if (!user) return;
    this.api.adminSetUserLimits(user.id, this.limitMaxResults, this.limitMaxClubs).subscribe({
      next: r => {
        this.users.update(arr => arr.map(u => u.id === user.id
          ? { ...u, maxResultsOverride: r.maxResults, maxClubsOverride: r.maxClubs } : u));
        this.showLimitsModal.set(false);
      },
      error: () => this.error.set('Failed to save limits.')
    });
  }

  get totalPages() { return Math.ceil(this.total() / this.pageSize); }
  prevPage() { if (this.page > 1) { this.page--; this.load(); } }
  nextPage() { if (this.page < this.totalPages) { this.page++; this.load(); } }
  onPageSizeChange() { this.page = 1; this.load(); }
}
