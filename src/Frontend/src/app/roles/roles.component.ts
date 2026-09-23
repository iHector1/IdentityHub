import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Role, RoleAssignment, RolePayload, User } from '../core/models/models';
import { RolesApiService } from '../core/services/roles-api.service';
import { UsersApiService } from '../core/services/users-api.service';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './roles.component.html'
})
export class RolesComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly rolesApi = inject(RolesApiService);
  private readonly usersApi = inject(UsersApiService);

  readonly roleForm = this.formBuilder.nonNullable.group({
    name: ['', Validators.required],
    description: ['']
  });

  readonly assignmentForm = this.formBuilder.nonNullable.group({
    userId: ['', Validators.required],
    roleId: ['', Validators.required]
  });

  roles: Role[] = [];
  users: User[] = [];
  assignedRoles: RoleAssignment[] = [];
  loading = false;
  saving = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.loadRoles();
    this.loadUsers();
  }

  loadRoles(): void {
    this.loading = true;
    this.rolesApi.getAll().subscribe({
      next: roles => {
        this.roles = roles;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Could not load roles.';
      }
    });
  }

  loadUsers(): void {
    this.usersApi.getAll().subscribe({
      next: users => this.users = users,
      error: () => this.errorMessage = 'Could not load users for assignment.'
    });
  }

  createRole(): void {
    if (this.roleForm.invalid) {
      this.roleForm.markAllAsTouched();
      return;
    }

    this.saveRole(this.roleForm.getRawValue());
  }

  private saveRole(payload: RolePayload): void {
    this.saving = true;
    this.errorMessage = '';
    this.rolesApi.create(payload).subscribe({
      next: () => {
        this.saving = false;
        this.successMessage = 'Role created.';
        this.roleForm.reset({ name: '', description: '' });
        this.loadRoles();
      },
      error: () => {
        this.saving = false;
        this.errorMessage = 'Could not create the role.';
      }
    });
  }

  onUserChange(): void {
    const userId = this.assignmentForm.controls.userId.value;
    this.assignedRoles = [];
    if (!userId) {
      return;
    }

    this.rolesApi.getByUser(userId).subscribe({
      next: assignments => this.assignedRoles = assignments,
      error: () => this.errorMessage = 'Could not load assigned roles.'
    });
  }

  assignRole(): void {
    if (this.assignmentForm.invalid) {
      this.assignmentForm.markAllAsTouched();
      return;
    }

    const { userId, roleId } = this.assignmentForm.getRawValue();
    this.saving = true;
    this.errorMessage = '';
    this.rolesApi.assign(roleId, userId).subscribe({
      next: () => {
        this.saving = false;
        this.successMessage = 'Role assigned.';
        this.onUserChange();
      },
      error: () => {
        this.saving = false;
        this.errorMessage = 'Could not assign the role.';
      }
    });
  }

  removeRole(assignment: RoleAssignment): void {
    const userId = this.assignmentForm.controls.userId.value;
    const roleId = assignment.roleId || assignment.id;
    if (!roleId || !userId) {
      return;
    }

    this.rolesApi.remove(roleId, userId).subscribe({
      next: () => {
        this.successMessage = 'Role removed.';
        this.onUserChange();
      },
      error: () => this.errorMessage = 'Could not remove the role.'
    });
  }
}
