using IdentityHub.AIService.Application.Models;

namespace IdentityHub.AIService.Application.Abstractions;

public interface IKnowledgeDocumentReader
{
    Task<IReadOnlyCollection<KnowledgeDocument>> ReadAsync(CancellationToken cancellationToken = default);
}
