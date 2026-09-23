import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { API_URLS } from '../config/api.config';
import { AuthService } from './auth.service';

export const jwtInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const token = authService.getToken();
  const isLoginRequest = request.url === `${API_URLS.auth}/api/auth/login`;
  const authorizedRequest = token && !isLoginRequest
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(authorizedRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        authService.logout();
        void router.navigate(['/login']);
      }

      return throwError(() => error);
    })
  );
};
