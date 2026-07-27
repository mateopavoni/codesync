import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap } from 'rxjs';
import { AuthService } from '../services/auth.service';

// Adjunta el ID token de Firebase como Bearer en cada llamada a la API del backend.
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  // Solo intercepta llamadas a nuestra API (no a recursos externos)
  if (!req.url.includes('/api/')) {
    return next(req);
  }

  return from(authService.getIdToken()).pipe(
    switchMap((token) => {
      if (!token) return next(req);
      return next(
        req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }),
      );
    }),
  );
};
