using System.Text.Json;
using IdentityHub.AuditService.Application.Abstractions;
using IdentityHub.AuditService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace IdentityHub.AuditService.Infrastructure.Messaging;

public sealed class RabbitMqAuditConsumer(
    IConfiguration configuration,
    IAuditRepository auditRepository,
    ILogger<RabbitMqAuditConsumer> logger) : BackgroundService
{
    private const string ExchangeName = "identityhub.events";
    private const string QueueName = "audit.events";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ConsumeUntilCancelled(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Audit consumer is waiting for RabbitMQ.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private void ConsumeUntilCancelled(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(configuration["RabbitMQ:ConnectionString"] ?? "amqp://guest:guest@localhost:5672/"),
            RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(QueueName, ExchangeName, "#");
        channel.BasicQos(0, 10, false);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (_, arguments) =>
        {
            try
            {
                var integrationEvent = JsonSerializer.Deserialize<IntegrationEvent>(
                    arguments.Body.Span,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (integrationEvent is null)
                    throw new InvalidOperationException("The integration event payload was empty.");

                var auditEvent = ToAuditEvent(integrationEvent);
                auditRepository.AddAsync(auditEvent).GetAwaiter().GetResult();
                channel.BasicAck(arguments.DeliveryTag, multiple: false);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not persist an audit event; message will be discarded.");
                channel.BasicNack(arguments.DeliveryTag, multiple: false, requeue: false);
            }
        };

        channel.BasicConsume(QueueName, autoAck: false, consumer);

        try
        {
            Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Dispose the connection below.
        }
    }

    private static AuditEvent ToAuditEvent(IntegrationEvent integrationEvent)
    {
        var data = integrationEvent.Data ?? new Dictionary<string, object?>();
        var userId = GetString(data, "UserId");
        var roleId = GetString(data, "RoleId");
        var entityId = roleId ?? GetString(data, "EntityId") ?? userId;
        var entityType = roleId is not null ? "Role" : userId is not null ? "User" : null;
        var metadata = data.ToDictionary(pair => pair.Key, pair => ToObject(pair.Value));

        return new AuditEvent(
            integrationEvent.EventType,
            integrationEvent.ServiceName,
            userId,
            actorId: null,
            entityId: entityId,
            entityType: entityType,
            occurredAt: integrationEvent.OccurredAt,
            correlationId: integrationEvent.CorrelationId,
            description: $"{integrationEvent.EventType} published by {integrationEvent.ServiceName}.",
            metadata: metadata.ToDictionary(pair => pair.Key, pair => pair.Value!));
    }

    private static string? GetString(Dictionary<string, object?> data, string key) =>
        data.TryGetValue(key, out var value) ? ToObject(value)?.ToString() : null;

    private static object? ToObject(object? value)
    {
        if (value is not JsonElement jsonElement) return value;
        return jsonElement.ValueKind switch
        {
            JsonValueKind.String => jsonElement.GetString(),
            JsonValueKind.Number when jsonElement.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number when jsonElement.TryGetDouble(out var number) => number,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => jsonElement.ToString()
        };
    }
}
