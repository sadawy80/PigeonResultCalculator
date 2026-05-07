import { Component, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-admin-clubs',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './admin-clubs.component.html',
  styleUrls: ['./admin-clubs.component.scss']
})
export class AdminClubsComponent implements OnInit {
  search        = '';
  countryFilter = '';
  clubs         = signal<any[]>([]);

  filteredClubs() {
    return this.clubs().filter(c =>
      (!this.search || c.name.toLowerCase().includes(this.search.toLowerCase())) &&
      (!this.countryFilter || c.countryCode === this.countryFilter)
    );
  }

  ngOnInit() {
    this.clubs.set([
      { id: '1', name: 'Brussels Racing Club', code: 'BRC', country: 'Belgium',        countryCode: 'BE', memberCount: 45, plan: 'Professional', isActive: true },
      { id: '2', name: 'Antwerp Flyers',        code: 'ANT', country: 'Belgium',        countryCode: 'BE', memberCount: 32, plan: 'Standard',     isActive: true },
      { id: '3', name: 'Amsterdam Wings',       code: 'AMW', country: 'Netherlands',    countryCode: 'NL', memberCount: 28, plan: 'Standard',     isActive: true },
      { id: '4', name: 'London Racers',         code: 'LDN', country: 'United Kingdom', countryCode: 'GB', memberCount: 67, plan: 'Enterprise',   isActive: true },
      { id: '5', name: 'Ghent Pigeons',         code: 'GHT', country: 'Belgium',        countryCode: 'BE', memberCount: 19, plan: 'Starter',      isActive: false },
    ]);
  }
}
