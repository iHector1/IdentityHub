using System.Text;
using System.Text.Json;
using IdentityHub.RoleService.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace IdentityHub.RoleService.Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher(
    IConfiguration configuration,
    ILogger<RabbitMqEventPublisher> logger) : IIntegrationEventPublisher
{
    private const string ExchangeName = "identityhub.events";

    public Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(configuration["RabbitMQ:ConnectionString"] ?? "amqp://guest:guest@localhost:5672/")
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);

            var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(integrationEvent));
            channel.BasicPublish(ExchangeName, integrationEvent.EventType, basicProperties: null, payload);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not publish integration event {EventType}; primary operation will continue.", integrationEvent.EventType);
        }

        return Task.CompletedTask;
    }
}
