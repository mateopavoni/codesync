import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { DashboardService, type LeaderboardEntry } from '../../core/services/dashboard.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorMessageComponent } from '../../shared/components/error-message/error-message.component';

@Component({
  selector: 'app-leaderboard',
  standalone: true,
  imports: [RouterLink, LoadingSpinnerComponent, ErrorMessageComponent],
  templateUrl: './leaderboard.component.html',
  styleUrl: './leaderboard.component.css',
})
export class LeaderboardComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly dashboardService = inject(DashboardService);

  readonly entries = signal<LeaderboardEntry[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly currentUid = this.authService.currentUser?.uid ?? null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.dashboardService.getLeaderboard().subscribe({
      next: (data) => {
        this.entries.set(data.entries);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No se pudo cargar el ranking.');
        this.loading.set(false);
      },
    });
  }
}
