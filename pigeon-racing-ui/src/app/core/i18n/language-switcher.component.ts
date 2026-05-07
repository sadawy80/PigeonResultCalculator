import {
  Component, inject, signal, HostListener, ElementRef,
  ChangeDetectionStrategy
} from '@angular/core';
import { NgClass } from '@angular/common';
import { TranslationService, SUPPORTED_LOCALES } from './translation.service';

// ─────────────────────────────────────────────────────────────────────────────
//  LanguageSwitcherComponent
//  Renders as a compact dropdown showing the current locale flag + name.
//  Placed in the shell header so it's always accessible.
// ─────────────────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ls-wrapper" [class.ls-wrapper--rtl]="i18n.isRtl()">

      <button
        class="ls-trigger"
        [class.ls-trigger--open]="open()"
        (click)="toggle()"
        [attr.aria-label]="'Change language — ' + i18n.localeConfig().name"
        [attr.aria-expanded]="open()">
        <span class="ls-flag">{{ i18n.localeConfig().flag }}</span>
        <span class="ls-code">{{ i18n.localeConfig().code.toUpperCase().slice(0,2) }}</span>
        <span class="ls-chevron" [class.ls-chevron--up]="open()">▾</span>
      </button>

      @if (open()) {
        <div class="ls-dropdown" role="listbox">
          @for (locale of locales; track locale.code) {
            <button
              class="ls-option"
              [class.ls-option--active]="locale.code === i18n.locale()"
              [class.ls-option--rtl]="locale.dir === 'rtl'"
              role="option"
              [attr.aria-selected]="locale.code === i18n.locale()"
              (click)="select(locale.code)">
              <span class="ls-option-flag">{{ locale.flag }}</span>
              <span class="ls-option-name">{{ locale.name }}</span>
              @if (locale.code === i18n.locale()) {
                <span class="ls-option-check">✓</span>
              }
              @if (locale.dir === 'rtl') {
                <span class="ls-rtl-badge">RTL</span>
              }
            </button>
          }
        </div>
      }

    </div>
  `,
  styles: [`
    .ls-wrapper {
      position: relative;
      display: inline-block;
    }

    .ls-trigger {
      display: flex;
      align-items: center;
      gap: 4px;
      padding: 5px 10px;
      background: var(--pr-surface-2);
      border: 1px solid var(--pr-border);
      border-radius: var(--pr-radius);
      cursor: pointer;
      font-size: 0.82rem;
      color: var(--pr-text);
      transition: all var(--t-fast);
      white-space: nowrap;
    }

    .ls-trigger:hover { border-color: var(--pr-primary); }
    .ls-trigger--open { border-color: var(--pr-primary); background: var(--pr-primary-10, rgba(30,144,255,.06)); }

    .ls-flag  { font-size: 1rem; line-height: 1; }
    .ls-code  { font-weight: 700; font-size: 0.75rem; }
    .ls-chevron {
      font-size: 0.65rem;
      transition: transform var(--t-fast);
      margin-left: 2px;
    }
    .ls-chevron--up { transform: rotate(180deg); }

    .ls-dropdown {
      position: absolute;
      top: calc(100% + 6px);
      right: 0;
      min-width: 200px;
      background: var(--pr-surface);
      border: 1px solid var(--pr-border);
      border-radius: var(--pr-radius);
      box-shadow: var(--shadow-lg, 0 8px 24px rgba(0,0,0,.15));
      z-index: 500;
      overflow: hidden;
      animation: ls-drop 0.15s ease;
    }

    /* RTL: open left instead of right */
    .ls-wrapper--rtl .ls-dropdown { right: auto; left: 0; }

    @keyframes ls-drop {
      from { opacity: 0; transform: translateY(-6px); }
      to   { opacity: 1; transform: translateY(0); }
    }

    .ls-option {
      display: flex;
      align-items: center;
      gap: 10px;
      width: 100%;
      padding: 9px 14px;
      background: none;
      border: none;
      cursor: pointer;
      font-size: 0.875rem;
      color: var(--pr-text);
      text-align: left;
      transition: background var(--t-fast);
    }

    .ls-option:hover { background: var(--pr-surface-2); }
    .ls-option--active { background: var(--pr-primary-10, rgba(30,144,255,.08)); font-weight: 600; }
    .ls-option--rtl .ls-option-name { direction: rtl; font-family: 'Segoe UI', Arial, sans-serif; }

    .ls-option-flag { font-size: 1.1rem; flex-shrink: 0; }
    .ls-option-name { flex: 1; }
    .ls-option-check { color: var(--pr-primary); font-size: 0.85rem; margin-left: auto; }

    .ls-rtl-badge {
      font-size: 0.62rem;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      padding: 1px 5px;
      border-radius: 3px;
      background: var(--pr-surface-2);
      color: var(--pr-text-muted);
      border: 1px solid var(--pr-border);
    }
  `]
})
export class LanguageSwitcherComponent {
  i18n    = inject(TranslationService);
  elRef   = inject(ElementRef);

  open    = signal(false);
  locales = SUPPORTED_LOCALES;

  toggle() { this.open.update(v => !v); }

  async select(code: string) {
    this.open.set(false);
    await this.i18n.setLocale(code);
  }

  @HostListener('document:click', ['$event'])
  onOutsideClick(e: MouseEvent) {
    if (!this.elRef.nativeElement.contains(e.target as Node)) {
      this.open.set(false);
    }
  }
}
