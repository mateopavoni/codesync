import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  // Landing pública — redirige a /dashboard si ya está autenticado
  {
    path: '',
    loadComponent: () =>
      import('./features/home/home.component').then((m) => m.HomeComponent),
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'registro',
    loadComponent: () =>
      import('./features/auth/signup/signup.component').then((m) => m.SignupComponent),
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
  },
  {
    path: 'perfil',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/auth/profile/profile.component').then((m) => m.ProfileComponent),
  },
  {
    path: 'desafios',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/challenges/challenge-list/challenge-list.component').then(
        (m) => m.ChallengeListComponent,
      ),
  },
  {
    path: 'desafios/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/challenges/challenge-detail/challenge-detail.component').then(
        (m) => m.ChallengeDetailComponent,
      ),
  },
  {
    path: 'editor/:challengeId',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/editor/editor-page/editor-page.component').then(
        (m) => m.EditorPageComponent,
      ),
  },
  {
    path: 'editor/:challengeId/sala/:roomId',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/editor/editor-page/editor-page.component').then(
        (m) => m.EditorPageComponent,
      ),
  },
  {
    path: 'colaboracion',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/collaboration/room/room.component').then((m) => m.RoomComponent),
  },
  {
    path: '**',
    redirectTo: '',
  },
];
