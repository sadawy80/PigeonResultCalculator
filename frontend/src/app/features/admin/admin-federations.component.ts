import { Component, signal, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-admin-federations',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './admin-federations.component.html',
  styleUrls: ['./admin-federations.component.scss']
})
export class AdminFederationsComponent implements OnInit {
  private api = inject(ApiService);

  showForm   = signal(false);
  countries  = signal<any[]>([]);
  loading    = signal(false);
  saving     = signal(false);
  error      = signal<string | null>(null);
  newCountry = { name: '', code: '', slug: '' };

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.error.set(null);
    this.api.adminGetCountries(1, 100).subscribe({
      next: r => { this.countries.set(r.items ?? []); this.loading.set(false); },
      error: () => { this.error.set('Failed to load countries.'); this.loading.set(false); }
    });
  }

  saveCountry() {
    if (!this.newCountry.name || !this.newCountry.code || !this.newCountry.slug) return;
    this.saving.set(true);
    this.api.adminCreateCountry(this.newCountry.name, this.newCountry.code, this.newCountry.slug).subscribe({
      next: r => {
        this.countries.update(arr => [...arr, { ...r, clubCount: 0, isActive: true }]);
        this.newCountry = { name: '', code: '', slug: '' };
        this.showForm.set(false);
        this.saving.set(false);
      },
      error: () => { this.error.set('Failed to create country.'); this.saving.set(false); }
    });
  }

  toggleCountry(id: string) {
    this.api.adminToggleCountry(id).subscribe({
      next: r => this.countries.update(arr => arr.map(c => c.id === id ? { ...c, isActive: r.isActive } : c)),
      error: () => this.error.set('Failed to toggle country.')
    });
  }
}
