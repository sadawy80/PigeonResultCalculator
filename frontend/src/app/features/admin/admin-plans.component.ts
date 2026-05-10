import { Component, signal, computed, OnInit, inject } from '@angular/core';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

interface PlanCard {
  id: string;
  name: string;
  description: string | null;
  type: string;
  billingCycle: string;
  price: number;
  currency: string;
  maxClubs: number;
  maxResultsPerClub: number;
  isActive: boolean;
  isHighlighted: boolean;
  sortOrder: number;
  features: string[];
}

interface TierGroup {
  name: string;
  description: string | null;
  isHighlighted: boolean;
  sortOrder: number;
  monthly: PlanCard | null;
  seasonal: PlanCard | null;
  annual: PlanCard | null;
}

function parsePlanCard(p: any): PlanCard {
  let features: string[] = [];
  if (p.features) {
    try { features = JSON.parse(p.features); } catch { features = []; }
  }
  return {
    id: p.id, name: p.name, description: p.description ?? null,
    type: p.type, billingCycle: p.billingCycle,
    price: p.price, currency: p.currency ?? 'EUR',
    maxClubs: p.maxClubs ?? 0, maxResultsPerClub: p.maxResultsPerClub ?? 0,
    isActive: p.isActive, isHighlighted: p.isHighlighted,
    sortOrder: p.sortOrder, features
  };
}

function groupByTier(cards: PlanCard[]): TierGroup[] {
  const map = new Map<string, TierGroup>();
  for (const c of cards) {
    if (!map.has(c.name)) {
      map.set(c.name, { name: c.name, description: c.description, isHighlighted: c.isHighlighted, sortOrder: c.sortOrder, monthly: null, seasonal: null, annual: null });
    }
    const tier = map.get(c.name)!;
    if (c.billingCycle === 'Monthly')  tier.monthly  = c;
    if (c.billingCycle === 'Seasonal') tier.seasonal = c;
    if (c.billingCycle === 'Annual')   tier.annual   = c;
    if (c.isHighlighted) tier.isHighlighted = true;
    if (c.description)   tier.description   = c.description;
  }
  return [...map.values()].sort((a, b) => a.sortOrder - b.sortOrder);
}

@Component({
  selector: 'app-admin-plans',
  standalone: true,
  imports: [FormsModule, NgClass],
  templateUrl: './admin-plans.component.html',
  styleUrls: ['./admin-plans.component.scss']
})
export class AdminPlansComponent implements OnInit {
  private api = inject(ApiService);

  rawPlans     = signal<PlanCard[]>([]);
  loading      = signal(false);
  error        = signal<string | null>(null);

  selectedType  = signal<'Federation' | 'Club'>('Federation');
  selectedCycle = signal<'Monthly' | 'Seasonal' | 'Annual'>('Monthly');

  federationTiers = computed(() => groupByTier(this.rawPlans().filter(p => p.type === 'Federation')));
  clubTiers       = computed(() => groupByTier(this.rawPlans().filter(p => p.type === 'Club')));
  activeTiers     = computed(() => this.selectedType() === 'Federation' ? this.federationTiers() : this.clubTiers());

  editingPlan       = signal<PlanCard | null>(null);
  editName          = '';
  editDescription   = '';
  editPrice         = 0;
  editMaxClubs      = 0;
  editMaxResults    = 0;
  editIsActive      = true;
  editIsHighlighted = false;
  editSortOrder     = 0;
  saving            = signal(false);
  editError         = signal<string | null>(null);

  // Create plan
  showCreateModal   = signal(false);
  createName        = '';
  createDescription = '';
  createType        = 'Federation';
  createCycle       = 'Monthly';
  createPrice       = 0;
  createCurrency    = 'GBP';
  createMaxClubs    = 0;
  createMaxResults  = 0;
  createHighlighted = false;
  createSortOrder   = 10;
  creating          = signal(false);
  createError       = signal<string | null>(null);

  // Delete plan
  deletePlanTarget  = signal<PlanCard | null>(null);
  deletingPlanId    = signal<string | null>(null);
  deletePlanError   = signal<string | null>(null);

  readonly isoCurrencies = [
    { code: 'GBP', name: 'British Pound Sterling' },
    { code: 'EUR', name: 'Euro' },
    { code: 'USD', name: 'US Dollar' },
    { code: 'EGP', name: 'Egyptian Pound' },
    { code: 'AED', name: 'UAE Dirham' },
    { code: 'SAR', name: 'Saudi Riyal' },
    { code: 'CHF', name: 'Swiss Franc' },
    { code: 'NOK', name: 'Norwegian Krone' },
    { code: 'SEK', name: 'Swedish Krona' },
    { code: 'DKK', name: 'Danish Krone' },
    { code: 'PLN', name: 'Polish Złoty' },
    { code: 'CZK', name: 'Czech Koruna' },
    { code: 'HUF', name: 'Hungarian Forint' },
    { code: 'RON', name: 'Romanian Leu' },
    { code: 'BGN', name: 'Bulgarian Lev' },
    { code: 'HRK', name: 'Croatian Kuna' },
    { code: 'TRY', name: 'Turkish Lira' },
    { code: 'ZAR', name: 'South African Rand' },
    { code: 'MAD', name: 'Moroccan Dirham' },
    { code: 'TND', name: 'Tunisian Dinar' },
    { code: 'DZD', name: 'Algerian Dinar' },
    { code: 'AUD', name: 'Australian Dollar' },
    { code: 'CAD', name: 'Canadian Dollar' },
    { code: 'NZD', name: 'New Zealand Dollar' },
    { code: 'JPY', name: 'Japanese Yen' },
    { code: 'CNY', name: 'Chinese Yuan' },
    { code: 'INR', name: 'Indian Rupee' },
    { code: 'BRL', name: 'Brazilian Real' },
    { code: 'MXN', name: 'Mexican Peso' },
    { code: 'RUB', name: 'Russian Ruble' },
  ];

  readonly cycles: Array<'Monthly' | 'Seasonal' | 'Annual'> = ['Monthly', 'Seasonal', 'Annual'];
  readonly cycleLabels = { Monthly: '/mo', Seasonal: '/6mo', Annual: '/yr' };
  readonly cycleNotes = {
    Monthly:  'Billed monthly · Cancel anytime',
    Seasonal: 'One payment for the whole season (6 months)',
    Annual:   'Billed once yearly · Best value overall'
  };

  ngOnInit() { this.loadPlans(); }

  loadPlans() {
    this.loading.set(true);
    this.api.adminGetSubscriptionPlans().subscribe({
      next: plans => { this.rawPlans.set(plans.map(parsePlanCard)); this.loading.set(false); },
      error: () => { this.error.set('Failed to load plans.'); this.loading.set(false); }
    });
  }

  cardForCycle(tier: TierGroup): PlanCard | null {
    const c = this.selectedCycle();
    return c === 'Monthly' ? tier.monthly : c === 'Seasonal' ? tier.seasonal : tier.annual;
  }

  openEdit(plan: PlanCard) {
    this.editingPlan.set(plan);
    this.editName          = plan.name;
    this.editDescription   = plan.description ?? '';
    this.editPrice         = plan.price;
    this.editMaxClubs      = plan.maxClubs;
    this.editMaxResults    = plan.maxResultsPerClub;
    this.editIsActive      = plan.isActive;
    this.editIsHighlighted = plan.isHighlighted;
    this.editSortOrder     = plan.sortOrder;
    this.editError.set(null);
  }

  closeEdit() { this.editingPlan.set(null); }

  saveEdit() {
    const plan = this.editingPlan();
    if (!plan) return;
    this.saving.set(true);
    this.editError.set(null);
    this.api.adminUpdateSubscriptionPlan(plan.id, {
      name: this.editName,
      description: this.editDescription || null,
      price: this.editPrice,
      maxClubs: this.editMaxClubs,
      maxResultsPerClub: this.editMaxResults,
      isActive: this.editIsActive,
      isHighlighted: this.editIsHighlighted,
      sortOrder: this.editSortOrder,
      features: plan.features.length ? JSON.stringify(plan.features) : null
    }).subscribe({
      next: () => { this.saving.set(false); this.closeEdit(); this.loadPlans(); },
      error: err => { this.editError.set(err?.error?.message ?? 'Failed to save changes.'); this.saving.set(false); }
    });
  }

  formatLimit(val: number, unit: string): string {
    return val === 0 ? '∞' : `${val} ${unit}`;
  }

  openCreate() {
    this.createName = ''; this.createDescription = ''; this.createType = 'Federation';
    this.createCycle = 'Monthly'; this.createPrice = 0; this.createCurrency = 'GBP';
    this.createMaxClubs = 0; this.createMaxResults = 0;
    this.createHighlighted = false; this.createSortOrder = 10;
    this.createError.set(null);
    this.showCreateModal.set(true);
  }

  closeCreate() { this.showCreateModal.set(false); }

  saveCreate() {
    if (!this.createName.trim()) { this.createError.set('Name is required.'); return; }
    this.creating.set(true);
    this.createError.set(null);
    this.api.adminCreateSubscriptionPlan({
      name: this.createName, description: this.createDescription || null,
      type: this.createType, billingCycle: this.createCycle,
      price: this.createPrice, currency: this.createCurrency,
      maxClubs: this.createMaxClubs, maxResultsPerClub: this.createMaxResults,
      isHighlighted: this.createHighlighted, sortOrder: this.createSortOrder
    }).subscribe({
      next: () => { this.creating.set(false); this.closeCreate(); this.loadPlans(); },
      error: e => { this.createError.set(e?.error?.message ?? 'Failed to create plan.'); this.creating.set(false); }
    });
  }

  confirmDeletePlan(plan: PlanCard) {
    this.deletePlanError.set(null);
    this.deletePlanTarget.set(plan);
  }

  executeDeletePlan() {
    const plan = this.deletePlanTarget();
    if (!plan) return;
    this.deletingPlanId.set(plan.id);
    this.deletePlanError.set(null);
    this.api.adminDeleteSubscriptionPlan(plan.id).subscribe({
      next: () => {
        this.deletingPlanId.set(null);
        this.deletePlanTarget.set(null);
        this.loadPlans();
      },
      error: e => {
        this.deletePlanError.set(e?.error?.message ?? 'Failed to delete plan.');
        this.deletingPlanId.set(null);
      }
    });
  }
}
