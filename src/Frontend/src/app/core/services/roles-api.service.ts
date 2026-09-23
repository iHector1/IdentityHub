import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URLS } from '../config/api.config';
import { Role, RoleAssignment, RolePayload } from '../models/models';

@Injectable({ providedIn: 'root' })
export class RolesApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_URLS.roles}/api/roles`;

  getAll(): Observable<Role[]> {
    return this.http.get<Role[]>(this.baseUrl);
  }

  create(payload: RolePayload): Observable<Role> {
    return this.http.post<Role>(this.baseUrl, payload);
  }

  assign(roleId: string, userId: string): Observable<RoleAssignment> {
    return this.http.post<RoleAssignment>(`${this.baseUrl}/${roleId}/users/${userId}`, {});
  }

  getByUser(userId: string): Observable<RoleAssignment[]> {
    return this.http.get<RoleAssignment[]>(`${this.baseUrl}/users/${userId}`);
  }

  remove(roleId: string, userId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${roleId}/users/${userId}`);
  }
}
