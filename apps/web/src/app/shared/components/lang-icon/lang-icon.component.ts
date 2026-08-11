import { Component, Input } from '@angular/core';
import { ProgrammingLanguage } from '../../../core/models/challenge.model';

@Component({
  selector: 'app-lang-icon',
  standalone: true,
  template: `
    @switch (language) {
      @case ('javascript') {
        <svg class="lang-icon" viewBox="0 0 32 32" [attr.aria-label]="'JavaScript'" role="img">
          <rect width="32" height="32" rx="6" fill="#f0db4f" />
          <text x="16" y="22" text-anchor="middle" font-family="Arial, sans-serif" font-size="13" font-weight="700" fill="#1a1a1a">JS</text>
        </svg>
      }
      @case ('python') {
        <svg class="lang-icon" viewBox="0 0 32 32" [attr.aria-label]="'Python'" role="img">
          <rect width="32" height="32" rx="6" fill="#306998" />
          <text x="16" y="22" text-anchor="middle" font-family="Arial, sans-serif" font-size="12" font-weight="700" fill="#ffd43b">Py</text>
        </svg>
      }
    }
  `,
  styles: [`
    .lang-icon {
      width: 20px;
      height: 20px;
      border-radius: var(--cs-radius-md, 6px);
      flex-shrink: 0;
    }
  `],
})
export class LangIconComponent {
  @Input() language!: ProgrammingLanguage;
}
