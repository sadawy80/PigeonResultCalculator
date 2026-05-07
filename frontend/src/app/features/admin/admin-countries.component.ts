import { Component, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-admin-countries',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './admin-countries.component.html',
  styleUrls: ['./admin-countries.component.scss']
})
export class AdminCountriesComponent implements OnInit {
  showForm   = signal(false);
  countries  = signal<any[]>([]);
  newCountry = { name: '', code: '', slug: '' };

  ngOnInit() {
    this.countries.set([
      { id: '1', name: 'Belgium',        code: 'BE', clubCount: 24, isActive: true },
      { id: '2', name: 'Netherlands',    code: 'NL', clubCount: 18, isActive: true },
      { id: '3', name: 'United Kingdom', code: 'GB', clubCount: 31, isActive: true },
      { id: '4', name: 'Germany',        code: 'DE', clubCount: 11, isActive: false },
    ]);
  }

  saveCountry() {
    this.countries.update(arr => [...arr, { id: crypto.randomUUID(), ...this.newCountry, clubCount: 0, isActive: true }]);
    this.newCountry = { name: '', code: '', slug: '' };
    this.showForm.set(false);
  }

  deactivate(id: string) {
    this.countries.update(arr => arr.map(c => c.id === id ? { ...c, isActive: false } : c));
  }
}
