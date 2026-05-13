import { Component, signal, OnInit, inject, computed } from '@angular/core';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { getCountriesWithFlags, CountryOption } from '../../core/constants/countries';
import { TranslatePipe } from '../../core/i18n';

@Component({
  selector: 'app-admin-federations',
  standalone: true,
  imports: [FormsModule, NgClass, TranslatePipe],
  templateUrl: './admin-federations.component.html',
  styleUrls: ['./admin-federations.component.scss']
})
export class AdminFederationsComponent implements OnInit {
  private api = inject(ApiService);

  federations  = signal<any[]>([]);
  loading    = signal(false);
  saving     = signal(false);
  error      = signal<string | null>(null);

  // Search / filter
  listSearch  = '';
  listStatus  = '';
  listPage    = signal(1);
  listSize    = signal(10);
  listFiltered = signal<any[]>([]);
  listPaged    = computed(() => {
    const start = (this.listPage() - 1) * this.listSize();
    return this.listFiltered().slice(start, start + this.listSize());
  });
  listTotalPages = computed(() => Math.max(1, Math.ceil(this.listFiltered().length / this.listSize())));

  // Add modal
  showAddModal = signal(false);
  newFed = { name: '', code: '', slug: '' };

  // Country combobox for Add modal
  allCountryOptions: CountryOption[] = getCountriesWithFlags();
  fedSearch     = signal('');
  fedDropOpen   = signal(false);
  selectedFed   = signal<CountryOption | null>(null);
  filteredFeds = computed(() => {
    const q = this.fedSearch().toLowerCase().trim();
    return q ? this.allCountryOptions.filter(c => c.name.toLowerCase().includes(q)) : this.allCountryOptions;
  });
  fedDisplay = computed(() => this.selectedFed()?.name ?? this.fedSearch());

  // Delete modal
  showDeleteModal   = signal(false);
  deleteTarget      = signal<any>(null);
  deleting          = signal(false);

  openDelete(fed: any) { this.deleteTarget.set(fed); this.showDeleteModal.set(true); }
  closeDelete() { this.showDeleteModal.set(false); this.deleteTarget.set(null); }

  confirmDelete() {
    const fed = this.deleteTarget();
    if (!fed) return;
    this.deleting.set(true);
    this.api.adminDeleteFederation(fed.id).subscribe({
      next: () => {
        this.federations.update(arr => arr.filter(c => c.id !== fed.id));
        this.listFiltered.update(arr => arr.filter(c => c.id !== fed.id));
        this.deleting.set(false);
        this.closeDelete();
      },
      error: (e) => { this.error.set(e?.error?.message ?? 'Failed to delete federation.'); this.deleting.set(false); this.closeDelete(); }
    });
  }

  // Manager modal
  showManagerModal = signal(false);
  managerTarget    = signal<any>(null);
  managerEmail     = '';
  managerLookup    = signal<any>(null);
  managerLookupErr = signal<string | null>(null);
  managerSaving    = signal(false);
  managerLooking   = signal(false);

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.error.set(null);
    this.api.adminGetFederations(1, 500).subscribe({
      next: r => {
        this.federations.set((r as any).federations ?? r.items ?? []);
        this.applyFilter();
        this.loading.set(false);
      },
      error: () => { this.error.set('Failed to load federations.'); this.loading.set(false); }
    });
  }

  applyFilter() {
    const q = this.listSearch.toLowerCase().trim();
    const s = this.listStatus;
    this.listFiltered.set(this.federations().filter(f => {
      if (q && !f.name?.toLowerCase().includes(q)
             && !f.code?.toLowerCase().includes(q)
             && !f.managerEmail?.toLowerCase().includes(q)) return false;
      if (s === 'true'  && !f.isActive) return false;
      if (s === 'false' && f.isActive)  return false;
      return true;
    }));
    this.listPage.set(1);
  }

  onListSearch() { this.applyFilter(); }
  onListSizeChange() { this.listPage.set(1); }

  openAdd() {
    this.newFed = { name: '', code: '', slug: '' };
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
    this.newFed.name = c.name;
    this.newFed.code = c.code;
    this.newFed.slug = c.name.toLowerCase().replace(/\s+/g, '-');
  }

  saveFederation() {
    if (!this.newFed.name || !this.newFed.code || !this.newFed.slug) return;
    this.saving.set(true);
    this.api.adminCreateFederation(this.newFed.name, this.newFed.code, this.newFed.slug).subscribe({
      next: () => {
        this.showAddModal.set(false);
        this.saving.set(false);
        this.load();
      },
      error: (e) => { this.error.set(e?.error?.message ?? 'Failed to create federation.'); this.saving.set(false); }
    });
  }

  toggleFederation(id: string) {
    this.api.adminToggleFederation(id).subscribe({
      next: r => {
        this.federations.update(arr => arr.map(c => c.id === id ? { ...c, isActive: r.isActive } : c));
        this.listFiltered.update(arr => arr.map(c => c.id === id ? { ...c, isActive: r.isActive } : c));
      },
      error: () => this.error.set('Failed to toggle federation.')
    });
  }

  openManager(fed: any) {
    this.managerTarget.set(fed);
    this.managerEmail = '';
    this.managerLookup.set(null);
    this.managerLookupErr.set(null);
    this.showManagerModal.set(true);
  }
  closeManager() { this.showManagerModal.set(false); this.managerTarget.set(null); }

  lookupUser() {
    if (!this.managerEmail) return;
    this.managerLooking.set(true);
    this.managerLookupErr.set(null);
    this.managerLookup.set(null);
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

  assignManager() {
    const fed = this.managerTarget();
    if (!fed) return;
    this.managerSaving.set(true);
    const email = this.managerEmail;
    this.api.adminAssignFederationManager(fed.id, email).subscribe({
      next: () => {
        this.federations.update(arr => arr.map(c => c.id === fed.id ? { ...c, managerEmail: email } : c));
        this.managerSaving.set(false);
        this.closeManager();
      },
      error: (e) => { this.managerLookupErr.set(e?.error?.message ?? 'Failed to assign manager.'); this.managerSaving.set(false); }
    });
  }

  roleName(role: number): string {
    const map: Record<number, string> = { 1: 'Super Admin', 2: 'Federation Manager', 3: 'Club Manager', 4: 'Fancier' };
    return map[role] ?? `Role ${role}`;
  }
}
