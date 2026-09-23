using IdentityHub.AIService.Application.Abstractions;
using IdentityHub.AIService.Application.Models;
using IdentityHub.AIService.Application.Options;
using IdentityHub.AIService.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
        Assert.Equal("Grounded", result.Quality);
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
        Assert.Equal("NoContext", result.Quality);
        Assert.Null(result.Usage.TotalTokens);
    }

    [Fact]
    public async Task AskAsync_ShouldReturnTokenUsageAndConfiguredCost()
    {
        var chat = new FakeChatService(new ChatResult("Grounded answer", 100, 50, 150));
        var service = CreateService(
            new[] { new RetrievedChunk("roleservice.md", "RoleService", 0, "Roles are assigned.", 0.91) },
            chat,
            new OpenAiCostOptions
            {
                InputCostPerMillionTokens = 2m,
                OutputCostPerMillionTokens = 4m
            });

        var result = await service.AskAsync("How does role assignment work?");

        Assert.Equal(100, result.Usage.InputTokens);
        Assert.Equal(50, result.Usage.OutputTokens);
        Assert.Equal(150, result.Usage.TotalTokens);
        Assert.Equal(0.0004m, result.Usage.EstimatedCost);
    }

    private static RagService CreateService(
        IReadOnlyCollection<RetrievedChunk> results,
        FakeChatService? chat = null,
        OpenAiCostOptions? costOptions = null) =>
        new(
            new FakeDocumentReader(),
            new FakeEmbeddingService(),
            new FakeVectorStore(results),
            chat ?? new FakeChatService(),
            NullLogger<RagService>.Instance,
            Options.Create(costOptions ?? new OpenAiCostOptions()));

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

    private sealed class FakeChatService(ChatResult? result = null) : IChatService
    {
        public string? UserPrompt { get; private set; }
        public Task<ChatResult> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
        {
            UserPrompt = userPrompt;
            return Task.FromResult(result ?? new ChatResult("Grounded answer", 12, 8, 20));
        }
    }
}
