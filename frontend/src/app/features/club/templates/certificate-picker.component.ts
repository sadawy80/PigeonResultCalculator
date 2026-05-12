import { Component, Input, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DesignPickerComponent } from './design-picker.component';
import { TemplateCategory } from '../../../core/models/template.models';
import { CertType } from '../../../core/services/print-api.service';

/**
 * Opens the design picker scoped to a specific certificate sub-type (race,
 * ace, super-ace, best-loft). Picks the right backend endpoint based on the
 * entity IDs supplied — caller provides only what's relevant.
 */
@Component({
  selector: 'app-certificate-picker',
  standalone: true,
  imports: [FormsModule, DesignPickerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './certificate-picker.component.html',
  styleUrls: ['./certificate-picker.component.scss']
})
export class CertificatePickerComponent {
  @Input() certType: CertType = 'race';
  @Input() raceResultId?: string;
  @Input() programmeId?: string;
  @Input() fancierUserId?: string;
  @Input() ringNumber?: string;

  TemplateCategory = TemplateCategory;
  isOpen = signal(false);

  open()  { this.isOpen.set(true); }
  close() { this.isOpen.set(false); }
}
