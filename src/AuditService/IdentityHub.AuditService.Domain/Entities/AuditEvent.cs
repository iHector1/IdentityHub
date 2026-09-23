namespace IdentityHub.AuditService.Domain.Entities;

public sealed class AuditEvent
{
    public string Id { get; private set; }
    public string EventType { get; private set; }
    public string ServiceName { get; private set; }
    public string? UserId { get; private set; }
    public string? ActorId { get; private set; }
    public string? EntityId { get; private set; }
    public string? EntityType { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? Description { get; private set; }
    public Dictionary<string, object>? Metadata { get; private set; }

    private AuditEvent()
    {
        Id = string.Empty;
        EventType = string.Empty;
        ServiceName = string.Empty;
    }

    public AuditEvent(
        string eventType,
        string serviceName,
        string? userId = null,
        string? actorId = null,
        string? entityId = null,
        string? entityType = null,
        DateTime? occurredAt = null,
        string? correlationId = null,
        string? ipAddress = null,
        string? description = null,
        Dictionary<string, object>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(eventType)) throw new ArgumentException("EventType is required.", nameof(eventType));
        if (string.IsNullOrWhiteSpace(serviceName)) throw new ArgumentException("ServiceName is required.", nameof(serviceName));

        Id = Guid.NewGuid().ToString();
        EventType = eventType.Trim();
        ServiceName = serviceName.Trim();
        UserId = userId;
        ActorId = actorId;
        EntityId = entityId;
        EntityType = entityType;
        OccurredAt = occurredAt ?? DateTime.UtcNow;
        CorrelationId = correlationId;
        IpAddress = ipAddress;
        Description = description;
        Metadata = metadata;
    }
}
