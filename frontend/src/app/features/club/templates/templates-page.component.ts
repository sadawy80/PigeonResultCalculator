import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DesignPickerComponent } from './design-picker.component';
import { TemplateCategory } from '../../../core/models/template.models';

@Component({
  selector: 'app-templates-page',
  standalone: true,
  imports: [DesignPickerComponent],
  templateUrl: './templates-page.component.html',
  styleUrls: ['./templates-page.component.scss']
})
export class TemplatesPageComponent implements OnInit {
  private route = inject(ActivatedRoute);

  showPicker     = signal(false);
  activeCategory = signal<TemplateCategory>(TemplateCategory.RaceResults);

  queryRaceId?: string;
  queryProgrammeId?: string;
  queryRaceResultId?: string;

  // The 4 result tables plus the 4 cert sub-types. Picker opens with the right
  // context and the backend builds the data from entity IDs.
  quickCategories = [
    { cat: TemplateCategory.RaceResults,    icon: '🏁', label: 'Race Result Tables',   desc: 'Multi-page A4 portrait result sheet for a single race.' },
    { cat: TemplateCategory.AcePigeon,      icon: '🕊️', label: 'Ace Pigeon Tables',     desc: 'Season ranking of ace pigeons across a programme.' },
    { cat: TemplateCategory.SuperAcePigeon, icon: '⭐', label: 'Super Ace Tables',      desc: 'Elite-tier ranking with multi-race aggregation.' },
    { cat: TemplateCategory.BestLoft,       icon: '🏠', label: 'Best Loft Tables',     desc: 'Fancier leaderboard across all programme races.' },
    { cat: TemplateCategory.Certificate,    icon: '🏅', label: 'Award Certificates',   desc: 'Single-page A4 certificate, portrait or landscape.' },
  ];

  ngOnInit() {
    const p = this.route.snapshot.queryParamMap;
    this.queryRaceId        = p.get('raceId') ?? undefined;
    this.queryProgrammeId   = p.get('programmeId') ?? undefined;
    this.queryRaceResultId  = p.get('raceResultId') ?? undefined;

    const catParam = p.get('category');
    if (catParam) {
      this.activeCategory.set(parseInt(catParam) as TemplateCategory);
      this.showPicker.set(true);
    }
  }

  openCategory(cat: TemplateCategory) {
    this.activeCategory.set(cat);
    this.showPicker.set(true);
  }

  close() { this.showPicker.set(false); }
}
