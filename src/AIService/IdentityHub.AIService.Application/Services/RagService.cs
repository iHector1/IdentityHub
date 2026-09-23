using System.Diagnostics;
using IdentityHub.AIService.Application.Abstractions;
using IdentityHub.AIService.Application.Models;
using IdentityHub.AIService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityHub.AIService.Application.Services;

public sealed class RagService(
    IKnowledgeDocumentReader documentReader,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    IChatService chatService,
    ILogger<RagService> logger,
    IOptions<OpenAiCostOptions> costOptions)
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
                "RAG question completed without context {EmbeddingMs} ms embedding, {VectorSearchMs} ms vector search, {TotalLatencyMs} ms total, {ChunksUsed} chunks, quality {Quality}",
                embeddingStopwatch.ElapsedMilliseconds,
                searchStopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds,
                0,
                "NoContext");
            return new AiAnswer(
                NoKnowledgeAnswer,
                Array.Empty<string>(),
                stopwatch.ElapsedMilliseconds,
                new AiUsage(null, null, null, null),
                "NoContext");
        }

        var generationStopwatch = Stopwatch.StartNew();
        var chatResult = await chatService.GenerateAsync(
            RagPromptBuilder.SystemPrompt,
            RagPromptBuilder.BuildUserPrompt(question.Trim(), retrieved),
            cancellationToken);
        generationStopwatch.Stop();
        stopwatch.Stop();

        var estimatedCost = EstimateCost(chatResult);
        const string quality = "Grounded";

        var sources = retrieved
            .Select(chunk => chunk.Source)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        logger.LogInformation(
            "RAG question completed {EmbeddingMs} ms embedding, {VectorSearchMs} ms vector search, {GenerationMs} ms generation, {TotalLatencyMs} ms total, {ChunksUsed} chunks, quality {Quality}, {InputTokens} input tokens, {OutputTokens} output tokens, {TotalTokens} total tokens, {EstimatedCost} estimated cost",
            embeddingStopwatch.ElapsedMilliseconds,
            searchStopwatch.ElapsedMilliseconds,
            generationStopwatch.ElapsedMilliseconds,
            stopwatch.ElapsedMilliseconds,
            retrieved.Length,
            quality,
            chatResult.InputTokens,
            chatResult.OutputTokens,
            chatResult.TotalTokens,
            estimatedCost);
        return new AiAnswer(
            chatResult.Text,
            sources,
            stopwatch.ElapsedMilliseconds,
            new AiUsage(
                chatResult.InputTokens,
                chatResult.OutputTokens,
                chatResult.TotalTokens,
                estimatedCost),
            quality);
    }

    private decimal? EstimateCost(ChatResult chatResult)
    {
        var pricing = costOptions.Value;
        if (pricing.InputCostPerMillionTokens is not { } inputPrice ||
            pricing.OutputCostPerMillionTokens is not { } outputPrice ||
            chatResult.InputTokens is not { } inputTokens ||
            chatResult.OutputTokens is not { } outputTokens)
        {
            return null;
        }

        return inputTokens / 1_000_000m * inputPrice + outputTokens / 1_000_000m * outputPrice;
    }
}
