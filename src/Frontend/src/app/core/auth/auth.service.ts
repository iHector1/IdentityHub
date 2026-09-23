import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, finalize, tap } from 'rxjs';
import { API_URLS } from '../config/api.config';
import { LoginResponse } from '../models/models';
import { AuthStore } from './auth.store';

export interface LoginCredentials {
  email: string;
  password: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly authStore = inject(AuthStore);

  readonly token = this.authStore.token;
  readonly loading = this.authStore.loading;
  readonly error = this.authStore.error;

  login(credentials: LoginCredentials): Observable<LoginResponse> {
    this.authStore.setLoading(true);
    this.authStore.setError(null);

    return this.http
      .post<LoginResponse>(`${API_URLS.auth}/api/auth/login`, credentials)
      .pipe(
        tap(response => this.authStore.setToken(response.token)),
        tap({ error: () => this.authStore.setError('Invalid email or password.') }),
        finalize(() => this.authStore.setLoading(false))
      );
  }

  registerCredential(userId: string, email: string, password: string): Observable<unknown> {
    return this.http.post(`${API_URLS.auth}/api/auth/register`, {
      userId,
      email,
      password
    });
  }

  getToken(): string | null {
    return this.authStore.token();
  }

  isAuthenticated(): boolean {
    return this.authStore.isAuthenticated();
  }

  logout(): void {
    this.authStore.clearToken();
  }
}
