import { Component, signal, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-admin-subscriptions',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './admin-subscriptions.component.html',
  styleUrls: ['./admin-subscriptions.component.scss']
})
export class AdminSubscriptionsComponent implements OnInit {
  private api = inject(ApiService);

  plans         = signal<any[]>([]);
  subscriptions = signal<any[]>([]);
  countries     = signal<any[]>([]);
  total         = signal(0);
  page          = 1;
  pageSize      = 20;
  loading       = signal(false);
  error         = signal<string | null>(null);

  // New subscription modal
  showModal       = signal(false);
  newSubCountry   = '';
  newSubPlan      = '';
  newSubCycle     = 1; // 1=Monthly, 2=Annual, 3=Seasonal
  saving          = signal(false);

  readonly billingCycles = [
    { value: 1, label: 'Monthly' },
    { value: 2, label: 'Annual' },
    { value: 3, label: 'Seasonal (Apr–Sep)' }
  ];

  ngOnInit() {
    this.loadPlans();
    this.loadSubscriptions();
    this.loadCountries();
  }

  loadPlans() {
    this.api.adminGetSubscriptionPlans().subscribe({
      next: p => this.plans.set(p),
      error: () => this.error.set('Failed to load plans.')
    });
  }

  loadSubscriptions() {
    this.loading.set(true);
    this.api.adminGetSubscriptions(this.page, this.pageSize).subscribe({
      next: r => { this.subscriptions.set(r.items); this.total.set(r.totalCount); this.loading.set(false); },
      error: () => { this.error.set('Failed to load subscriptions.'); this.loading.set(false); }
    });
  }

  loadCountries() {
    this.api.adminGetCountries(1, 100).subscribe({
      next: r => this.countries.set(r.items ?? [])
    });
  }

  createSubscription() {
    if (!this.newSubCountry || !this.newSubPlan) return;
    this.saving.set(true);
    this.api.adminCreateSubscription(this.newSubCountry, this.newSubPlan, this.newSubCycle).subscribe({
      next: () => {
        this.showModal.set(false);
        this.newSubCountry = ''; this.newSubPlan = ''; this.newSubCycle = 1;
        this.saving.set(false);
        this.loadSubscriptions();
      },
      error: () => { this.error.set('Failed to create subscription.'); this.saving.set(false); }
    });
  }

  get totalPages() { return Math.ceil(this.total() / this.pageSize); }
  prevPage() { if (this.page > 1) { this.page--; this.loadSubscriptions(); } }
  nextPage() { if (this.page < this.totalPages) { this.page++; this.loadSubscriptions(); } }
}
