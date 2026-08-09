import { Component, computed, input } from '@angular/core';

// Puerto de la lógica (no la UI/motion) de diseños/componentes/password-strength.tsx:
// reglas + detección de patrones adivinables + scoring. Feedback de UI únicamente —
// la validación real que bloquea el submit sigue siendo Validators.minLength(6).

interface PasswordRule {
  id: string;
  label: string;
  test: (value: string) => boolean;
}

const COMMON =
  /^(?:password|contraseña|passw0rd|qwerty|letmein|welcome|admin|iloveyou|monkey|dragon|abc123|111111|123123|123456)/i;
const RUN = /(.)\1{3,}/;
const RUN_UP = /(?:0123|1234|2345|3456|4567|5678|6789|abcd|bcde|cdef|defg|qwer|wert|erty|asdf)/i;
const SYMBOL = /[!-/:-@[-`{-~]/;

const RULES: readonly PasswordRule[] = [
  { id: 'length', label: '12 caracteres o más', test: (v) => v.length >= 12 },
  { id: 'case', label: 'Mayúsculas y minúsculas', test: (v) => /[a-z]/.test(v) && /[A-Z]/.test(v) },
  { id: 'digit', label: 'Un número', test: (v) => /\d/.test(v) },
  { id: 'symbol', label: 'Un símbolo', test: (v) => SYMBOL.test(v) },
];

const LABELS = ['Vacía', 'Débil', 'Aceptable', 'Buena', 'Fuerte'] as const;

@Component({
  selector: 'app-password-strength',
  standalone: true,
  template: `
    @if (value().length > 0) {
      <div class="pw-strength">
        <div class="pw-bars">
          @for (i of bars; track i) {
            <div class="pw-bar-track">
              <span class="pw-bar-fill" [class]="'tone-' + tone()" [class.filled]="i < score()"></span>
            </div>
          }
        </div>

        <div class="pw-meta">
          <span class="pw-label" [class]="'tone-' + tone()">{{ label() }}</span>
          @if (guessable()) {
            <span class="pw-guessable">Patrón común</span>
          }
        </div>

        <ul class="pw-rules">
          @for (rule of evaluated(); track rule.id) {
            <li [class.met]="rule.met">
              <span class="pw-check" aria-hidden="true">
                @if (rule.met) {
                  <svg viewBox="0 0 12 12" width="9" height="9">
                    <path
                      d="M2 6.2 4.7 8.9 10 3.3"
                      stroke="currentColor"
                      stroke-width="1.9"
                      fill="none"
                      stroke-linecap="round"
                      stroke-linejoin="round"
                    />
                  </svg>
                }
              </span>
              {{ rule.label }}
              <span class="sr-only">{{ rule.met ? 'cumplido' : 'pendiente' }}</span>
            </li>
          }
        </ul>

        <p class="sr-only" aria-live="polite">
          Fuerza de contraseña: {{ label() }}.
          {{ guessable() ? 'Patrón común detectado.' : '' }}
        </p>
      </div>
    }
  `,
  styles: [
    `
      .pw-strength {
        display: flex;
        flex-direction: column;
        gap: 8px;
        margin-top: -4px;
      }

      .pw-bars {
        display: grid;
        grid-template-columns: repeat(4, 1fr);
        gap: 4px;
      }

      .pw-bar-track {
        height: 4px;
        border-radius: 2px;
        overflow: hidden;
        background: var(--cs-surface-highest);
      }

      .pw-bar-fill {
        display: block;
        height: 100%;
        border-radius: 2px;
        transform: scaleX(0);
        transform-origin: left;
        transition: transform 0.2s ease-out, background-color 0.2s;
        background: var(--cs-outline-var);
      }
      .pw-bar-fill.filled {
        transform: scaleX(1);
      }
      .pw-bar-fill.tone-danger.filled { background: var(--cs-outline); }
      .pw-bar-fill.tone-caution.filled { background: var(--cs-primary-container); }
      .pw-bar-fill.tone-safe.filled { background: var(--cs-primary); }
      .pw-bar-fill.tone-strong.filled { background: var(--cs-tertiary); }

      .pw-meta {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 8px;
        font-size: var(--cs-text-label);
      }

      .pw-label { font-weight: 600; color: var(--cs-on-surface-var); }
      .pw-label.tone-danger { color: var(--cs-outline); }
      .pw-label.tone-caution { color: var(--cs-primary-container); }
      .pw-label.tone-safe { color: var(--cs-primary); }
      .pw-label.tone-strong { color: var(--cs-tertiary); }

      .pw-guessable { color: var(--cs-primary); }

      .pw-rules {
        list-style: none;
        margin: 2px 0 0;
        padding: 0;
        display: grid;
        gap: 4px;
      }

      .pw-rules li {
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: var(--cs-text-label);
        color: var(--cs-on-surface-var);
        transition: color 0.15s;
      }
      .pw-rules li.met { color: var(--cs-on-surface); }

      .pw-check {
        display: grid;
        place-items: center;
        width: 14px;
        height: 14px;
        flex-shrink: 0;
        border-radius: 4px;
        border: 1px solid var(--cs-outline-var);
        color: var(--cs-on-primary);
        transition: background-color 0.15s, border-color 0.15s;
      }
      li.met .pw-check {
        background: var(--cs-primary);
        border-color: var(--cs-primary);
      }

      .sr-only {
        position: absolute;
        width: 1px;
        height: 1px;
        padding: 0;
        margin: -1px;
        overflow: hidden;
        clip: rect(0, 0, 0, 0);
        white-space: nowrap;
        border: 0;
      }
    `,
  ],
})
export class PasswordStrengthComponent {
  readonly value = input('');

  protected readonly bars = RULES.map((_, i) => i);

  protected readonly evaluated = computed(() =>
    RULES.map((rule) => ({ ...rule, met: rule.test(this.value()) })),
  );

  protected readonly guessable = computed(() => {
    const v = this.value();
    return v.length > 0 && (COMMON.test(v) || RUN.test(v) || RUN_UP.test(v));
  });

  protected readonly score = computed(() => {
    const v = this.value();
    if (v.length === 0) return 0;
    if (this.guessable()) return 1;
    const passed = this.evaluated().filter((r) => r.met).length;
    return Math.min(RULES.length, Math.max(1, passed));
  });

  protected readonly label = computed(() => LABELS[Math.min(this.score(), LABELS.length - 1)]);

  protected readonly tone = computed((): 'none' | 'danger' | 'caution' | 'safe' | 'strong' => {
    const s = this.score();
    if (s === 0) return 'none';
    if (s === 1) return 'danger';
    if (s === 2) return 'caution';
    if (s === 3) return 'safe';
    return 'strong';
  });
}
