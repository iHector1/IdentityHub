namespace IdentityHub.AuthService.Application.Abstractions;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}

public sealed record IntegrationEvent(
    Guid EventId,
    string EventType,
    string ServiceName,
    DateTime OccurredAt,
    string? CorrelationId,
    Dictionary<string, object?>? Data);
