import { Injectable, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { CanActivateFn } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { DOCUMENT } from '@angular/common';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { User, UserRole, Theme, SiteTheme } from '../models';
import { ApiService } from './api.service';

// ── Auth Service ──────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class AuthService {
  private api = inject(ApiService);
  private router = inject(Router);

  readonly currentUser = signal<User | null>(this.loadUser());
  readonly isAuthenticated = computed(() => !!this.currentUser());
  readonly isAdmin = computed(() => this.currentUser()?.role === UserRole.SuperAdmin);
  readonly isCountryManager = computed(() => this.currentUser()?.role === UserRole.CountryManager);
  readonly isClubManager = computed(() => this.currentUser()?.role === UserRole.ClubManager);
  readonly isFancier = computed(() => this.currentUser()?.role === UserRole.Fancier);
  readonly canManageRaces = computed(() => {
    const role = this.currentUser()?.role;
    return role != null && [UserRole.SuperAdmin, UserRole.CountryManager, UserRole.ClubManager].includes(role);
  });
  /** Primary club ID for ClubManagers and Fanciers — resolved by the backend on login */
  readonly clubId = computed(() => this.currentUser()?.clubId ?? null);
  /** Country ID for CountryManagers */
  readonly countryId = computed(() => this.currentUser()?.countryId ?? null);

  login(email: string, password: string) {
    return this.api.login(email, password).pipe(
      catchError(err => { throw err; })
    ).subscribe(tokens => {
      localStorage.setItem('access_token', tokens.accessToken);
      localStorage.setItem('refresh_token', tokens.refreshToken);
      localStorage.setItem('user', JSON.stringify(tokens.user));
      this.currentUser.set(tokens.user);
      this.navigateByRole(tokens.user.role);
    });
  }

  logout() {
    const rt = localStorage.getItem('refresh_token');
    if (rt) this.api.revokeToken(rt).subscribe({ error: () => {} });
    localStorage.clear();
    this.currentUser.set(null);
    this.router.navigate(['/auth/login']);
  }

  getAccessToken(): string | null {
    return localStorage.getItem('access_token');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refresh_token');
  }

  setTokens(accessToken: string, refreshToken: string, user: User) {
    localStorage.setItem('access_token', accessToken);
    localStorage.setItem('refresh_token', refreshToken);
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUser.set(user);
  }

  private loadUser(): User | null {
    const raw = localStorage.getItem('user');
    return raw ? JSON.parse(raw) : null;
  }

  private navigateByRole(role: UserRole) {
    const routes: Record<UserRole, string> = {
      [UserRole.SuperAdmin]: '/admin/dashboard',
      [UserRole.CountryManager]: '/country/dashboard',
      [UserRole.ClubManager]: '/club/dashboard',
      [UserRole.Fancier]: '/fancier/dashboard',
    };
    this.router.navigate([routes[role] ?? '/']);
  }
}

// ── Theme Service ─────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private doc = inject(DOCUMENT);
  private api = inject(ApiService);

  readonly activeTheme = signal<SiteTheme>(SiteTheme.Skyline);
  themes: Theme[] = [];

  private readonly themeVars: Record<SiteTheme, Record<string, string>> = {
    [SiteTheme.Skyline]: {
      '--pr-primary':    '#1E90FF',
      '--pr-primary-dk': '#0D6ECC',
      '--pr-accent':     '#00D4FF',
      '--pr-bg':         '#0A1520',
      '--pr-surface':    '#132030',
      '--pr-surface-2':  '#1A2940',
      '--pr-text':       '#E8F0FE',
      '--pr-text-muted': '#8FA8C8',
      '--pr-border':     '#1E3450',
      '--pr-success':    '#00E676',
      '--pr-warning':    '#FFD740',
      '--pr-error':      '#FF5252',
      '--pr-radius':     '8px',
    },
    [SiteTheme.Meadow]: {
      '--pr-primary':    '#2D6A4F',
      '--pr-primary-dk': '#1B4332',
      '--pr-accent':     '#F4A261',
      '--pr-bg':         '#F9F3E8',
      '--pr-surface':    '#FFFFFF',
      '--pr-surface-2':  '#F0E8D6',
      '--pr-text':       '#1B3A2D',
      '--pr-text-muted': '#5A7A6A',
      '--pr-border':     '#D4C5A9',
      '--pr-success':    '#52B788',
      '--pr-warning':    '#F4A261',
      '--pr-error':      '#D62828',
      '--pr-radius':     '12px',
    },
    [SiteTheme.Crimson]: {
      '--pr-primary':    '#C1121F',
      '--pr-primary-dk': '#9B0D18',
      '--pr-accent':     '#E63946',
      '--pr-bg':         '#F5F5F5',
      '--pr-surface':    '#FFFFFF',
      '--pr-surface-2':  '#EFEFEF',
      '--pr-text':       '#1A1A2E',
      '--pr-text-muted': '#555577',
      '--pr-border':     '#DDDDDD',
      '--pr-success':    '#2DC653',
      '--pr-warning':    '#F9A03F',
      '--pr-error':      '#C1121F',
      '--pr-radius':     '4px',
    },
    [SiteTheme.Ivory]: {
      '--pr-primary':    '#B8860B',
      '--pr-primary-dk': '#8B6508',
      '--pr-accent':     '#D4AF37',
      '--pr-bg':         '#FAF7F0',
      '--pr-surface':    '#FFFFFF',
      '--pr-surface-2':  '#F5EFE0',
      '--pr-text':       '#3D3320',
      '--pr-text-muted': '#7A6840',
      '--pr-border':     '#E2D5B0',
      '--pr-success':    '#4A7C59',
      '--pr-warning':    '#D4AF37',
      '--pr-error':      '#A0241B',
      '--pr-radius':     '6px',
    },
    [SiteTheme.Slate]: {
      '--pr-primary':    '#4A5568',
      '--pr-primary-dk': '#2D3748',
      '--pr-accent':     '#00B4D8',
      '--pr-bg':         '#F7FAFC',
      '--pr-surface':    '#FFFFFF',
      '--pr-surface-2':  '#EDF2F7',
      '--pr-text':       '#1A202C',
      '--pr-text-muted': '#718096',
      '--pr-border':     '#CBD5E0',
      '--pr-success':    '#38A169',
      '--pr-warning':    '#D69E2E',
      '--pr-error':      '#E53E3E',
      '--pr-radius':     '6px',
    }
  };

  applyTheme(theme: SiteTheme) {
    this.activeTheme.set(theme);
    const root = this.doc.documentElement;
    const vars = this.themeVars[theme];
    Object.entries(vars).forEach(([k, v]) => root.style.setProperty(k, v));
    localStorage.setItem('pr_theme', String(theme));
  }

  loadSavedTheme() {
    const saved = localStorage.getItem('pr_theme');
    const theme = saved ? (parseInt(saved) as SiteTheme) : SiteTheme.Skyline;
    this.applyTheme(theme);
  }

  loadThemesFromApi() {
    this.api.getThemes().subscribe(t => this.themes = t);
  }
}

// ── SignalR Live Race Service ──────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class LiveRaceService {
  private connection: signalR.HubConnection | null = null;
  private auth = inject(AuthService);

  connect() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl.replace('/api', '')}/hubs/live-race`, {
        accessTokenFactory: () => this.auth.getAccessToken() ?? ''
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.start().catch(err => console.error('SignalR connection failed:', err));
  }

  disconnect() {
    this.connection?.stop();
    this.connection = null;
  }

  joinRace(raceId: string) {
    this.connection?.invoke('JoinRaceGroup', raceId);
  }

  leaveRace(raceId: string) {
    this.connection?.invoke('LeaveRaceGroup', raceId);
  }

  joinClub(clubId: string) {
    this.connection?.invoke('JoinClubGroup', clubId);
  }

  onNewResult(callback: (result: any) => void) {
    this.connection?.on('NewResult', callback);
  }

  onRaceStatusChanged(callback: (data: { raceId: string; status: string }) => void) {
    this.connection?.on('RaceStatusChanged', callback);
  }

  offNewResult() { this.connection?.off('NewResult'); }
  offRaceStatusChanged() { this.connection?.off('RaceStatusChanged'); }
}

// ── JWT Interceptor ───────────────────────────────────────────────────────────

export const jwtInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>, next: HttpHandlerFn
) => {
  const auth = inject(AuthService);
  const api = inject(ApiService);

  const token = auth.getAccessToken();
  const cloned = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(cloned).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401 && auth.isAuthenticated()) {
        const rt = auth.getRefreshToken();
        const at = auth.getAccessToken();
        if (rt && at) {
          return api.refreshToken(at, rt).pipe(
            switchMap(tokens => {
              auth.setTokens(tokens.accessToken, tokens.refreshToken, tokens.user);
              const retried = req.clone({
                setHeaders: { Authorization: `Bearer ${tokens.accessToken}` }
              });
              return next(retried);
            }),
            catchError(() => {
              auth.logout();
              return throwError(() => err);
            })
          );
        }
        auth.logout();
      }
      return throwError(() => err);
    })
  );
};

// ── Auth Guards ───────────────────────────────────────────────────────────────

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  router.navigate(['/auth/login']);
  return false;
};

export const roleGuard = (allowedRoles: UserRole[]): CanActivateFn => () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const role = auth.currentUser()?.role;
  if (role != null && allowedRoles.includes(role)) return true;
  router.navigate(['/unauthorized']);
  return false;
};
