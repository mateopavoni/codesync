import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ShaderBackgroundComponent } from '../../shared/components/shader-background/shader-background.component';
import { PublicHeaderComponent } from '../../shared/components/public-header/public-header.component';
import { PublicFooterComponent } from '../../shared/components/public-footer/public-footer.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, ShaderBackgroundComponent, PublicHeaderComponent, PublicFooterComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent implements OnInit {
  private readonly authService = inject(AuthService);

  readonly isLoggedIn = signal(false);

  ngOnInit(): void {
    // Espera a que Firebase resuelva el estado de auth (currentUser$ arranca en
    // `undefined`) antes de decidir qué CTA mostrar — si se usa take(1) directo
    // sobre currentUser$, esa primera emisión suele ser `undefined`; con
    // authReady$ el chequeo es determinístico. Ya no redirige solo: el home
    // se muestra siempre y el usuario logueado decide si entra al dashboard.
    this.authService.authReady$.subscribe(() => {
      this.isLoggedIn.set(!!this.authService.currentUser);
    });
  }
}
