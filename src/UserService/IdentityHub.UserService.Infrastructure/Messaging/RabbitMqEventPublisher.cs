using System.Text;
using System.Text.Json;
using IdentityHub.UserService.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace IdentityHub.UserService.Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher(
    IConfiguration configuration,
    ILogger<RabbitMqEventPublisher> logger) : IIntegrationEventPublisher
{
    public Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(configuration["RabbitMQ:ConnectionString"] ?? "amqp://guest:guest@localhost:5672/"),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(1)
            };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.ExchangeDeclare("identityhub.events", ExchangeType.Topic, durable: true, autoDelete: false);
            var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(integrationEvent));
            channel.BasicPublish("identityhub.events", integrationEvent.EventType, null, payload);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not publish {EventType}; primary operation will continue.", integrationEvent.EventType);
        }

        return Task.CompletedTask;
    }
}
