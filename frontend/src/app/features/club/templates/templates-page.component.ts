import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { TemplateApiService } from '../../../core/services/template-api.service';
import { AuthService } from '../../../core/services/services';
import { TemplateBrowserComponent } from './template-browser.component';
import { PrintJob, TemplateCategory, PrintJobStatus } from '../../../core/models/template.models';

@Component({
  selector: 'app-templates-page',
  standalone: true,
  imports: [DatePipe, TemplateBrowserComponent],
  templateUrl: './templates-page.component.html',
  styleUrls: ['./templates-page.component.scss']
})
export class TemplatesPageComponent implements OnInit {
  private templateApi = inject(TemplateApiService);
  private auth        = inject(AuthService);
  private route       = inject(ActivatedRoute);

  PrintJobStatus = PrintJobStatus;

  totalTemplates = signal(160);
  printJobs      = signal<PrintJob[]>([]);
  loadingJobs    = signal(true);
  showBrowser    = signal(false);
  activeCategory = signal<TemplateCategory | undefined>(undefined);

  queryRaceId?: string;
  queryProgrammeId?: string;

  quickCategories = [
    { value: TemplateCategory.RaceResults,    icon: '🏁', label: 'Race Results',    count: 50, desc: 'Per-race result sheets in portrait or landscape' },
    { value: TemplateCategory.BestLoft,       icon: '🏠', label: 'Best Loft',       count: 20, desc: 'Fancier leaderboards across all programme races' },
    { value: TemplateCategory.AcePigeon,      icon: '🕊️', label: 'Ace Pigeon',      count: 20, desc: 'Individual pigeon performance rankings' },
    { value: TemplateCategory.SuperAcePigeon, icon: '⭐', label: 'Super Ace',       count: 20, desc: 'Elite pigeons meeting strict qualification criteria' },
    { value: TemplateCategory.Certificate,    icon: '🏅', label: 'Certificates',    count: 50, desc: 'Award certificates for winners and participants' },
  ];

  ngOnInit() {
    this.queryRaceId      = this.route.snapshot.queryParamMap.get('raceId') ?? undefined;
    this.queryProgrammeId = this.route.snapshot.queryParamMap.get('programmeId') ?? undefined;

    const catParam = this.route.snapshot.queryParamMap.get('category');
    if (catParam) {
      this.activeCategory.set(parseInt(catParam) as TemplateCategory);
      this.showBrowser.set(true);
    }

    const clubId = this.auth.clubId();
    if (clubId) {
      this.templateApi.getPrintJobs(clubId).subscribe((p: { items: PrintJob[] }) => {
        this.printJobs.set(p.items);
        this.loadingJobs.set(false);
      });
    } else {
      this.loadingJobs.set(false);
    }
  }

  launchBrowser(cat: TemplateCategory) {
    this.activeCategory.set(cat);
    this.showBrowser.set(true);
  }

  activeCategoryLabel(): string {
    return this.quickCategories.find(c => c.value === this.activeCategory())?.label ?? '';
  }

  categoryName(cat: TemplateCategory): string {
    const m: Record<number, string> = {
      1: 'Race Results', 2: 'Best Loft', 3: 'Ace Pigeon', 4: 'Super Ace', 5: 'Certificates'
    };
    return m[cat] ?? '';
  }

  statusBadge(s: PrintJobStatus): string {
    const m: Record<number, string> = {
      1: 'pr-badge--muted', 2: 'pr-badge--info', 3: 'pr-badge--success', 4: 'pr-badge--error'
    };
    return m[s] ?? 'pr-badge--muted';
  }
}
