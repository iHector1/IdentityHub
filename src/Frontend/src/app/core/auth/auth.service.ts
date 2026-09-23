import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { API_URLS } from '../config/api.config';
import { LoginResponse } from '../models/models';

export interface LoginCredentials {
  email: string;
  password: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenKey = 'identityhub_token';

  login(credentials: LoginCredentials): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${API_URLS.auth}/api/auth/login`, credentials)
      .pipe(tap(response => localStorage.setItem(this.tokenKey, response.token)));
  }

  registerCredential(userId: string, email: string, password: string): Observable<unknown> {
    return this.http.post(`${API_URLS.auth}/api/auth/register`, {
      userId,
      email,
      password
    });
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
  }
}
