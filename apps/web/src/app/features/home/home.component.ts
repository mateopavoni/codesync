import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { take } from 'rxjs/operators';
import { AuthService } from '../../core/services/auth.service';
import { ShaderBackgroundComponent } from '../../shared/components/shader-background/shader-background.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, ShaderBackgroundComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly mobileNavOpen = signal(false);

  ngOnInit(): void {
    // Redirige a dashboard si el usuario ya está autenticado
    this.authService.currentUser$.pipe(take(1)).subscribe((user) => {
      if (user) {
        void this.router.navigate(['/dashboard'], { replaceUrl: true });
      }
    });
  }

  toggleMobileNav(): void {
    this.mobileNavOpen.update((open) => !open);
  }

  closeMobileNav(): void {
    this.mobileNavOpen.set(false);
  }
}
