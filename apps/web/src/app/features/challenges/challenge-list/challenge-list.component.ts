import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ChallengeService } from '../../../core/services/challenge.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorMessageComponent } from '../../../shared/components/error-message/error-message.component';
import type { ChallengeSummary, DifficultyLevel, ProgrammingLanguage } from '../../../core/models/challenge.model';
import { DIFFICULTY_LABEL, LANGUAGE_LABEL, PROGRAMMING_LANGUAGES } from '../../../core/models/challenge.model';

@Component({
  selector: 'app-challenge-list',
  standalone: true,
  imports: [RouterLink, LoadingSpinnerComponent, ErrorMessageComponent],
  templateUrl: './challenge-list.component.html',
  styleUrl: './challenge-list.component.css',
})
export class ChallengeListComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly challengeService = inject(ChallengeService);

  readonly challenges = signal<ChallengeSummary[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly selectedDifficulty = signal<DifficultyLevel | undefined>(undefined);

  // Fijo por la ruta (/desafios/lenguaje/:language) — esta vista ya no filtra por lenguaje.
  language!: ProgrammingLanguage;
  languageLabel = '';

  readonly difficultyOptions: { value: DifficultyLevel | undefined; label: string }[] = [
    { value: undefined, label: 'Todos' },
    { value: 'facil', label: 'Fácil' },
    { value: 'medio', label: 'Medio' },
    { value: 'dificil', label: 'Difícil' },
  ];

  readonly difficultyLabel = DIFFICULTY_LABEL;

  ngOnInit(): void {
    const param = this.route.snapshot.paramMap.get('language') as ProgrammingLanguage | null;
    if (!param || !PROGRAMMING_LANGUAGES.includes(param)) {
      this.router.navigate(['/desafios']);
      return;
    }
    this.language = param;
    this.languageLabel = LANGUAGE_LABEL[param];
    this.loadChallenges();
  }

  selectDifficulty(value: DifficultyLevel | undefined): void {
    this.selectedDifficulty.set(value);
    this.loadChallenges();
  }

  loadChallenges(): void {
    this.loading.set(true);
    this.error.set(null);
    this.challengeService.getChallenges(this.selectedDifficulty(), this.language).subscribe({
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
