import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { initializeApp, getApps } from 'firebase/app';
import { environment } from '../environments/environment';
import { App } from './app';

// AuthService llama a getAuth() al construirse, que requiere una app de
// Firebase ya inicializada — normalmente lo hace app.config.ts al arrancar.
if (getApps().length === 0) {
  initializeApp(environment.firebase);
}

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideRouter([])],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the skip link', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.skip-link')).toBeTruthy();
  });
});
