using System.Net;
using System.Net.Http.Json;
using IdentityHub.RoleService.Infrastructure.Clients;
using Microsoft.Extensions.Configuration;

namespace IdentityHub.RoleService.Tests;

public sealed class UserServiceClientTests
{
    private const string ApiKey = "test-internal-api-key";

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ShouldReturnUserAndSendApiKey()
    {
        var userId = Guid.NewGuid();
        HttpRequestMessage? capturedRequest = null;
        var handler = new TestHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { Id = userId, Email = "user@example.com", IsActive = true })
            };
        });
        var client = CreateClient(handler);

        var result = await client.GetByIdAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("user@example.com", result.Email);
        Assert.True(result.IsActive);
        Assert.NotNull(capturedRequest);
        Assert.Equal($"/internal/users/{userId}", capturedRequest.RequestUri!.AbsolutePath);
        Assert.Equal(ApiKey, capturedRequest.Headers.GetValues("X-Internal-Api-Key").Single());
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(handler);

        var result = await client.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenServiceFails_ShouldThrowHttpRequestException()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetByIdAsync(Guid.NewGuid()));
    }

    private static UserServiceClient CreateClient(TestHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://userservice") };
        return new UserServiceClient(
            httpClient,
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["InternalServices:ApiKey"] = ApiKey
                })
                .Build());
    }

    private sealed class TestHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
