export interface CoachFeedback {
  id: string;
  submissionId: string;
  challengeId: string;
  challengeTitle: string;
  message: string;
  hints: string[];
  createdAt: string;
}
