import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { SlicePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { DashboardService, type DashboardData } from '../../core/services/dashboard.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorMessageComponent } from '../../shared/components/error-message/error-message.component';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { DIFFICULTY_LABEL } from '../../core/models/challenge.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [SlicePipe, RouterLink, LoadingSpinnerComponent, ErrorMessageComponent, ConfirmDialogComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
})
export class DashboardComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly dashboardService = inject(DashboardService);

  readonly data = signal<DashboardData | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly difficultyLabel = DIFFICULTY_LABEL;

  readonly currentUser = this.authService.currentUser;

  readonly clearingHistory = signal(false);
  readonly showClearHistoryConfirm = signal(false);

  /** Fill percent + remaining-count for the level progress bar. null while loading. */
  readonly levelProgress = computed(() => {
    const d = this.data();
    if (!d) return null;

    if (d.nextLevelThreshold == null) {
      return { percent: 100, remaining: 0, maxed: true };
    }

    const completed = d.completedChallenges.length;
    const span = d.nextLevelThreshold - d.currentLevelFloor;
    const percent = span > 0 ? Math.min(100, Math.max(0, ((completed - d.currentLevelFloor) / span) * 100)) : 100;
    return { percent, remaining: d.nextLevelThreshold - completed, maxed: false };
  });

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading.set(true);
    this.error.set(null);
    this.dashboardService.getDashboard().subscribe({
      next: (d) => {
        this.data.set(d);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se pudo cargar el dashboard.');
        this.loading.set(false);
      },
    });
  }

  clearFeedbackHistory(): void {
    this.showClearHistoryConfirm.set(true);
  }

  onConfirmClearHistory(): void {
    this.showClearHistoryConfirm.set(false);
    this.clearingHistory.set(true);
    this.dashboardService.clearFeedbackHistory().subscribe({
      next: () => {
        const d = this.data();
        if (d) this.data.set({ ...d, recentFeedback: [] });
        this.clearingHistory.set(false);
      },
      error: () => this.clearingHistory.set(false),
    });
  }
}
