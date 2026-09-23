import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { LayoutComponent } from './layout/layout.component';
import { LoginComponent } from './login/login.component';
import { UsersComponent } from './users/users.component';
import { RolesComponent } from './roles/roles.component';
import { AuditComponent } from './audit/audit.component';
import { AiComponent } from './ai/ai.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: 'users', component: UsersComponent },
      { path: 'roles', component: RolesComponent },
      { path: 'audit', component: AuditComponent },
      { path: 'ai', component: AiComponent },
      { path: '', pathMatch: 'full', redirectTo: 'users' }
    ]
  },
  { path: '**', redirectTo: 'users' }
];
