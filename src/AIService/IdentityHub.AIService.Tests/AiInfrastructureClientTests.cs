using System.Net;
using System.Text;
using IdentityHub.AIService.Application.Models;
using IdentityHub.AIService.Infrastructure.Clients;
using IdentityHub.AIService.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IdentityHub.AIService.Tests;

public sealed class AiInfrastructureClientTests
{
    [Fact]
    public async Task OpenAiChatService_ShouldReadOutputTextAndUsage()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(
            "{\"output_text\":\"Hello\",\"usage\":{\"input_tokens\":10,\"output_tokens\":5,\"total_tokens\":15}}"));
        var service = CreateChatService(handler);

        var result = await service.GenerateAsync("system", "user");

        Assert.Equal("Hello", result.Text);
        Assert.Equal(10, result.InputTokens);
        Assert.Equal(5, result.OutputTokens);
        Assert.Equal(15, result.TotalTokens);
    }

    [Fact]
    public async Task OpenAiChatService_ShouldReadNestedOutputText()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(
            "{\"output\":[{\"content\":[{\"type\":\"output_text\",\"text\":\"Nested\"}]}]}"));
        var service = CreateChatService(handler);

        var result = await service.GenerateAsync("system", "user");

        Assert.Equal("Nested", result.Text);
        Assert.Null(result.TotalTokens);
    }

    [Fact]
    public async Task OpenAiChatService_WhenApiFails_ShouldThrow()
    {
        var service = CreateChatService(new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadGateway)));

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GenerateAsync("system", "user"));
    }

    [Fact]
    public async Task OpenAiChatService_WhenApiKeyIsMissing_ShouldThrow()
    {
        var service = new OpenAiChatService(
            new HttpClient(new FakeHttpMessageHandler(_ => JsonResponse("{}"))),
            Options.Create(new OpenAiOptions()),
            NullLogger<OpenAiChatService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateAsync("system", "user"));
    }

    [Fact]
    public async Task OpenAiChatService_WhenTextIsMissing_ShouldThrow()
    {
        var service = CreateChatService(new FakeHttpMessageHandler(_ => JsonResponse("{\"output\":[]}")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateAsync("system", "user"));
    }

    [Fact]
    public async Task OpenAiEmbeddingService_ShouldOrderResultsByIndex()
    {
        var service = CreateEmbeddingService(new FakeHttpMessageHandler(_ => JsonResponse(
            "{\"data\":[{\"index\":1,\"embedding\":[0.2,0.3]},{\"index\":0,\"embedding\":[0.1,0.2]}]}")));

        var result = await service.CreateEmbeddingsAsync(new[] { "first", "second" });

        Assert.Equal(new[] { 0.1f, 0.2f }, result[0]);
        Assert.Equal(new[] { 0.2f, 0.3f }, result[1]);
    }

    [Fact]
    public async Task OpenAiEmbeddingService_WithEmptyInputs_ShouldNotCallProvider()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new InvalidOperationException("Should not call HTTP."));
        var service = CreateEmbeddingService(handler);

        var result = await service.CreateEmbeddingsAsync(Array.Empty<string>());

        Assert.Empty(result);
    }

    [Fact]
    public async Task OpenAiEmbeddingService_WhenApiFails_ShouldThrow()
    {
        var service = CreateEmbeddingService(new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        await Assert.ThrowsAsync<HttpRequestException>(() => service.CreateEmbeddingsAsync(new[] { "input" }));
    }

    [Fact]
    public async Task OpenAiEmbeddingService_WhenApiKeyIsMissing_ShouldThrow()
    {
        var service = new OpenAiEmbeddingService(
            new HttpClient(new FakeHttpMessageHandler(_ => JsonResponse("{}"))),
            Options.Create(new OpenAiOptions()),
            NullLogger<OpenAiEmbeddingService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateEmbeddingsAsync(new[] { "input" }));
    }

    [Fact]
    public async Task UserServiceClient_ShouldSendApiKeyAndMapUser()
    {
        var userId = Guid.NewGuid();
        HttpRequestMessage? capturedRequest = null;
        var client = CreateUserServiceClient(new FakeHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return JsonResponse($"{{\"id\":\"{userId}\",\"email\":\"user@example.com\",\"isActive\":true}}");
        }));

        var result = await client.GetByIdAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("user@example.com", result.Email);
        Assert.True(result.IsActive);
        Assert.Equal($"/internal/users/{userId}", capturedRequest!.RequestUri!.AbsolutePath);
        Assert.Equal("test-api-key", capturedRequest.Headers.GetValues("X-Internal-Api-Key").Single());
    }

    [Fact]
    public async Task UserServiceClient_WhenUserDoesNotExist_ShouldReturnNull()
    {
        var client = CreateUserServiceClient(new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)));

        Assert.Null(await client.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UserServiceClient_WhenServiceFails_ShouldThrow()
    {
        var client = CreateUserServiceClient(new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public void UserServiceClient_WhenApiKeyIsMissing_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() => new UserServiceClient(
            new HttpClient(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))),
            new ConfigurationBuilder().Build()));
    }

    private static OpenAiChatService CreateChatService(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.test/v1/") },
            Options.Create(new OpenAiOptions { ApiKey = "test-key" }),
            NullLogger<OpenAiChatService>.Instance);

    private static OpenAiEmbeddingService CreateEmbeddingService(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.test/v1/") },
            Options.Create(new OpenAiOptions { ApiKey = "test-key" }),
            NullLogger<OpenAiEmbeddingService>.Instance);

    private static UserServiceClient CreateUserServiceClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("http://userservice") },
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["InternalServices:ApiKey"] = "test-api-key"
                })
                .Build());

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class FakeHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
