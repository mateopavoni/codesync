import { Component, inject } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AsyncPipe } from '@angular/common';
import { combineLatest, filter, map, startWith } from 'rxjs';
import { AuthService } from './core/services/auth.service';

// Rutas públicas: nunca deben mostrar el shell autenticado (sidebar/topbar),
// aunque currentUser$ ya haya resuelto a un usuario (ej: onAuthStateChanged
// dispara apenas cierra el popup de OAuth, antes de que el login termine de
// navegar a /dashboard — sin este filtro el sidebar aparecía superpuesto al
// formulario de login durante esa ventana).
const PUBLIC_ROUTES = new Set(['/', '/login', '/registro']);

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, AsyncPipe],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  readonly authService = inject(AuthService);
  readonly router = inject(Router);

  readonly currentUser$ = this.authService.currentUser$;

  private readonly url$ = this.router.events.pipe(
    filter((e): e is NavigationEnd => e instanceof NavigationEnd),
    map((e) => e.urlAfterRedirects.split('?')[0]),
    startWith(this.router.url.split('?')[0]),
  );

  readonly showShell$ = combineLatest([this.currentUser$, this.url$]).pipe(
    map(([user, url]) => !!user && !PUBLIC_ROUTES.has(url)),
  );

  async onLogout(): Promise<void> {
    await this.authService.logout();
  }
}
