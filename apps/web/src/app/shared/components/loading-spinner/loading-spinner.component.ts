import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  template: `
    <div class="spinner-wrapper" [class.overlay]="overlay" [attr.aria-label]="label" role="status">
      <div class="spinner" aria-hidden="true"></div>
      @if (message) {
        <p class="spinner-message">{{ message }}</p>
      }
    </div>
  `,
  styles: [`
    .spinner-wrapper {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 12px;
      padding: 24px;
    }
    .spinner-wrapper.overlay {
      position: absolute;
      inset: 0;
      background: rgba(11, 19, 38, 0.75);
      backdrop-filter: blur(2px);
      -webkit-backdrop-filter: blur(2px);
      z-index: 10;
      border-radius: inherit;
    }
    .spinner {
      width: 28px;
      height: 28px;
      border: 2px solid rgba(173, 198, 255, 0.2);
      border-top-color: var(--cs-primary);
      border-radius: 50%;
      animation: spin 0.75s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
    .spinner-message {
      font-size: var(--cs-text-body-sm);
      color: var(--cs-on-surface-var);
    }
  `],
})
export class LoadingSpinnerComponent {
  @Input() message = '';
  @Input() overlay = false;
  @Input() label = 'Cargando';
}
