import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
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
  private readonly router = inject(Router);

  ngOnInit(): void {
    // Espera a que Firebase resuelva el estado de auth (currentUser$ arranca en
    // `undefined`) antes de decidir si redirige — si se usa take(1) directo sobre
    // currentUser$, esa primera emisión suele ser `undefined` y nunca redirige a
    // un usuario ya logueado; con authReady$ el chequeo es determinístico.
    this.authService.authReady$.subscribe(() => {
      if (this.authService.currentUser) {
        void this.router.navigate(['/dashboard'], { replaceUrl: true });
      }
    });
  }
}
