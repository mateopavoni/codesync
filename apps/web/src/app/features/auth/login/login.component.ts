import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { ErrorMessageComponent } from '../../../shared/components/error-message/error-message.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, ErrorMessageComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  async onEmailLogin(): Promise<void> {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);
    try {
      const { email, password } = this.form.getRawValue();
      await this.authService.loginWithEmail(email!, password!);
    } catch (err) {
      console.error("[CodeSync auth]", err);
      this.error.set(this.mapFirebaseError(err));
    } finally {
      this.loading.set(false);
    }
  }

  async onGoogleLogin(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      await this.authService.loginWithGoogle();
    } catch (err) {
      console.error("[CodeSync auth]", err);
      this.error.set(this.mapFirebaseError(err));
    } finally {
      this.loading.set(false);
    }
  }

  async onGithubLogin(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      await this.authService.loginWithGithub();
    } catch (err) {
      console.error("[CodeSync auth]", err);
      this.error.set(this.mapFirebaseError(err));
    } finally {
      this.loading.set(false);
    }
  }

  private mapFirebaseError(err: unknown): string {
    if (err instanceof Error) {
      if (err.message.includes('user-not-found') || err.message.includes('wrong-password')) {
        return 'Email o contraseña incorrectos.';
      }
      if (err.message.includes('too-many-requests')) {
        return 'Demasiados intentos. Intentá más tarde.';
      }
      if (err.message.includes('popup-closed-by-user')) {
        return 'Inicio de sesión cancelado.';
      }
    }
    return 'Error al iniciar sesión. Intentá de nuevo.';
  }
}
