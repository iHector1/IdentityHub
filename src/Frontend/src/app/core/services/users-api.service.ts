import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URLS } from '../config/api.config';
import { User, UserPayload } from '../models/models';

@Injectable({ providedIn: 'root' })
export class UsersApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_URLS.users}/api/users`;

  getAll(): Observable<User[]> {
    return this.http.get<User[]>(this.baseUrl);
  }

  create(payload: UserPayload): Observable<User> {
    return this.http.post<User>(this.baseUrl, payload);
  }

  update(id: string, payload: UserPayload): Observable<User> {
    return this.http.put<User>(`${this.baseUrl}/${id}`, payload);
  }

  setStatus(id: string, isActive: boolean): Observable<User> {
    return this.http.put<User>(`${this.baseUrl}/${id}/status`, { isActive });
  }
}
