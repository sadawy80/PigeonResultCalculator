import { Injectable, signal } from '@angular/core';

export type ModalKind = 'alert' | 'info' | 'confirm' | 'prompt';

export interface BaseModalOptions {
  title?: string;
  message: string;
}

export interface AlertOptions extends BaseModalOptions {
  /** Button label. Defaults to "OK". */
  okLabel?: string;
}

export interface ConfirmOptions extends BaseModalOptions {
  confirmLabel?: string;   // default "Confirm"
  cancelLabel?: string;    // default "Cancel"
  /** "danger" tints the confirm button red — use for delete / destructive actions. */
  variant?: 'default' | 'danger';
}

export interface PromptOptions extends BaseModalOptions {
  initial?: string;
  placeholder?: string;
  confirmLabel?: string;
  cancelLabel?: string;
}

interface PendingAlert {
  id: number; kind: 'alert' | 'info';
  options: AlertOptions;
  resolve: () => void;
}

interface PendingConfirm {
  id: number; kind: 'confirm';
  options: ConfirmOptions;
  resolve: (ok: boolean) => void;
}

interface PendingPrompt {
  id: number; kind: 'prompt';
  options: PromptOptions;
  resolve: (value: string | null) => void;
}

export type PendingModal = PendingAlert | PendingConfirm | PendingPrompt;

/**
 * Single entry point for every blocking dialog in the app.
 *
 *  - `alert(opts)`   — single OK button. Resolves when dismissed.
 *  - `info(opts)`    — alert with info styling. Same resolution.
 *  - `confirm(opts)` — yes/no, returns Promise&lt;boolean&gt;.
 *  - `prompt(opts)`  — text input, returns Promise&lt;string | null&gt;
 *    (null = user cancelled).
 *
 * Multiple calls queue: the next modal appears once the current one is
 * resolved. For non-blocking notifications use ToasterService instead.
 *
 * Host UI lives in <app-modal-host> — mounted once in <app-root>.
 */
@Injectable({ providedIn: 'root' })
export class ModalService {
  private nextId = 1;
  private queue: PendingModal[] = [];
  readonly current = signal<PendingModal | null>(null);

  alert(options: AlertOptions): Promise<void> {
    return new Promise(resolve => {
      this.enqueue({ id: this.nextId++, kind: 'alert', options, resolve });
    });
  }

  info(options: AlertOptions): Promise<void> {
    return new Promise(resolve => {
      this.enqueue({ id: this.nextId++, kind: 'info', options, resolve });
    });
  }

  confirm(options: ConfirmOptions): Promise<boolean> {
    return new Promise<boolean>(resolve => {
      this.enqueue({ id: this.nextId++, kind: 'confirm', options, resolve });
    });
  }

  prompt(options: PromptOptions): Promise<string | null> {
    return new Promise<string | null>(resolve => {
      this.enqueue({ id: this.nextId++, kind: 'prompt', options, resolve });
    });
  }

  /** Called by <app-modal-host> when the current modal closes. */
  resolveCurrent(value: boolean | string | null | void) {
    const c = this.current();
    if (!c) return;
    this.current.set(null);
    if      (c.kind === 'alert' || c.kind === 'info') c.resolve();
    else if (c.kind === 'confirm') c.resolve(value === true);
    else                            c.resolve(typeof value === 'string' ? value : null);
    this.flush();
  }

  private enqueue(m: PendingModal) {
    this.queue.push(m);
    if (!this.current()) this.flush();
  }

  private flush() {
    if (this.current()) return;
    const next = this.queue.shift() ?? null;
    this.current.set(next);
  }
}
