import { computed, Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly tokenKey = 'identityhub_token';
  private readonly tokenState = signal<string | null>(localStorage.getItem(this.tokenKey));
  private readonly loadingState = signal(false);
  private readonly errorState = signal<string | null>(null);

  readonly token = this.tokenState.asReadonly();
  readonly isAuthenticated = computed(() => !!this.tokenState());
  readonly loading = this.loadingState.asReadonly();
  readonly error = this.errorState.asReadonly();

  setToken(token: string): void {
    this.tokenState.set(token);
    localStorage.setItem(this.tokenKey, token);
  }

  clearToken(): void {
    this.tokenState.set(null);
    localStorage.removeItem(this.tokenKey);
  }

  setLoading(loading: boolean): void {
    this.loadingState.set(loading);
  }

  setError(error: string | null): void {
    this.errorState.set(error);
  }
}
