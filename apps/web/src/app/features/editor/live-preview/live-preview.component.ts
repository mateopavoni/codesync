import { Component, computed, inject, Input, signal } from '@angular/core';
import { DomSanitizer, type SafeHtml } from '@angular/platform-browser';

/**
 * Renders the student's HTML in a sandboxed iframe — no automated grading,
 * just a live view of what they wrote (freeCodeCamp-style practice).
 *
 * `sandbox="allow-scripts"` with no `allow-same-origin`: the iframe gets its
 * own opaque origin, so student code can't reach the app (no cookies, no
 * localStorage, no parent-window access) even if it runs JS.
 */
@Component({
  selector: 'app-live-preview',
  standalone: true,
  template: `
    <div class="panel">
      <h3 class="panel-title">Vista previa</h3>
      <iframe
        class="preview-frame"
        [srcdoc]="safeCode()"
        sandbox="allow-scripts"
        title="Vista previa en vivo"
      ></iframe>
    </div>
  `,
  styles: [`
    .panel {
      height: 100%;
      display: flex;
      flex-direction: column;
      gap: 10px;
      padding: 16px;
    }
    .panel-title {
      font-size: var(--cs-text-label);
      font-weight: 600;
      letter-spacing: var(--cs-ls-label);
      text-transform: uppercase;
      color: var(--cs-on-surface-var);
      margin: 0;
    }
    .preview-frame {
      flex: 1;
      min-height: 0;
      width: 100%;
      border: 1px solid var(--cs-outline-var);
      border-radius: var(--cs-radius-md);
      background: #fff;
    }
  `],
})
export class LivePreviewComponent {
  private readonly sanitizer = inject(DomSanitizer);

  private readonly rawCode = signal('');
  @Input() set code(value: string) {
    this.rawCode.set(value ?? '');
  }

  readonly safeCode = computed<SafeHtml>(() =>
    this.sanitizer.bypassSecurityTrustHtml(this.rawCode())
  );
}
