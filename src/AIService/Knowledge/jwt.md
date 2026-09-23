# JWT validation

AuthService creates bearer JWTs after successful credential validation. UserService, RoleService, AuditService and AIService validate the token locally using the shared `Jwt:Key`, `Jwt:Issuer` and `Jwt:Audience` configuration.

Protected API routes require the `Authorization: Bearer <token>` header. Internal service lookups use the separate internal API key instead of a user JWT.
