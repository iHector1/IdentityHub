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

public sealed record ChatResult(
    string Text,
    int? InputTokens,
    int? OutputTokens,
    int? TotalTokens);

public sealed record AiUsage(
    int? InputTokens,
    int? OutputTokens,
    int? TotalTokens,
    decimal? EstimatedCost);

public sealed record AiUserInfo(Guid Id, string Email, bool IsActive);

public sealed record AiUserSummary(
    Guid UserId,
    string Answer,
    long LatencyMs,
    AiUsage Usage);

public sealed record AiAnswer(
    string Answer,
    IReadOnlyCollection<string> Sources,
    long LatencyMs,
    AiUsage Usage,
    string Quality);

public sealed record IndexResult(int DocumentsIndexed, int ChunksIndexed, long LatencyMs);
