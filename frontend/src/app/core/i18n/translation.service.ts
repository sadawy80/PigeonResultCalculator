import { Injectable, signal, computed, effect, inject, PLATFORM_ID, NgZone } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

// ─────────────────────────────────────────────────────────────────────────────
//  Supported locales
// ─────────────────────────────────────────────────────────────────────────────

export interface LocaleConfig {
  code:   string;   // e.g. 'en', 'fr', 'ar', 'nl-BE'
  name:   string;   // native name
  flag:   string;   // emoji flag
  dir:    'ltr' | 'rtl';
  file:   string;   // asset file path
}

export const SUPPORTED_LOCALES: LocaleConfig[] = [
  { code: 'en',    name: 'English',             flag: '🇬🇧', dir: 'ltr', file: 'en.json'    },
  { code: 'fr',    name: 'Français',             flag: '🇫🇷', dir: 'ltr', file: 'fr.json'    },
  { code: 'nl-BE', name: 'Belgisch (Vlaams)',    flag: '🇧🇪', dir: 'ltr', file: 'nl-BE.json' },
  { code: 'ar',    name: 'العربية',              flag: '🇸🇦', dir: 'rtl', file: 'ar.json'    },
  { code: 'zh',    name: '中文（简体）',           flag: '🇨🇳', dir: 'ltr', file: 'zh.json'    },
  { code: 'es',    name: 'Español',              flag: '🇪🇸', dir: 'ltr', file: 'es.json'    },
];

export const DEFAULT_LOCALE = 'en';
const STORAGE_KEY = 'pigeon-racing-locale';

// ─────────────────────────────────────────────────────────────────────────────
//  TranslationService
//  Loads JSON locale files at runtime, exposes t() for string lookup.
//  Switching locale re-loads the JSON file and updates document dir.
// ─────────────────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class TranslationService {
  private http       = inject(HttpClient);
  private platformId = inject(PLATFORM_ID);
  private ngZone     = inject(NgZone);

  // Current locale code — reactive
  private _locale = signal<string>(DEFAULT_LOCALE);
  readonly locale  = this._locale.asReadonly();

  // Loaded translations dictionary
  private _translations = signal<Record<string, any>>({});

  // Currently loading?
  readonly loading = signal(false);

  // Computed locale config
  readonly localeConfig = computed(() =>
    SUPPORTED_LOCALES.find(l => l.code === this._locale()) ?? SUPPORTED_LOCALES[0]);

  readonly isRtl  = computed(() => this.localeConfig().dir === 'rtl');
  readonly dir    = computed(() => this.localeConfig().dir);

  readonly allLocales = SUPPORTED_LOCALES;

  constructor() {
    // Apply RTL/LTR to document when locale changes
    if (isPlatformBrowser(this.platformId)) {
      effect(() => {
        const config = this.localeConfig();
        document.documentElement.dir  = config.dir;
        document.documentElement.lang = config.code;
        document.documentElement.setAttribute('data-locale', config.code);
      });
    }
  }

  // ── Initialise (called in app.config.ts) ────────────────────────────────────

  async init(): Promise<void> {
    const saved = this.getSavedLocale();
    const detected = saved ? null : this.detectBrowserLocale();
    await this.setLocale(saved || detected || DEFAULT_LOCALE);
  }

  // ── Browser locale detection ──────────────────────────────────────────────

  private detectBrowserLocale(): string | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    const candidates: readonly string[] =
      (navigator.languages?.length ? navigator.languages : [navigator.language])
        .filter(Boolean);

    for (const lang of candidates) {
      // Exact match first (e.g. 'nl-BE')
      const exact = SUPPORTED_LOCALES.find(
        l => l.code.toLowerCase() === lang.toLowerCase());
      if (exact) return exact.code;

      // Primary-tag match (e.g. 'en-US' → 'en', 'fr-CA' → 'fr')
      const primary = lang.split('-')[0].toLowerCase();
      const byPrimary = SUPPORTED_LOCALES.find(
        l => l.code.split('-')[0].toLowerCase() === primary);
      if (byPrimary) return byPrimary.code;
    }
    return null;
  }

  // ── Set locale ───────────────────────────────────────────────────────────────

  async setLocale(code: string): Promise<void> {
    const config = SUPPORTED_LOCALES.find(l => l.code === code);
    if (!config) {
      console.warn(`[i18n] Locale "${code}" not supported. Falling back to English.`);
      return this.setLocale(DEFAULT_LOCALE);
    }

    this.loading.set(true);
    try {
      const data = await firstValueFrom(
        this.http.get<Record<string, any>>(`/assets/i18n/${config.file}`)
      );
      // ngZone.run() guarantees the signal writes happen inside Angular's zone,
      // so Zone.js triggers a full CD cycle for ALL components (incl. OnPush).
      this.ngZone.run(() => {
        this._translations.set(data);
        this._locale.set(code);
        this.saveLocale(code);
      });
    } catch (err) {
      console.error(`[i18n] Failed to load locale "${code}":`, err);
      if (code !== DEFAULT_LOCALE) {
        await this.setLocale(DEFAULT_LOCALE);
      }
    } finally {
      this.loading.set(false);
    }
  }

  // ── Translate ─────────────────────────────────────────────────────────────────

  /**
   * Resolve a dot-notation translation key.
   * Supports {{interpolation}} placeholders.
   *
   * @example
   *   t('nav.dashboard')           → 'Dashboard'
   *   t('errors.minLength', {min: 8}) → 'Minimum 8 characters required'
   *   t('result.velocity')         → 'Velocity'
   */
  t(key: string, params?: Record<string, string | number>): string {
    const keys  = key.split('.');
    const dict  = this._translations();
    let   value: any = dict;

    for (const k of keys) {
      if (value == null || typeof value !== 'object') {
        // Key not found — return key as fallback (English key is readable)
        return this.fallback(key);
      }
      value = value[k];
    }

    if (typeof value !== 'string') {
      return this.fallback(key);
    }

    // Interpolate {{param}} placeholders
    if (params) {
      return Object.entries(params).reduce(
        (str, [k, v]) => str.replace(new RegExp(`\\{\\{${k}\\}\\}`, 'g'), String(v)),
        value
      );
    }

    return value;
  }

  /**
   * Shorthand alias — identical to t() but makes templates cleaner.
   */
  get = (key: string, params?: Record<string, string | number>) => this.t(key, params);

  // ── Persistence ──────────────────────────────────────────────────────────────

  private getSavedLocale(): string | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    try { return localStorage.getItem(STORAGE_KEY); }
    catch { return null; }
  }

  private saveLocale(code: string): void {
    if (!isPlatformBrowser(this.platformId)) return;
    try { localStorage.setItem(STORAGE_KEY, code); }
    catch { /* ignore */ }
  }

  private fallback(key: string): string {
    return key;
  }
}
