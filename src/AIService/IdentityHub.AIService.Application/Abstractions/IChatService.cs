using IdentityHub.AIService.Application.Models;

namespace IdentityHub.AIService.Application.Abstractions;

public interface IChatService
{
    Task<ChatResult> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
