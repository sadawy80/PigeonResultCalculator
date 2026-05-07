import {
  Component, inject, signal, OnInit, Input, Output, EventEmitter,
  ElementRef, ViewChild, ChangeDetectionStrategy
} from '@angular/core';
import { NgStyle } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TemplateApiService } from '../../../core/services/template-api.service';
import {
  PrintTemplate, TemplateCategory, TemplateStyle, TemplatePaperSize, RenderTemplateRequest
} from '../../../core/models/template.models';

@Component({
  selector: 'app-template-browser',
  standalone: true,
  imports: [NgStyle, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './template-browser.component.html',
  styleUrls: ['./template-browser.component.scss']
})
export class TemplateBrowserComponent implements OnInit {
  private templateApi = inject(TemplateApiService);

  @Input() initialCategory?: TemplateCategory;
  @Input() raceId?: string;
  @Input() programmeId?: string;
  @Input() raceResultId?: string;
  @Output() templateSelected = new EventEmitter<PrintTemplate>();

  allTemplates     = signal<PrintTemplate[]>([]);
  filtered         = signal<PrintTemplate[]>([]);
  loading          = signal(true);
  selectedTemplate = signal<PrintTemplate | null>(null);
  showPreview      = signal(false);
  previewHtml      = signal<string | null>(null);
  previewing       = signal(false);

  selectedCategory = signal<TemplateCategory | null>(null);
  selectedStyle: TemplateStyle | null = null;
  selectedPaper: number | null = null;
  searchText = '';

  @ViewChild('previewFrame') previewFrame?: ElementRef<HTMLIFrameElement>;

  categories = [
    { value: TemplateCategory.RaceResults,    icon: '🏁', label: 'Race Results' },
    { value: TemplateCategory.BestLoft,       icon: '🏠', label: 'Best Loft' },
    { value: TemplateCategory.AcePigeon,      icon: '🕊️', label: 'Ace Pigeon' },
    { value: TemplateCategory.SuperAcePigeon, icon: '⭐', label: 'Super Ace' },
    { value: TemplateCategory.Certificate,    icon: '🏅', label: 'Certificates' },
  ];

  styles = [
    { value: TemplateStyle.Classic,   label: 'Classic' },
    { value: TemplateStyle.Modern,    label: 'Modern' },
    { value: TemplateStyle.Elegant,   label: 'Elegant' },
    { value: TemplateStyle.Minimal,   label: 'Minimal' },
    { value: TemplateStyle.Sporty,    label: 'Sporty' },
    { value: TemplateStyle.Heritage,  label: 'Heritage' },
    { value: TemplateStyle.Corporate, label: 'Corporate' },
    { value: TemplateStyle.Branded,   label: 'Branded' },
    { value: TemplateStyle.Dark,      label: 'Dark' },
  ];

  ngOnInit() {
    if (this.initialCategory != null) this.selectedCategory.set(this.initialCategory);
    this.templateApi.getTemplates().subscribe((templates: PrintTemplate[]) => {
      this.allTemplates.set(templates);
      this.applyFilters();
      this.loading.set(false);
    });
  }

  selectCategory(cat: TemplateCategory) {
    this.selectedCategory.set(this.selectedCategory() === cat ? null : cat);
    this.applyFilters();
  }

  applyFilters() {
    let f = this.allTemplates();
    if (this.selectedCategory() != null) f = f.filter(t => t.category === this.selectedCategory());
    if (this.selectedStyle != null)       f = f.filter(t => t.style === this.selectedStyle);
    if (this.selectedPaper != null)       f = f.filter(t => t.paperSize === this.selectedPaper);
    if (this.searchText.trim())
      f = f.filter(t => t.name.toLowerCase().includes(this.searchText.toLowerCase())
                     || t.description.toLowerCase().includes(this.searchText.toLowerCase()));
    this.filtered.set(f);
  }

  selectTemplate(t: PrintTemplate) {
    this.selectedTemplate.set(t);
    this.templateSelected.emit(t);
    this.previewHtml.set(null);
    this.showPreview.set(false);
  }

  clearSelection() { this.selectedTemplate.set(null); this.showPreview.set(false); }

  previewInModal() {
    if (!this.selectedTemplate()) return;
    this.showPreview.set(true);
    this.previewing.set(true);
    this.previewHtml.set(null);
    this.templateApi.renderTemplate(this.selectedTemplate()!.id, this.buildRequest()).subscribe({
      next: (html: string) => { this.previewHtml.set(html); this.previewing.set(false); },
      error: () => this.previewing.set(false)
    });
  }

  closePreview() { this.showPreview.set(false); }

  openPrint() {
    if (!this.selectedTemplate()) return;
    const url = this.templateApi.buildPrintUrl(this.selectedTemplate()!.id, this.buildRequest());
    window.open(url, '_blank');
  }

  private buildRequest(): Omit<RenderTemplateRequest, 'templateId'> {
    return {
      category:    this.selectedTemplate()!.category,
      raceId:      this.raceId,
      programmeId: this.programmeId,
      raceResultId: this.raceResultId,
    };
  }

  canPrint(): boolean {
    const t = this.selectedTemplate();
    if (!t) return false;
    if (t.category === TemplateCategory.Certificate) return true;
    if ([TemplateCategory.RaceResults].includes(t.category)) return !!this.raceId;
    return !!this.programmeId;
  }

  contextLabel(): string {
    if (this.raceId)      return 'Race context loaded';
    if (this.programmeId) return 'Programme context loaded';
    return '';
  }

  contextHint(): string {
    const t = this.selectedTemplate();
    if (!t) return 'race or programme';
    return t.category === TemplateCategory.RaceResults ? 'race' : 'programme';
  }

  countForCategory(cat: TemplateCategory): number {
    return this.allTemplates().filter(t => t.category === cat).length;
  }

  catIcon(cat: TemplateCategory): string {
    const m: Record<number, string> = { 1:'🏁', 2:'🏠', 3:'🕊️', 4:'⭐', 5:'🏅' };
    return m[cat] ?? '📄';
  }

  paperLabel(p: TemplatePaperSize): string {
    const m: Record<number, string> = { 1:'A4↕', 2:'A4↔', 3:'A3↕', 4:'A3↔', 5:'Letter↕', 6:'Letter↔' };
    return m[p] ?? '';
  }

  isLandscape(): boolean {
    const p = this.selectedTemplate()?.paperSize;
    return p === TemplatePaperSize.A4Landscape || p === TemplatePaperSize.A3Landscape;
  }
}
