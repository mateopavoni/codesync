import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { switchMap, take, map } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

export const authGuard = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // authReady$ emits true exactly once when onAuthStateChanged has fired for the
  // first time (resolving the initial undefined state of the BehaviorSubject).
  // This prevents redirecting to /login while Firebase restores a persisted session
  // after a full page reload (e.g., Playwright page.goto() or a hard refresh).
  return authService.authReady$.pipe(
    take(1),
    switchMap(() =>
      authService.currentUser$.pipe(
        take(1),
        map((user) => (user ? true : router.createUrlTree(['/login']))),
      ),
    ),
  );
};
