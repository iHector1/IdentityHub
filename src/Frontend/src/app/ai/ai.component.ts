import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AiApiService } from '../core/services/ai-api.service';
import { AiUsage } from '../core/models/models';

@Component({
  selector: 'app-ai',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai.component.html'
})
export class AiComponent {
  private readonly aiApi = inject(AiApiService);

  question = '';
  answer = '';
  sources: string[] = [];
  latencyMs: number | null = null;
  quality: 'Grounded' | 'NoContext' | null = null;
  usage: AiUsage | null = null;
  loading = false;
  errorMessage = '';

  ask(): void {
    if (!this.question.trim() || this.loading) return;

    this.loading = true;
    this.answer = '';
    this.sources = [];
    this.latencyMs = null;
    this.quality = null;
    this.usage = null;
    this.errorMessage = '';
    this.aiApi.ask(this.question.trim()).subscribe({
      next: response => {
        this.answer = response.answer;
        this.sources = response.sources;
        this.latencyMs = response.latencyMs;
        this.quality = response.quality;
        this.usage = response.usage;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Could not get an answer from AIService.';
        this.loading = false;
      }
    });
  }
}
