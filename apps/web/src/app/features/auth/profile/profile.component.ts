import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { DashboardService } from '../../../core/services/dashboard.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorMessageComponent } from '../../../shared/components/error-message/error-message.component';
import { PasswordStrengthComponent } from '../../../shared/components/password-strength/password-strength.component';
import { passwordValidators } from '../../../shared/validators/password.validator';
import type { UserProfile } from '../../../core/models/user.model';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    ReactiveFormsModule,
    LoadingSpinnerComponent,
    ErrorMessageComponent,
    PasswordStrengthComponent,
  ],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css',
})
export class ProfileComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly dashboardService = inject(DashboardService);
  private readonly fb = inject(FormBuilder);

  readonly currentUser$ = this.authService.currentUser$;

  isPasswordUser(): boolean {
    return this.authService.isPasswordUser;
  }

  readonly profile = signal<UserProfile | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly avatarUploading = signal(false);
  readonly avatarError = signal<string | null>(null);

  readonly showPasswordForm = signal(false);
  readonly passwordLoading = signal(false);
  readonly passwordError = signal<string | null>(null);
  readonly passwordSuccess = signal(false);

  readonly passwordForm = this.fb.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', passwordValidators],
    confirmNewPassword: ['', [Validators.required]],
  });

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.loading.set(true);
    this.error.set(null);
    this.dashboardService.getUserProfile().subscribe({
      next: (p) => {
        this.profile.set(p);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se pudo cargar el perfil.');
        this.loading.set(false);
      },
    });
  }

  retryLoad(): void {
    this.loadProfile();
  }

  async onAvatarSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.avatarError.set(null);
    this.avatarUploading.set(true);
    try {
      await this.authService.changeAvatar(file);
      this.loadProfile();
    } catch (err) {
      const backendMessage = err instanceof HttpErrorResponse ? err.error?.error : undefined;
      this.avatarError.set(backendMessage ?? 'No se pudo subir la foto. Intentá de nuevo.');
    } finally {
      this.avatarUploading.set(false);
      input.value = '';
    }
  }

  togglePasswordForm(): void {
    this.showPasswordForm.update((v) => !v);
    this.passwordError.set(null);
    this.passwordSuccess.set(false);
    this.passwordForm.reset();
  }

  async onChangePassword(): Promise<void> {
    if (this.passwordForm.invalid) return;
    const { currentPassword, newPassword, confirmNewPassword } = this.passwordForm.getRawValue();
    if (newPassword !== confirmNewPassword) {
      this.passwordError.set('Las contraseñas nuevas no coinciden.');
      return;
    }

    this.passwordLoading.set(true);
    this.passwordError.set(null);
    this.passwordSuccess.set(false);
    try {
      await this.authService.changePassword(currentPassword!, newPassword!);
      this.passwordSuccess.set(true);
      this.passwordForm.reset();
    } catch (err) {
      this.passwordError.set(this.mapPasswordError(err));
    } finally {
      this.passwordLoading.set(false);
    }
  }

  private mapPasswordError(err: unknown): string {
    if (err instanceof Error) {
      if (err.message.includes('wrong-password') || err.message.includes('invalid-credential')) {
        return 'La contraseña actual es incorrecta.';
      }
      if (err.message.includes('requires-recent-login')) {
        return 'Por seguridad, cerrá sesión y volvé a entrar antes de cambiar la contraseña.';
      }
      if (err.message.includes('weak-password')) {
        return 'La contraseña nueva es muy débil.';
      }
    }
    return 'No se pudo cambiar la contraseña. Intentá de nuevo.';
  }
}
