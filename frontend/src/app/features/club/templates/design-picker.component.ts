import { Component, EventEmitter, Input, OnInit, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  CertDesignCatalogue, CertType, DesignInfo, PrintApiService, ResultType
} from '../../../core/services/print-api.service';
import { TemplateCategory } from '../../../core/models/template.models';

/**
 * Modal that lists every design available for one of the 5 print categories,
 * lets the user pick design + language (and orientation for certs), then POSTs
 * to the matching `/api/print/*` endpoint and downloads the returned PDF or
 * Excel blob. Replaces the old DB-template browser end-to-end.
 *
 * Inputs:
 *  - `category` — which kind of artefact to render
 *  - `raceId` / `raceResultId` / `programmeId` / `fancierUserId` / `ringNumber`
 *    — the entity IDs the orchestrator needs. Required varies per category.
 */
@Component({
  selector: 'app-design-picker',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="pr-modal" (click)="onBackdrop($event)">
      <div class="pr-modal__panel pr-modal__panel--wide">
        <header class="pr-modal__header">
          <h2>{{ headerLabel() }}</h2>
          <button class="pr-modal__close" (click)="close.emit()">×</button>
        </header>

        <div class="pr-modal__body">
          @if (!canRender()) {
            <p class="pr-alert pr-alert--warn">{{ missingContextMessage() }}</p>
          }

          <div class="filter-row">
            <label>
              <span>Language</span>
              <select [(ngModel)]="language">
                @for (l of availableLanguages(); track l.code) {
                  <option [value]="l.code">{{ l.label }}</option>
                }
              </select>
            </label>

            @if (isCert()) {
              <label>
                <span>Orientation</span>
                <select [(ngModel)]="orientation" (change)="recomputeList()">
                  <option value="portrait">Portrait</option>
                  <option value="landscape">Landscape</option>
                </select>
              </label>
            }
          </div>

          @if (loading()) {
            <div class="pr-empty"><div class="pr-spinner" style="margin:0 auto"></div></div>
          } @else if (designs().length === 0) {
            <div class="pr-empty">No designs available for this combination.</div>
          } @else {
            <div class="design-grid">
              @for (d of designs(); track d.id) {
                <button
                  type="button"
                  class="design-card"
                  [class.active]="selected()?.id === d.id"
                  (click)="select(d)">
                  <div class="design-id">{{ d.id }}</div>
                  <div class="design-name">{{ d.name }}</div>
                  @if (d.arabic) { <div class="arabic-badge">AR</div> }
                </button>
              }
            </div>
          }

          @if (error()) { <p class="pr-alert pr-alert--error">{{ error() }}</p> }
        </div>

        <footer class="pr-modal__footer">
          <button class="pr-btn pr-btn--ghost" (click)="close.emit()">Cancel</button>
          @if (isResult()) {
            <button
              type="button"
              class="pr-btn pr-btn--ghost"
              (click)="downloadExcel()"
              [disabled]="busy() || !canRender()">
              {{ busy() && lastAction === 'xlsx' ? 'Building…' : 'Download Excel' }}
            </button>
          }
          <button
            type="button"
            class="pr-btn pr-btn--primary"
            (click)="downloadPdf()"
            [disabled]="busy() || !canRender() || !selected()">
            {{ busy() && lastAction === 'pdf' ? 'Rendering…' : 'Download PDF' }}
          </button>
        </footer>
      </div>
    </div>
  `,
  styles: [`
    .pr-modal__panel--wide { max-width: 880px; width: 100%; }
    .filter-row { display: flex; gap: 1rem; margin-bottom: 1rem; }
    .filter-row label { display: flex; flex-direction: column; gap: .25rem; font-size: .85rem; color: #475569; }
    .filter-row select { padding: .4rem .6rem; border: 1px solid #cbd5e1; border-radius: 6px; min-width: 160px; }
    .design-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(160px, 1fr)); gap: .5rem; }
    .design-card { position: relative; padding: .9rem .8rem; border: 1px solid #d4d4d8; background: #fafafa;
                   border-radius: 8px; cursor: pointer; text-align: left; font: inherit; }
    .design-card:hover { background: #f1f5f9; border-color: #94a3b8; }
    .design-card.active { background: #eef2ff; border-color: #4f46e5; box-shadow: 0 0 0 2px rgba(79,70,229,.15); }
    .design-id { font-family: 'JetBrains Mono', monospace; font-weight: 700; font-size: .9rem; color: #1e3a8a; }
    .design-name { color: #475569; font-size: .8rem; margin-top: .25rem; }
    .arabic-badge { position: absolute; top: .35rem; right: .5rem; font-size: .65rem; font-weight: 700;
                    background: #d4af5a; color: #0a1428; padding: .1rem .35rem; border-radius: 3px; }
  `]
})
export class DesignPickerComponent implements OnInit {
  private api = inject(PrintApiService);

  @Input() category!: TemplateCategory;
  @Input() raceId?: string;
  @Input() raceResultId?: string;
  @Input() programmeId?: string;
  @Input() fancierUserId?: string;
  @Input() ringNumber?: string;

  @Output() close = new EventEmitter<void>();

  loading      = signal(true);
  busy         = signal(false);
  designs      = signal<DesignInfo[]>([]);
  selected     = signal<DesignInfo | null>(null);
  error        = signal<string | null>(null);

  // For cert categories — Portrait or Landscape (results are portrait only).
  orientation: 'portrait' | 'landscape' = 'portrait';
  language    = 'en';
  lastAction: 'pdf' | 'xlsx' | null = null;

  private catalogue: CertDesignCatalogue | null = null;

  ngOnInit() {
    this.fetchDesigns();
  }

  private fetchDesigns() {
    this.loading.set(true);
    this.error.set(null);

    if (this.isCert()) {
      const ct = this.certType();
      this.api.getCertDesigns(ct).subscribe({
        next: cat => {
          this.catalogue = cat;
          this.recomputeList();
          this.loading.set(false);
        },
        error: err => { this.error.set(err?.error?.detail || 'Failed to load designs.'); this.loading.set(false); }
      });
    } else {
      const rt = this.resultType();
      this.api.getResultDesigns(rt).subscribe({
        next: list => { this.designs.set(list); this.loading.set(false); },
        error: err => { this.error.set(err?.error?.detail || 'Failed to load designs.'); this.loading.set(false); }
      });
    }
  }

  recomputeList() {
    if (!this.isCert() || !this.catalogue) return;
    this.designs.set(this.orientation === 'landscape' ? this.catalogue.landscape : this.catalogue.portrait);
    if (this.selected() && !this.designs().some(d => d.id === this.selected()!.id)) {
      this.selected.set(null);
    }
  }

  select(d: DesignInfo) { this.selected.set(d); }
  onBackdrop(e: MouseEvent) { if ((e.target as HTMLElement).classList.contains('pr-modal')) this.close.emit(); }

  isCert():   boolean { return this.category === TemplateCategory.Certificate; }
  isResult(): boolean { return !this.isCert(); }

  certType(): CertType {
    // Without a sub-type field we treat any Certificate request as the race-style cert.
    // Callers wanting ace/super-ace/best-loft pass them through dedicated entry components.
    return (this as any)._certSubType || 'race';
  }
  resultType(): ResultType {
    return ({
      [TemplateCategory.RaceResults]:    'race',
      [TemplateCategory.AcePigeon]:      'ace',
      [TemplateCategory.SuperAcePigeon]: 'super-ace',
      [TemplateCategory.BestLoft]:       'best-loft',
    } as Record<number, ResultType>)[this.category];
  }

  /** Optional: set by parent components to scope a Certificate run to a specific cert type. */
  @Input() set certSubType(value: CertType | undefined) {
    (this as any)._certSubType = value;
    if (this.isCert() && value) this.fetchDesigns();
  }

  availableLanguages(): { code: string; label: string }[] {
    // Race results template supports the full set; everything else is en+ar.
    if (this.resultType() === 'race' && this.isResult()) return [
      { code: 'en', label: 'English' },
      { code: 'ar', label: 'العربية' },
      { code: 'fa', label: 'فارسی' },
      { code: 'es', label: 'Español' },
      { code: 'de', label: 'Deutsch' },
      { code: 'zh', label: '中文' },
    ];
    return [{ code: 'en', label: 'English' }, { code: 'ar', label: 'العربية' }];
  }

  canRender(): boolean {
    if (this.isCert()) {
      const sub = this.certType();
      if (sub === 'race')                                 return !!this.raceResultId;
      if (sub === 'ace' || sub === 'super-ace')           return !!this.programmeId && !!this.ringNumber;
      if (sub === 'best-loft')                            return !!this.programmeId && !!this.fancierUserId;
    }
    const rt = this.resultType();
    return rt === 'race' ? !!this.raceId : !!this.programmeId;
  }

  missingContextMessage(): string {
    if (this.isCert()) {
      const sub = this.certType();
      if (sub === 'race')      return 'Open this dialog from a race result row.';
      if (sub === 'best-loft') return 'Open this dialog from a Best Loft programme entry.';
      return 'Open this dialog from a programme ace/super-ace entry.';
    }
    return this.resultType() === 'race'
      ? 'Open this dialog from a race detail page.'
      : 'Open this dialog from a programme detail page.';
  }

  headerLabel(): string {
    if (this.isCert()) return 'Choose certificate design';
    return ({
      [TemplateCategory.RaceResults]:    'Race result table — choose design',
      [TemplateCategory.AcePigeon]:      'Ace result table — choose design',
      [TemplateCategory.SuperAcePigeon]: 'Super Ace result table — choose design',
      [TemplateCategory.BestLoft]:       'Best Loft result table — choose design',
    } as Record<number, string>)[this.category] ?? 'Choose design';
  }

  downloadPdf() {
    if (this.busy() || !this.canRender() || !this.selected()) return;
    this.busy.set(true);
    this.lastAction = 'pdf';
    this.error.set(null);

    const designId = this.selected()!.id;
    const language = this.language;
    const obs = this.isCert() ? this.callCert(designId, language) : this.callResultPdf(designId, language);

    obs.subscribe({
      next: blob => {
        this.api.download(blob, `${this.fileSlug()}-${designId}-${language}.pdf`);
        this.busy.set(false);
      },
      error: err => { this.error.set(err?.error?.detail || 'Render failed.'); this.busy.set(false); }
    });
  }

  downloadExcel() {
    if (this.busy() || !this.canRender() || !this.isResult()) return;
    this.busy.set(true);
    this.lastAction = 'xlsx';
    this.error.set(null);

    const language = this.language;
    const obs = this.callResultExcel(language);

    obs.subscribe({
      next: blob => {
        this.api.download(blob, `${this.fileSlug()}-${language}.xlsx`);
        this.busy.set(false);
      },
      error: err => { this.error.set(err?.error?.detail || 'Excel export failed.'); this.busy.set(false); }
    });
  }

  private callCert(designId: string, language: string) {
    const sub = this.certType();
    if (sub === 'race')      return this.api.renderRaceCert     ({ raceResultId: this.raceResultId!, designId, language });
    if (sub === 'ace')       return this.api.renderAceCert      ({ programmeId:  this.programmeId!,  ringNumber: this.ringNumber!, designId, language });
    if (sub === 'super-ace') return this.api.renderSuperAceCert ({ programmeId:  this.programmeId!,  ringNumber: this.ringNumber!, designId, language });
    return this.api.renderBestLoftCert({ programmeId: this.programmeId!, fancierUserId: this.fancierUserId!, designId, language });
  }

  private callResultPdf(designId: string, language: string) {
    const rt = this.resultType();
    if (rt === 'race')      return this.api.renderRaceResultsPdf    ({ raceId: this.raceId!,            designId, language });
    if (rt === 'ace')       return this.api.renderAceResultsPdf     ({ programmeId: this.programmeId!,  designId, language });
    if (rt === 'super-ace') return this.api.renderSuperAceResultsPdf({ programmeId: this.programmeId!,  designId, language });
    return this.api.renderBestLoftResultsPdf({ programmeId: this.programmeId!, designId, language });
  }

  private callResultExcel(language: string) {
    const rt = this.resultType();
    if (rt === 'race')      return this.api.renderRaceResultsExcel    ({ raceId: this.raceId!,           language });
    if (rt === 'ace')       return this.api.renderAceResultsExcel     ({ programmeId: this.programmeId!, language });
    if (rt === 'super-ace') return this.api.renderSuperAceResultsExcel({ programmeId: this.programmeId!, language });
    return this.api.renderBestLoftResultsExcel({ programmeId: this.programmeId!, language });
  }

  private fileSlug(): string {
    if (this.isCert()) return `${this.certType()}-cert`;
    return `${this.resultType()}-result`;
  }
}
