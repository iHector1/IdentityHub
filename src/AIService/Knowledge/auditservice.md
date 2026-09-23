# AuditService

AuditService stores audit events in MongoDB. It consumes integration events from RabbitMQ and persists the event type, source service, user or entity identifiers, timestamp, correlation ID and metadata.

The protected API supports paged audit queries and filters by event ID, user ID and event type. AuditService does not own user, credential or role data.
