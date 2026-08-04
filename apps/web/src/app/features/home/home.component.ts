import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { take } from 'rxjs/operators';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
})
export class HomeComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    // Redirige a dashboard si el usuario ya está autenticado
    this.authService.currentUser$.pipe(take(1)).subscribe((user) => {
      if (user) {
        void this.router.navigate(['/dashboard'], { replaceUrl: true });
      }
    });
  }
}
