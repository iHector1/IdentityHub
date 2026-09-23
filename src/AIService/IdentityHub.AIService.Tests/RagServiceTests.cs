using IdentityHub.AIService.Application.Abstractions;
using IdentityHub.AIService.Application.Models;
using IdentityHub.AIService.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentityHub.AIService.Tests;

public sealed class RagServiceTests
{
    [Fact]
    public async Task AskAsync_WithEmptyQuestion_ShouldFail()
    {
        var service = CreateService(new[] { new RetrievedChunk("userservice.md", "UserService", 0, "users", 0.9) });

        await Assert.ThrowsAsync<ArgumentException>(() => service.AskAsync(" "));
    }

    [Fact]
    public async Task AskAsync_ShouldRetrieveContextBeforeGenerating()
    {
        var chat = new FakeChatService();
        var service = CreateService(
            new[] { new RetrievedChunk("roleservice.md", "RoleService", 0, "Roles are assigned after checking the user.", 0.91) },
            chat);

        await service.AskAsync("How does role assignment work?");

        Assert.NotNull(chat.UserPrompt);
        Assert.Contains("Roles are assigned", chat.UserPrompt);
        Assert.Contains("How does role assignment work?", chat.UserPrompt);
    }

    [Fact]
    public void PromptBuilder_ShouldIncludeContextAndQuestion()
    {
        var prompt = RagPromptBuilder.BuildUserPrompt(
            "Which database does AuditService use?",
            new[] { new RetrievedChunk("auditservice.md", "AuditService", 0, "AuditService uses MongoDB.", 0.8) });

        Assert.Contains("CONTEXT:", prompt);
        Assert.Contains("AuditService uses MongoDB.", prompt);
        Assert.Contains("QUESTION:", prompt);
    }

    [Fact]
    public async Task AskAsync_ShouldReturnDistinctSources()
    {
        var service = CreateService(new[]
        {
            new RetrievedChunk("architecture.md", "Architecture", 0, "Service boundaries.", 0.9),
            new RetrievedChunk("architecture.md", "Architecture", 1, "More boundaries.", 0.8),
            new RetrievedChunk("jwt.md", "JWT", 0, "JWT validation.", 0.7)
        });

        var result = await service.AskAsync("How is JWT validated?");

        Assert.Equal(new[] { "architecture.md", "jwt.md" }, result.Sources);
    }

    [Fact]
    public async Task AskAsync_WithoutContext_ShouldReturnUnavailableMessage()
    {
        var chat = new FakeChatService();
        var service = CreateService(Array.Empty<RetrievedChunk>(), chat);

        var result = await service.AskAsync("What is not indexed?");

        Assert.Contains("not available", result.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Sources);
        Assert.Null(chat.UserPrompt);
    }

    private static RagService CreateService(
        IReadOnlyCollection<RetrievedChunk> results,
        FakeChatService? chat = null) =>
        new(
            new FakeDocumentReader(),
            new FakeEmbeddingService(),
            new FakeVectorStore(results),
            chat ?? new FakeChatService(),
            NullLogger<RagService>.Instance);

    private sealed class FakeDocumentReader : IKnowledgeDocumentReader
    {
        public Task<IReadOnlyCollection<KnowledgeDocument>> ReadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<KnowledgeDocument>>(new[] { new KnowledgeDocument("test.md", "Test", "test") });
    }

    private sealed class FakeEmbeddingService : IEmbeddingService
    {
        public Task<IReadOnlyList<float[]>> CreateEmbeddingsAsync(IReadOnlyCollection<string> inputs, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<float[]>>(inputs.Select(_ => new[] { 0.1f, 0.2f, 0.3f }).ToArray());
    }

    private sealed class FakeVectorStore(IReadOnlyCollection<RetrievedChunk> results) : IVectorStore
    {
        public Task EnsureCollectionAsync(int vectorSize, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpsertAsync(IReadOnlyCollection<VectorDocument> documents, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyCollection<RetrievedChunk>> SearchAsync(IReadOnlyList<float> vector, int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult(results);
    }

    private sealed class FakeChatService : IChatService
    {
        public string? UserPrompt { get; private set; }
        public Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
        {
            UserPrompt = userPrompt;
            return Task.FromResult("Grounded answer");
        }
    }
}
