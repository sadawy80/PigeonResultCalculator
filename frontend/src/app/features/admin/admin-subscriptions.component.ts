import { Component, signal } from '@angular/core';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-admin-subscriptions',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './admin-subscriptions.component.html',
  styleUrls: ['./admin-subscriptions.component.scss']
})
export class AdminSubscriptionsComponent {
  plans = [
    { name: 'Starter',      price: 29,  maxClubs: 5,   maxMembers: 50,   activeCount: 12 },
    { name: 'Standard',     price: 79,  maxClubs: 20,  maxMembers: 200,  activeCount: 34 },
    { name: 'Professional', price: 149, maxClubs: 50,  maxMembers: 500,  activeCount: 21 },
    { name: 'Enterprise',   price: 299, maxClubs: 999, maxMembers: 9999, activeCount: 9  },
  ];

  subscriptions = signal([
    { id: '1', country: 'Belgium',        plan: 'Professional', clubCount: 24, maxClubs: 50,  billing: 'Annual',  renewsAt: '2025-12-31', status: 'Active' },
    { id: '2', country: 'Netherlands',    plan: 'Standard',     clubCount: 18, maxClubs: 20,  billing: 'Monthly', renewsAt: '2025-05-15', status: 'Active' },
    { id: '3', country: 'United Kingdom', plan: 'Enterprise',   clubCount: 31, maxClubs: 999, billing: 'Annual',  renewsAt: '2025-09-01', status: 'Active' },
    { id: '4', country: 'Germany',        plan: 'Starter',      clubCount: 3,  maxClubs: 5,   billing: 'Monthly', renewsAt: '2025-05-01', status: 'Overdue' },
  ]);
}
