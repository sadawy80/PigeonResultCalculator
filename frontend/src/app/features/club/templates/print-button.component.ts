import { Component, Input, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { TemplateApiService } from '../../../core/services/template-api.service';
import { TemplateBrowserComponent } from './template-browser.component';
import { PrintTemplate, TemplateCategory } from '../../../core/models/template.models';

@Component({
  selector: 'app-print-button',
  standalone: true,
  imports: [TemplateBrowserComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './print-button.component.html',
  styleUrls: ['./print-button.component.scss']
})
export class PrintButtonComponent implements OnInit {
  private templateApi = inject(TemplateApiService);

  @Input() category: TemplateCategory = TemplateCategory.RaceResults;
  @Input() raceId?: string;
  @Input() programmeId?: string;
  @Input() raceResultId?: string;
  @Input() label = 'Print / PDF';

  pickerOpen = signal(false);
  templates  = signal<PrintTemplate[]>([]);

  ngOnInit() {
    this.templateApi.getTemplates(this.category).subscribe(t => this.templates.set(t));
  }

  openPicker()  { this.pickerOpen.set(true); }
  closePicker() { this.pickerOpen.set(false); }

  onTemplateSelected(t: PrintTemplate) {
    const url = this.templateApi.buildPrintUrl(t.id, {
      category:     this.category,
      raceId:       this.raceId,
      programmeId:  this.programmeId,
      raceResultId: this.raceResultId,
    });
    window.open(url, '_blank');
    this.closePicker();
  }

  categoryLabel(): string {
    const m: Record<number, string> = {
      1: 'Race Results', 2: 'Best Loft', 3: 'Ace Pigeon', 4: 'Super Ace Pigeon', 5: 'Certificates'
    };
    return m[this.category] ?? 'Templates';
  }
}
