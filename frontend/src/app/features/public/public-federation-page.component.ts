import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-public-federation-page',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe],
  template: `
    @if (loading()) {
      <div class="pub-loading">
        <div class="pr-spinner" style="margin:0 auto 16px"></div>
        <p>Loading...</p>
      </div>
    } @else if (notFound()) {
      <div class="pub-not-found">
        <div style="font-size:4rem">🌍</div>
        <h1>federation page not found</h1>
        <p>This federation page may not be published yet.</p>
        <a routerLink="/" class="pr-btn pr-btn--primary" style="margin-top:16px">Go Home</a>
      </div>
    } @else {
      <div class="pub-country theme-{{ pageData()?.theme ?? 1 }}">

        <!-- Hero -->
        <header class="pub-country__hero">
          <div class="pub-country__hero-content">
            <div class="pub-country__flag-code">{{ pageData()?.country?.code }}</div>
            <h1 class="pub-country__title">{{ pageData()?.country?.name }}</h1>
            <p class="pub-country__meta">
              {{ pageData()?.clubCount }} active clubs · Pigeon Racing Federation
            </p>
          </div>
        </header>

        <div class="pub-country__body">

          <!-- Announcements -->
          @if (pageData()?.announcements?.length > 0) {
            <section class="pub-section">
              <h2 class="pub-section__title">Announcements</h2>
              <div class="pub-announcements">
                @for (a of pageData()?.announcements; track $index) {
                  <div class="pub-announcement">
                    <div class="pub-announcement__title">{{ a.title }}</div>
                    @if (a.body) { <div class="pub-announcement__body">{{ a.body }}</div> }
                    @if (a.date) { <div class="pub-announcement__date">{{ a.date | date:'dd MMM yyyy' }}</div> }
                  </div>
                }
              </div>
            </section>
          }

          <!-- Recent national results -->
          @if (pageData()?.recentResults?.length > 0) {
            <section class="pub-section">
              <h2 class="pub-section__title">Recent National Results</h2>
              <div class="pub-results-list">
                @for (r of pageData()?.recentResults; track r.id) {
                  <div class="pub-result-card">
                    <div class="pub-result-card__header">
                      <div>
                        <div class="pub-result-card__name">{{ r.name }}</div>
                        @if (r.description) { <div class="pub-result-card__desc">{{ r.description }}</div> }
                      </div>
                      <div class="pub-result-card__meta">
                        <span>{{ r.totalClubsCount }} clubs</span>
                        <span>{{ r.totalEntriesCount }} entries</span>
                        <span>{{ r.publishedAt | date:'dd MMM yyyy' }}</span>
                      </div>
                    </div>
                    <div class="pr-table-wrapper mt-3">
                      <table class="pr-table pr-table--compact">
                        <thead>
                          <tr><th>#</th><th>Ring</th><th>Fancier</th><th>Club</th><th style="text-align:right">Velocity (m/min)</th></tr>
                        </thead>
                        <tbody>
                          @for (e of r.topEntries; track e.nationalRank) {
                            <tr>
                              <td><span class="pr-rank pr-rank--{{ e.nationalRank <= 3 ? e.nationalRank : 'other' }}">{{ e.nationalRank }}</span></td>
                              <td><code style="font-size:0.8rem">{{ e.ringNumber }}</code></td>
                              <td>{{ e.fancierName ?? '—' }}</td>
                              <td class="text-muted text-sm">{{ e.clubName }}</td>
                              <td style="text-align:right" class="font-bold">{{ e.velocityMperMin | number:'1.4-4' }}</td>
                            </tr>
                          }
                        </tbody>
                      </table>
                    </div>
                  </div>
                }
              </div>
            </section>
          }

          <!-- Clubs directory -->
          <section class="pub-section">
            <h2 class="pub-section__title">Clubs in {{ pageData()?.country?.name }}</h2>
            @if (pageData()?.clubPages?.length === 0) {
              <p class="text-muted">No published club pages yet.</p>
            } @else {
              <div class="pub-clubs-grid">
                @for (c of pageData()?.clubPages; track c.slug) {
                  <a [routerLink]="['/p', c.slug]" class="pub-club-card">
                    @if (c.logoUrl) {
                      <img [src]="c.logoUrl" [alt]="c.name + ' logo'" class="pub-club-card__logo">
                    } @else {
                      <div class="pub-club-card__logo pub-club-card__logo--placeholder">🕊️</div>
                    }
                    <div class="pub-club-card__name">{{ c.name }}</div>
                    @if (c.city) { <div class="pub-club-card__city">{{ c.city }}</div> }
                  </a>
                }
              </div>
            }
          </section>

        </div>
      </div>
    }
  `,
  styles: [`
    .pub-loading, .pub-not-found {
      min-height:60vh; display:flex; flex-direction:column;
      align-items:center; justify-content:center; text-align:center;
      padding:40px;
    }

    .pub-country__hero {
      background: linear-gradient(135deg, var(--pr-primary) 0%, color-mix(in srgb, var(--pr-primary) 70%, #000) 100%);
      color: #fff; padding: 80px 24px; text-align: center;
    }
    .pub-country__flag-code {
      font-size: 3.5rem; font-weight: 900; letter-spacing: 0.15em;
      opacity: 0.3; margin-bottom: 8px;
    }
    .pub-country__title {
      font-family: var(--font-display); font-size: clamp(2rem, 5vw, 3.5rem);
      font-weight: 800; margin: 0 0 12px;
    }
    .pub-country__meta { opacity: 0.85; font-size: 1rem; }

    .pub-country__body { max-width: 960px; margin: 0 auto; padding: 40px 24px; }

    .pub-section { margin-bottom: 48px; }
    .pub-section__title {
      font-family: var(--font-display); font-size: 1.5rem; font-weight: 700;
      margin: 0 0 20px; padding-bottom: 10px;
      border-bottom: 2px solid var(--pr-border);
    }

    .pub-announcements { display: flex; flex-direction: column; gap: 12px; }
    .pub-announcement {
      background: var(--pr-surface); border: 1px solid var(--pr-border);
      border-left: 4px solid var(--pr-primary);
      border-radius: var(--pr-radius); padding: 16px 20px;
    }
    .pub-announcement__title { font-weight: 700; font-size: 1rem; }
    .pub-announcement__body { color: var(--pr-text-muted); margin-top: 6px; font-size: 0.9rem; }
    .pub-announcement__date { color: var(--pr-text-muted); font-size: 0.8rem; margin-top: 8px; }

    .pub-results-list { display: flex; flex-direction: column; gap: 24px; }
    .pub-result-card {
      background: var(--pr-surface); border: 1px solid var(--pr-border);
      border-radius: calc(var(--pr-radius) * 1.5); padding: 20px 24px;
    }
    .pub-result-card__header { display: flex; justify-content: space-between; align-items: flex-start; gap: 16px; flex-wrap: wrap; }
    .pub-result-card__name { font-weight: 700; font-size: 1.1rem; }
    .pub-result-card__desc { color: var(--pr-text-muted); font-size: 0.875rem; margin-top: 4px; }
    .pub-result-card__meta { display: flex; gap: 12px; font-size: 0.8rem; color: var(--pr-text-muted); flex-wrap: wrap; }

    .pub-clubs-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
      gap: 16px;
    }
    .pub-club-card {
      background: var(--pr-surface); border: 1px solid var(--pr-border);
      border-radius: calc(var(--pr-radius) * 1.5); padding: 20px 12px;
      text-align: center; text-decoration: none; color: inherit;
      transition: border-color 0.2s, box-shadow 0.2s; display: flex;
      flex-direction: column; align-items: center; gap: 8px;
    }
    .pub-club-card:hover { border-color: var(--pr-primary); box-shadow: 0 4px 16px rgba(0,0,0,0.1); }
    .pub-club-card__logo { width: 56px; height: 56px; object-fit: contain; border-radius: 8px; }
    .pub-club-card__logo--placeholder { font-size: 2rem; display: flex; align-items: center; justify-content: center; }
    .pub-club-card__name { font-weight: 700; font-size: 0.875rem; }
    .pub-club-card__city { color: var(--pr-text-muted); font-size: 0.75rem; }

    .pr-table--compact td, .pr-table--compact th { padding: 8px 12px; }
  `]
})
export class PublicFederationPageComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private api   = inject(ApiService);

  pageData  = signal<any>(null);
  loading   = signal(true);
  notFound  = signal(false);

  ngOnInit() {
    const slug = this.route.snapshot.paramMap.get('slug')!;
    this.api.getPublicFederationPage(slug).subscribe({
      next: d  => { this.pageData.set(d); this.loading.set(false); },
      error: () => { this.notFound.set(true); this.loading.set(false); }
    });
  }
}
