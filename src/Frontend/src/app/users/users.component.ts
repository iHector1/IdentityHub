import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../core/auth/auth.service';
import { User, UserPayload } from '../core/models/models';
import { UsersApiService } from '../core/services/users-api.service';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './users.component.html'
})
export class UsersComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly usersApi = inject(UsersApiService);
  private readonly authService = inject(AuthService);

  readonly userForm = this.formBuilder.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]]
  });

  readonly accessForm = this.formBuilder.nonNullable.group({
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  users: User[] = [];
  loading = false;
  saving = false;
  errorMessage = '';
  successMessage = '';
  editingUserId: string | null = null;
  accessUser: User | null = null;
  accessLoading = false;
  accessErrorMessage = '';
  accessSuccessMessage = '';

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.errorMessage = '';

    this.usersApi.getAll().subscribe({
      next: users => {
        this.users = users;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Could not load users.';
      }
    });
  }

  submit(): void {
    if (this.userForm.invalid) {
      this.userForm.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';
    const payload: UserPayload = this.userForm.getRawValue();
    const request = this.editingUserId
      ? this.usersApi.update(this.editingUserId, payload)
      : this.usersApi.create(payload);

    request.subscribe({
      next: () => {
        this.saving = false;
        this.successMessage = this.editingUserId ? 'User updated.' : 'User created.';
        this.cancelEdit();
        this.loadUsers();
      },
      error: () => {
        this.saving = false;
        this.errorMessage = 'Could not save the user.';
      }
    });
  }

  edit(user: User): void {
    this.editingUserId = user.id;
    this.userForm.setValue({
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email
    });
    this.successMessage = '';
  }

  cancelEdit(): void {
    this.editingUserId = null;
    this.userForm.reset({ firstName: '', lastName: '', email: '' });
  }

  openAccess(user: User): void {
    this.accessUser = user;
    this.accessForm.reset({ password: '' });
    this.accessErrorMessage = '';
    this.accessSuccessMessage = '';
  }

  closeAccess(): void {
    this.accessUser = null;
    this.accessForm.reset({ password: '' });
  }

  registerAccess(): void {
    if (!this.accessUser) {
      return;
    }

    if (this.accessForm.invalid) {
      this.accessForm.markAllAsTouched();
      return;
    }

    this.accessLoading = true;
    this.accessErrorMessage = '';
    this.accessSuccessMessage = '';
    const { password } = this.accessForm.getRawValue();

    this.authService.registerCredential(this.accessUser.id, this.accessUser.email, password).subscribe({
      next: () => {
        this.accessLoading = false;
        this.accessSuccessMessage = 'Access created.';
        this.accessForm.reset({ password: '' });
      },
      error: (error: HttpErrorResponse) => {
        this.accessLoading = false;
        this.accessErrorMessage = error.status === 409
          ? 'This user already has access. Password changes are not available yet.'
          : 'Could not create access.';
      }
    });
  }

  toggleStatus(user: User): void {
    const nextStatus = !user.isActive;
    const action = nextStatus ? 'activate' : 'deactivate';
    const displayAction = nextStatus ? 'Activate' : 'Deactivate';
    if (!window.confirm(`${displayAction} ${user.firstName} ${user.lastName}?`)) {
      return;
    }

    this.usersApi.setStatus(user.id, nextStatus).subscribe({
      next: () => {
        this.successMessage = nextStatus ? 'User activated.' : 'User deactivated.';
        this.loadUsers();
      },
      error: () => this.errorMessage = `Could not ${action} the user.`
    });
  }
}
