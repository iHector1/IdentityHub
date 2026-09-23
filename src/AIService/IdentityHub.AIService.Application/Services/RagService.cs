using System.Diagnostics;
using IdentityHub.AIService.Application.Abstractions;
using IdentityHub.AIService.Application.Models;
using Microsoft.Extensions.Logging;

namespace IdentityHub.AIService.Application.Services;

public sealed class RagService(
    IKnowledgeDocumentReader documentReader,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    IChatService chatService,
    ILogger<RagService> logger)
{
    private const int RetrievalLimit = 5;
    private const string NoKnowledgeAnswer = "The information is not available in the indexed knowledge.";
    private readonly MarkdownChunker chunker = new();

    public async Task<IndexResult> IndexAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var documents = await documentReader.ReadAsync(cancellationToken);
        var chunks = documents.SelectMany(document => chunker.Chunk(document)).ToArray();
        if (chunks.Length == 0)
        {
            logger.LogInformation("RAG indexing completed with no knowledge chunks {LatencyMs}", stopwatch.ElapsedMilliseconds);
            return new IndexResult(documents.Count, 0, stopwatch.ElapsedMilliseconds);
        }

        var embeddingStopwatch = Stopwatch.StartNew();
        var embeddings = await embeddingService.CreateEmbeddingsAsync(
            chunks.Select(chunk => chunk.Text).ToArray(),
            cancellationToken);
        embeddingStopwatch.Stop();

        if (embeddings.Count != chunks.Length)
            throw new InvalidOperationException("The embedding provider returned an unexpected number of vectors.");

        var vectors = chunks
            .Select((chunk, index) => new VectorDocument(chunk, embeddings[index]))
            .ToArray();
        await vectorStore.EnsureCollectionAsync(vectors[0].Vector.Count, cancellationToken);
        await vectorStore.UpsertAsync(vectors, cancellationToken);

        stopwatch.Stop();
        logger.LogInformation(
            "RAG indexing completed {DocumentsIndexed} documents and {ChunksIndexed} chunks in {LatencyMs} ms; embeddings took {EmbeddingMs} ms",
            documents.Count,
            chunks.Length,
            stopwatch.ElapsedMilliseconds,
            embeddingStopwatch.ElapsedMilliseconds);
        return new IndexResult(documents.Count, chunks.Length, stopwatch.ElapsedMilliseconds);
    }

    public async Task<AiAnswer> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question is required.", nameof(question));

        var stopwatch = Stopwatch.StartNew();
        var embeddingStopwatch = Stopwatch.StartNew();
        var embeddings = await embeddingService.CreateEmbeddingsAsync(new[] { question.Trim() }, cancellationToken);
        embeddingStopwatch.Stop();
        if (embeddings.Count == 0)
            throw new InvalidOperationException("The embedding provider returned no vector.");

        var searchStopwatch = Stopwatch.StartNew();
        var retrieved = (await vectorStore.SearchAsync(embeddings[0], RetrievalLimit, cancellationToken)).ToArray();
        searchStopwatch.Stop();

        if (retrieved.Length == 0)
        {
            stopwatch.Stop();
            logger.LogInformation(
                "RAG question completed without context {EmbeddingMs} ms embedding, {VectorSearchMs} ms vector search, {TotalLatencyMs} ms total, {ChunksUsed} chunks",
                embeddingStopwatch.ElapsedMilliseconds,
                searchStopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds,
                0);
            return new AiAnswer(NoKnowledgeAnswer, Array.Empty<string>(), stopwatch.ElapsedMilliseconds);
        }

        var generationStopwatch = Stopwatch.StartNew();
        var answer = await chatService.GenerateAsync(
            RagPromptBuilder.SystemPrompt,
            RagPromptBuilder.BuildUserPrompt(question.Trim(), retrieved),
            cancellationToken);
        generationStopwatch.Stop();
        stopwatch.Stop();

        var sources = retrieved
            .Select(chunk => chunk.Source)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        logger.LogInformation(
            "RAG question completed {EmbeddingMs} ms embedding, {VectorSearchMs} ms vector search, {GenerationMs} ms generation, {TotalLatencyMs} ms total, {ChunksUsed} chunks",
            embeddingStopwatch.ElapsedMilliseconds,
            searchStopwatch.ElapsedMilliseconds,
            generationStopwatch.ElapsedMilliseconds,
            stopwatch.ElapsedMilliseconds,
            retrieved.Length);
        return new AiAnswer(answer, sources, stopwatch.ElapsedMilliseconds);
    }
}
