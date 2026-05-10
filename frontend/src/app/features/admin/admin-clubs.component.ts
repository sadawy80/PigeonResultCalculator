import { Component, signal, OnInit, inject, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { getCountriesWithFlags, CountryOption } from '../../core/constants/countries';

@Component({
  selector: 'app-admin-clubs',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './admin-clubs.component.html',
  styleUrls: ['./admin-clubs.component.scss']
})
export class AdminClubsComponent implements OnInit {
  private api = inject(ApiService);

  search            = '';
  federationFilter  = '';
  page          = 1;
  pageSize = 10;
  total         = signal(0);
  clubs         = signal<any[]>([]);
  federations   = signal<any[]>([]);
  loading       = signal(false);
  error         = signal<string | null>(null);

  // Add club modal
  showAddModal = signal(false);
  addSaving    = signal(false);
  newClub      = { federationId: '', name: '', code: '', city: '' };

  // Country combobox for Add Club modal
  allCountryOptions: CountryOption[] = getCountriesWithFlags();
  fedSearch     = signal('');
  fedDropOpen   = signal(false);
  selectedFed   = signal<CountryOption | null>(null);
  filteredFeds = computed(() => {
    const q = this.fedSearch().toLowerCase().trim();
    return q ? this.allCountryOptions.filter(c => c.name.toLowerCase().includes(q)) : this.allCountryOptions;
  });
  fedDisplay = computed(() => this.selectedFed()?.name ?? this.fedSearch());

  // Assign manager modal
  showManagerModal  = signal(false);
  managerTarget     = signal<any>(null);
  managerEmail      = '';
  managerLookup     = signal<any>(null);
  managerLookupErr  = signal<string | null>(null);
  managerLooking    = signal(false);
  managerSaving     = signal(false);

  // Conflict state (user already manages another club)
  conflict = signal<{ clubId: string; clubName: string; userId: string; email: string; fullName: string } | null>(null);

  // Subscription expiry modal
  showExpiryModal  = signal(false);
  expiryTarget     = signal<any>(null);
  expiryDate       = '';
  expirySaving     = signal(false);
  expiryError      = signal<string | null>(null);

  openExpiry(club: any) {
    this.expiryTarget.set(club);
    this.expiryDate  = club.subscriptionExpiresAt ? club.subscriptionExpiresAt.substring(0, 10) : '';
    this.expiryError.set(null);
    this.showExpiryModal.set(true);
  }
  closeExpiry() { this.showExpiryModal.set(false); this.expiryTarget.set(null); }

  saveExpiry() {
    const club = this.expiryTarget();
    if (!club) return;
    this.expirySaving.set(true);
    this.expiryError.set(null);
    const expiresAt = this.expiryDate || null;
    this.api.adminSetClubSubscriptionExpiry(club.id, expiresAt).subscribe({
      next: () => {
        this.clubs.update(arr => arr.map(c => c.id === club.id ? { ...c, subscriptionExpiresAt: expiresAt } : c));
        this.expirySaving.set(false);
        this.closeExpiry();
      },
      error: (e: any) => { this.expiryError.set(e?.error?.message ?? 'Failed to save.'); this.expirySaving.set(false); }
    });
  }

  expiryStatus(club: any): 'none' | 'active' | 'expiring' | 'expired' {
    if (!club.subscriptionExpiresAt) return 'none';
    const exp = new Date(club.subscriptionExpiresAt);
    const now = new Date();
    if (exp < now) return 'expired';
    const daysLeft = (exp.getTime() - now.getTime()) / 86400000;
    return daysLeft <= 30 ? 'expiring' : 'active';
  }

  // Delete modal
  showDeleteModal = signal(false);
  deleteTarget    = signal<any>(null);
  deleting        = signal(false);

  openDelete(club: any) { this.deleteTarget.set(club); this.showDeleteModal.set(true); }
  closeDelete() { this.showDeleteModal.set(false); this.deleteTarget.set(null); }

  confirmDelete() {
    const club = this.deleteTarget();
    if (!club) return;
    this.deleting.set(true);
    this.api.adminDeleteClub(club.id).subscribe({
      next: () => {
        this.clubs.update(arr => arr.filter(c => c.id !== club.id));
        this.total.update(n => n - 1);
        this.deleting.set(false);
        this.closeDelete();
      },
      error: (e) => { this.error.set(e?.error?.message ?? 'Failed to delete club.'); this.deleting.set(false); this.closeDelete(); }
    });
  }

  ngOnInit() {
    this.load();
    this.api.getPublicFederations().subscribe({
      next: (feds: any[]) => this.federations.set(feds ?? []),
      error: () => {}
    });
  }

  load() {
    this.loading.set(true);
    this.error.set(null);
    this.api.adminGetClubs({
      search: this.search || undefined,
      FederationId: this.federationFilter || undefined,
      page: this.page,
      pageSize: this.pageSize
    }).subscribe({
      next: r => { this.clubs.set((r as any).clubs ?? r.items ?? []); this.total.set(r.totalCount); this.loading.set(false); },
      error: () => { this.error.set('Failed to load clubs.'); this.loading.set(false); }
    });
  }

  onSearch() { this.page = 1; this.load(); }

  toggleClub(id: string) {
    this.api.adminToggleClub(id).subscribe({
      next: r => this.clubs.update(arr => arr.map(c => c.id === id ? { ...c, isActive: r.isActive } : c)),
      error: () => this.error.set('Failed to toggle club.')
    });
  }

  get totalPages() { return Math.max(1, Math.ceil(this.total() / this.pageSize)); }
  prevPage() { if (this.page > 1) { this.page--; this.load(); } }
  nextPage() { if (this.page < this.totalPages) { this.page++; this.load(); } }
  onPageSizeChange() { this.page = 1; this.load(); }

  // ── Add club modal ────────────────────────────────────────────────

  openAdd() {
    this.newClub = { federationId: '', name: '', code: '', city: '' };
    this.selectedFed.set(null);
    this.fedSearch.set('');
    this.showAddModal.set(true);
  }
  closeAdd() { this.showAddModal.set(false); }

  onFedSearch(val: string) { this.fedSearch.set(val); this.selectedFed.set(null); }
  onFedBlur()  { setTimeout(() => this.fedDropOpen.set(false), 150); }
  selectFed(c: CountryOption) {
    this.selectedFed.set(c);
    this.fedSearch.set('');
    this.fedDropOpen.set(false);
    this.newClub.code = c.code;
  }

  saveClub() {
    if (!this.newClub.name || !this.newClub.code) return;
    this.addSaving.set(true);
    this.api.adminCreateClub({
      federationId: this.newClub.federationId || undefined,
      name: this.newClub.name,
      code: this.newClub.code,
      city: this.newClub.city || undefined
    }).subscribe({
      next: () => { this.addSaving.set(false); this.showAddModal.set(false); this.load(); },
      error: (e) => { this.error.set(e?.error?.message ?? 'Failed to create club.'); this.addSaving.set(false); }
    });
  }

  // ── Assign manager modal ──────────────────────────────────────────

  openManager(club: any) {
    this.managerTarget.set(club);
    this.managerEmail = '';
    this.managerLookup.set(null);
    this.managerLookupErr.set(null);
    this.conflict.set(null);
    this.showManagerModal.set(true);
  }
  closeManager() { this.showManagerModal.set(false); this.managerTarget.set(null); this.conflict.set(null); }

  lookupUser() {
    if (!this.managerEmail) return;
    this.managerLooking.set(true);
    this.managerLookupErr.set(null);
    this.managerLookup.set(null);
    this.conflict.set(null);
    this.api.adminGetUsers({ search: this.managerEmail, page: 1, pageSize: 5 }).subscribe({
      next: (r: any) => {
        const users: any[] = r.users ?? r.items ?? [];
        const match = users.find((u: any) => (u.email ?? '').toLowerCase() === this.managerEmail.toLowerCase());
        if (match) this.managerLookup.set(match);
        else this.managerLookupErr.set(`No user found with email "${this.managerEmail}".`);
        this.managerLooking.set(false);
      },
      error: () => { this.managerLookupErr.set('Failed to look up user.'); this.managerLooking.set(false); }
    });
  }

  assignManager(force = false) {
    const club = this.managerTarget();
    if (!club) return;
    this.managerSaving.set(true);
    this.managerLookupErr.set(null);
    this.api.adminAssignClubManager(club.id, this.managerEmail, force).subscribe({
      next: () => {
        this.managerSaving.set(false);
        this.closeManager();
        this.load();
      },
      error: (e: any) => {
        // HTTP 409 Conflict — user is already managing another club
        if (e.status === 409 && e.error?.data?.conflict) {
          const d = e.error.data;
          this.conflict.set({ clubId: d.conflictClubId, clubName: d.conflictClubName, userId: d.userId, email: d.email, fullName: d.fullName });
          this.managerSaving.set(false);
        } else {
          this.managerLookupErr.set(e?.error?.message ?? 'Failed to assign manager.');
          this.managerSaving.set(false);
        }
      }
    });
  }

  roleName(role: number): string {
    const map: Record<number, string> = { 1: 'Super Admin', 2: 'Federation Manager', 3: 'Club Manager', 4: 'Fancier' };
    return map[role] ?? `Role ${role}`;
  }

  fedName(id: string): string {
    return this.federations().find(f => f.id === id)?.name ?? id;
  }
}
