import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-coach-panel',
  standalone: true,
  template: `
    <div class="panel">
      <h3 class="panel-title">IA Coach</h3>

      @if (feedback) {
        <!-- Tarjeta con acento lateral púrpura (Stitch: tertiary left-border) -->
        <div class="feedback-card" role="status" aria-live="polite">
          <div class="coach-header">
            <div class="coach-icon-wrap" aria-hidden="true">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M12 2a7 7 0 0 1 7 7c0 2.73-1.56 5.11-3.86 6.34l-.14.08V18H9v-2.58l-.14-.08A7 7 0 0 1 5 9a7 7 0 0 1 7-7z"/>
                <line x1="9" y1="21" x2="15" y2="21"/>
              </svg>
            </div>
            <span class="coach-label">Análisis del Coach</span>
          </div>
          <p class="feedback-message">{{ feedback }}</p>
        </div>
      }

      @if (hints.length > 0) {
        <div class="hints-section">
          <h4 class="hints-title">Pistas</h4>
          <ol class="hints-list">
            @for (hint of hints; track $index) {
              <li class="hint-item">{{ hint }}</li>
            }
          </ol>
        </div>
      }

      @if (!feedback && hints.length === 0) {
        <p class="empty-hint">
          El IA Coach aparece automáticamente cuando un test falla.
        </p>
      }
    </div>
  `,
  styles: [`
    .panel {
      height: 100%;
      overflow-y: auto;
      padding: 16px;
      display: flex;
      flex-direction: column;
      gap: 14px;
    }
    .panel-title {
      font-size: var(--cs-text-label);
      font-weight: 600;
      letter-spacing: var(--cs-ls-label);
      text-transform: uppercase;
      color: var(--cs-on-surface-var);
      margin: 0;
    }
    /* Tarjeta con acento lateral púrpura (AI Coach) — Stitch pattern */
    .feedback-card {
      position: relative;
      background: var(--cs-surface-container);
      border: 1px solid var(--cs-outline-var);
      border-radius: 8px;
      padding: 14px 16px;
      overflow: hidden;
    }
    .feedback-card::before {
      content: '';
      position: absolute;
      left: 0;
      top: 0;
      bottom: 0;
      width: 3px;
      background: var(--cs-tertiary-cont);
    }
    .coach-header {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 10px;
    }
    .coach-icon-wrap {
      width: 28px;
      height: 28px;
      border-radius: 6px;
      background: rgba(160, 120, 255, 0.12);
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--cs-tertiary);
      flex-shrink: 0;
    }
    .coach-label {
      font-size: var(--cs-text-label);
      font-weight: 600;
      letter-spacing: var(--cs-ls-label);
      text-transform: uppercase;
      color: var(--cs-tertiary);
    }
    .feedback-message {
      font-size: var(--cs-text-body-sm);
      line-height: 1.65;
      margin: 0;
      color: var(--cs-on-surface);
    }
    .hints-section { display: flex; flex-direction: column; gap: 8px; }
    .hints-title {
      font-size: var(--cs-text-label);
      font-weight: 600;
      letter-spacing: var(--cs-ls-label);
      text-transform: uppercase;
      color: var(--cs-on-surface-var);
      margin: 0;
    }
    .hints-list {
      margin: 0;
      padding-left: 18px;
      display: flex;
      flex-direction: column;
      gap: 6px;
    }
    .hint-item {
      font-size: var(--cs-text-body-sm);
      line-height: 1.55;
      color: var(--cs-on-surface);
    }
    .empty-hint {
      font-size: var(--cs-text-body-sm);
      color: var(--cs-on-surface-var);
      text-align: center;
      padding: 24px 0;
    }
  `],
})
export class CoachPanelComponent {
  @Input() feedback: string | null = null;
  @Input() hints: string[] = [];
}
