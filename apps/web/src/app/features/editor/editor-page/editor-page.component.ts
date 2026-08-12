import {
  Component,
  inject,
  OnDestroy,
  OnInit,
  signal,
  ViewChild,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { ChallengeService } from '../../../core/services/challenge.service';
import { SubmissionService } from '../../../core/services/submission.service';
import { CollaborationService } from '../../../core/services/collaboration.service';
import { MonacoEditorComponent } from '../monaco-editor/monaco-editor.component';
import { ExecutionPanelComponent } from '../execution-panel/execution-panel.component';
import { CoachPanelComponent } from '../coach-panel/coach-panel.component';
import { LivePreviewComponent } from '../live-preview/live-preview.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorMessageComponent } from '../../../shared/components/error-message/error-message.component';
import type { Challenge } from '../../../core/models/challenge.model';
import type { SubmissionResult } from '../../../core/models/submission.model';
import type { CursorPosition } from '../../../core/models/room.model';
import { CURSOR_COLORS } from '../../../core/models/room.model';

type ActivePanel = 'resultado' | 'coach' | 'preview';

@Component({
  selector: 'app-editor-page',
  standalone: true,
  imports: [
    RouterLink,
    MonacoEditorComponent,
    ExecutionPanelComponent,
    CoachPanelComponent,
    LivePreviewComponent,
    LoadingSpinnerComponent,
    ErrorMessageComponent,
  ],
  templateUrl: './editor-page.component.html',
  styleUrl: './editor-page.component.css',
})
export class EditorPageComponent implements OnInit, OnDestroy {
  @ViewChild(MonacoEditorComponent) editorRef?: MonacoEditorComponent;

  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);
  private readonly challengeService = inject(ChallengeService);
  private readonly submissionService = inject(SubmissionService);
  private readonly collaborationService = inject(CollaborationService);

  private readonly destroy$ = new Subject<void>();
  private readonly codeChange$ = new Subject<string>();

  readonly challenge = signal<Challenge | null>(null);
  readonly challengeLoading = signal(true);
  readonly challengeError = signal<string | null>(null);

  readonly submissionResult = signal<SubmissionResult | null>(null);
  readonly submitting = signal(false);
  readonly submissionError = signal<string | null>(null);

  readonly coachFeedback = signal<string | null>(null);
  readonly coachHints = signal<string[]>([]);

  readonly activePanel = signal<ActivePanel>('resultado');

  // Colaboración
  readonly roomId = signal<string | null>(null);
  readonly remoteCursors = signal<CursorPosition[]>([]);
  readonly currentCode = signal('');

  // Desafíos HTML: preview en vivo (iframe), sin submission — se actualiza con
  // más frecuencia que el auto-save pero sin recargar el iframe en cada tecla.
  readonly previewCode = signal('');
  private readonly PREVIEW_DEBOUNCE_MS = 400;

  // Auto-save periódico via debounce sobre los cambios del editor
  private readonly AUTO_SAVE_DEBOUNCE_MS = 2000;

  ngOnInit(): void {
    const challengeId = this.route.snapshot.paramMap.get('challengeId');
    const roomId = this.route.snapshot.paramMap.get('roomId');

    if (challengeId) this.loadChallenge(challengeId);
    if (roomId) {
      this.roomId.set(roomId);
      this.setupCollaboration(roomId);
    }

    // Preview en vivo (solo desafíos HTML) — debounce corto, distinto del
    // auto-save, para que el iframe no se recargue en cada tecla.
    this.codeChange$
      .pipe(debounceTime(this.PREVIEW_DEBOUNCE_MS), takeUntil(this.destroy$))
      .subscribe((code) => this.previewCode.set(code));

    // Auto-save: sincroniza con Firebase Realtime DB si hay sala activa
    this.codeChange$
      .pipe(debounceTime(this.AUTO_SAVE_DEBOUNCE_MS), takeUntil(this.destroy$))
      .subscribe((code) => {
        const rId = this.roomId();
        if (rId) {
          this.collaborationService.setCode(rId, code).catch(() => {
            // Auto-save silencioso — no interrumpir al usuario
          });
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    // Limpiar cursor del usuario al salir de la sala
    const rId = this.roomId();
    const uid = this.authService.currentUser?.uid;
    if (rId && uid) {
      this.collaborationService.removeCursor(rId, uid).catch(() => undefined);
    }
  }

  onCodeChange(code: string): void {
    this.currentCode.set(code);
    this.codeChange$.next(code);
  }

  onCursorChange(pos: { lineNumber: number; column: number }): void {
    const rId = this.roomId();
    const user = this.authService.currentUser;
    if (!rId || !user) return;

    const colorIndex = this.getColorIndexForUser(user.uid);
    const cursor: CursorPosition = {
      uid: user.uid,
      displayName: user.displayName ?? user.email ?? 'Anónimo',
      lineNumber: pos.lineNumber,
      column: pos.column,
      color: CURSOR_COLORS[colorIndex],
    };

    this.collaborationService.updateCursor(rId, user.uid, cursor).catch(() => undefined);
  }

  async onRunCode(): Promise<void> {
    const code = this.editorRef?.getCurrentValue() ?? '';
    const ch = this.challenge();
    if (!ch || !code.trim()) return;

    this.submitting.set(true);
    this.submissionError.set(null);
    this.activePanel.set('resultado');

    this.submissionService
      .submit({
        challengeId: ch.id,
        code,
        language: ch.language,
        roomId: this.roomId(),
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.submissionResult.set(result);
          this.submitting.set(false);

          if (!result.allPassed && result.feedback) {
            this.coachFeedback.set(result.feedback);
            this.coachHints.set([]);
            this.activePanel.set('coach');
          }
        },
        error: () => {
          this.submissionError.set('Error al ejecutar el código. Intentá de nuevo.');
          this.submitting.set(false);
        },
      });
  }

  setActivePanel(panel: ActivePanel): void {
    this.activePanel.set(panel);
  }

  retryChallenge(): void {
    const id = this.route.snapshot.paramMap.get('challengeId');
    if (id) this.loadChallenge(id);
  }

  private loadChallenge(id: string): void {
    this.challengeLoading.set(true);
    this.challengeError.set(null);
    this.challengeService
      .getChallengeById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (c) => {
          this.challenge.set(c);
          this.currentCode.set(c.starterCode);
          this.previewCode.set(c.starterCode);
          if (c.language === 'html' || c.language === 'css') this.activePanel.set('preview');
          this.challengeLoading.set(false);
        },
        error: () => {
          this.challengeError.set('No se pudo cargar el desafío.');
          this.challengeLoading.set(false);
        },
      });
  }

  private setupCollaboration(roomId: string): void {
    // Recibir código de otros usuarios
    this.collaborationService
      .watchCode(roomId)
      .pipe(takeUntil(this.destroy$))
      .subscribe((code) => {
        if (code !== null && code !== this.currentCode()) {
          this.currentCode.set(code);
        }
      });

    // Recibir cursores de otros usuarios
    this.collaborationService
      .watchCursors(roomId)
      .pipe(takeUntil(this.destroy$))
      .subscribe((cursorsMap) => {
        const myUid = this.authService.currentUser?.uid;
        if (!cursorsMap) {
          this.remoteCursors.set([]);
          return;
        }
        const others = Object.values(cursorsMap).filter((c) => c.uid !== myUid);
        this.remoteCursors.set(others);
      });
  }

  private getColorIndexForUser(uid: string): number {
    // Asigna un color consistente por uid usando el hash del string
    let hash = 0;
    for (let i = 0; i < uid.length; i++) {
      hash = (hash * 31 + uid.charCodeAt(i)) | 0;
    }
    return Math.abs(hash) % CURSOR_COLORS.length;
  }
}
