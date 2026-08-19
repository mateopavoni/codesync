import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CollaborationService } from '../../../core/services/collaboration.service';
import { ChallengeService } from '../../../core/services/challenge.service';
import { AuthService } from '../../../core/services/auth.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorMessageComponent } from '../../../shared/components/error-message/error-message.component';
import { LangIconComponent } from '../../../shared/components/lang-icon/lang-icon.component';
import { DIFFICULTY_LABEL, LANGUAGE_LABEL, PROGRAMMING_LANGUAGES } from '../../../core/models/challenge.model';
import type { ChallengeSummary, DifficultyLevel, ProgrammingLanguage } from '../../../core/models/challenge.model';
import type { RoomStatus } from '../../../core/models/room.model';

@Component({
  selector: 'app-room-lobby',
  standalone: true,
  imports: [LoadingSpinnerComponent, ErrorMessageComponent, LangIconComponent],
  templateUrl: './room-lobby.component.html',
  styleUrl: './room-lobby.component.css',
})
export class RoomLobbyComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly collaborationService = inject(CollaborationService);
  private readonly challengeService = inject(ChallengeService);
  private readonly authService = inject(AuthService);

  readonly difficultyLabel = DIFFICULTY_LABEL;
  readonly languageLabel = LANGUAGE_LABEL;
  readonly languages = PROGRAMMING_LANGUAGES;

  readonly difficultyOptions: { value: DifficultyLevel | undefined; label: string }[] = [
    { value: undefined, label: 'Todos' },
    { value: 'facil', label: 'Fácil' },
    { value: 'medio', label: 'Medio' },
    { value: 'dificil', label: 'Difícil' },
  ];

  readonly room = signal<RoomStatus | null>(null);
  readonly challenges = signal<ChallengeSummary[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly selecting = signal(false);
  readonly closing = signal(false);
  readonly selectedLanguage = signal<ProgrammingLanguage | undefined>(undefined);
  readonly selectedDifficulty = signal<DifficultyLevel | undefined>(undefined);

  private roomId = '';

  get isHost(): boolean {
    return this.room()?.hostUserId === this.authService.currentUser?.uid;
  }

  ngOnInit(): void {
    this.roomId = this.route.snapshot.paramMap.get('roomId') ?? '';
    if (!this.roomId) {
      this.error.set('Sala inválida.');
      this.loading.set(false);
      return;
    }
    this.loadRoom();
  }

  selectLanguage(value: ProgrammingLanguage | undefined): void {
    this.selectedLanguage.set(value);
    this.loadChallenges();
  }

  selectDifficulty(value: DifficultyLevel | undefined): void {
    this.selectedDifficulty.set(value);
    this.loadChallenges();
  }

  private loadRoom(): void {
    this.loading.set(true);
    this.error.set(null);
    this.collaborationService.getRoom(this.roomId).subscribe({
      next: (room) => {
        if (room.challengeId) {
          // Otro miembro ya eligió desafío mientras tanto — ir directo al editor.
          this.router.navigate(['/editor', room.challengeId, 'sala', this.roomId]);
          return;
        }
        this.room.set(room);
        this.loading.set(false);
        this.loadChallenges();
      },
      error: () => {
        this.error.set('No se pudo cargar la sala.');
        this.loading.set(false);
      },
    });
  }

  private loadChallenges(): void {
    this.challengeService
      .getChallenges(this.selectedDifficulty(), this.selectedLanguage())
      .subscribe((list) => this.challenges.set(list));
  }

  closeRoom(): void {
    if (this.closing() || !confirm('¿Cerrar esta sala? No se va a poder volver a usar.')) return;
    this.closing.set(true);
    this.error.set(null);
    this.collaborationService.closeRoom(this.roomId).subscribe({
      next: () => this.router.navigate(['/colaboracion']),
      error: () => {
        this.error.set('No se pudo cerrar la sala. Intentá de nuevo.');
        this.closing.set(false);
      },
    });
  }

  selectChallenge(challengeId: string): void {
    if (this.selecting()) return;
    this.selecting.set(true);
    this.error.set(null);
    this.collaborationService.selectChallenge(this.roomId, challengeId).subscribe({
      next: () => {
        this.router.navigate(['/editor', challengeId, 'sala', this.roomId]);
      },
      error: () => {
        this.error.set('No se pudo asignar el desafío. Intentá de nuevo.');
        this.selecting.set(false);
      },
    });
  }
}
