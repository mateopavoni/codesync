import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-public-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './public-header.component.html',
  styleUrl: './public-header.component.css',
})
export class PublicHeaderComponent {
  private readonly authService = inject(AuthService);

  readonly mobileNavOpen = signal(false);
  // Resuelto acá (no por @Input) porque este header lo comparten home,
  // funcionalidades y acerca-de: cada página que lo use ve el CTA correcto sin plumbing extra.
  readonly isLoggedIn = signal(false);

  constructor() {
    this.authService.authReady$.subscribe(() => {
      this.isLoggedIn.set(!!this.authService.currentUser);
    });
  }

  toggleMobileNav(): void {
    this.mobileNavOpen.update((open) => !open);
  }

  closeMobileNav(): void {
    this.mobileNavOpen.set(false);
  }
}
