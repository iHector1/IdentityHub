namespace IdentityHub.AuditService.Application.Abstractions;

public sealed record IntegrationEvent(
    Guid EventId,
    string EventType,
    string ServiceName,
    DateTime OccurredAt,
    string? CorrelationId,
    Dictionary<string, object?>? Data);
