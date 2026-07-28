import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { getDatabase, ref, set, onValue, push } from 'firebase/database';

// Servicio de bajo nivel sobre Firebase Realtime Database.
// Expone primitivas genéricas (watch, set, push) que los servicios de dominio consumen.
@Injectable({ providedIn: 'root' })
export class FirebaseRealtimeService {
  private readonly db = getDatabase();

  watch<T>(path: string): Observable<T | null> {
    return new Observable<T | null>((observer) => {
      const dbRef = ref(this.db, path);
      const unsubscribe = onValue(
        dbRef,
        (snapshot) => observer.next(snapshot.val() as T | null),
        (error) => observer.error(error),
      );
      // Al completar la suscripción Angular cancela el listener de Firebase
      return () => unsubscribe();
    });
  }

  async set(path: string, value: unknown): Promise<void> {
    await set(ref(this.db, path), value);
  }

  async push(path: string, value: unknown): Promise<string | null> {
    const result = await push(ref(this.db, path), value);
    return result.key;
  }
}
