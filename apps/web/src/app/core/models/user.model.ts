export interface UserProfile {
  uid: string;
  email: string | null;
  displayName: string | null;
  photoUrl: string | null;
  level: number;
  xp: number;
  completedChallengeCount: number;
  createdAt: string;
}
