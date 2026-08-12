export type DifficultyLevel = 'facil' | 'medio' | 'dificil';
// 'html' es solo preview en vivo (iframe) — sin calificación automática todavía.
export type ProgrammingLanguage = 'python' | 'javascript' | 'html' | 'ruby' | 'java' | 'csharp';

export interface TestCase {
  id: string;
  input: string;
  expectedOutput: string;
  isPublic: boolean;
}

export interface Challenge {
  id: string;
  title: string;
  description: string;
  difficulty: DifficultyLevel;
  language: ProgrammingLanguage;
  starterCode: string;
  testCases: TestCase[];
  tags?: string[];
  createdAt: string;
}

export interface ChallengeSummary {
  id: string;
  title: string;
  difficulty: DifficultyLevel;
  language: ProgrammingLanguage;
  tags?: string[];
  completedAt: string | null;
}

export const DIFFICULTY_LABEL: Record<DifficultyLevel, string> = {
  facil: 'Fácil',
  medio: 'Medio',
  dificil: 'Difícil',
};

export const LANGUAGE_LABEL: Record<ProgrammingLanguage, string> = {
  javascript: 'JavaScript',
  python: 'Python',
  html: 'HTML',
  ruby: 'Ruby',
  java: 'Java',
  csharp: 'C#',
};

// Orden de visualización en el grid de /desafios
export const PROGRAMMING_LANGUAGES: ProgrammingLanguage[] = [
  'python',
  'javascript',
  'html',
  'ruby',
  'java',
  'csharp',
];
