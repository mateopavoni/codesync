import { Component, Input } from '@angular/core';
import type { SubmissionResult } from '../../../core/models/submission.model';

@Component({
  selector: 'app-execution-panel',
  standalone: true,
  template: `
    <div class="panel" aria-live="polite" aria-atomic="false">
      <h3 class="panel-title">Resultado de ejecución</h3>

      @if (result) {
        <div class="summary"
             [class.all-passed]="result.allPassed"
             [class.has-failures]="!result.allPassed"
             role="status">
          <span class="summary-icon" aria-hidden="true">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              @if (result.allPassed) {
                <polyline points="20 6 9 17 4 12"/>
              } @else {
                <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
              }
            </svg>
          </span>
          <span class="summary-text">
            {{ result.allPassed ? 'Todos los tests pasaron' : 'Algunos tests fallaron' }}
          </span>
          <span class="exec-time">{{ result.totalExecutionTimeMs }}ms</span>
        </div>

        <div class="test-results">
          @for (tc of result.results; track tc.testCaseId) {
            <div class="tc-row"
                 [class.passed]="tc.passed"
                 [class.failed]="!tc.passed">
              <span class="tc-status" aria-hidden="true">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                  @if (tc.passed) {
                    <polyline points="20 6 9 17 4 12"/>
                  } @else {
                    <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
                  }
                </svg>
              </span>
              <span class="tc-sr-only">{{ tc.passed ? 'Test pasado' : 'Test fallado' }}:</span>
              <div class="tc-detail">
                <div class="tc-meta">
                  <span class="tc-label">Entrada:</span>
                  <code>{{ tc.input || '(vacía)' }}</code>
                </div>
                <div class="tc-meta">
                  <span class="tc-label">Esperado:</span>
                  <code>{{ tc.expectedOutput }}</code>
                </div>
                @if (!tc.passed) {
                  <div class="tc-meta">
                    <span class="tc-label">Obtenido:</span>
                    <code class="actual-wrong">{{ tc.actualOutput }}</code>
                  </div>
                }
              </div>
              <span class="tc-time">{{ tc.executionTimeMs }}ms</span>
            </div>
          }
        </div>
      } @else {
        <p class="empty-hint">Ejecutá tu código para ver los resultados aquí.</p>
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
    .summary {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 12px 14px;
      border-radius: 8px;
      animation: fade-in-up 0.3s ease-out both;
    }
    .summary.all-passed {
      background: rgba(78, 222, 163, 0.1);
      border: 1px solid rgba(78, 222, 163, 0.3);
    }
    .summary.has-failures {
      background: rgba(255, 180, 171, 0.08);
      border: 1px solid rgba(255, 180, 171, 0.25);
    }
    .summary-icon {
      display: flex;
      align-items: center;
      flex-shrink: 0;
    }
    .summary.all-passed .summary-icon { color: var(--cs-secondary); }
    .summary.has-failures .summary-icon { color: var(--cs-error); }
    .summary-text { flex: 1; font-size: var(--cs-text-body-sm); }
    .exec-time { font-size: var(--cs-text-label); color: var(--cs-on-surface-var); }
    .test-results { display: flex; flex-direction: column; gap: 8px; }
    .tc-row {
      display: flex;
      align-items: flex-start;
      gap: 10px;
      padding: 10px 12px;
      border-radius: 8px;
      background: var(--cs-surface-container);
      border: 1px solid var(--cs-outline-var);
      transition: border-color 0.15s;
      animation: fade-in-up 0.3s ease-out both;
    }
    .tc-row.passed { border-color: rgba(78, 222, 163, 0.3); }
    .tc-row.failed { border-color: rgba(255, 180, 171, 0.3); }
    .tc-status {
      display: flex;
      align-items: center;
      flex-shrink: 0;
      margin-top: 2px;
    }
    .tc-sr-only {
      position: absolute;
      width: 1px;
      height: 1px;
      padding: 0;
      margin: -1px;
      overflow: hidden;
      clip: rect(0,0,0,0);
      border: 0;
    }
    .tc-row.passed .tc-status { color: var(--cs-secondary); }
    .tc-row.failed .tc-status { color: var(--cs-error); }
    .tc-detail { flex: 1; display: flex; flex-direction: column; gap: 4px; }
    .tc-meta { display: flex; align-items: baseline; gap: 8px; }
    .tc-label {
      font-size: var(--cs-text-label);
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: var(--cs-ls-label);
      color: var(--cs-on-surface-var);
      min-width: 80px;
    }
    code {
      font-size: var(--cs-text-code-sm);
      font-family: var(--cs-font-code);
      color: var(--cs-on-surface);
    }
    .actual-wrong { color: var(--cs-error); }
    .tc-time {
      font-size: var(--cs-text-label);
      color: var(--cs-on-surface-var);
      flex-shrink: 0;
    }
    .empty-hint {
      color: var(--cs-on-surface-var);
      font-size: var(--cs-text-body-sm);
      text-align: center;
      padding: 24px 0;
    }
  `],
})
export class ExecutionPanelComponent {
  @Input() result: SubmissionResult | null = null;
}
