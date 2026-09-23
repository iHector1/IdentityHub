import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';
import { AiApiService } from '../core/services/ai-api.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly aiApi = inject(AiApiService);
  private readonly router = inject(Router);

  readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  loading = false;
  errorMessage = '';

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.authService.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.loading = false;
        void this.router.navigate(['/users']);

        // The login response has already been saved by AuthService.tap().
        // The JWT interceptor adds it to the protected indexing request.
        this.aiApi.indexKnowledge().subscribe({
          error: error => console.error('AI knowledge indexing failed after login.', error)
        });
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Invalid email or password.';
      }
    });
  }
}
