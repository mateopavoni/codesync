export type DifficultyLevel = 'facil' | 'medio' | 'dificil';
export type ProgrammingLanguage = 'python' | 'javascript';

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
