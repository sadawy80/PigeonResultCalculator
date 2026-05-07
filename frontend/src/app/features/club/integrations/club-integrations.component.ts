import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IntegrationApiService } from '../../../core/services/integration-api.service';
import { AuthService } from '../../../core/services/services';
import { TranslationService, TranslatePipe } from '../../../core/i18n';
import {
  ExternalLink, ExternalLinkStatus,
  IntegrationRaceResult, IntegrationSummary
} from '../../../core/models/integration.models';

@Component({
  selector: 'app-club-integrations',
  standalone: true,
  imports: [DatePipe, DecimalPipe, FormsModule, TranslatePipe],
  templateUrl: './club-integrations.component.html',
  styleUrls: ['./club-integrations.component.scss']
})
export class ClubIntegrationsComponent implements OnInit {
  private api  = inject(IntegrationApiService);
  private auth = inject(AuthService);
  i18n         = inject(TranslationService);

  allLinks     = signal<ExternalLink[]>([]);
  loading      = signal(true);
  activeTab    = signal<'pending' | 'active' | 'history'>('pending');
  expandedId   = signal<string | null>(null);
  actioning    = signal(false);
  rejectingId  = signal<string | null>(null);
  rejectReason = '';

  previewLink    = signal<ExternalLink | null>(null);
  previewLoading = signal(false);
  previewSummary = signal<IntegrationSummary | null>(null);
  previewResults = signal<IntegrationRaceResult[]>([]);

  tabs = [
    { key: 'pending' as const, icon: '⏳', labelKey: 'integ.tabPending' },
    { key: 'active'  as const, icon: '✅', labelKey: 'integ.tabActive'  },
    { key: 'history' as const, icon: '📋', labelKey: 'integ.tabHistory' },
  ];

  pending()  { return this.allLinks().filter(l => l.status === ExternalLinkStatus.Pending); }
  approved() { return this.allLinks().filter(l => l.status === ExternalLinkStatus.Approved); }
  rejected() { return this.allLinks().filter(l => l.status === ExternalLinkStatus.Rejected); }
  revoked()  { return this.allLinks().filter(l => l.status === ExternalLinkStatus.Revoked); }
  rejectedAndRevoked() { return [...this.rejected(), ...this.revoked()]; }

  ngOnInit() {
    const clubId = this.auth.clubId();
    if (!clubId) { this.loading.set(false); return; }

    this.api.getClubLinks(clubId).subscribe((links: ExternalLink[]) => {
      this.allLinks.set(links);
      this.loading.set(false);
      if (this.pending().length === 0 && this.approved().length > 0) {
        this.activeTab.set('active');
      }
    });
  }

  tabCount(key: string): number {
    if (key === 'pending') return this.pending().length;
    if (key === 'active')  return this.approved().length;
    return this.rejected().length + this.revoked().length;
  }

  toggleExpand(id: string) {
    this.expandedId.set(this.expandedId() === id ? null : id);
    this.rejectingId.set(null);
  }

  approve(linkId: string) {
    this.actioning.set(true);
    this.api.approveLink(linkId).subscribe({
      next: (updated: ExternalLink) => {
        this.allLinks.update(links => links.map(l => l.id === linkId ? updated : l));
        this.expandedId.set(null);
        this.actioning.set(false);
        this.activeTab.set('active');
      },
      error: () => this.actioning.set(false)
    });
  }

  startReject(linkId: string) {
    this.rejectReason = '';
    this.rejectingId.set(linkId);
  }

  confirmReject(linkId: string) {
    this.api.rejectLink(linkId, this.rejectReason || undefined).subscribe((updated: ExternalLink) => {
      this.allLinks.update(links => links.map(l => l.id === linkId ? updated : l));
      this.rejectingId.set(null);
      this.expandedId.set(null);
    });
  }

  revoke(linkId: string) {
    if (!confirm(this.i18n.t('integ.revokeConfirm'))) return;
    this.api.revokeLink(linkId, 'Revoked by club manager').subscribe(() => {
      this.allLinks.update(links =>
        links.map(l => l.id === linkId
          ? { ...l, status: ExternalLinkStatus.Revoked, statusName: 'Revoked' }
          : l));
      this.activeTab.set('history');
    });
  }

  previewData(link: ExternalLink) {
    this.previewLink.set(link);
    this.previewLoading.set(true);
    this.previewSummary.set(null);
    this.previewResults.set([]);
    this.previewLoading.set(false);
  }

  closePreview() {
    this.previewLink.set(null);
    this.previewSummary.set(null);
  }

  copyToken(token: string) {
    navigator.clipboard.writeText(token).catch(() => {});
  }

  achIcon(cat: string): string {
    if (cat === 'AcePigeon')      return '🕊️';
    if (cat === 'SuperAcePigeon') return '⭐';
    if (cat === 'BestLoft')       return '🏠';
    return '🏆';
  }

  statusBadge(status: ExternalLinkStatus): string {
    const m: Record<number, string> = {
      1: 'pr-badge--warning',
      2: 'pr-badge--success',
      3: 'pr-badge--error',
      4: 'pr-badge--muted'
    };
    return m[status] ?? 'pr-badge--muted';
  }

  rankClass(rank?: number | null): string {
    if (!rank) return 'pr-rank--other';
    return rank <= 3 ? `pr-rank--${rank}` : 'pr-rank--other';
  }
}
