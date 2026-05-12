import { Component, Input, signal, ChangeDetectionStrategy } from '@angular/core';
import { DesignPickerComponent } from './design-picker.component';
import { TemplateCategory } from '../../../core/models/template.models';

/**
 * Print / Excel entry point for a single race or programme. Renders a button
 * that opens the new design picker, which handles fetching the design
 * catalogue and downloading the rendered PDF / XLSX.
 */
@Component({
  selector: 'app-print-button',
  standalone: true,
  imports: [DesignPickerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './print-button.component.html',
  styleUrls: ['./print-button.component.scss']
})
export class PrintButtonComponent {
  @Input() category: TemplateCategory = TemplateCategory.RaceResults;
  @Input() raceId?: string;
  @Input() programmeId?: string;
  @Input() raceResultId?: string;
  @Input() label = 'Print / PDF';

  pickerOpen = signal(false);

  open()  { this.pickerOpen.set(true); }
  close() { this.pickerOpen.set(false); }

  categoryLabel(): string {
    const m: Record<number, string> = {
      1: 'Race Results', 2: 'Best Loft', 3: 'Ace Pigeon', 4: 'Super Ace Pigeon', 5: 'Certificates'
    };
    return m[this.category] ?? 'Templates';
  }
}
