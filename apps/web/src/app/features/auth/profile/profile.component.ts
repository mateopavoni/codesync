import { Component, inject, OnInit, signal } from '@angular/core';
import { SlicePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { DashboardService } from '../../../core/services/dashboard.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorMessageComponent } from '../../../shared/components/error-message/error-message.component';
import type { UserProfile } from '../../../core/models/user.model';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [SlicePipe, RouterLink, LoadingSpinnerComponent, ErrorMessageComponent],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css',
})
export class ProfileComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly dashboardService = inject(DashboardService);

  readonly currentUser$ = this.authService.currentUser$;

  readonly profile = signal<UserProfile | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

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
}
