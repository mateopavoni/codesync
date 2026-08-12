import { Component, Input } from '@angular/core';
import { LANGUAGE_LABEL, ProgrammingLanguage } from '../../../core/models/challenge.model';

@Component({
  selector: 'app-lang-icon',
  standalone: true,
  template: `<img class="lang-icon" [src]="'icons/' + language + '.svg'" [alt]="LANGUAGE_LABEL[language]" [title]="LANGUAGE_LABEL[language]" [style.width.px]="size" [style.height.px]="size" />`,
  styles: [`
    /* inline-flex + align-items:center para que el host quede centrado
       verticalmente sea cual sea el texto/elemento que tenga al lado */
    :host {
      display: inline-flex;
      align-items: center;
    }
    .lang-icon {
      display: block;
      flex-shrink: 0;
    }
  `],
})
export class LangIconComponent {
  @Input() language!: ProgrammingLanguage;
  @Input() size = 28;
  protected readonly LANGUAGE_LABEL = LANGUAGE_LABEL;
}
