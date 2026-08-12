import { Component, HostListener, Input, Output, EventEmitter } from '@angular/core';

/** Modal de confirmación genérico, reemplazo del `confirm()` nativo del navegador. */
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  template: `
    <div class="overlay" (click)="cancel.emit()">
      <div class="dialog" role="alertdialog" aria-modal="true" [attr.aria-label]="title" (click)="$event.stopPropagation()">
        <h2>{{ title }}</h2>
        <p>{{ message }}</p>
        <div class="actions">
          <button type="button" class="btn-cancel" (click)="cancel.emit()">Cancelar</button>
          <button type="button" class="btn-confirm" (click)="confirm.emit()">{{ confirmLabel }}</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .overlay {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.55);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
    }
    .dialog {
      background: var(--cs-surface-container, #171f33);
      border: 1px solid var(--cs-surface-highest, #2d3449);
      border-radius: 12px;
      padding: 24px;
      max-width: 400px;
      width: calc(100% - 32px);
      font-family: var(--cs-font-ui);
    }
    h2 { margin: 0 0 8px; font-size: var(--cs-text-body, 16px); }
    p { margin: 0 0 20px; font-size: var(--cs-text-body-sm, 14px); opacity: 0.85; line-height: 1.5; }
    .actions { display: flex; justify-content: flex-end; gap: 12px; }
    .btn-cancel, .btn-confirm {
      padding: 8px 16px;
      border-radius: 6px;
      font-size: var(--cs-text-body-sm, 14px);
      font-family: var(--cs-font-ui);
      cursor: pointer;
      border: 1px solid var(--cs-surface-highest, #2d3449);
      background: transparent;
      color: inherit;
    }
    .btn-cancel:hover { background: var(--cs-surface-high, #222a3d); }
    .btn-confirm {
      border-color: var(--cs-error, #ffb4ab);
      color: var(--cs-error, #ffb4ab);
    }
    .btn-confirm:hover { background: rgba(255, 180, 171, 0.12); }
  `],
})
export class ConfirmDialogComponent {
  @Input({ required: true }) title!: string;
  @Input({ required: true }) message!: string;
  @Input() confirmLabel = 'Confirmar';
  @Output() readonly confirm = new EventEmitter<void>();
  @Output() readonly cancel = new EventEmitter<void>();

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.cancel.emit();
  }
}
