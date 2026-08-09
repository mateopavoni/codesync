import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { ErrorMessageComponent } from '../../../shared/components/error-message/error-message.component';
import { AuthShellComponent } from '../../../shared/components/auth-shell/auth-shell.component';
import { PasswordStrengthComponent } from '../../../shared/components/password-strength/password-strength.component';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, ErrorMessageComponent, AuthShellComponent, PasswordStrengthComponent],
  templateUrl: './signup.component.html',
  styleUrl: './signup.component.css',
})
export class SignupComponent {
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required]],
  });

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  async onSubmit(): Promise<void> {
    if (this.form.invalid) return;
    const { email, password, confirmPassword } = this.form.getRawValue();
    if (password !== confirmPassword) {
      this.error.set('Las contraseñas no coinciden.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    try {
      await this.authService.signupWithEmail(email!, password!);
    } catch (err) {
      this.error.set(this.mapFirebaseError(err));
    } finally {
      this.loading.set(false);
    }
  }

  private mapFirebaseError(err: unknown): string {
    if (err instanceof Error) {
      if (err.message.includes('email-already-in-use')) {
        return 'Ya existe una cuenta con ese email.';
      }
      if (err.message.includes('weak-password')) {
        return 'La contraseña es muy débil.';
      }
    }
    return 'Error al crear la cuenta. Intentá de nuevo.';
  }
}
