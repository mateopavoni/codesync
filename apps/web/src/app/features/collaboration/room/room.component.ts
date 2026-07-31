import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CollaborationService } from '../../../core/services/collaboration.service';
import { ErrorMessageComponent } from '../../../shared/components/error-message/error-message.component';

@Component({
  selector: 'app-room',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, ErrorMessageComponent],
  templateUrl: './room.component.html',
  styleUrl: './room.component.css',
})
export class RoomComponent {
  private readonly collaborationService = inject(CollaborationService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly createForm = this.fb.group({
    challengeId: ['', Validators.required],
  });

  readonly joinForm = this.fb.group({
    inviteCode: ['', [Validators.required, Validators.minLength(6)]],
  });

  readonly creating = signal(false);
  readonly joining = signal(false);
  readonly createError = signal<string | null>(null);
  readonly joinError = signal<string | null>(null);

  async onCreate(): Promise<void> {
    if (this.createForm.invalid) return;
    this.creating.set(true);
    this.createError.set(null);
    const { challengeId } = this.createForm.getRawValue();
    this.collaborationService.createRoom(challengeId!).subscribe({
      next: (room) => {
        this.creating.set(false);
        this.router.navigate(['/editor', room.challengeId, 'sala', room.roomId]);
      },
      error: () => {
        this.createError.set('No se pudo crear la sala. Intentá de nuevo.');
        this.creating.set(false);
      },
    });
  }

  async onJoin(): Promise<void> {
    if (this.joinForm.invalid) return;
    this.joining.set(true);
    this.joinError.set(null);
    const { inviteCode } = this.joinForm.getRawValue();
    this.collaborationService.joinRoom(inviteCode!.toUpperCase()).subscribe({
      next: (room) => {
        this.joining.set(false);
        this.router.navigate(['/editor', room.challengeId, 'sala', room.roomId]);
      },
      error: () => {
        this.joinError.set('Código de invitación inválido o sala llena.');
        this.joining.set(false);
      },
    });
  }
}
