import { Component, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-admin-events',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './admin-events.component.html',
  styleUrls: ['./admin-events.component.scss']
})
export class AdminEventsComponent implements OnInit {
  search     = '';
  typeFilter = '';
  events     = signal<any[]>([]);

  filteredEvents() {
    return this.events().filter(e =>
      (!this.search || e.eventType.includes(this.search) || e.aggregateId.includes(this.search)) &&
      (!this.typeFilter || e.eventType === this.typeFilter)
    );
  }

  ngOnInit() {
    const now = new Date();
    this.events.set([
      { id: '1', eventType: 'RaceCreated',           aggregateType: 'Race',    aggregateId: 'f3a2-1b4c-9d8e', triggeredBy: 'Alice Martin', createdAt: new Date(now.getTime() - 120000), isProcessed: true },
      { id: '2', eventType: 'ResultPublished',        aggregateType: 'Race',    aggregateId: 'f3a2-1b4c-9d8e', triggeredBy: 'Alice Martin', createdAt: new Date(now.getTime() - 90000),  isProcessed: true },
      { id: '3', eventType: 'UserInvited',            aggregateType: 'Club',    aggregateId: 'a8c3-2f1d-4e7b', triggeredBy: 'Alice Martin', createdAt: new Date(now.getTime() - 60000),  isProcessed: true },
      { id: '4', eventType: 'CountryResultPublished', aggregateType: 'Country', aggregateId: 'b1e5-3a2c-7f9d', triggeredBy: 'David Hughes', createdAt: new Date(now.getTime() - 30000),  isProcessed: true },
      { id: '5', eventType: 'SubscriptionChanged',    aggregateType: 'Country', aggregateId: 'c9d7-4b3e-2a1f', triggeredBy: 'System',       createdAt: now,                              isProcessed: false },
    ]);
  }
}
