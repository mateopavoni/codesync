import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-error-message',
  standalone: true,
  template: `
    <div class="error-box" role="alert">
      <span class="error-icon" aria-hidden="true">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
          <circle cx="12" cy="12" r="10"/>
          <line x1="12" y1="8" x2="12" y2="12"/>
          <line x1="12" y1="16" x2="12.01" y2="16"/>
        </svg>
      </span>
      <p class="error-text">{{ message }}</p>
      @if (retryable) {
        <button class="retry-btn" type="button" (click)="retry.emit()">Reintentar</button>
      }
    </div>
  `,
  styles: [`
    .error-box {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px 16px;
      background: rgba(255, 180, 171, 0.08);
      border: 1px solid rgba(255, 180, 171, 0.25);
      border-radius: 8px;
    }
    .error-icon {
      color: var(--cs-error);
      flex-shrink: 0;
      display: flex;
      align-items: center;
    }
    .error-text {
      color: var(--cs-on-error-cont, #ffdad6);
      font-size: var(--cs-text-body-sm);
      flex: 1;
      margin: 0;
      line-height: 1.5;
    }
    .retry-btn {
      padding: 6px 14px;
      background: transparent;
      border: 1px solid var(--cs-error);
      color: var(--cs-error);
      border-radius: 6px;
      cursor: pointer;
      font-size: var(--cs-text-body-sm);
      font-family: var(--cs-font-ui);
      white-space: nowrap;
      transition: background 0.15s;
    }
    .retry-btn:hover { background: rgba(255, 180, 171, 0.12); }
  `],
})
export class ErrorMessageComponent {
  @Input({ required: true }) message!: string;
  @Input() retryable = false;
  @Output() readonly retry = new EventEmitter<void>();
}
