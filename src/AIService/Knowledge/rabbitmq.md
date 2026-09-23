# RabbitMQ

IdentityHub uses RabbitMQ for integration events between services. Services publish to the durable `identityhub.events` topic exchange.

AuditService binds its durable `audit.events` queue to that exchange with the `#` routing key, so it can consume the integration events published by the other services and store them in MongoDB.
