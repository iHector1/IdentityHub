import { provideHttpClient } from '@angular/common/http';
import { ActivatedRouteSnapshot, RouterStateSnapshot, provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideRouter([])]
    });
  });

  it('redirects unauthenticated users to login', () => {
    const result = TestBed.runInInjectionContext(() => authGuard(
      {} as ActivatedRouteSnapshot,
      {} as RouterStateSnapshot
    ));

    expect(result?.toString()).toBe('/login');
  });

  it('allows users with a token', () => {
    localStorage.setItem('identityhub_token', 'test-token');
    const authService = TestBed.inject(AuthService);

    expect(authService.isAuthenticated()).toBeTrue();
    const result = TestBed.runInInjectionContext(() => authGuard(
      {} as ActivatedRouteSnapshot,
      {} as RouterStateSnapshot
    ));

    expect(result).toBeTrue();
  });
});
