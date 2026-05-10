import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/services/api.service';

interface Announcement {
  title: string;
  body?: string;
  date?: string;
}

@Component({
  selector: 'app-federation-page-editor',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './federation-page-editor.component.html',
  styleUrls: ['./federation-page-editor.component.scss']
})
export class FederationPageEditorComponent implements OnInit {
  private api = inject(ApiService);

  page        = signal<any>(null);
  loading     = signal(true);
  saving      = signal(false);
  error       = signal<string | null>(null);
  success     = signal<string | null>(null);

  selectedTheme   = 1;
  isPublished     = false;
  announcements   = signal<Announcement[]>([]);
  headerHtml      = '';

  publicUrl       = signal('');

  readonly themes = [
    { value: 1, label: 'Skyline — dark navy + electric blue' },
    { value: 2, label: 'Meadow — earthy greens + warm amber' },
    { value: 3, label: 'Crimson — bold red + charcoal' },
    { value: 4, label: 'Ivory — light cream + gold' },
    { value: 5, label: 'Slate — cool grey + cyan' }
  ];

  ngOnInit() {
    this.api.getMyFederationPage().subscribe({
      next: p => {
        this.page.set(p);
        this.selectedTheme = p.theme ?? 1;
        this.isPublished   = p.isPublished ?? false;
        this.headerHtml    = p.headerHtml ?? '';
        this.publicUrl.set(`/c/${p.slug}`);
        try {
          this.announcements.set(JSON.parse(p.announcementsJson ?? '[]') ?? []);
        } catch {
          this.announcements.set([]);
        }
        this.loading.set(false);
      },
      error: () => { this.error.set('Failed to load federation page.'); this.loading.set(false); }
    });
  }

  addAnnouncement() {
    this.announcements.update(arr => [...arr, { title: '', body: '', date: '' }]);
  }

  removeAnnouncement(i: number) {
    this.announcements.update(arr => arr.filter((_, idx) => idx !== i));
  }

  save() {
    this.saving.set(true);
    this.error.set(null);
    this.success.set(null);
    this.api.updateMyFederationPage({
      theme: this.selectedTheme,
      isPublished: this.isPublished,
      announcementsJson: JSON.stringify(this.announcements()),
      headerHtml: this.headerHtml || undefined
    }).subscribe({
      next: () => {
        this.success.set('federation page saved successfully.');
        this.saving.set(false);
      },
      error: () => { this.error.set('Failed to save.'); this.saving.set(false); }
    });
  }
}
