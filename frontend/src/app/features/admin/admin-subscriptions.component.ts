import { Component, signal, OnInit, OnDestroy, inject } from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { TranslatePipe } from '../../core/i18n';

@Component({
  selector: 'app-admin-subscriptions',
  standalone: true,
  imports: [DatePipe, FormsModule, NgClass, TranslatePipe],
  templateUrl: './admin-subscriptions.component.html',
  styleUrls: ['./admin-subscriptions.component.scss']
})
export class AdminSubscriptionsComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private destroy$ = new Subject<void>();
  private searchChange$ = new Subject<string>();

  subscriptions = signal<any[]>([]);
  total         = signal(0);
  page          = 1;
  pageSize      = 10;
  loading       = signal(false);
  error         = signal<string | null>(null);

  // Filters
  searchValue    = '';
  filterCycle    = '';
  filterDateFrom = '';
  filterDateTo   = '';

  // New subscription modal
  showNewSubModal  = signal(false);
  newSubFederation = '';
  newSubPlan       = '';
  newSubCycle: 'Monthly' | 'Seasonal' | 'Annual' = 'Monthly';
  newSubSaving     = signal(false);
  federations      = signal<any[]>([]);
  availablePlans   = signal<any[]>([]);

  readonly cycles: Array<'Monthly' | 'Seasonal' | 'Annual'> = ['Monthly', 'Seasonal', 'Annual'];
  readonly cycleLabels = { Monthly: '/mo', Seasonal: '/6mo', Annual: '/yr' };

  get totalPages() { return Math.ceil(this.total() / this.pageSize); }

  ngOnInit() {
    this.searchChange$.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => { this.page = 1; this.loadSubscriptions(); });

    this.loadSubscriptions();
    this.loadFederations();
    this.loadPlans();
  }

  ngOnDestroy() { this.destroy$.next(); this.destroy$.complete(); }

  onSearchInput() { this.searchChange$.next(this.searchValue); }

  onFilterChange() { this.page = 1; this.loadSubscriptions(); }

  clearFilters() {
    this.searchValue = '';
    this.filterCycle = '';
    this.filterDateFrom = '';
    this.filterDateTo = '';
    this.page = 1;
    this.loadSubscriptions();
  }

  get hasFilters() {
    return !!(this.searchValue || this.filterCycle || this.filterDateFrom || this.filterDateTo);
  }

  loadSubscriptions() {
    this.loading.set(true);
    const params: any = { page: this.page, pageSize: this.pageSize };
    if (this.searchValue)    params.search       = this.searchValue;
    if (this.filterCycle)    params.billingCycle  = this.filterCycle;
    if (this.filterDateFrom) params.dateFrom      = this.filterDateFrom;
    if (this.filterDateTo)   params.dateTo        = this.filterDateTo;

    this.api.adminGetSubscriptions(params).subscribe({
      next: r => { this.subscriptions.set(r.items ?? []); this.total.set(r.totalCount); this.loading.set(false); },
      error: () => { this.error.set('Failed to load subscriptions.'); this.loading.set(false); }
    });
  }

  loadFederations() {
    this.api.adminGetFederations(1, 200).subscribe({
      next: r => this.federations.set(r.items ?? []),
      error: () => {}
    });
  }

  loadPlans() {
    this.api.adminGetSubscriptionPlans().subscribe({
      next: plans => this.availablePlans.set(plans ?? []),
      error: () => {}
    });
  }

  allPlansForNewSub() {
    return this.availablePlans().filter((p: any) =>
      p.type === 'Federation' && p.billingCycle === this.newSubCycle && p.isActive
    );
  }

  createSubscription() {
    if (!this.newSubFederation || !this.newSubPlan) return;
    this.newSubSaving.set(true);
    const fed = this.federations().find(f => f.id === this.newSubFederation);
    this.api.adminCreateSubscription({
      federationId: this.newSubFederation,
      federationName: fed?.name ?? '',
      planId: this.newSubPlan,
      billingCycle: this.newSubCycle,
      amountPaid: 0
    }).subscribe({
      next: () => {
        this.showNewSubModal.set(false);
        this.newSubFederation = '';
        this.newSubPlan = '';
        this.newSubCycle = 'Monthly';
        this.newSubSaving.set(false);
        this.loadSubscriptions();
      },
      error: () => { this.error.set('Failed to create subscription.'); this.newSubSaving.set(false); }
    });
  }

  prevPage() { if (this.page > 1) { this.page--; this.loadSubscriptions(); } }
  nextPage() { if (this.page < this.totalPages) { this.page++; this.loadSubscriptions(); } }
  onPageSizeChange() { this.page = 1; this.loadSubscriptions(); }
}
