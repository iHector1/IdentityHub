# Databases

SQL Server is used independently by UserService, AuthService and RoleService, with a separate database per service.

MongoDB is used by AuditService for the `identityhub_audit` database and its audit event collection.

Qdrant is used only by AIService for the `identityhub_knowledge` vector collection. It stores the embedding vector and payload metadata for each indexed Markdown chunk.
