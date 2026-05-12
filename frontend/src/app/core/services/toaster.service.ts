import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info' | 'warning';

export interface Toast {
  id: number;
  kind: ToastKind;
  message: string;
  /** Auto-dismiss in ms. 0 = stay until user closes. Default 4000. */
  durationMs: number;
}

/**
 * Non-blocking transient notifications stacked in the bottom-right.
 *
 *  - `success` / `error` / `info` / `warning` push a toast that auto-dismisses
 *    after a few seconds.
 *
 * For blocking confirm / alert / prompt dialogs, use ModalService instead.
 * The host UI lives in <app-toaster> — mounted once in <app-root>.
 */
@Injectable({ providedIn: 'root' })
export class ToasterService {
  private nextId = 1;
  readonly toasts = signal<Toast[]>([]);

  success(message: string, durationMs = 4000) { this.push('success', message, durationMs); }
  error(message: string,   durationMs = 6000) { this.push('error',   message, durationMs); }
  info(message: string,    durationMs = 4000) { this.push('info',    message, durationMs); }
  warning(message: string, durationMs = 5000) { this.push('warning', message, durationMs); }

  dismiss(id: number) {
    this.toasts.update(list => list.filter(t => t.id !== id));
  }

  clear() { this.toasts.set([]); }

  private push(kind: ToastKind, message: string, durationMs: number) {
    const t: Toast = { id: this.nextId++, kind, message, durationMs };
    this.toasts.update(list => [...list, t]);
    if (durationMs > 0) {
      setTimeout(() => this.dismiss(t.id), durationMs);
    }
  }
}
