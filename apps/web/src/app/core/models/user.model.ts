export interface UserProfile {
  uid: string;
  email: string | null;
  displayName: string | null;
  photoURL: string | null;
  level: number;
  completedChallengesCount: number;
  createdAt: string;
}
