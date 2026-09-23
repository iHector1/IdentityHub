import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URLS } from '../config/api.config';
import { AiAnswer, AiIndexResult } from '../models/models';

@Injectable({ providedIn: 'root' })
export class AiApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_URLS.ai}/api/ai`;

  indexKnowledge(): Observable<AiIndexResult> {
    return this.http.post<AiIndexResult>(`${this.baseUrl}/index`, {});
  }

  ask(question: string): Observable<AiAnswer> {
    return this.http.post<AiAnswer>(`${this.baseUrl}/ask`, { question });
  }
}
