import { Component, Input, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TemplateApiService } from '../../../core/services/template-api.service';
import { TemplateBrowserComponent } from './template-browser.component';
import { PrintTemplate, TemplateCategory } from '../../../core/models/template.models';

@Component({
  selector: 'app-certificate-picker',
  standalone: true,
  imports: [FormsModule, TemplateBrowserComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './certificate-picker.component.html',
  styleUrls: ['./certificate-picker.component.scss']
})
export class CertificatePickerComponent {
  private templateApi = inject(TemplateApiService);

  @Input() raceId?: string;
  @Input() programmeId?: string;
  @Input() raceResultId?: string;

  TemplateCategory = TemplateCategory;

  isOpen         = signal(false);
  templateChosen = signal(false);
  recipientName  = '';
  rank           = '';
  achievement    = '';

  open()  { this.isOpen.set(true); }
  close() { this.isOpen.set(false); this.templateChosen.set(false); }

  showTemplates() {
    if (!this.recipientName) return;
    this.templateChosen.set(true);
  }

  printCertificate(t: PrintTemplate) {
    const url = this.templateApi.buildPrintUrl(t.id, {
      category:                   TemplateCategory.Certificate,
      raceId:                     this.raceId,
      programmeId:                this.programmeId,
      raceResultId:               this.raceResultId,
      certificateRecipientName:   this.recipientName,
      certificateRank:            this.rank,
      certificateAchievement:     this.achievement,
    });
    window.open(url, '_blank');
    this.close();
  }
}
