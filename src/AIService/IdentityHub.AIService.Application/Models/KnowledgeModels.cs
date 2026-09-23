namespace IdentityHub.AIService.Application.Models;

public sealed record KnowledgeDocument(string Source, string Title, string Text);

public sealed record KnowledgeChunk(
    string Id,
    string Source,
    string Title,
    int ChunkIndex,
    string Text);

public sealed record VectorDocument(KnowledgeChunk Chunk, IReadOnlyList<float> Vector);

public sealed record RetrievedChunk(
    string Source,
    string Title,
    int ChunkIndex,
    string Text,
    double Score);

public sealed record AiAnswer(string Answer, IReadOnlyCollection<string> Sources, long LatencyMs);

public sealed record IndexResult(int DocumentsIndexed, int ChunksIndexed, long LatencyMs);
