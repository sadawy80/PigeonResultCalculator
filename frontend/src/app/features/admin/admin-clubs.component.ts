import { Component, signal, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-admin-clubs',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './admin-clubs.component.html',
  styleUrls: ['./admin-clubs.component.scss']
})
export class AdminClubsComponent implements OnInit {
  private api = inject(ApiService);

  search        = '';
  countryFilter = '';
  page          = 1;
  pageSize      = 20;
  total         = signal(0);
  clubs         = signal<any[]>([]);
  loading       = signal(false);
  error         = signal<string | null>(null);

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.error.set(null);
    this.api.adminGetClubs({
      search: this.search || undefined,
      FederationId: this.countryFilter || undefined,
      page: this.page,
      pageSize: this.pageSize
    }).subscribe({
      next: r => { this.clubs.set(r.items); this.total.set(r.totalCount); this.loading.set(false); },
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

  get totalPages() { return Math.ceil(this.total() / this.pageSize); }
  prevPage() { if (this.page > 1) { this.page--; this.load(); } }
  nextPage() { if (this.page < this.totalPages) { this.page++; this.load(); } }
}
