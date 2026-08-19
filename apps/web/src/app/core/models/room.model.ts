export interface Room {
  id: string;
  inviteCode: string;
  challengeId: string | null;
  hostUid: string;
  participants: RoomParticipant[];
  maxParticipants: number;
  createdAt: string;
}

// Respuesta real de POST /api/rooms (CreateRoomDto del backend)
export interface CreateRoomResult {
  roomId: string;
  inviteCode: string;
  challengeId: string | null;
  maxMembers: number;
}

// Respuesta real de POST /api/rooms/{code}/join (JoinRoomDto del backend)
export interface JoinRoomResult {
  roomId: string;
  challengeId: string | null;
  memberIds: string[];
  maxMembers: number;
}

// Respuesta real de GET /api/rooms/{id} (RoomDto del backend)
export interface RoomStatus {
  id: string;
  inviteCode: string;
  challengeId: string | null;
  memberIds: string[];
  maxMembers: number;
  hostUserId: string;
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
