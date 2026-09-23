using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IdentityHub.AIService.Application.Abstractions;
using IdentityHub.AIService.Application.Models;
using IdentityHub.AIService.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityHub.AIService.Infrastructure.VectorStore;

public sealed class QdrantVectorStore(
    HttpClient httpClient,
    IOptions<QdrantOptions> options,
    ILogger<QdrantVectorStore> logger) : IVectorStore
{
    public async Task EnsureCollectionAsync(int vectorSize, CancellationToken cancellationToken = default)
    {
        var collection = Uri.EscapeDataString(options.Value.CollectionName);
        using var get = await httpClient.GetAsync($"collections/{collection}", cancellationToken);
        if (get.IsSuccessStatusCode) return;
        if (get.StatusCode != HttpStatusCode.NotFound) get.EnsureSuccessStatusCode();

        using var create = await httpClient.PutAsJsonAsync(
            $"collections/{collection}",
            new { vectors = new { size = vectorSize, distance = "Cosine" } },
            cancellationToken);
        create.EnsureSuccessStatusCode();
        logger.LogInformation("Qdrant collection created {CollectionName} with vector size {VectorSize}", options.Value.CollectionName, vectorSize);
    }

    public async Task UpsertAsync(
        IReadOnlyCollection<VectorDocument> documents,
        CancellationToken cancellationToken = default)
    {
        if (documents.Count == 0) return;
        var collection = Uri.EscapeDataString(options.Value.CollectionName);
        var points = documents.Select(document => new
        {
            id = document.Chunk.Id,
            vector = document.Vector,
            payload = new
            {
                text = document.Chunk.Text,
                source = document.Chunk.Source,
                title = document.Chunk.Title,
                chunkIndex = document.Chunk.ChunkIndex
            }
        });
        using var response = await httpClient.PutAsJsonAsync(
            $"collections/{collection}/points?wait=true",
            new { points },
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyCollection<RetrievedChunk>> SearchAsync(
        IReadOnlyList<float> vector,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var collection = Uri.EscapeDataString(options.Value.CollectionName);
        using var response = await httpClient.PostAsJsonAsync(
            $"collections/{collection}/points/query",
            new { query = vector, limit, with_payload = true },
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return Array.Empty<RetrievedChunk>();
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!document.RootElement.TryGetProperty("result", out var result))
            return Array.Empty<RetrievedChunk>();

        // Qdrant returns query results as { result: { points: [...] } }.
        // Keep accepting a direct array as well for compatibility with older APIs.
        var points = result.ValueKind == JsonValueKind.Array
            ? result
            : result.TryGetProperty("points", out var resultPoints)
                ? resultPoints
                : default;

        if (points.ValueKind != JsonValueKind.Array)
            return Array.Empty<RetrievedChunk>();

        return points.EnumerateArray()
            .Select(point =>
            {
                var payload = point.GetProperty("payload");
                return new RetrievedChunk(
                    payload.GetProperty("source").GetString() ?? "unknown",
                    payload.GetProperty("title").GetString() ?? "unknown",
                    payload.GetProperty("chunkIndex").GetInt32(),
                    payload.GetProperty("text").GetString() ?? string.Empty,
                    point.GetProperty("score").GetDouble());
            })
            .ToArray();
    }
}
