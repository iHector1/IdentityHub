# IdentityHub architecture

IdentityHub is a .NET 9 solution organized as independent services: UserService, AuthService, RoleService, AuditService and AIService. The Angular frontend calls the protected HTTP APIs with a bearer JWT.

Each backend service keeps its own application boundaries and persistence concerns. Authentication is issued by AuthService and the same JWT issuer, audience and signing key are configured in the protected services.

AIService is an additive RAG service. It reads the Markdown files in its `Knowledge` folder, creates embeddings, stores them in Qdrant and uses the retrieved text as context for an OpenAI response.
