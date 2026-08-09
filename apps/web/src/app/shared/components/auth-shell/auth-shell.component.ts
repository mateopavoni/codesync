import { Component } from '@angular/core';
import { ShaderBackgroundComponent } from '../shader-background/shader-background.component';

// Shell compartido de login/signup: fondo shader sutil ("veil") + card glass
// encima. Extraído porque login/signup tenían .auth-page/.auth-card
// duplicadas casi byte a byte.
@Component({
  selector: 'app-auth-shell',
  standalone: true,
  imports: [ShaderBackgroundComponent],
  templateUrl: './auth-shell.component.html',
  styleUrl: './auth-shell.component.css',
})
export class AuthShellComponent {}
