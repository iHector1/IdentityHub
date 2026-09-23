using IdentityHub.AIService.Application.Abstractions;
using IdentityHub.AIService.Application.Models;
using IdentityHub.AIService.Application.Options;
using IdentityHub.AIService.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IdentityHub.AIService.Tests;

public sealed class RagIndexingTests
{
    [Fact]
    public async Task IndexAsync_WithNoDocuments_ShouldReturnZeroChunks()
    {
        var vectorStore = new RecordingVectorStore();
        var service = CreateService(Array.Empty<KnowledgeDocument>(), vectorStore, new[] { new[] { 0.1f } });

        var result = await service.IndexAsync();

        Assert.Equal(0, result.DocumentsIndexed);
        Assert.Equal(0, result.ChunksIndexed);
        Assert.Null(vectorStore.VectorSize);
    }

    [Fact]
    public async Task IndexAsync_WithDocuments_ShouldStoreEmbeddings()
    {
        var vectorStore = new RecordingVectorStore();
        var document = new KnowledgeDocument("guide.md", "Guide", "IdentityHub uses services.");
        var service = CreateService(new[] { document }, vectorStore, new[] { new[] { 0.1f, 0.2f } });

        var result = await service.IndexAsync();

        Assert.Equal(1, result.DocumentsIndexed);
        Assert.Equal(1, result.ChunksIndexed);
        Assert.Equal(2, vectorStore.VectorSize);
        Assert.Single(vectorStore.Documents);
    }

    [Fact]
    public async Task IndexAsync_WhenEmbeddingCountDoesNotMatch_ShouldThrow()
    {
        var document = new KnowledgeDocument("guide.md", "Guide", "IdentityHub uses services.");
        var service = CreateService(new[] { document }, new RecordingVectorStore(), Array.Empty<float[]>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.IndexAsync());
    }

    private static RagService CreateService(
        IReadOnlyCollection<KnowledgeDocument> documents,
        RecordingVectorStore vectorStore,
        IReadOnlyList<float[]> embeddings) =>
        new(
            new FakeDocumentReader(documents),
            new FakeEmbeddingService(embeddings),
            vectorStore,
            new FakeChatService(),
            NullLogger<RagService>.Instance,
            Options.Create(new OpenAiCostOptions()));

    private sealed class FakeDocumentReader(IReadOnlyCollection<KnowledgeDocument> documents) : IKnowledgeDocumentReader
    {
        public Task<IReadOnlyCollection<KnowledgeDocument>> ReadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(documents);
    }

    private sealed class FakeEmbeddingService(IReadOnlyList<float[]> embeddings) : IEmbeddingService
    {
        public Task<IReadOnlyList<float[]>> CreateEmbeddingsAsync(
            IReadOnlyCollection<string> inputs,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(embeddings);
    }

    private sealed class RecordingVectorStore : IVectorStore
    {
        public int? VectorSize { get; private set; }
        public IReadOnlyCollection<VectorDocument> Documents { get; private set; } = Array.Empty<VectorDocument>();

        public Task EnsureCollectionAsync(int vectorSize, CancellationToken cancellationToken = default)
        {
            VectorSize = vectorSize;
            return Task.CompletedTask;
        }

        public Task UpsertAsync(
            IReadOnlyCollection<VectorDocument> documents,
            CancellationToken cancellationToken = default)
        {
            Documents = documents;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<RetrievedChunk>> SearchAsync(
            IReadOnlyList<float> vector,
            int limit,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<RetrievedChunk>>(Array.Empty<RetrievedChunk>());
    }

    private sealed class FakeChatService : IChatService
    {
        public Task<ChatResult> GenerateAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ChatResult("answer", 1, 1, 2));
    }
}
