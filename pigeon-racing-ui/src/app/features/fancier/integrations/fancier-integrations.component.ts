import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe, DecimalPipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IntegrationApiService } from '../../../core/services/integration-api.service';
import { AuthService } from '../../../core/services/services';
import { TranslationService, TranslatePipe } from '../../../core/i18n';
import {
  ExternalLink, ExternalLinkStatus,
  IntegrationRaceResult, IntegrationAcePigeon,
  IntegrationSuperAce, IntegrationBestLoft, IntegrationSummary
} from '../../../core/models/integration.models';

// ─────────────────────────────────────────────────────────────────────────────
//  FancierIntegrationsComponent — /fancier/integrations
//
//  The fancier sees their own links: status, token, and (if approved)
//  a live view of exactly what PLM can read from their account.
// ─────────────────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-fancier-integrations',
  standalone: true,
  imports: [DatePipe, DecimalPipe, NgClass, FormsModule, TranslatePipe],
  template: `
    <div class="pr-page-header">
      <h1 class="pr-page-header__title">🔗 {{ 'fancierInteg.title' | translate }}</h1>
      <p class="pr-page-header__subtitle">{{ 'fancierInteg.subtitle' | translate }}</p>
    </div>

    @if (loading()) {
      <div class="pr-empty"><div class="pr-spinner" style="margin:0 auto"></div></div>
    } @else if (links().length === 0) {

      <!-- Explainer for fanciers with no links -->
      <div class="fi-explainer pr-card">
        <div class="fi-explainer__hero">🔗</div>
        <h2>{{ 'fancierInteg.explainerTitle' | translate }}</h2>
        <p>{{ 'fancierInteg.explainerDesc' | translate }}</p>

        <div class="fi-how-it-works">
          <h3>{{ 'fancierInteg.howToLink' | translate }}</h3>
          <ol class="fi-steps">
            <li>{{ 'fancierInteg.step1' | translate }}</li>
            <li>{{ 'fancierInteg.step2' | translate }}</li>
            <li>{{ 'fancierInteg.step3' | translate }}: <code class="ring-code">{{ auth.clubId() ?? '—' }}</code></li>
            <li>{{ 'fancierInteg.step4' | translate }}</li>
            <li>{{ 'fancierInteg.step5' | translate }}</li>
            <li>{{ 'fancierInteg.step6' | translate }}</li>
          </ol>
        </div>

        <div class="fi-note">
          <strong>🔒</strong> {{ 'fancierInteg.privacy' | translate }}
        </div>
      </div>

    } @else {

      <!-- Link cards -->
      <div class="fi-link-list">
        @for (link of links(); track link.id) {
          <div class="fi-link-card pr-card fi-link-card--{{ statusClass(link.status) }}">

            <!-- Header row -->
            <div class="fi-link-card__header">
              <div class="fi-platform">
                <span class="fi-platform__icon">🏠</span>
                <span class="fi-platform__name">{{ link.externalPlatformName }}</span>
              </div>
              <span [class]="'pr-badge ' + statusBadge(link.status)">
                {{ statusLabel(link.status) }}
              </span>
            </div>

            <div class="fi-link-card__loft">{{ link.externalLoftName }}</div>
            <div class="text-muted text-sm">{{ 'fancierInteg.club' | translate }}: {{ link.clubName }}</div>

            <!-- STATUS-SPECIFIC CONTENT -->

            <!-- PENDING -->
            @if (link.status === ExternalLinkStatus.Pending) {
              <div class="fi-status-block fi-status-block--pending">
                <div class="fi-status-block__icon">⏳</div>
                <div>
                  <div class="font-bold">{{ 'fancierInteg.awaitingApproval' | translate }}</div>
                  <div class="text-muted text-sm mt-1">
                    {{ i18n.t('fancierInteg.requestSent', { date: (link.requestedAt | date:'dd MMM yyyy') ?? '' }) }}
                  </div>
                  <div class="fi-token-row mt-3">
                    <div class="fi-token-label">{{ 'fancierInteg.linkTokenHint' | translate }}:</div>
                    <div class="fi-token-value">
                      <code class="ring-code">{{ link.linkToken }}</code>
                      <button class="pr-btn pr-btn--ghost pr-btn--sm ml-2"
                              (click)="copy(link.linkToken)">📋</button>
                    </div>
                    <div class="text-muted text-sm mt-1">{{ 'fancierInteg.tokenDesc' | translate }}</div>
                  </div>
                </div>
              </div>
            }

            <!-- APPROVED -->
            @if (link.status === ExternalLinkStatus.Approved) {
              <div class="fi-status-block fi-status-block--approved">
                <div class="fi-approved-meta">
                  <div class="fi-approved-meta__item">
                    <span class="fi-approved-meta__label">{{ 'fancierInteg.approved' | translate }}</span>
                    <span>{{ link.approvedAt | date:'dd MMM yyyy' }}</span>
                  </div>
                  <div class="fi-approved-meta__item">
                    <span class="fi-approved-meta__label">{{ 'fancierInteg.approvedBy' | translate }}</span>
                    <span>{{ link.reviewedByName ?? ('fancierInteg.clubManager' | translate) }}</span>
                  </div>
                  <div class="fi-approved-meta__item">
                    <span class="fi-approved-meta__label">{{ 'fancierInteg.lastAccessed' | translate }}</span>
                    <span>{{ link.lastDataAccessAt ? (link.lastDataAccessAt | date:'dd MMM HH:mm') : ('fancierInteg.notYet' | translate) }}</span>
                  </div>
                </div>

                <!-- Preview tabs -->
                <div class="fi-data-tabs mt-4">
                  @for (tab of dataTabs; track tab.key) {
                    <button
                      class="fi-data-tab"
                      [class.fi-data-tab--active]="activeDataTab() === tab.key"
                      (click)="loadDataTab(tab.key)">
                      {{ tab.icon }} {{ tab.labelKey | translate }}
                    </button>
                  }
                </div>

                <!-- Summary tab -->
                @if (activeDataTab() === 'summary') {
                  @if (summaryLoading()) {
                    <div class="pr-empty py-4"><div class="pr-spinner" style="margin:0 auto"></div></div>
                  } @else if (summary()) {
                    <div class="fi-summary mt-4">
                      <div class="fi-kpi-grid">
                        <div class="fi-kpi">
                          <span class="fi-kpi__val">{{ summary()!.totalRaceResults }}</span>
                          <span class="fi-kpi__label">{{ 'integ.raceResults' | translate }}</span>
                        </div>
                        <div class="fi-kpi">
                          <span class="fi-kpi__val">{{ summary()!.totalAcePigeonResults }}</span>
                          <span class="fi-kpi__label">🕊️ {{ 'integ.acePigeon' | translate }}</span>
                        </div>
                        <div class="fi-kpi">
                          <span class="fi-kpi__val">{{ summary()!.totalSuperAcePigeonResults }}</span>
                          <span class="fi-kpi__label">⭐ {{ 'integ.superAce' | translate }}</span>
                        </div>
                        <div class="fi-kpi">
                          <span class="fi-kpi__val">{{ summary()!.totalBestLoftResults }}</span>
                          <span class="fi-kpi__label">🏠 {{ 'integ.bestLoft' | translate }}</span>
                        </div>
                        <div class="fi-kpi">
                          <span class="fi-kpi__val">#{{ summary()!.bestEverClubRank || '—' }}</span>
                          <span class="fi-kpi__label">{{ 'integ.bestRank' | translate }}</span>
                        </div>
                        <div class="fi-kpi">
                          <span class="fi-kpi__val">{{ summary()!.bestEverVelocityMperMin | number:'1.0-0' }}</span>
                          <span class="fi-kpi__label">{{ 'integ.bestMpm' | translate }}</span>
                        </div>
                      </div>

                      @if (summary()!.achievements.length > 0) {
                        <h4 class="mt-4 mb-3">{{ 'fancierInteg.achievementsVisible' | translate }}</h4>
                        <div class="fi-ach-list">
                          @for (a of summary()!.achievements; track a.description) {
                            <div class="fi-ach" [class]="'fi-ach--' + a.category.toLowerCase()">
                              <span class="fi-ach__icon">{{ achIcon(a.category) }}</span>
                              <span class="fi-ach__text">{{ a.description }}</span>
                              <span class="fi-ach__score">{{ a.score | number:'1.2-2' }} pts</span>
                            </div>
                          }
                        </div>
                      }
                    </div>
                  }
                }

                <!-- Race results tab -->
                @if (activeDataTab() === 'results') {
                  @if (resultsLoading()) {
                    <div class="pr-empty py-4"><div class="pr-spinner" style="margin:0 auto"></div></div>
                  } @else {
                    <div class="fi-results mt-4">
                      <table class="pr-table">
                        <thead>
                          <tr>
                            <th>{{ 'result.rank' | translate }}</th>
                            <th>{{ 'result.ring' | translate }}</th>
                            <th>{{ 'result.pigeon' | translate }}</th>
                            <th>{{ 'race.race' | translate }}</th>
                            <th>{{ 'result.mPerMin' | translate }}</th>
                            <th>{{ 'result.dist' | translate }}</th>
                            <th>{{ 'race.category' | translate }}</th>
                          </tr>
                        </thead>
                        <tbody>
                          @for (r of results(); track r.ringNumber + r.raceName) {
                            <tr>
                              <td>
                                <span [class]="'pr-rank ' + rankClass(r.clubRank)">
                                  {{ r.clubRank ?? '—' }}
                                </span>
                              </td>
                              <td><code class="ring-code">{{ r.ringNumber }}</code></td>
                              <td>{{ r.pigeonName ?? '—' }}</td>
                              <td class="text-sm">
                                {{ r.raceName }}
                                <div class="text-muted" style="font-size:0.7rem">{{ r.raceDate | date:'dd MMM yyyy' }}</div>
                              </td>
                              <td class="font-bold">{{ r.velocityMperMin | number:'1.4-4' }}</td>
                              <td class="text-sm">{{ r.distanceKm | number:'1.0-0' }} km</td>
                              <td class="flex gap-1 flex-wrap">
                                @if (r.isAcePigeon) {
                                  <span class="fi-badge fi-badge--ace">🕊️ {{ 'fancierInteg.tabAce' | translate }} #{{ r.aceRank }}</span>
                                }
                                @if (r.isSuperAcePigeon) {
                                  <span class="fi-badge fi-badge--super">⭐ {{ 'fancierInteg.tabSuperAce' | translate }} #{{ r.superAceRank }}</span>
                                }
                                @if (r.isBestLoft) {
                                  <span class="fi-badge fi-badge--loft">🏠 {{ 'integ.bestLoft' | translate }} #{{ r.loftRank }}</span>
                                }
                              </td>
                            </tr>
                          }
                        </tbody>
                      </table>
                    </div>
                  }
                }

                <!-- Ace Pigeon tab -->
                @if (activeDataTab() === 'ace') {
                  @if (aceLoading()) {
                    <div class="pr-empty py-4"><div class="pr-spinner" style="margin:0 auto"></div></div>
                  } @else if (acePigeons().length === 0) {
                    <div class="pr-empty py-4">
                      <div class="pr-empty__title">{{ 'fancierInteg.noAce' | translate }}</div>
                    </div>
                  } @else {
                    <table class="pr-table mt-4">
                      <thead>
                        <tr>
                          <th>{{ 'fancierInteg.colAceRank' | translate }}</th>
                          <th>{{ 'result.ring' | translate }}</th>
                          <th>{{ 'result.pigeon' | translate }}</th>
                          <th>{{ 'programme.programme' | translate }}</th>
                          <th>{{ 'result.yearOfBirth' | translate }}</th>
                          <th>{{ 'result.score' | translate }}</th>
                          <th>{{ 'race.races' | translate }}</th>
                          <th>{{ 'integ.bestMpm' | translate }}</th>
                        </tr>
                      </thead>
                      <tbody>
                        @for (a of acePigeons(); track a.ringNumber + a.programmeName) {
                          <tr>
                            <td><span class="pr-rank pr-rank--{{ a.aceRank <= 3 ? a.aceRank : 'other' }}" style="width:auto;padding:2px 8px;border-radius:4px">{{ a.aceRank }}</span></td>
                            <td><code class="ring-code">{{ a.ringNumber }}</code></td>
                            <td>{{ a.pigeonName ?? '—' }} <span class="text-muted text-sm">{{ a.pigeonSex }}</span></td>
                            <td>{{ a.programmeName }}</td>
                            <td>{{ a.programmeYear }}</td>
                            <td class="font-bold">{{ a.totalScore | number:'1.2-2' }}</td>
                            <td>{{ a.racesEntered }}/{{ a.racesInProgramme }}</td>
                            <td>{{ a.bestVelocityMperMin | number:'1.4-4' }}</td>
                          </tr>
                        }
                      </tbody>
                    </table>
                  }
                }

                <!-- Super Ace tab -->
                @if (activeDataTab() === 'superAce') {
                  @if (superAceLoading()) {
                    <div class="pr-empty py-4"><div class="pr-spinner" style="margin:0 auto"></div></div>
                  } @else if (superAces().length === 0) {
                    <div class="pr-empty py-4">
                      <div class="pr-empty__title">{{ 'fancierInteg.noSuperAce' | translate }}</div>
                    </div>
                  } @else {
                    <table class="pr-table mt-4">
                      <thead>
                        <tr>
                          <th>{{ 'fancierInteg.colSuperRank' | translate }}</th>
                          <th>{{ 'result.ring' | translate }}</th>
                          <th>{{ 'result.pigeon' | translate }}</th>
                          <th>{{ 'programme.programme' | translate }}</th>
                          <th>{{ 'result.yearOfBirth' | translate }}</th>
                          <th>{{ 'result.score' | translate }}</th>
                          <th>{{ 'result.participationRate' | translate }}</th>
                          <th>{{ 'integ.bestMpm' | translate }}</th>
                        </tr>
                      </thead>
                      <tbody>
                        @for (a of superAces(); track a.ringNumber + a.programmeName) {
                          <tr>
                            <td><span class="pr-rank pr-rank--{{ a.superAceRank <= 3 ? a.superAceRank : 'other' }}" style="width:auto;padding:2px 8px;border-radius:4px">{{ a.superAceRank }}</span></td>
                            <td><code class="ring-code">{{ a.ringNumber }}</code></td>
                            <td>{{ a.pigeonName ?? '—' }}</td>
                            <td>{{ a.programmeName }}</td>
                            <td>{{ a.programmeYear }}</td>
                            <td class="font-bold">{{ a.totalScore | number:'1.2-2' }}</td>
                            <td>{{ a.participationRate | number:'1.0-0' }}%</td>
                            <td>{{ a.bestVelocityMperMin | number:'1.4-4' }}</td>
                          </tr>
                        }
                      </tbody>
                    </table>
                  }
                }

                <!-- Best Loft tab -->
                @if (activeDataTab() === 'bestLoft') {
                  @if (bestLoftLoading()) {
                    <div class="pr-empty py-4"><div class="pr-spinner" style="margin:0 auto"></div></div>
                  } @else if (bestLofts().length === 0) {
                    <div class="pr-empty py-4">
                      <div class="pr-empty__title">{{ 'fancierInteg.noBestLoft' | translate }}</div>
                    </div>
                  } @else {
                    <table class="pr-table mt-4">
                      <thead>
                        <tr>
                          <th>{{ 'result.rank' | translate }}</th>
                          <th>{{ 'programme.programme' | translate }}</th>
                          <th>{{ 'result.yearOfBirth' | translate }}</th>
                          <th>{{ 'result.totalScore' | translate }}</th>
                          <th>{{ 'race.races' | translate }}</th>
                          <th>{{ 'fancierInteg.colPigeons' | translate }}</th>
                          <th>{{ 'integ.bestMpm' | translate }}</th>
                        </tr>
                      </thead>
                      <tbody>
                        @for (b of bestLofts(); track b.programmeName + b.programmeYear) {
                          <tr>
                            <td><span class="pr-rank pr-rank--{{ b.loftRank <= 3 ? b.loftRank : 'other' }}" style="width:auto;padding:2px 8px;border-radius:4px">{{ b.loftRank }}</span></td>
                            <td>{{ b.programmeName }}</td>
                            <td>{{ b.programmeYear }}</td>
                            <td class="font-bold">{{ b.totalScore | number:'1.2-2' }}</td>
                            <td>{{ b.racesEntered }}</td>
                            <td>{{ b.pigeonsEntered }}</td>
                            <td>{{ b.bestSingleVelocityMperMin | number:'1.4-4' }}</td>
                          </tr>
                        }
                      </tbody>
                    </table>
                  }
                }

              </div>

              <!-- Revoke button for active links -->
              <div class="fi-link-card__footer">
                <button class="pr-btn pr-btn--danger pr-btn--outline pr-btn--sm"
                        (click)="revokeLink(link.id)">
                  {{ 'fancierInteg.revokeAccess' | translate }}
                </button>
                <div class="text-muted text-sm">{{ 'fancierInteg.revokeDesc' | translate }}</div>
              </div>
            }

            <!-- REJECTED -->
            @if (link.status === ExternalLinkStatus.Rejected) {
              <div class="fi-status-block fi-status-block--rejected">
                <div class="fi-status-block__icon">❌</div>
                <div>
                  <div class="font-bold">{{ 'fancierInteg.requestRejected' | translate }}</div>
                  @if (link.rejectionReason) {
                    <div class="text-muted text-sm mt-1">
                      {{ 'fancierInteg.rejectedReason' | translate }}: {{ link.rejectionReason }}
                    </div>
                  }
                  <div class="text-muted text-sm mt-1">
                    {{ i18n.t('fancierInteg.rejectedOn', { date: (link.rejectedAt | date:'dd MMM yyyy') ?? '' }) }}
                    @if (link.reviewedByName) {
                      {{ i18n.t('fancierInteg.rejectedBy', { name: link.reviewedByName }) }}
                    }
                  </div>
                  <div class="mt-3 text-sm">{{ 'fancierInteg.rejectedRetry' | translate }}</div>
                </div>
              </div>
            }

            <!-- REVOKED -->
            @if (link.status === ExternalLinkStatus.Revoked) {
              <div class="fi-status-block fi-status-block--revoked">
                <div class="fi-status-block__icon">🚫</div>
                <div>
                  <div class="font-bold">{{ 'fancierInteg.accessRevoked' | translate }}</div>
                  @if (link.revokedReason) {
                    <div class="text-muted text-sm mt-1">{{ link.revokedReason }}</div>
                  }
                  <div class="text-muted text-sm mt-1">
                    {{ i18n.t('fancierInteg.revokedOn', { date: (link.revokedAt | date:'dd MMM yyyy') ?? '' }) }}
                  </div>
                  <div class="mt-3 text-sm">{{ 'fancierInteg.revokedRetry' | translate }}</div>
                </div>
              </div>
            }

          </div>
        }
      </div>
    }
  `,
  styles: [`
    .fi-explainer { padding:32px; text-align:center; display:flex; flex-direction:column; align-items:center; gap:16px; }
    .fi-explainer__hero { font-size:4rem; }
    .fi-how-it-works { text-align:left; background:var(--pr-surface-2); border-radius:var(--pr-radius); padding:16px 20px; width:100%; max-width:520px; }
    .fi-steps { padding-left:20px; display:flex; flex-direction:column; gap:8px; margin-top:10px; font-size:0.9rem; line-height:1.5; }
    .fi-note { background:rgba(30,144,255,.06); border:1px solid rgba(30,144,255,.2); border-radius:var(--pr-radius); padding:12px 16px; font-size:0.875rem; max-width:520px; text-align:left; }

    .fi-link-list { display:flex; flex-direction:column; gap:16px; }
    .fi-link-card { padding:20px; }
    .fi-link-card--approved { border-left:4px solid #2D6A4F; }
    .fi-link-card--pending  { border-left:4px solid #F59E0B; }
    .fi-link-card--rejected { border-left:4px solid #C1121F; opacity:.85; }
    .fi-link-card--revoked  { border-left:4px solid #6B7280; opacity:.75; }

    .fi-link-card__header { display:flex; justify-content:space-between; align-items:center; margin-bottom:6px; }
    .fi-platform { display:inline-flex; align-items:center; gap:6px; background:var(--pr-surface-2); border:1px solid var(--pr-border); border-radius:var(--pr-radius); padding:4px 10px; font-size:0.82rem; font-weight:600; }
    .fi-platform__icon { font-size:1rem; }
    .fi-link-card__loft { font-size:1.1rem; font-weight:700; margin-bottom:2px; }

    .fi-status-block { display:flex; gap:14px; align-items:flex-start; background:var(--pr-surface-2); border-radius:var(--pr-radius); padding:16px; margin-top:14px; }
    .fi-status-block__icon { font-size:1.5rem; flex-shrink:0; }
    .fi-status-block--pending  { border:1px solid #F59E0B22; }
    .fi-status-block--approved { border:1px solid #2D6A4F22; background:transparent; display:block; }
    .fi-status-block--rejected { border:1px solid #C1121F22; }
    .fi-status-block--revoked  { border:1px solid #6B728022; }

    .fi-token-row { display:flex; flex-direction:column; gap:4px; }
    .fi-token-label { font-size:0.78rem; font-weight:700; text-transform:uppercase; letter-spacing:0.06em; color:var(--pr-text-muted); }
    .fi-token-value { display:flex; align-items:center; }

    .fi-approved-meta { display:flex; gap:24px; flex-wrap:wrap; margin-bottom:8px; }
    .fi-approved-meta__item { display:flex; flex-direction:column; font-size:0.875rem; }
    .fi-approved-meta__label { font-size:0.72rem; font-weight:700; text-transform:uppercase; letter-spacing:0.06em; color:var(--pr-text-muted); }

    .fi-data-tabs { display:flex; gap:4px; border-bottom:2px solid var(--pr-border); }
    .fi-data-tab { display:flex; align-items:center; gap:6px; padding:8px 14px; background:none; border:none; cursor:pointer; font-size:0.85rem; color:var(--pr-text-muted); border-bottom:2px solid transparent; margin-bottom:-2px; transition:all var(--t-fast); }
    .fi-data-tab:hover { color:var(--pr-text); }
    .fi-data-tab--active { color:var(--pr-primary); border-bottom-color:var(--pr-primary); font-weight:600; }

    .fi-kpi-grid { display:grid; grid-template-columns:repeat(6,1fr); gap:10px; }
    @media(max-width:700px) { .fi-kpi-grid{grid-template-columns:repeat(3,1fr)} }
    .fi-kpi { text-align:center; background:var(--pr-surface-2); border-radius:var(--pr-radius); padding:10px 6px; }
    .fi-kpi__val { display:block; font-size:1.4rem; font-weight:900; color:var(--pr-primary); }
    .fi-kpi__label { font-size:0.68rem; text-transform:uppercase; letter-spacing:0.06em; color:var(--pr-text-muted); }

    .fi-ach-list { display:flex; flex-direction:column; gap:8px; }
    .fi-ach { display:flex; align-items:center; gap:10px; background:var(--pr-surface-2); border-radius:var(--pr-radius); padding:10px 12px; }
    .fi-ach--acepigeon    { border-left:3px solid #1E3A5F; }
    .fi-ach--superacepigeon { border-left:3px solid #C9A84C; }
    .fi-ach--bestloft     { border-left:3px solid #2D6A4F; }
    .fi-ach__icon  { font-size:1.2rem; flex-shrink:0; }
    .fi-ach__text  { flex:1; font-size:0.875rem; }
    .fi-ach__score { font-weight:700; font-size:0.85rem; color:var(--pr-primary); }

    .fi-badge { font-size:0.68rem; font-weight:700; padding:2px 7px; border-radius:3px; white-space:nowrap; }
    .fi-badge--ace   { background:rgba(30,58,95,.1);   color:#1E3A5F; }
    .fi-badge--super { background:rgba(201,168,76,.15); color:#7D5A00; }
    .fi-badge--loft  { background:rgba(45,106,79,.1);  color:#2D6A4F; }

    .fi-link-card__footer { display:flex; align-items:center; justify-content:space-between; gap:12px; margin-top:14px; padding-top:12px; border-top:1px solid var(--pr-border); }
    .ring-code { font-size:0.8rem; background:var(--pr-surface-2); padding:2px 6px; border-radius:4px; }

    .py-4 { padding:24px 0; }
  `]
})
export class FancierIntegrationsComponent implements OnInit {
  private api  = inject(IntegrationApiService);
  auth         = inject(AuthService);
  i18n         = inject(TranslationService);

  ExternalLinkStatus = ExternalLinkStatus;

  links   = signal<ExternalLink[]>([]);
  loading = signal(true);

  // Data state (for active links)
  activeDataTab    = signal<'summary' | 'results' | 'ace' | 'superAce' | 'bestLoft'>('summary');
  summary          = signal<IntegrationSummary | null>(null);
  summaryLoading   = signal(false);
  results          = signal<IntegrationRaceResult[]>([]);
  resultsLoading   = signal(false);
  acePigeons       = signal<IntegrationAcePigeon[]>([]);
  aceLoading       = signal(false);
  superAces        = signal<IntegrationSuperAce[]>([]);
  superAceLoading  = signal(false);
  bestLofts        = signal<IntegrationBestLoft[]>([]);
  bestLoftLoading  = signal(false);

  // Currently selected active link (for data display)
  private activeLink: ExternalLink | null = null;

  dataTabs = [
    { key: 'summary'  as const, icon: '📊', labelKey: 'fancierInteg.tabSummary'  },
    { key: 'results'  as const, icon: '🏁', labelKey: 'fancierInteg.tabResults'  },
    { key: 'ace'      as const, icon: '🕊️', labelKey: 'fancierInteg.tabAce'      },
    { key: 'superAce' as const, icon: '⭐', labelKey: 'fancierInteg.tabSuperAce' },
    { key: 'bestLoft' as const, icon: '🏠', labelKey: 'fancierInteg.tabBestLoft' },
  ];

  ngOnInit() {
    this.api.getMyLinks().subscribe(links => {
      this.links.set(links);
      this.loading.set(false);
      // Auto-load data for the first active link
      const active = links.find(l => l.status === ExternalLinkStatus.Approved);
      if (active) this.autoLoadActive(active);
    });
  }

  private autoLoadActive(link: ExternalLink) {
    this.activeLink = link;
    this.loadDataTab('summary');
  }

  loadDataTab(key: typeof this.activeDataTab extends { set: (v: infer T) => void } ? T : never) {
    this.activeDataTab.set(key as any);

    if (!this.activeLink) return;
    // We use the link token as a proxy; in prod the fancier would have
    // their own session-based view endpoint. For now reuse the same token endpoint.
    const token = this.activeLink.linkToken; // Note: this is linkToken not accessToken
    // In a production setup, there would be a fancier-specific preview endpoint
    // using their JWT. Here we demonstrate the data shape using the linkToken.

    if (key === 'summary' && !this.summary()) {
      this.summaryLoading.set(true);
      this.api.getSummary(token).subscribe({
        next: s => { this.summary.set(s); this.summaryLoading.set(false); },
        error: () => this.summaryLoading.set(false)
      });
    }
    if (key === 'results' && this.results().length === 0) {
      this.resultsLoading.set(true);
      this.api.getResults(token).subscribe({
        next: p => { this.results.set(p.items as IntegrationRaceResult[]); this.resultsLoading.set(false); },
        error: () => this.resultsLoading.set(false)
      });
    }
    if (key === 'ace' && this.acePigeons().length === 0) {
      this.aceLoading.set(true);
      this.api.getAcePigeon(token).subscribe({
        next: a => { this.acePigeons.set(a); this.aceLoading.set(false); },
        error: () => this.aceLoading.set(false)
      });
    }
    if (key === 'superAce' && this.superAces().length === 0) {
      this.superAceLoading.set(true);
      this.api.getSuperAce(token).subscribe({
        next: a => { this.superAces.set(a as IntegrationSuperAce[]); this.superAceLoading.set(false); },
        error: () => this.superAceLoading.set(false)
      });
    }
    if (key === 'bestLoft' && this.bestLofts().length === 0) {
      this.bestLoftLoading.set(true);
      this.api.getBestLoft(token).subscribe({
        next: b => { this.bestLofts.set(b); this.bestLoftLoading.set(false); },
        error: () => this.bestLoftLoading.set(false)
      });
    }
  }

  revokeLink(linkId: string) {
    if (!confirm(this.i18n.t('fancierInteg.revokeConfirm'))) return;
    this.api.revokeMyLink(linkId).subscribe(() => {
      this.links.update(ls =>
        ls.map(l => l.id === linkId
          ? { ...l, status: ExternalLinkStatus.Revoked, statusName: 'Revoked' }
          : l));
      this.summary.set(null);
      this.results.set([]);
    });
  }

  copy(text: string) { navigator.clipboard.writeText(text).catch(() => {}); }

  achIcon(cat: string): string {
    if (cat === 'AcePigeon') return '🕊️';
    if (cat === 'SuperAcePigeon') return '⭐';
    return '🏠';
  }

  statusClass(s: ExternalLinkStatus): string {
    return { 1: 'pending', 2: 'approved', 3: 'rejected', 4: 'revoked' }[s] ?? 'pending';
  }

  statusLabel(s: ExternalLinkStatus): string {
    const key = ({ 1: 'fancierInteg.statusPending', 2: 'fancierInteg.statusActive',
                   3: 'fancierInteg.statusRejected', 4: 'fancierInteg.statusRevoked' } as Record<number,string>)[s];
    return key ? this.i18n.t(key) : '';
  }

  statusBadge(s: ExternalLinkStatus): string {
    return { 1: 'pr-badge--warning', 2: 'pr-badge--success', 3: 'pr-badge--error', 4: 'pr-badge--muted' }[s] ?? 'pr-badge--muted';
  }

  rankClass(rank?: number | null): string {
    if (!rank) return 'pr-rank--other';
    return rank <= 3 ? `pr-rank--${rank}` : 'pr-rank--other';
  }
}
