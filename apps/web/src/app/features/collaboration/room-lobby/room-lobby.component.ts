import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CollaborationService } from '../../../core/services/collaboration.service';
import { ChallengeService } from '../../../core/services/challenge.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorMessageComponent } from '../../../shared/components/error-message/error-message.component';
import { DIFFICULTY_LABEL, LANGUAGE_LABEL } from '../../../core/models/challenge.model';
import type { ChallengeSummary } from '../../../core/models/challenge.model';
import type { RoomStatus } from '../../../core/models/room.model';

@Component({
  selector: 'app-room-lobby',
  standalone: true,
  imports: [LoadingSpinnerComponent, ErrorMessageComponent],
  templateUrl: './room-lobby.component.html',
  styleUrl: './room-lobby.component.css',
})
export class RoomLobbyComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly collaborationService = inject(CollaborationService);
  private readonly challengeService = inject(ChallengeService);

  readonly difficultyLabel = DIFFICULTY_LABEL;
  readonly languageLabel = LANGUAGE_LABEL;

  readonly room = signal<RoomStatus | null>(null);
  readonly challenges = signal<ChallengeSummary[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly selecting = signal(false);

  private roomId = '';

  ngOnInit(): void {
    this.roomId = this.route.snapshot.paramMap.get('roomId') ?? '';
    if (!this.roomId) {
      this.error.set('Sala inválida.');
      this.loading.set(false);
      return;
    }
    this.loadRoom();
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
        this.challengeService.getChallenges().subscribe((list) => this.challenges.set(list));
      },
      error: () => {
        this.error.set('No se pudo cargar la sala.');
        this.loading.set(false);
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
