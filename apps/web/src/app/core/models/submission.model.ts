export interface TestResult {
  testCaseId: string;
  input: string;
  expectedOutput: string;
  actualOutput: string;
  passed: boolean;
  executionTimeMs: number;
}

export interface SubmissionResult {
  id: string;
  challengeId: string;
  code: string;
  language: string;
  allPassed: boolean;
  results: TestResult[];
  totalExecutionTimeMs: number;
  feedback: string | null;
  submittedAt: string;
}

export interface SubmissionRequest {
  challengeId: string;
  code: string;
  language: string;
  roomId: string | null;
}
