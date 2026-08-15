import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, firstValueFrom } from 'rxjs';
import { filter, map, take } from 'rxjs/operators';
import {
  getAuth,
  GoogleAuthProvider,
  GithubAuthProvider,
  EmailAuthProvider,
  signInWithPopup,
  signInWithEmailAndPassword,
  createUserWithEmailAndPassword,
  reauthenticateWithCredential,
  updatePassword,
  updateProfile,
  signOut,
  onAuthStateChanged,
  type User as FirebaseUser,
} from 'firebase/auth';
import { getStorage, ref, uploadBytes, getDownloadURL } from 'firebase/storage';
import { environment } from '../../../environments/environment';

export type AuthProvider = 'google' | 'github';

const LAST_PROVIDER_KEY = 'cs_last_auth_provider';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);
  private readonly auth = getAuth();

  // undefined = auth state not yet resolved (before onAuthStateChanged fires on page load)
  // null      = resolved, not authenticated
  // FirebaseUser = resolved, authenticated
  private readonly _currentUser = new BehaviorSubject<FirebaseUser | null | undefined>(undefined);
  readonly currentUser$ = this._currentUser.asObservable();

  /** Emits true once (and only once) when the initial Firebase auth state is known. */
  readonly authReady$ = this._currentUser.pipe(
    filter((u) => u !== undefined),
    take(1),
    map(() => true as const),
  );

  constructor() {
    onAuthStateChanged(this.auth, (user) => {
      this._currentUser.next(user);
    });
  }

  get currentUser(): FirebaseUser | null {
    const v = this._currentUser.value;
    return v === undefined ? null : v;
  }

  get isAuthenticated(): boolean {
    return this._currentUser.value !== null && this._currentUser.value !== undefined;
  }

  /** false para usuarios que entraron con Google/GitHub — no tienen contraseña que cambiar. */
  get isPasswordUser(): boolean {
    return this.currentUser?.providerData.some((p) => p.providerId === 'password') ?? false;
  }

  async getIdToken(): Promise<string | null> {
    const user = this.auth.currentUser;
    if (!user) return null;
    return user.getIdToken();
  }

  async loginWithGoogle(): Promise<void> {
    const provider = new GoogleAuthProvider();
    await signInWithPopup(this.auth, provider);
    await this.syncProfile();
    localStorage.setItem(LAST_PROVIDER_KEY, 'google');
    await this.router.navigate(['/dashboard']);
  }

  async loginWithGithub(): Promise<void> {
    const provider = new GithubAuthProvider();
    await signInWithPopup(this.auth, provider);
    await this.syncProfile();
    localStorage.setItem(LAST_PROVIDER_KEY, 'github');
    await this.router.navigate(['/dashboard']);
  }

  /** Último proveedor OAuth usado con éxito, para mostrar el badge "Usado recientemente". */
  getLastAuthProvider(): AuthProvider | null {
    const value = localStorage.getItem(LAST_PROVIDER_KEY);
    return value === 'google' || value === 'github' ? value : null;
  }

  async loginWithEmail(email: string, password: string): Promise<void> {
    await signInWithEmailAndPassword(this.auth, email, password);
    await this.syncProfile();
    await this.router.navigate(['/dashboard']);
  }

  async signupWithEmail(email: string, password: string): Promise<void> {
    await createUserWithEmailAndPassword(this.auth, email, password);
    await this.syncProfile();
    await this.router.navigate(['/dashboard']);
  }

  /** Crea o actualiza el documento de perfil en Firestore a partir de los claims de Firebase Auth. */
  private async syncProfile(): Promise<void> {
    const user = this.auth.currentUser;
    if (!user) return;
    await firstValueFrom(
      this.http.post<void>(`${environment.apiUrl}/users/me`, {
        displayName: user.displayName ?? user.email ?? 'Usuario',
        email: user.email ?? '',
        photoUrl: user.photoURL,
      }),
    );
  }

  /** Solo para usuarios de email/password — requiere reautenticarse por seguridad. */
  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    const user = this.auth.currentUser;
    if (!user?.email) throw new Error('no-current-user');
    const credential = EmailAuthProvider.credential(user.email, currentPassword);
    await reauthenticateWithCredential(user, credential);
    await updatePassword(user, newPassword);
  }

  /** Sube el archivo a Storage, actualiza el perfil de Firebase Auth y sincroniza el backend. */
  async changeAvatar(file: File): Promise<void> {
    const user = this.auth.currentUser;
    if (!user) throw new Error('no-current-user');

    const storage = getStorage();
    const avatarRef = ref(storage, `avatars/${user.uid}`);
    await uploadBytes(avatarRef, file);
    const photoUrl = await getDownloadURL(avatarRef);

    await updateProfile(user, { photoURL: photoUrl });
    await firstValueFrom(
      this.http.post<void>(`${environment.apiUrl}/users/me`, {
        displayName: user.displayName ?? user.email ?? 'Usuario',
        email: user.email ?? '',
        photoUrl,
      }),
    );
  }

  async logout(): Promise<void> {
    await signOut(this.auth);
    await this.router.navigate(['/login']);
  }
}
