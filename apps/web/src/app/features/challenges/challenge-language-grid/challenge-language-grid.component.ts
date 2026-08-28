import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LangIconComponent } from '../../../shared/components/lang-icon/lang-icon.component';
import { LANGUAGE_LABEL, PROGRAMMING_LANGUAGES } from '../../../core/models/challenge.model';

// landing estática de los 6 lenguajes soportados, sin pegarle al
// backend — evita un spinner de carga para algo que no cambia en runtime.
@Component({
  selector: 'app-challenge-language-grid',
  standalone: true,
  imports: [RouterLink, LangIconComponent],
  templateUrl: './challenge-language-grid.component.html',
  styleUrl: './challenge-language-grid.component.css',
})
export class ChallengeLanguageGridComponent {
  readonly languages = PROGRAMMING_LANGUAGES;
  readonly languageLabel = LANGUAGE_LABEL;
}
