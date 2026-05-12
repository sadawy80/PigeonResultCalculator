import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalService } from '../../core/services/modal.service';

/**
 * Renders blocking modals (alert / info / confirm / prompt) produced by
 * ModalService. Mount once at the app shell (already done in <app-root>).
 */
@Component({
  selector: 'app-modal-host',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    @if (modals.current(); as m) {
      <div class="modal-backdrop" (click)="cancel()">
        <div class="modal" role="dialog" aria-modal="true" [attr.data-kind]="m.kind" (click)="$event.stopPropagation()">
          @if (m.options.title) { <h3 class="modal__title">{{ m.options.title }}</h3> }
          <p class="modal__msg">{{ m.options.message }}</p>

          @if (m.kind === 'prompt') {
            <input
              type="text"
              class="modal__input"
              [(ngModel)]="promptValue"
              [placeholder]="$any(m.options).placeholder || ''"
              (keyup.enter)="acceptPrompt()"
              autofocus />
          }

          <div class="modal__actions">
            @if (m.kind === 'alert' || m.kind === 'info') {
              <button type="button" class="btn primary" (click)="modals.resolveCurrent()">{{ $any(m.options).okLabel || 'OK' }}</button>
            } @else if (m.kind === 'confirm') {
              <button type="button" class="btn ghost"   (click)="cancel()">{{ $any(m.options).cancelLabel || 'Cancel' }}</button>
              <button type="button" class="btn"
                      [class.danger]="$any(m.options).variant === 'danger'"
                      [class.primary]="$any(m.options).variant !== 'danger'"
                      (click)="modals.resolveCurrent(true)">
                {{ $any(m.options).confirmLabel || 'Confirm' }}
              </button>
            } @else if (m.kind === 'prompt') {
              <button type="button" class="btn ghost"   (click)="cancel()">{{ $any(m.options).cancelLabel || 'Cancel' }}</button>
              <button type="button" class="btn primary" (click)="acceptPrompt()">{{ $any(m.options).confirmLabel || 'OK' }}</button>
            }
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .modal-backdrop {
      position: fixed; inset: 0; z-index: 10000;
      background: rgba(15, 23, 42, .45);
      display: flex; align-items: center; justify-content: center;
      padding: 1rem;
    }
    .modal {
      background: #fff; border-radius: 12px;
      max-width: 460px; width: 100%;
      padding: 1.3rem 1.4rem; box-shadow: 0 12px 40px rgba(15, 23, 42, .25);
      animation: modal-in .18s ease-out;
      border-top: 4px solid #94a3b8;
    }
    .modal[data-kind="info"]    { border-top-color: #2563eb; }
    .modal[data-kind="alert"]   { border-top-color: #d97706; }
    .modal[data-kind="confirm"] { border-top-color: #4f46e5; }
    .modal[data-kind="prompt"]  { border-top-color: #4f46e5; }
    .modal__title { margin: 0 0 .35rem; font: 600 1.05rem/1.3 system-ui, sans-serif; color: #0f172a; }
    .modal__msg   { margin: 0 0 1rem; color: #334155; font: 400 .92rem/1.45 system-ui, sans-serif; white-space: pre-line; }
    .modal__input {
      width: 100%; padding: .55rem .7rem; margin-bottom: 1rem;
      border: 1px solid #cbd5e1; border-radius: 7px;
      font: 400 .9rem/1.3 system-ui, sans-serif;
    }
    .modal__input:focus { outline: 2px solid #6366f1; outline-offset: -1px; border-color: #6366f1; }
    .modal__actions { display: flex; justify-content: flex-end; gap: .55rem; }
    .btn {
      padding: .5rem 1rem; border: 0; border-radius: 7px;
      font: 600 .85rem/1 system-ui, sans-serif; cursor: pointer;
    }
    .btn.primary { background: #4f46e5; color: #fff; }
    .btn.primary:hover { background: #4338ca; }
    .btn.danger  { background: #dc2626; color: #fff; }
    .btn.danger:hover  { background: #b91c1c; }
    .btn.ghost   { background: transparent; color: #475569; border: 1px solid #cbd5e1; }
    .btn.ghost:hover   { background: #f1f5f9; }
    @keyframes modal-in {
      from { opacity: 0; transform: translateY(8px) scale(.98); }
      to   { opacity: 1; transform: translateY(0) scale(1); }
    }
  `]
})
export class ModalHostComponent {
  modals = inject(ModalService);
  promptValue = '';

  cancel() {
    const c = this.modals.current();
    if (!c) return;
    if (c.kind === 'confirm') this.modals.resolveCurrent(false);
    else if (c.kind === 'prompt') this.modals.resolveCurrent(null);
    else this.modals.resolveCurrent();
  }

  acceptPrompt() {
    this.modals.resolveCurrent(this.promptValue);
    this.promptValue = '';
  }
}
