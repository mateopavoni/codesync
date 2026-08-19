import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FirebaseRealtimeService } from './firebase-realtime.service';
import { environment } from '../../../environments/environment';
import type { ChatMessage, CreateRoomResult, CursorPosition, JoinRoomResult, RoomStatus } from '../models/room.model';

@Injectable({ providedIn: 'root' })
export class CollaborationService {
  private readonly http = inject(HttpClient);
  private readonly rtDb = inject(FirebaseRealtimeService);
  private readonly baseUrl = `${environment.apiUrl}/rooms`;

  createRoom(): Observable<CreateRoomResult> {
    return this.http.post<CreateRoomResult>(this.baseUrl, {});
  }

  joinRoom(inviteCode: string): Observable<JoinRoomResult> {
    return this.http.post<JoinRoomResult>(`${this.baseUrl}/${inviteCode}/join`, {});
  }

  getRoom(roomId: string): Observable<RoomStatus> {
    return this.http.get<RoomStatus>(`${this.baseUrl}/${roomId}`);
  }

  selectChallenge(roomId: string, challengeId: string): Observable<{ roomId: string; challengeId: string }> {
    return this.http.post<{ roomId: string; challengeId: string }>(`${this.baseUrl}/${roomId}/challenge`, {
      challengeId,
    });
  }

  closeRoom(roomId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${roomId}`);
  }

  // Realtime DB — código compartido
  watchCode(roomId: string): Observable<string | null> {
    return this.rtDb.watch<string>(`collaborations/${roomId}/code`);
  }

  setCode(roomId: string, code: string): Promise<void> {
    return this.rtDb.set(`collaborations/${roomId}/code`, code);
  }

  // Realtime DB — cursores de otros usuarios
  watchCursors(roomId: string): Observable<Record<string, CursorPosition> | null> {
    return this.rtDb.watch<Record<string, CursorPosition>>(`collaborations/${roomId}/cursors`);
  }

  updateCursor(roomId: string, uid: string, position: CursorPosition): Promise<void> {
    return this.rtDb.set(`collaborations/${roomId}/cursors/${uid}`, position);
  }

  removeCursor(roomId: string, uid: string): Promise<void> {
    return this.rtDb.set(`collaborations/${roomId}/cursors/${uid}`, null);
  }

  // Realtime DB — chat
  watchMessages(roomId: string): Observable<Record<string, ChatMessage> | null> {
    return this.rtDb.watch<Record<string, ChatMessage>>(`collaborations/${roomId}/chat`);
  }

  sendMessage(roomId: string, message: Omit<ChatMessage, 'id'>): Promise<string | null> {
    return this.rtDb.push(`collaborations/${roomId}/chat`, message);
  }
}
