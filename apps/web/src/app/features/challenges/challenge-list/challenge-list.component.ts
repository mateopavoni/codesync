import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ChallengeService } from '../../../core/services/challenge.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorMessageComponent } from '../../../shared/components/error-message/error-message.component';
import { LangIconComponent } from '../../../shared/components/lang-icon/lang-icon.component';
import type { ChallengeSummary, DifficultyLevel, ProgrammingLanguage } from '../../../core/models/challenge.model';
import { DIFFICULTY_LABEL } from '../../../core/models/challenge.model';

@Component({
  selector: 'app-challenge-list',
  standalone: true,
  imports: [RouterLink, LoadingSpinnerComponent, ErrorMessageComponent, LangIconComponent],
  templateUrl: './challenge-list.component.html',
  styleUrl: './challenge-list.component.css',
})
export class ChallengeListComponent implements OnInit {
  private readonly challengeService = inject(ChallengeService);

  readonly challenges = signal<ChallengeSummary[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly selectedDifficulty = signal<DifficultyLevel | undefined>(undefined);
  readonly selectedLanguage = signal<ProgrammingLanguage | undefined>(undefined);

  readonly difficultyOptions: { value: DifficultyLevel | undefined; label: string }[] = [
    { value: undefined, label: 'Todos' },
    { value: 'facil', label: 'Fácil' },
    { value: 'medio', label: 'Medio' },
    { value: 'dificil', label: 'Difícil' },
  ];

  // undefined = "todos los lenguajes"; el resto son los ícono-botones del filtro.
  readonly languageOptions: (ProgrammingLanguage | undefined)[] = [
    undefined,
    'javascript',
    'python',
    'html',
    'ruby',
    'java',
    'csharp',
  ];

  readonly difficultyLabel = DIFFICULTY_LABEL;

  ngOnInit(): void {
    this.loadChallenges();
  }

  selectDifficulty(value: DifficultyLevel | undefined): void {
    this.selectedDifficulty.set(value);
    this.loadChallenges();
  }

  selectLanguage(value: ProgrammingLanguage | undefined): void {
    this.selectedLanguage.set(value);
    this.loadChallenges();
  }

  loadChallenges(): void {
    this.loading.set(true);
    this.error.set(null);
    this.challengeService.getChallenges(this.selectedDifficulty(), this.selectedLanguage()).subscribe({
      next: (list) => {
        this.challenges.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se pudieron cargar los desafíos.');
        this.loading.set(false);
      },
    });
  }

  trackById(_index: number, item: ChallengeSummary): string {
    return item.id;
  }
}
