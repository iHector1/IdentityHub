namespace IdentityHub.AIService.Application.Abstractions;

public interface IEmbeddingService
{
    Task<IReadOnlyList<float[]>> CreateEmbeddingsAsync(
        IReadOnlyCollection<string> inputs,
        CancellationToken cancellationToken = default);
}
