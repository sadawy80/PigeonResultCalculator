import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToasterService } from '../../core/services/toaster.service';

/**
 * Stack of auto-dismiss toasts in the bottom-right. Mount once at the app
 * shell (already done in <app-root>). For blocking modals use
 * <app-modal-host> instead.
 */
@Component({
  selector: 'app-toaster',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-stack" role="region" aria-live="polite">
      @for (t of toaster.toasts(); track t.id) {
        <div class="toast" [attr.data-kind]="t.kind" role="status">
          <span class="toast__icon">{{ icon(t.kind) }}</span>
          <span class="toast__msg">{{ t.message }}</span>
          <button class="toast__close" aria-label="Dismiss" (click)="toaster.dismiss(t.id)">×</button>
        </div>
      }
    </div>
  `,
  styles: [`
    .toast-stack {
      position: fixed; right: 1rem; bottom: 1rem; z-index: 9999;
      display: flex; flex-direction: column; gap: .5rem;
      max-width: min(420px, calc(100vw - 2rem));
      pointer-events: none;
    }
    .toast {
      pointer-events: auto;
      display: flex; align-items: flex-start; gap: .65rem;
      padding: .7rem .9rem;
      border-radius: 8px;
      box-shadow: 0 4px 14px rgba(15, 23, 42, .15);
      background: #ffffff;
      border-left: 4px solid #94a3b8;
      color: #0f172a;
      font: 500 .9rem/1.35 system-ui, sans-serif;
      animation: toast-in .18s ease-out;
    }
    .toast[data-kind="success"] { border-left-color: #16a34a; }
    .toast[data-kind="error"]   { border-left-color: #dc2626; }
    .toast[data-kind="info"]    { border-left-color: #2563eb; }
    .toast[data-kind="warning"] { border-left-color: #d97706; }
    .toast__icon { flex: 0 0 auto; font-size: 1.1rem; line-height: 1; }
    .toast__msg  { flex: 1 1 auto; white-space: pre-line; }
    .toast__close {
      flex: 0 0 auto; background: transparent; border: 0; cursor: pointer;
      color: #64748b; font-size: 1.2rem; line-height: 1;
      padding: 0 .25rem; margin: -.15rem -.25rem -.15rem 0;
    }
    .toast__close:hover { color: #0f172a; }
    @keyframes toast-in {
      from { opacity: 0; transform: translateY(8px); }
      to   { opacity: 1; transform: translateY(0); }
    }
  `]
})
export class ToasterComponent {
  toaster = inject(ToasterService);
  icon(kind: string): string {
    return ({ success: '✓', error: '⚠', info: 'ℹ', warning: '⚠' } as Record<string, string>)[kind] || 'ℹ';
  }
}
