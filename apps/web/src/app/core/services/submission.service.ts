import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import type { SubmissionRequest, SubmissionResult } from '../models/submission.model';

// API response shape (C# SubmissionResultDto serialized as camelCase)
interface ApiSubmissionResult {
  submissionId: string;
  allTestsPassed: boolean;
  // args/expectedOutput come back blank for hidden test cases (backend never
  // leaks a hidden test's expected answer) — see CreateSubmissionHandler.
  testResults: Array<{
    testCaseIndex: number;
    passed: boolean;
    actualOutput: string;
    error?: string;
    args?: string;
    expectedOutput?: string;
    isHidden?: boolean;
  }>;
  timedOut: boolean;
  error?: string;
  // C# "AIFeedback" camelCases to "aiFeedback" — lowercase 'ai' prefix
  aiFeedback?: string;
  feedbackIsFallback: boolean;
  executionTimeMs: number;
  consoleOutput?: string;
}

@Injectable({ providedIn: 'root' })
export class SubmissionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/submissions`;

  submit(request: SubmissionRequest): Observable<SubmissionResult> {
    return this.http.post<ApiSubmissionResult>(this.baseUrl, request).pipe(
      map((dto) => this.mapResult(dto)),
    );
  }

  getSubmission(id: string): Observable<SubmissionResult> {
    return this.http.get<ApiSubmissionResult>(`${this.baseUrl}/${id}`).pipe(
      map((dto) => this.mapResult(dto)),
    );
  }

  getUserSubmissions(challengeId: string): Observable<SubmissionResult[]> {
    return this.http.get<ApiSubmissionResult[]>(`${this.baseUrl}?challengeId=${challengeId}`).pipe(
      map((dtos) => dtos.map((dto) => this.mapResult(dto))),
    );
  }

  private mapResult(dto: ApiSubmissionResult): SubmissionResult {
    return {
      id: dto.submissionId,
      challengeId: '',
      code: '',
      language: '',
      allPassed: dto.allTestsPassed,
      results: (dto.testResults ?? []).map((r) => ({
        testCaseId: String(r.testCaseIndex),
        input: r.args ?? '',
        expectedOutput: r.expectedOutput ?? '',
        actualOutput: r.actualOutput ?? '',
        passed: r.passed,
        executionTimeMs: 0,
        isHidden: r.isHidden ?? false,
      })),
      totalExecutionTimeMs: dto.executionTimeMs,
      feedback: dto.aiFeedback ?? null,
      submittedAt: '',
      error: dto.error ?? null,
      timedOut: dto.timedOut,
      consoleOutput: dto.consoleOutput ?? null,
    };
  }
}
