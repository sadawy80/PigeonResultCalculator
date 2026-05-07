import { Pipe, PipeTransform, inject, ChangeDetectorRef, DestroyRef } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { TranslationService } from './translation.service';

// ─────────────────────────────────────────────────────────────────────────────
//  TranslatePipe  |  {{ 'key.path' | translate }}
//  pure: false  → re-evaluates every CD cycle (Zone.js default strategy).
//  toObservable  → marks OnPush views for check when the locale signal fires,
//                  synchronously inside Zone.js so a CD cycle is guaranteed.
// ─────────────────────────────────────────────────────────────────────────────

@Pipe({
  name: 'translate',
  pure: false,
  standalone: true
})
export class TranslatePipe implements PipeTransform {
  private i18n       = inject(TranslationService);
  private cdr        = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);

  constructor() {
    const sub = toObservable(this.i18n.locale)
      .subscribe(() => this.cdr.markForCheck());
    this.destroyRef.onDestroy(() => sub.unsubscribe());
  }

  transform(key: string, params?: Record<string, string | number>): string {
    return this.i18n.t(key, params);
  }
}
