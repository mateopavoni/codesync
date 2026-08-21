import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ChallengeService } from '../../../core/services/challenge.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorMessageComponent } from '../../../shared/components/error-message/error-message.component';
import type { Challenge } from '../../../core/models/challenge.model';
import { DIFFICULTY_LABEL, DIFFICULTY_XP, LANGUAGE_LABEL } from '../../../core/models/challenge.model';

@Component({
  selector: 'app-challenge-detail',
  standalone: true,
  imports: [RouterLink, LoadingSpinnerComponent, ErrorMessageComponent],
  templateUrl: './challenge-detail.component.html',
  styleUrl: './challenge-detail.component.css',
})
export class ChallengeDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly challengeService = inject(ChallengeService);

  readonly challenge = signal<Challenge | null>(null);
  readonly languageLabel = LANGUAGE_LABEL;
  readonly difficultyLabel = DIFFICULTY_LABEL;
  readonly difficultyXp = DIFFICULTY_XP;
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadChallenge(id);
  }

  loadChallenge(id: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.challengeService.getChallengeById(id).subscribe({
      next: (c) => {
        this.challenge.set(c);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se pudo cargar el desafío.');
        this.loading.set(false);
      },
    });
  }

  retryLoad(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadChallenge(id);
  }
}
