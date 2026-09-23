using System.Net;
using System.Text;
using IdentityHub.AIService.Application.Models;
using IdentityHub.AIService.Infrastructure.Knowledge;
using IdentityHub.AIService.Infrastructure.Options;
using IdentityHub.AIService.Infrastructure.VectorStore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IdentityHub.AIService.Tests;

public sealed class AiInfrastructureStorageTests
{
    [Fact]
    public async Task FileKnowledgeDocumentReader_ShouldReadMarkdownAndTitle()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "Knowledge"));
            await File.WriteAllTextAsync(Path.Combine(root, "Knowledge", "guide.md"), "# Guide\nContent");
            var reader = new FileKnowledgeDocumentReader(new TestHostEnvironment(root));

            var result = await reader.ReadAsync();

            var document = Assert.Single(result);
            Assert.Equal("guide.md", document.Source);
            Assert.Equal("Guide", document.Title);
            Assert.Contains("Content", document.Text);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task FileKnowledgeDocumentReader_WhenKnowledgeDirectoryDoesNotExist_ShouldReturnEmpty()
    {
        var reader = new FileKnowledgeDocumentReader(new TestHostEnvironment(
            Path.Combine(Path.GetTempPath(), $"identityhub-missing-{Guid.NewGuid():N}")));

        Assert.Empty(await reader.ReadAsync());
    }

    [Fact]
    public async Task QdrantVectorStore_WhenCollectionExists_ShouldNotCreateIt()
    {
        var putCalled = false;
        var handler = new FakeHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Put)
                putCalled = true;
            return JsonResponse(HttpStatusCode.OK, "{}");
        });
        var store = CreateStore(handler);

        await store.EnsureCollectionAsync(3);

        Assert.False(putCalled);
    }

    [Fact]
    public async Task QdrantVectorStore_WhenCollectionIsMissing_ShouldCreateIt()
    {
        var methods = new List<HttpMethod>();
        var handler = new FakeHttpMessageHandler(request =>
        {
            methods.Add(request.Method);
            return request.Method == HttpMethod.Get
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : JsonResponse(HttpStatusCode.OK, "{}");
        });
        var store = CreateStore(handler);

        await store.EnsureCollectionAsync(3);

        Assert.Equal(new[] { HttpMethod.Get, HttpMethod.Put }, methods);
    }

    [Fact]
    public async Task QdrantVectorStore_ShouldReadSearchPointsAndNotFound()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse(
            HttpStatusCode.OK,
            "{\"result\":{\"points\":[{\"score\":0.9,\"payload\":{\"source\":\"guide.md\",\"title\":\"Guide\",\"chunkIndex\":0,\"text\":\"content\"}}]}}"));
        var store = CreateStore(handler);

        var result = await store.SearchAsync(new[] { 0.1f }, 5);

        var chunk = Assert.Single(result);
        Assert.Equal("guide.md", chunk.Source);
        Assert.Equal(0.9, chunk.Score);
    }

    [Fact]
    public async Task QdrantVectorStore_WhenSearchReturnsNotFound_ShouldReturnEmpty()
    {
        var store = CreateStore(new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)));

        Assert.Empty(await store.SearchAsync(new[] { 0.1f }, 5));
    }

    [Fact]
    public async Task QdrantVectorStore_WhenUpsertHasNoDocuments_ShouldNotCallHttp()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new InvalidOperationException("Should not call HTTP."));
        var store = CreateStore(handler);

        await store.UpsertAsync(Array.Empty<VectorDocument>());
    }

    private static QdrantVectorStore CreateStore(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("http://qdrant:6333/") },
            Options.Create(new QdrantOptions { CollectionName = "test_collection" }),
            NullLogger<QdrantVectorStore>.Instance);

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"identityhub-ai-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) => new(statusCode)
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

    private sealed class TestHostEnvironment(string root) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "IdentityHub.AIService.Tests";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
