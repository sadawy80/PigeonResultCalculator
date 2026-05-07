import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ThemeService } from '../../core/services/services';
import { SiteTheme, RaceResult, Race } from '../../core/models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-public-club-page',
  standalone: true,
  imports: [DatePipe, DecimalPipe],
  templateUrl: './public-club-page.component.html',
  styleUrls: ['./public-club-page.component.scss']
})
export class PublicClubPageComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private http  = inject(HttpClient);
  private theme = inject(ThemeService);

  loading        = signal(true);
  club           = signal<any | null>(null);
  publishedRaces = signal<Race[]>([]);
  liveRaces      = signal<any[]>([]);
  resultsByRace  = signal<Record<string, RaceResult[]>>({});
  announcements  = signal<any[]>([]);
  currentYear    = new Date().getFullYear();

  ngOnInit() {
    const slug = this.route.snapshot.paramMap.get('slug')!;

    this.http.get<any>(`${environment.apiUrl}/public/clubs/${slug}`).subscribe({
      next: page => {
        this.club.set(page.club);
        this.publishedRaces.set(page.races ?? []);
        this.liveRaces.set(page.liveRaces ?? []);
        this.announcements.set(page.announcements ?? []);

        if (page.theme) this.theme.applyTheme(page.theme as SiteTheme);

        const raceMap: Record<string, RaceResult[]> = {};
        (page.races ?? []).forEach((r: Race) => { raceMap[r.id] = page.resultsByRace?.[r.id] ?? []; });
        this.resultsByRace.set(raceMap);
        this.loading.set(false);
      },
      error: () => {
        this.club.set(null);
        this.loading.set(false);
      }
    });
  }

  rankClass(rank?: number | null) {
    if (!rank) return 'pr-rank--other';
    return rank <= 3 ? `pr-rank--${rank}` : 'pr-rank--other';
  }
}
