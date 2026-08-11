import { Component, Input } from '@angular/core';
import { ProgrammingLanguage } from '../../../core/models/challenge.model';

const LANGUAGE_LABEL: Record<ProgrammingLanguage, string> = {
  javascript: 'JavaScript',
  python: 'Python',
  html: 'HTML',
};

@Component({
  selector: 'app-lang-icon',
  standalone: true,
  template: `<img class="lang-icon" [src]="'icons/' + language + '.svg'" [alt]="LANGUAGE_LABEL[language]" />`,
  styles: [`
    /* inline-flex + align-items:center para que el host quede centrado
       verticalmente sea cual sea el texto/elemento que tenga al lado */
    :host {
      display: inline-flex;
      align-items: center;
    }
    .lang-icon {
      display: block;
      width: 20px;
      height: 20px;
      flex-shrink: 0;
    }
  `],
})
export class LangIconComponent {
  @Input() language!: ProgrammingLanguage;
  protected readonly LANGUAGE_LABEL = LANGUAGE_LABEL;
}
