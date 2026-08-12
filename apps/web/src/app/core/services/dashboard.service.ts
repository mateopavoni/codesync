import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import type { DifficultyLevel } from '../models/challenge.model';
import type { UserProfile } from '../models/user.model';

export interface DashboardChallenge {
  id: string;
  title: string;
  difficulty: DifficultyLevel;
}

export interface DashboardFeedback {
  id: string;
  challengeTitle: string;
  message: string;
  createdAt: string;
}

export interface DashboardData {
  level: number;
  completedChallenges: DashboardChallenge[];
  pendingChallenges: DashboardChallenge[];
  recentFeedback: DashboardFeedback[];
  // null when already at the max level (5) — nothing left to reach.
  nextLevelThreshold: number | null;
  currentLevelFloor: number;
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getDashboard(): Observable<DashboardData> {
    return this.http.get<DashboardData>(`${this.baseUrl}/dashboard`);
  }

  clearFeedbackHistory(): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/dashboard/feedback`);
  }

  getUserProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.baseUrl}/users/me`);
  }
}
