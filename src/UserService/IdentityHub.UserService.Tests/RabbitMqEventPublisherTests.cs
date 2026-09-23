using IdentityHub.UserService.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentityHub.UserService.Tests;

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

        await publisher.PublishAsync(new IdentityHub.UserService.Application.Abstractions.IntegrationEvent(
            Guid.NewGuid(),
            "Test.Event",
            "UserService",
            DateTime.UtcNow,
            null,
            null));
    }
}
