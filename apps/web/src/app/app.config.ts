import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { initializeApp } from 'firebase/app';
import { getAuth, connectAuthEmulator } from 'firebase/auth';
import { getDatabase, connectDatabaseEmulator } from 'firebase/database';
import { environment } from '../environments/environment';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';

// Firebase se inicializa una sola vez al arrancar la aplicación.
// Todos los servicios que usen getApp() / getAuth() / getDatabase()
// encontrarán la instancia ya configurada.
initializeApp(environment.firebase);

// En modo E2E, apuntar el SDK de cliente a los emuladores locales.
// connectAuthEmulator / connectDatabaseEmulator deben llamarse UNA sola vez
// después de initializeApp y antes de cualquier operación de auth o database.
if (environment.useEmulators) {
  connectAuthEmulator(getAuth(), environment.emulatorAuthUrl, { disableWarnings: true });
  connectDatabaseEmulator(getDatabase(), '127.0.0.1', 9000);
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor])),
  ],
};
