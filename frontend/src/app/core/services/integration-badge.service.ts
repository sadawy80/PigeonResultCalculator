import { Injectable, inject, signal, effect } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from './services';

// ─────────────────────────────────────────────────────────────────────────────
//  IntegrationBadgeService
//  Polls (or receives SSE/push) the count of pending integration requests
//  so the shell sidebar can show a badge on the Integrations nav item.
// ─────────────────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class IntegrationBadgeService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);

  readonly pendingCount = signal(0);

  private pollInterval?: ReturnType<typeof setInterval>;

  constructor() {
    effect(() => {
      const isClubManager = this.auth.isClubManager();
      const clubId = this.auth.clubId();
      if (isClubManager && clubId) {
        this.startPolling(clubId);
      } else {
        this.stopPolling();
      }
    });
  }

  private startPolling(clubId: string) {
    this.fetchCount(clubId);
    this.stopPolling();
    // Poll every 60 seconds
    this.pollInterval = setInterval(() => this.fetchCount(clubId), 60_000);
  }

  private stopPolling() {
    if (this.pollInterval) {
      clearInterval(this.pollInterval);
      this.pollInterval = undefined;
    }
    this.pendingCount.set(0);
  }

  private fetchCount(clubId: string) {
    this.http.get<{ data: any[] }>(
      `${environment.apiUrl}/integrations/club/${clubId}/links?status=1`
    ).subscribe({
      next: r => this.pendingCount.set(r.data?.length ?? 0),
      error: () => { /* silently ignore */ }
    });
  }

  /** Call after approving/rejecting to immediately update the badge */
  refresh(clubId: string) {
    this.fetchCount(clubId);
  }
}
