import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CollaborationService } from '../../../core/services/collaboration.service';
import { ErrorMessageComponent } from '../../../shared/components/error-message/error-message.component';

@Component({
  selector: 'app-room',
  standalone: true,
  imports: [ReactiveFormsModule, ErrorMessageComponent],
  templateUrl: './room.component.html',
  styleUrl: './room.component.css',
})
export class RoomComponent {
  private readonly collaborationService = inject(CollaborationService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly joinForm = this.fb.group({
    inviteCode: ['', [Validators.required, Validators.minLength(6)]],
  });

  readonly creating = signal(false);
  readonly joining = signal(false);
  readonly createError = signal<string | null>(null);
  readonly joinError = signal<string | null>(null);

  async onCreate(): Promise<void> {
    this.creating.set(true);
    this.createError.set(null);
    this.collaborationService.createRoom().subscribe({
      next: (room) => {
        this.creating.set(false);
        this.router.navigate(['/sala', room.roomId]);
      },
      error: (err: HttpErrorResponse) => {
        this.createError.set(err.error?.error ?? 'No se pudo crear la sala. Intentá de nuevo.');
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
        if (room.challengeId) {
          this.router.navigate(['/editor', room.challengeId, 'sala', room.roomId]);
        } else {
          this.router.navigate(['/sala', room.roomId]);
        }
      },
      error: () => {
        this.joinError.set('Código de invitación inválido o sala llena.');
        this.joining.set(false);
      },
    });
  }
}
