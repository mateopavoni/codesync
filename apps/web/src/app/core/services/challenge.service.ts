import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import type { Challenge, ChallengeSummary, DifficultyLevel } from '../models/challenge.model';

@Injectable({ providedIn: 'root' })
export class ChallengeService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/challenges`;

  getChallenges(difficulty?: DifficultyLevel): Observable<ChallengeSummary[]> {
    let params = new HttpParams();
    if (difficulty) params = params.set('difficulty', difficulty);
    return this.http.get<ChallengeSummary[]>(this.baseUrl, { params });
  }

  getChallengeById(id: string): Observable<Challenge> {
    // API returns solutionTemplate/visibleTestCases; map to Angular model shape
    return this.http.get<any>(`${this.baseUrl}/${id}`).pipe(
      map((dto) => ({
        id: dto.id,
        title: dto.title,
        description: dto.description,
        difficulty: dto.difficulty,
        language: dto.language,
        starterCode: dto.solutionTemplate ?? '',
        testCases: (dto.visibleTestCases ?? []).map((tc: any, i: number) => ({
          id: String(i),
          input: tc.args ?? '',
          expectedOutput: tc.expectedOutput ?? '',
          isPublic: true,
        })),
        tags: dto.tags ?? [],
        createdAt: dto.createdAt ?? '',
      })),
    );
  }
}
