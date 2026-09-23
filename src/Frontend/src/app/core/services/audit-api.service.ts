import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URLS } from '../config/api.config';
import { AuditPage } from '../models/models';

@Injectable({ providedIn: 'root' })
export class AuditApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_URLS.audit}/api/audit`;

  getPage(page: number, pageSize: number): Observable<AuditPage> {
    return this.http.get<AuditPage>(`${this.baseUrl}?page=${page}&pageSize=${pageSize}`);
  }
}
