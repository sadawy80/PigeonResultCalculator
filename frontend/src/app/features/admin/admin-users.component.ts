import { Component, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './admin-users.component.html',
  styleUrls: ['./admin-users.component.scss']
})
export class AdminUsersComponent implements OnInit {
  search     = '';
  roleFilter = '';
  users      = signal<any[]>([]);

  ngOnInit() {
    this.users.set([
      { id: '1', fullName: 'Alice Martin',   email: 'alice@brc.be',    role: 'ClubManager',    club: 'Brussels Racing Club', joinedAt: '2024-03-01', isActive: true },
      { id: '2', fullName: 'Bob Janssen',    email: 'bob@ant.be',      role: 'Fancier',        club: 'Antwerp Flyers',       joinedAt: '2024-05-14', isActive: true },
      { id: '3', fullName: 'Clara van Dijk', email: 'clara@amw.nl',    role: 'ClubManager',    club: 'Amsterdam Wings',      joinedAt: '2023-11-22', isActive: true },
      { id: '4', fullName: 'David Hughes',   email: 'david@ldn.co.uk', role: 'CountryManager', country: 'United Kingdom',    joinedAt: '2023-08-10', isActive: true },
      { id: '5', fullName: 'Emma Peeters',   email: 'emma@be.org',     role: 'CountryManager', country: 'Belgium',           joinedAt: '2024-01-15', isActive: false },
    ]);
  }

  toggleUser(id: string) {
    this.users.update(arr => arr.map(u => u.id === id ? { ...u, isActive: !u.isActive } : u));
  }
}
