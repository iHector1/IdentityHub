using IdentityHub.AuthService.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentityHub.AuthService.Tests;

public sealed class RabbitMqEventPublisherTests
{
    [Fact]
    public async Task PublishAsync_WhenRabbitMqIsUnavailable_ShouldNotBreakTheOperation()
    {
        var publisher = new RabbitMqEventPublisher(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RabbitMQ:ConnectionString"] = "not-a-valid-uri"
                })
                .Build(),
            NullLogger<RabbitMqEventPublisher>.Instance);

        await publisher.PublishAsync(new IdentityHub.AuthService.Application.Abstractions.IntegrationEvent(
            Guid.NewGuid(),
            "Test.Event",
            "AuthService",
            DateTime.UtcNow,
            null,
            null));
    }
}
