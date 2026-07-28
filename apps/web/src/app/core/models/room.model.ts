export interface Room {
  id: string;
  inviteCode: string;
  challengeId: string;
  hostUid: string;
  participants: RoomParticipant[];
  maxParticipants: number;
  createdAt: string;
}

// Respuesta real de POST /api/rooms (CreateRoomDto del backend)
export interface CreateRoomResult {
  roomId: string;
  inviteCode: string;
  challengeId: string;
  maxMembers: number;
}

// Respuesta real de POST /api/rooms/{code}/join (JoinRoomDto del backend)
export interface JoinRoomResult {
  roomId: string;
  challengeId: string;
  memberIds: string[];
  maxMembers: number;
}

export interface RoomParticipant {
  uid: string;
  displayName: string;
  photoURL: string | null;
  joinedAt: string;
}

export interface ChatMessage {
  id: string;
  uid: string;
  displayName: string;
  text: string;
  timestamp: number;
}

export interface CursorPosition {
  uid: string;
  displayName: string;
  lineNumber: number;
  column: number;
  color: string;
}

// Colores para los cursores de usuarios remotos
export const CURSOR_COLORS = [
  '#f97316', // naranja
  '#a855f7', // violeta
  '#ec4899', // rosa
  '#14b8a6', // teal
  '#eab308', // amarillo
] as const;
