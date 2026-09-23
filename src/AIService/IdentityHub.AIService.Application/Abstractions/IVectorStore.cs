using IdentityHub.AIService.Application.Models;

namespace IdentityHub.AIService.Application.Abstractions;

public interface IVectorStore
{
    Task EnsureCollectionAsync(int vectorSize, CancellationToken cancellationToken = default);

    Task UpsertAsync(
        IReadOnlyCollection<VectorDocument> documents,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RetrievedChunk>> SearchAsync(
        IReadOnlyList<float> vector,
        int limit,
        CancellationToken cancellationToken = default);
}
