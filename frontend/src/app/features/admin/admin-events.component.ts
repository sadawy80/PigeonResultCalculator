import { Component, signal, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-admin-events',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './admin-events.component.html',
  styleUrls: ['./admin-events.component.scss']
})
export class AdminEventsComponent implements OnInit {
  private api = inject(ApiService);

  typeFilter      = '';
  aggregateFilter = '';
  page            = 1;
  pageSize        = 50;
  total           = signal(0);
  events          = signal<any[]>([]);
  loading         = signal(false);
  error           = signal<string | null>(null);

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.error.set(null);
    this.api.adminGetEvents({
      eventType:     this.typeFilter || undefined,
      aggregateType: this.aggregateFilter || undefined,
      page:          this.page,
      pageSize:      this.pageSize
    }).subscribe({
      next: r => { this.events.set(r.items); this.total.set(r.totalCount); this.loading.set(false); },
      error: () => { this.error.set('Failed to load events.'); this.loading.set(false); }
    });
  }

  onFilter() { this.page = 1; this.load(); }

  get totalPages() { return Math.ceil(this.total() / this.pageSize); }
  prevPage() { if (this.page > 1) { this.page--; this.load(); } }
  nextPage() { if (this.page < this.totalPages) { this.page++; this.load(); } }
}
