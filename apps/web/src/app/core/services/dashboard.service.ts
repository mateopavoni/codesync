import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import type { CoachFeedback } from '../models/feedback.model';
import type { ChallengeSummary } from '../models/challenge.model';
import type { UserProfile } from '../models/user.model';

export interface DashboardData {
  profile: UserProfile;
  completedChallenges: ChallengeSummary[];
  pendingChallenges: ChallengeSummary[];
  recentFeedback: CoachFeedback[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getDashboard(): Observable<DashboardData> {
    return this.http.get<DashboardData>(`${this.baseUrl}/dashboard`);
  }

  getUserProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.baseUrl}/users/me`);
  }
}
