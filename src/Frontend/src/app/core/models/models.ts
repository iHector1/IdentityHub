export interface LoginResponse {
  token: string;
  expiresAt: string;
}

export interface User {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface UserPayload {
  firstName: string;
  lastName: string;
  email: string;
}

export interface Role {
  id: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface RolePayload {
  name: string;
  description?: string | null;
}

export interface RoleAssignment {
  id?: string;
  roleId: string;
  userId: string;
  assignedAt: string;
  name?: string;
  description?: string | null;
  isActive?: boolean;
}

export interface AuditEvent {
  id: string;
  eventType: string;
  serviceName: string;
  userId?: string | null;
  entityId?: string | null;
  entityType?: string | null;
  occurredAt: string;
  correlationId?: string | null;
}

export interface AuditPage {
  page: number;
  pageSize: number;
  items: AuditEvent[];
}
export interface AiAnswer {
  answer: string;
  sources: string[];
  latencyMs: number;
}

export interface AiIndexResult {
  documentsIndexed: number;
  chunksIndexed: number;
  latencyMs: number;
}
