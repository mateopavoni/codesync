export interface TestResult {
  testCaseId: string;
  input: string;
  expectedOutput: string;
  actualOutput: string;
  passed: boolean;
  executionTimeMs: number;
  /** true si es un test case oculto (TestCase.IsVisible == false): sin input/esperado. */
  isHidden: boolean;
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
  /** Error de infraestructura/interprete (no un test fallido) — p.ej. sandbox caido. */
  error: string | null;
  timedOut: boolean;
  /** stdout del código del usuario (console.log/print), separado de los resultados de los tests. */
  consoleOutput: string | null;
}

export interface SubmissionRequest {
  challengeId: string;
  code: string;
  language: string;
  roomId: string | null;
}
