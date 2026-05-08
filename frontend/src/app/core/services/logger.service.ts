import { Injectable, inject, isDevMode } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export type LogLevel = 'debug' | 'information' | 'warning' | 'error' | 'fatal';

export interface LogEvent {
  Timestamp:        string;
  Level:            LogLevel;
  Message:          string;
  MessageTemplate?: string;
  Exception?:       string;
  SourceContext?:   string;
  SessionId?:       string;
  UserId?:          string;
  PageUrl?:         string;
  AppVersion?:      string;
  UserAgent?:       string;
  Browser?:         string;
  Os?:              string;
  Country?:         string;
  City?:            string;
  ScreenResolution?:string;
  Viewport?:        string;
  Properties?:      Record<string, unknown>;
}

@Injectable({ providedIn: 'root' })
export class LoggerService {
  private http    = inject(HttpClient);
  private buffer: LogEvent[] = [];
  private sessionId = crypto.randomUUID();
  private userId: string | null = null;

  // Lazily resolved once
  private readonly geoPromise = this.resolveGeo();
  private geo: { country?: string; city?: string } = {};

  constructor() {
    this.geoPromise.then(g => { this.geo = g; });
    this.scheduleFlush();
    window.addEventListener('beforeunload', () => this.flush(true));
  }

  setUserId(id: string | null) { this.userId = id; }

  debug(message: string, props?: Record<string, unknown>, source?: string) {
    this.push('debug', message, undefined, undefined, props, source);
  }

  info(message: string, props?: Record<string, unknown>, source?: string) {
    this.push('information', message, undefined, undefined, props, source);
  }

  warn(message: string, props?: Record<string, unknown>, source?: string) {
    this.push('warning', message, undefined, undefined, props, source);
  }

  error(message: string, exception?: string, props?: Record<string, unknown>, source?: string) {
    this.push('error', message, undefined, exception, props, source);
    this.flush();  // flush immediately on errors
  }

  fatal(message: string, exception?: string, props?: Record<string, unknown>, source?: string) {
    this.push('fatal', message, undefined, exception, props, source);
    this.flush(true);
  }

  private push(
    level: LogLevel,
    message: string,
    template?: string,
    exception?: string,
    props?: Record<string, unknown>,
    source?: string
  ) {
    const ev: LogEvent = {
      Timestamp:         new Date().toISOString(),
      Level:             level,
      Message:           message,
      MessageTemplate:   template,
      Exception:         exception,
      SourceContext:     source ?? 'Angular',
      SessionId:         this.sessionId,
      UserId:            this.userId ?? undefined,
      PageUrl:           window.location.href,
      AppVersion:        environment.version ?? 'dev',
      UserAgent:         navigator.userAgent,
      Browser:           this.detectBrowser(),
      Os:                this.detectOs(),
      Country:           this.geo.country,
      City:              this.geo.city,
      ScreenResolution:  `${screen.width}x${screen.height}`,
      Viewport:          `${window.innerWidth}x${window.innerHeight}`,
      Properties:        props
    };

    this.buffer.push(ev);

    if (this.buffer.length >= 20) this.flush();
  }

  private flush(sync = false) {
    if (this.buffer.length === 0) return;
    const events = this.buffer.splice(0);

    const body = JSON.stringify({ events });
    const url  = `${environment.apiUrl}/logs`;

    if (sync && navigator.sendBeacon) {
      navigator.sendBeacon(url, new Blob([body], { type: 'application/json' }));
    } else {
      this.http.post(url, { events }, { headers: { 'Content-Type': 'application/json' } })
        .subscribe({ error: () => { /* silently discard — avoid infinite loop */ } });
    }
  }

  private scheduleFlush() {
    setInterval(() => this.flush(), 10_000);
  }

  private async resolveGeo(): Promise<{ country?: string; city?: string }> {
    try {
      const res = await fetch('https://ipapi.co/json/', { signal: AbortSignal.timeout(3000) });
      if (!res.ok) return {};
      const data = await res.json();
      return { country: data.country_name, city: data.city };
    } catch {
      return {};
    }
  }

  private detectBrowser(): string {
    const ua = navigator.userAgent;
    if (ua.includes('Edg/'))    return 'Edge';
    if (ua.includes('Chrome/')) return 'Chrome';
    if (ua.includes('Firefox/'))return 'Firefox';
    if (ua.includes('Safari/')) return 'Safari';
    if (ua.includes('OPR/'))    return 'Opera';
    return 'Unknown';
  }

  private detectOs(): string {
    const ua = navigator.userAgent;
    if (ua.includes('Windows')) return 'Windows';
    if (ua.includes('Mac OS'))  return 'macOS';
    if (ua.includes('Linux'))   return 'Linux';
    if (ua.includes('Android')) return 'Android';
    if (ua.includes('iPhone') || ua.includes('iPad')) return 'iOS';
    return 'Unknown';
  }
}
