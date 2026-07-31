import {
  AfterViewChecked,
  Component,
  ElementRef,
  inject,
  Input,
  OnChanges,
  OnInit,
  OnDestroy,
  signal,
  SimpleChanges,
  ViewChild,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { CollaborationService } from '../../../core/services/collaboration.service';
import type { ChatMessage } from '../../../core/models/room.model';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="chat-panel">
      <div class="chat-header">
        <h3>Chat de la sala</h3>
      </div>

      <div class="messages"
           #messagesContainer
           aria-live="polite"
           aria-label="Mensajes del chat"
           aria-relevant="additions">
        @if (messages().length === 0) {
          <p class="empty-chat">Todavía no hay mensajes. ¡Empezá la conversación!</p>
        }
        @for (msg of messages(); track msg.id) {
          <div class="message" [class.own]="msg.uid === myUid()">
            <span class="msg-author">{{ msg.uid === myUid() ? 'Vos' : msg.displayName }}</span>
            <p class="msg-text">{{ msg.text }}</p>
          </div>
        }
      </div>

      <form [formGroup]="form" (ngSubmit)="onSend()" class="chat-form">
        <input
          type="text"
          formControlName="text"
          placeholder="Escribí un mensaje..."
          autocomplete="off"
          aria-label="Mensaje"
        />
        <button type="submit" [disabled]="form.invalid || sending()" aria-label="Enviar">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
            <path d="M2.01 21L23 12 2.01 3 2 10l15 2-15 2z"/>
          </svg>
        </button>
      </form>
    </div>
  `,
  styles: [`
    .chat-panel {
      display: flex;
      flex-direction: column;
      height: 100%;
      background: var(--cs-surface-low);
    }
    .chat-header {
      padding: 10px 16px;
      border-bottom: 1px solid var(--cs-outline-var);
      flex-shrink: 0;
    }
    .chat-header h3 {
      font-size: var(--cs-text-label);
      font-weight: 600;
      color: var(--cs-on-surface-var);
      margin: 0;
      text-transform: uppercase;
      letter-spacing: var(--cs-ls-label);
    }
    .messages {
      flex: 1;
      overflow-y: auto;
      padding: 12px;
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    .empty-chat {
      font-size: var(--cs-text-label);
      color: var(--cs-on-surface-var);
      text-align: center;
      padding: 16px 0;
    }
    .message {
      display: flex;
      flex-direction: column;
      gap: 2px;
      max-width: 85%;
    }
    .message.own { align-self: flex-end; align-items: flex-end; }
    .msg-author {
      font-size: var(--cs-text-label);
      color: var(--cs-on-surface-var);
    }
    .msg-text {
      background: var(--cs-surface-container);
      border: 1px solid var(--cs-outline-var);
      border-radius: 8px;
      padding: 8px 12px;
      font-size: var(--cs-text-body-sm);
      margin: 0;
      line-height: 1.45;
      word-break: break-word;
      color: var(--cs-on-surface);
    }
    .message.own .msg-text {
      background: rgba(173, 198, 255, 0.1);
      border-color: rgba(173, 198, 255, 0.25);
    }
    .chat-form {
      display: flex;
      gap: 8px;
      padding: 10px 12px;
      border-top: 1px solid var(--cs-outline-var);
      flex-shrink: 0;
    }
    .chat-form input {
      flex: 1;
      padding: 8px 12px;
      background: var(--cs-surface-container);
      border: 1px solid var(--cs-outline-var);
      border-radius: 8px;
      color: var(--cs-on-surface);
      font-size: var(--cs-text-body-sm);
      font-family: var(--cs-font-ui);
      outline: none;
      transition: border-color 0.15s;
    }
    .chat-form input:focus { border-color: var(--cs-primary); }
    .chat-form input::placeholder { color: var(--cs-outline); }
    .chat-form button {
      padding: 8px 12px;
      background: var(--cs-primary);
      color: var(--cs-on-primary);
      border: none;
      border-radius: 8px;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: opacity 0.15s;
    }
    .chat-form button:hover:not(:disabled) { opacity: 0.88; }
    .chat-form button:disabled { opacity: 0.5; cursor: not-allowed; }
  `],
})
export class ChatComponent implements OnInit, OnChanges, OnDestroy, AfterViewChecked {
  @ViewChild('messagesContainer') messagesContainer?: ElementRef<HTMLDivElement>;
  @Input({ required: true }) roomId!: string;

  private readonly authService = inject(AuthService);
  private readonly collaborationService = inject(CollaborationService);
  private readonly fb = inject(FormBuilder);
  private readonly destroy$ = new Subject<void>();

  readonly messages = signal<ChatMessage[]>([]);
  readonly sending = signal(false);
  readonly myUid = signal<string | null>(null);

  readonly form = this.fb.group({
    text: ['', [Validators.required, Validators.maxLength(500)]],
  });

  private shouldScrollToBottom = false;

  ngOnInit(): void {
    this.myUid.set(this.authService.currentUser?.uid ?? null);
    this.subscribeToMessages();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['roomId'] && !changes['roomId'].firstChange) {
      this.subscribeToMessages();
    }
  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollToBottom) {
      this.scrollToBottom();
      this.shouldScrollToBottom = false;
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  async onSend(): Promise<void> {
    const text = this.form.getRawValue().text?.trim();
    if (!text || this.sending()) return;

    const user = this.authService.currentUser;
    if (!user) return;

    this.sending.set(true);
    try {
      await this.collaborationService.sendMessage(this.roomId, {
        uid: user.uid,
        displayName: user.displayName ?? user.email ?? 'Anónimo',
        text,
        timestamp: Date.now(),
      });
      this.form.reset();
    } finally {
      this.sending.set(false);
    }
  }

  private subscribeToMessages(): void {
    this.destroy$.next(); // Cancela suscripción anterior si roomId cambió
    this.collaborationService
      .watchMessages(this.roomId)
      .pipe(takeUntil(this.destroy$))
      .subscribe((map) => {
        if (!map) {
          this.messages.set([]);
          return;
        }
        const msgs = Object.entries(map)
          .map(([id, msg]) => ({ ...msg, id }))
          .sort((a, b) => a.timestamp - b.timestamp);
        this.messages.set(msgs);
        this.shouldScrollToBottom = true;
      });
  }

  private scrollToBottom(): void {
    const el = this.messagesContainer?.nativeElement;
    if (el) el.scrollTop = el.scrollHeight;
  }
}
